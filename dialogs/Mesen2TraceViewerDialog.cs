using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Diz.Core.Interfaces;

namespace Diz.Ui.Winforms.dialogs
{
    /// <summary>
    /// Enhanced trace viewer for Mesen2 execution traces
    /// </summary>
    public partial class Mesen2TraceViewerDialog : Form
    {
        private readonly IMesen2IntegrationController _controller;
        private readonly Timer _updateTimer;
        private bool _autoScroll = true;
        private bool _pauseUpdates = false;
        
        public Mesen2TraceViewerDialog(IMesen2IntegrationController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            InitializeComponent();
            
            _updateTimer = new Timer { Interval = 500 };
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
            
            // Subscribe to trace events
            if (_controller.StreamingClient != null)
            {
                _controller.StreamingClient.ExecutionTraceReceived += OnExecutionTraceReceived;
            }
            
            SetupColumns();
            UpdateDisplay();
        }

        private void SetupColumns()
        {
            listViewTraces.View = View.Details;
            listViewTraces.FullRowSelect = true;
            listViewTraces.GridLines = true;
            listViewTraces.VirtualMode = false; // We'll manage items manually for better performance
            
            // Add columns
            listViewTraces.Columns.Add("Time", 80);
            listViewTraces.Columns.Add("PC", 60);
            listViewTraces.Columns.Add("Bank", 40);
            listViewTraces.Columns.Add("Instruction", 100);
            listViewTraces.Columns.Add("Operand", 80);
            listViewTraces.Columns.Add("A", 40);
            listViewTraces.Columns.Add("X", 40);
            listViewTraces.Columns.Add("Y", 40);
            listViewTraces.Columns.Add("SP", 40);
            listViewTraces.Columns.Add("P", 40);
            listViewTraces.Columns.Add("DBR", 40);
            listViewTraces.Columns.Add("DPR", 40);
            listViewTraces.Columns.Add("Cycles", 60);
            
            // Set font to monospace for better alignment
            listViewTraces.Font = new Font("Consolas", 9);
        }

        private void OnExecutionTraceReceived(object sender, Mesen2TraceEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<object, Mesen2TraceEventArgs>(OnExecutionTraceReceived), sender, e);
                return;
            }
            
            if (!_pauseUpdates)
            {
                AddTraceEntry(e.Trace);
            }
        }

        private void AddTraceEntry(Mesen2ExecutionTrace trace)
        {
            var item = new ListViewItem(DateTime.Now.ToString("HH:mm:ss.fff"));
            item.SubItems.Add($"${trace.PC:X4}");
            item.SubItems.Add($"${trace.PC >> 16:X2}");
            item.SubItems.Add(trace.Instruction ?? "???");
            item.SubItems.Add(trace.Operand ?? "");
            item.SubItems.Add($"${trace.A:X4}");
            item.SubItems.Add($"${trace.X:X4}");
            item.SubItems.Add($"${trace.Y:X4}");
            item.SubItems.Add($"${trace.SP:X4}");
            item.SubItems.Add($"${trace.ProcessorFlags:X2}");
            item.SubItems.Add($"${trace.DBR:X2}");
            item.SubItems.Add($"${trace.DPR:X4}");
            item.SubItems.Add(trace.Cycles.ToString());
            
            // Color code based on PC range
            if (trace.PC >= 0x8000)
            {
                item.BackColor = Color.LightBlue; // ROM area
            }
            else if (trace.PC >= 0x2000)
            {
                item.BackColor = Color.LightYellow; // Hardware registers
            }
            else
            {
                item.BackColor = Color.LightGreen; // RAM area
            }
            
            listViewTraces.Items.Add(item);
            
            // Limit items for performance
            if (listViewTraces.Items.Count > (int)numMaxEntries.Value)
            {
                for (int i = 0; i < 100; i++) // Remove in batches
                {
                    if (listViewTraces.Items.Count > 0)
                        listViewTraces.Items.RemoveAt(0);
                }
            }
            
            // Auto-scroll to bottom
            if (_autoScroll && listViewTraces.Items.Count > 0)
            {
                listViewTraces.EnsureVisible(listViewTraces.Items.Count - 1);
            }
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            UpdateStatusInfo();
        }

        private void UpdateDisplay()
        {
            UpdateStatusInfo();
            UpdateControlStates();
        }

        private void UpdateStatusInfo()
        {
            var client = _controller.StreamingClient;
            if (client != null)
            {
                lblConnectionStatus.Text = client.IsConnected ? "Connected" : "Disconnected";
                lblConnectionStatus.ForeColor = client.IsConnected ? Color.Green : Color.Red;
                
                lblTraceCount.Text = listViewTraces.Items.Count.ToString();
                lblTotalTraces.Text = client.MessagesReceived.ToString();
                
                var memUsage = GC.GetTotalMemory(false) / (1024 * 1024);
                lblMemoryUsage.Text = $"{memUsage} MB";
            }
        }

        private void UpdateControlStates()
        {
            btnPause.Text = _pauseUpdates ? "Resume" : "Pause";
            btnAutoScroll.Text = _autoScroll ? "Disable Auto-Scroll" : "Enable Auto-Scroll";
        }

        private void btnPause_Click(object sender, EventArgs e)
        {
            _pauseUpdates = !_pauseUpdates;
            UpdateControlStates();
        }

        private void btnAutoScroll_Click(object sender, EventArgs e)
        {
            _autoScroll = !_autoScroll;
            UpdateControlStates();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            listViewTraces.Items.Clear();
            UpdateDisplay();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            using var saveDialog = new SaveFileDialog
            {
                Title = "Save Execution Trace",
                Filter = "CSV files (*.csv)|*.csv|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = "csv"
            };

            if (saveDialog.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    SaveTraceToFile(saveDialog.FileName);
                    MessageBox.Show(this, $"Trace saved successfully to {saveDialog.FileName}", "Save Complete", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Failed to save trace: {ex.Message}", "Save Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SaveTraceToFile(string fileName)
        {
            using var writer = new System.IO.StreamWriter(fileName);
            
            // Write header
            var headers = listViewTraces.Columns.Cast<ColumnHeader>().Select(c => c.Text);
            writer.WriteLine(string.Join(",", headers));
            
            // Write data
            foreach (ListViewItem item in listViewTraces.Items)
            {
                var values = item.SubItems.Cast<ListViewItem.ListViewSubItem>().Select(s => s.Text);
                writer.WriteLine(string.Join(",", values));
            }
        }

        private void btnFilter_Click(object sender, EventArgs e)
        {
            var filterText = txtFilter.Text.Trim();
            if (string.IsNullOrEmpty(filterText))
            {
                // Show all items
                foreach (ListViewItem item in listViewTraces.Items)
                {
                    item.Font = listViewTraces.Font;
                    item.ForeColor = listViewTraces.ForeColor;
                }
            }
            else
            {
                // Highlight matching items
                foreach (ListViewItem item in listViewTraces.Items)
                {
                    bool matches = item.SubItems.Cast<ListViewItem.ListViewSubItem>()
                        .Any(subItem => subItem.Text.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0);
                    
                    if (matches)
                    {
                        item.Font = new Font(listViewTraces.Font, FontStyle.Bold);
                        item.ForeColor = Color.Blue;
                    }
                    else
                    {
                        item.Font = listViewTraces.Font;
                        item.ForeColor = Color.Gray;
                    }
                }
            }
        }

        private void btnClearFilter_Click(object sender, EventArgs e)
        {
            txtFilter.Text = "";
            btnFilter_Click(sender, e);
        }

        private void numMaxEntries_ValueChanged(object sender, EventArgs e)
        {
            // Trim excess entries if needed
            while (listViewTraces.Items.Count > (int)numMaxEntries.Value)
            {
                listViewTraces.Items.RemoveAt(0);
            }
        }

        private void listViewTraces_DoubleClick(object sender, EventArgs e)
        {
            if (listViewTraces.SelectedItems.Count > 0)
            {
                var item = listViewTraces.SelectedItems[0];
                var pc = item.SubItems[1].Text; // PC column
                var instruction = item.SubItems[3].Text; // Instruction column
                var operand = item.SubItems[4].Text; // Operand column
                
                var details = $"Program Counter: {pc}\n" +
                             $"Instruction: {instruction}\n" +
                             $"Operand: {operand}\n\n" +
                             $"Full Trace Entry:\n" +
                             string.Join(" | ", item.SubItems.Cast<ListViewItem.ListViewSubItem>().Select(s => s.Text));
                
                MessageBox.Show(this, details, "Trace Entry Details", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _updateTimer?.Stop();
            _updateTimer?.Dispose();
            
            if (_controller.StreamingClient != null)
            {
                _controller.StreamingClient.ExecutionTraceReceived -= OnExecutionTraceReceived;
            }
            
            base.OnFormClosing(e);
        }

        #region Windows Form Designer generated code

        private System.ComponentModel.IContainer components = null;
        private ListView listViewTraces;
        private Panel panelControls;
        private Panel panelStatus;
        private Button btnPause;
        private Button btnAutoScroll;
        private Button btnClear;
        private Button btnSave;
        private Button btnFilter;
        private Button btnClearFilter;
        private TextBox txtFilter;
        private NumericUpDown numMaxEntries;
        private Label lblConnectionStatus;
        private Label lblTraceCount;
        private Label lblTotalTraces;
        private Label lblMemoryUsage;
        private Label lblConnectionLabel;
        private Label lblTraceCountLabel;
        private Label lblTotalTracesLabel;
        private Label lblMemoryUsageLabel;
        private Label lblMaxEntriesLabel;
        private Label lblFilterLabel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.listViewTraces = new ListView();
            this.panelControls = new Panel();
            this.panelStatus = new Panel();
            this.btnPause = new Button();
            this.btnAutoScroll = new Button();
            this.btnClear = new Button();
            this.btnSave = new Button();
            this.btnFilter = new Button();
            this.btnClearFilter = new Button();
            this.txtFilter = new TextBox();
            this.numMaxEntries = new NumericUpDown();
            this.lblConnectionStatus = new Label();
            this.lblTraceCount = new Label();
            this.lblTotalTraces = new Label();
            this.lblMemoryUsage = new Label();
            this.lblConnectionLabel = new Label();
            this.lblTraceCountLabel = new Label();
            this.lblTotalTracesLabel = new Label();
            this.lblMemoryUsageLabel = new Label();
            this.lblMaxEntriesLabel = new Label();
            this.lblFilterLabel = new Label();
            
            ((ISupportInitialize)(this.numMaxEntries)).BeginInit();
            this.panelControls.SuspendLayout();
            this.panelStatus.SuspendLayout();
            this.SuspendLayout();
            
            // Form
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1000, 600);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(800, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Mesen2 Execution Trace Viewer";
            
            // ListView
            this.listViewTraces.Dock = DockStyle.Fill;
            this.listViewTraces.HideSelection = false;
            this.listViewTraces.FullRowSelect = true;
            this.listViewTraces.GridLines = true;
            this.listViewTraces.View = View.Details;
            this.listViewTraces.DoubleClick += new EventHandler(this.listViewTraces_DoubleClick);
            
            // Controls Panel
            this.panelControls.Dock = DockStyle.Top;
            this.panelControls.Height = 80;
            this.panelControls.Padding = new Padding(10);
            
            // Filter controls (top row)
            this.lblFilterLabel.Text = "Filter:";
            this.lblFilterLabel.Location = new Point(10, 15);
            this.lblFilterLabel.Size = new Size(40, 20);
            
            this.txtFilter.Location = new Point(55, 12);
            this.txtFilter.Size = new Size(150, 23);
            
            this.btnFilter.Text = "Apply Filter";
            this.btnFilter.Location = new Point(215, 11);
            this.btnFilter.Size = new Size(80, 25);
            this.btnFilter.UseVisualStyleBackColor = true;
            this.btnFilter.Click += new EventHandler(this.btnFilter_Click);
            
            this.btnClearFilter.Text = "Clear";
            this.btnClearFilter.Location = new Point(305, 11);
            this.btnClearFilter.Size = new Size(50, 25);
            this.btnClearFilter.UseVisualStyleBackColor = true;
            this.btnClearFilter.Click += new EventHandler(this.btnClearFilter_Click);
            
            this.lblMaxEntriesLabel.Text = "Max Entries:";
            this.lblMaxEntriesLabel.Location = new Point(400, 15);
            this.lblMaxEntriesLabel.Size = new Size(75, 20);
            
            this.numMaxEntries.Location = new Point(480, 12);
            this.numMaxEntries.Size = new Size(80, 23);
            this.numMaxEntries.Minimum = 100;
            this.numMaxEntries.Maximum = 50000;
            this.numMaxEntries.Value = 10000;
            this.numMaxEntries.ValueChanged += new EventHandler(this.numMaxEntries_ValueChanged);
            
            // Control buttons (bottom row)
            this.btnPause.Text = "Pause";
            this.btnPause.Location = new Point(10, 45);
            this.btnPause.Size = new Size(70, 25);
            this.btnPause.UseVisualStyleBackColor = true;
            this.btnPause.Click += new EventHandler(this.btnPause_Click);
            
            this.btnAutoScroll.Text = "Disable Auto-Scroll";
            this.btnAutoScroll.Location = new Point(90, 45);
            this.btnAutoScroll.Size = new Size(120, 25);
            this.btnAutoScroll.UseVisualStyleBackColor = true;
            this.btnAutoScroll.Click += new EventHandler(this.btnAutoScroll_Click);
            
            this.btnClear.Text = "Clear All";
            this.btnClear.Location = new Point(220, 45);
            this.btnClear.Size = new Size(75, 25);
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new EventHandler(this.btnClear_Click);
            
            this.btnSave.Text = "Save...";
            this.btnSave.Location = new Point(305, 45);
            this.btnSave.Size = new Size(70, 25);
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new EventHandler(this.btnSave_Click);
            
            this.panelControls.Controls.Add(this.lblFilterLabel);
            this.panelControls.Controls.Add(this.txtFilter);
            this.panelControls.Controls.Add(this.btnFilter);
            this.panelControls.Controls.Add(this.btnClearFilter);
            this.panelControls.Controls.Add(this.lblMaxEntriesLabel);
            this.panelControls.Controls.Add(this.numMaxEntries);
            this.panelControls.Controls.Add(this.btnPause);
            this.panelControls.Controls.Add(this.btnAutoScroll);
            this.panelControls.Controls.Add(this.btnClear);
            this.panelControls.Controls.Add(this.btnSave);
            
            // Status Panel
            this.panelStatus.Dock = DockStyle.Bottom;
            this.panelStatus.Height = 30;
            this.panelStatus.Padding = new Padding(10, 5, 10, 5);
            
            this.lblConnectionLabel.Text = "Connection:";
            this.lblConnectionLabel.Location = new Point(10, 7);
            this.lblConnectionLabel.Size = new Size(70, 20);
            
            this.lblConnectionStatus.Text = "Unknown";
            this.lblConnectionStatus.Location = new Point(85, 7);
            this.lblConnectionStatus.Size = new Size(100, 20);
            this.lblConnectionStatus.Font = new Font(this.lblConnectionStatus.Font, FontStyle.Bold);
            
            this.lblTraceCountLabel.Text = "Showing:";
            this.lblTraceCountLabel.Location = new Point(200, 7);
            this.lblTraceCountLabel.Size = new Size(60, 20);
            
            this.lblTraceCount.Text = "0";
            this.lblTraceCount.Location = new Point(265, 7);
            this.lblTraceCount.Size = new Size(50, 20);
            
            this.lblTotalTracesLabel.Text = "Total:";
            this.lblTotalTracesLabel.Location = new Point(330, 7);
            this.lblTotalTracesLabel.Size = new Size(40, 20);
            
            this.lblTotalTraces.Text = "0";
            this.lblTotalTraces.Location = new Point(375, 7);
            this.lblTotalTraces.Size = new Size(50, 20);
            
            this.lblMemoryUsageLabel.Text = "Memory:";
            this.lblMemoryUsageLabel.Location = new Point(440, 7);
            this.lblMemoryUsageLabel.Size = new Size(55, 20);
            
            this.lblMemoryUsage.Text = "0 MB";
            this.lblMemoryUsage.Location = new Point(500, 7);
            this.lblMemoryUsage.Size = new Size(60, 20);
            
            this.panelStatus.Controls.Add(this.lblConnectionLabel);
            this.panelStatus.Controls.Add(this.lblConnectionStatus);
            this.panelStatus.Controls.Add(this.lblTraceCountLabel);
            this.panelStatus.Controls.Add(this.lblTraceCount);
            this.panelStatus.Controls.Add(this.lblTotalTracesLabel);
            this.panelStatus.Controls.Add(this.lblTotalTraces);
            this.panelStatus.Controls.Add(this.lblMemoryUsageLabel);
            this.panelStatus.Controls.Add(this.lblMemoryUsage);
            
            // Add controls to form
            this.Controls.Add(this.listViewTraces);
            this.Controls.Add(this.panelControls);
            this.Controls.Add(this.panelStatus);
            
            ((ISupportInitialize)(this.numMaxEntries)).EndInit();
            this.panelControls.ResumeLayout(false);
            this.panelControls.PerformLayout();
            this.panelStatus.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion
    }
}