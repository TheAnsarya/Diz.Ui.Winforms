using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Diz.Core.Interfaces;

namespace Diz.Ui.Winforms.dialogs
{
    /// <summary>
    /// Status window for monitoring Mesen2 integration
    /// </summary>
    public partial class Mesen2StatusDialog : Form
    {
        private readonly IMesen2IntegrationController _controller;
        private readonly Timer _updateTimer;
        private DateTime _lastUpdateTime;
        
        public Mesen2StatusDialog(IMesen2IntegrationController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            InitializeComponent();
            
            _updateTimer = new Timer { Interval = 1000 };
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
            
            // Subscribe to events
            if (_controller.StreamingClient != null)
            {
                _controller.StreamingClient.DataReceived += OnDataReceived;
                _controller.StreamingClient.ConnectionStateChanged += OnConnectionStateChanged;
            }
            
            UpdateDisplay();
        }

        private void OnDataReceived(object sender, EventArgs e)
        {
            _lastUpdateTime = DateTime.Now;
            
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateDisplay));
            }
            else
            {
                UpdateDisplay();
            }
        }

        private void OnConnectionStateChanged(object sender, bool isConnected)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateDisplay));
            }
            else
            {
                UpdateDisplay();
            }
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            try
            {
                var client = _controller.StreamingClient;
                
                // Connection status
                if (client?.IsConnected == true)
                {
                    lblConnectionStatus.Text = "Connected";
                    lblConnectionStatus.ForeColor = Color.Green;
                    btnToggleConnection.Text = "Disconnect";
                }
                else
                {
                    lblConnectionStatus.Text = "Disconnected";
                    lblConnectionStatus.ForeColor = Color.Red;
                    btnToggleConnection.Text = "Connect";
                }

                // Host and port
                var config = _controller.Configuration;
                lblHostPort.Text = $"{config?.DefaultHost ?? "Unknown"}:{config?.DefaultPort ?? 0}";

                // Last activity
                if (_lastUpdateTime != default)
                {
                    var timeDiff = DateTime.Now - _lastUpdateTime;
                    lblLastActivity.Text = timeDiff.TotalSeconds < 1 ? "Just now" : 
                                         timeDiff.TotalSeconds < 60 ? $"{(int)timeDiff.TotalSeconds}s ago" :
                                         timeDiff.TotalMinutes < 60 ? $"{(int)timeDiff.TotalMinutes}m ago" :
                                         $"{(int)timeDiff.TotalHours}h ago";
                }
                else
                {
                    lblLastActivity.Text = "Never";
                }

                // Message statistics
                if (client != null)
                {
                    lblMessagesReceived.Text = client.MessagesReceived.ToString();
                    lblMessagesSent.Text = client.MessagesSent.ToString();
                    lblBytesReceived.Text = FormatBytes(client.BytesReceived);
                    lblBytesSent.Text = FormatBytes(client.BytesSent);
                }

                // Performance metrics
                if (client?.IsConnected == true)
                {
                    lblLatency.Text = client.LastLatency > 0 ? $"{client.LastLatency}ms" : "Unknown";
                    lblThroughput.Text = $"{client.MessagesPerSecond:F1} msg/s";
                }
                else
                {
                    lblLatency.Text = "N/A";
                    lblThroughput.Text = "N/A";
                }

                // Update log
                UpdateLogDisplay();
            }
            catch (Exception ex)
            {
                // Handle gracefully
                lblConnectionStatus.Text = $"Error: {ex.Message}";
                lblConnectionStatus.ForeColor = Color.Red;
            }
        }

        private void UpdateLogDisplay()
        {
            if (_controller.StreamingClient?.RecentLogs?.Any() == true)
            {
                var logs = _controller.StreamingClient.RecentLogs
                    .TakeLast(100)
                    .Select(log => $"[{log.Timestamp:HH:mm:ss}] {log.Level}: {log.Message}")
                    .ToArray();

                if (!logs.SequenceEqual(txtLog.Lines))
                {
                    txtLog.Lines = logs;
                    txtLog.SelectionStart = txtLog.Text.Length;
                    txtLog.ScrollToCaret();
                }
            }
        }

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private async void btnToggleConnection_Click(object sender, EventArgs e)
        {
            try
            {
                btnToggleConnection.Enabled = false;
                
                if (_controller.StreamingClient?.IsConnected == true)
                {
                    await _controller.DisconnectAsync();
                }
                else
                {
                    await _controller.ConnectAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Connection operation failed: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnToggleConnection.Enabled = true;
            }
        }

        private void btnClearLog_Click(object sender, EventArgs e)
        {
            txtLog.Clear();
            _controller.StreamingClient?.ClearLogs();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            UpdateDisplay();
        }

        private void btnClearStats_Click(object sender, EventArgs e)
        {
            _controller.StreamingClient?.ResetStatistics();
            UpdateDisplay();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _updateTimer?.Stop();
            _updateTimer?.Dispose();
            
            if (_controller.StreamingClient != null)
            {
                _controller.StreamingClient.DataReceived -= OnDataReceived;
                _controller.StreamingClient.ConnectionStateChanged -= OnConnectionStateChanged;
            }
            
            base.OnFormClosing(e);
        }

        #region Windows Form Designer generated code

        private System.ComponentModel.IContainer components = null;
        private Label lblConnectionStatusLabel;
        private Label lblConnectionStatus;
        private Label lblHostPortLabel;
        private Label lblHostPort;
        private Label lblLastActivityLabel;
        private Label lblLastActivity;
        private Label lblMessagesReceivedLabel;
        private Label lblMessagesReceived;
        private Label lblMessagesSentLabel;
        private Label lblMessagesSent;
        private Label lblBytesReceivedLabel;
        private Label lblBytesReceived;
        private Label lblBytesSentLabel;
        private Label lblBytesSent;
        private Label lblLatencyLabel;
        private Label lblLatency;
        private Label lblThroughputLabel;
        private Label lblThroughput;
        private TextBox txtLog;
        private Button btnToggleConnection;
        private Button btnClearLog;
        private Button btnRefresh;
        private Button btnClearStats;
        private GroupBox grpConnection;
        private GroupBox grpStatistics;
        private GroupBox grpPerformance;
        private GroupBox grpLog;

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
            this.lblConnectionStatusLabel = new Label();
            this.lblConnectionStatus = new Label();
            this.lblHostPortLabel = new Label();
            this.lblHostPort = new Label();
            this.lblLastActivityLabel = new Label();
            this.lblLastActivity = new Label();
            this.lblMessagesReceivedLabel = new Label();
            this.lblMessagesReceived = new Label();
            this.lblMessagesSentLabel = new Label();
            this.lblMessagesSent = new Label();
            this.lblBytesReceivedLabel = new Label();
            this.lblBytesReceived = new Label();
            this.lblBytesSentLabel = new Label();
            this.lblBytesSent = new Label();
            this.lblLatencyLabel = new Label();
            this.lblLatency = new Label();
            this.lblThroughputLabel = new Label();
            this.lblThroughput = new Label();
            this.txtLog = new TextBox();
            this.btnToggleConnection = new Button();
            this.btnClearLog = new Button();
            this.btnRefresh = new Button();
            this.btnClearStats = new Button();
            this.grpConnection = new GroupBox();
            this.grpStatistics = new GroupBox();
            this.grpPerformance = new GroupBox();
            this.grpLog = new GroupBox();
            
            this.grpConnection.SuspendLayout();
            this.grpStatistics.SuspendLayout();
            this.grpPerformance.SuspendLayout();
            this.grpLog.SuspendLayout();
            this.SuspendLayout();
            
            // Form
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(600, 500);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Mesen2 Integration Status";
            
            // Connection Group
            this.grpConnection.Text = "Connection";
            this.grpConnection.Location = new Point(12, 12);
            this.grpConnection.Size = new Size(280, 120);
            
            this.lblConnectionStatusLabel.Text = "Status:";
            this.lblConnectionStatusLabel.Location = new Point(15, 25);
            this.lblConnectionStatusLabel.Size = new Size(50, 20);
            
            this.lblConnectionStatus.Text = "Unknown";
            this.lblConnectionStatus.Location = new Point(70, 25);
            this.lblConnectionStatus.Size = new Size(100, 20);
            this.lblConnectionStatus.Font = new Font(this.lblConnectionStatus.Font, FontStyle.Bold);
            
            this.lblHostPortLabel.Text = "Host:Port:";
            this.lblHostPortLabel.Location = new Point(15, 50);
            this.lblHostPortLabel.Size = new Size(60, 20);
            
            this.lblHostPort.Text = "Unknown";
            this.lblHostPort.Location = new Point(80, 50);
            this.lblHostPort.Size = new Size(120, 20);
            
            this.lblLastActivityLabel.Text = "Last Activity:";
            this.lblLastActivityLabel.Location = new Point(15, 75);
            this.lblLastActivityLabel.Size = new Size(80, 20);
            
            this.lblLastActivity.Text = "Never";
            this.lblLastActivity.Location = new Point(100, 75);
            this.lblLastActivity.Size = new Size(100, 20);
            
            this.btnToggleConnection.Text = "Connect";
            this.btnToggleConnection.Location = new Point(200, 25);
            this.btnToggleConnection.Size = new Size(70, 25);
            this.btnToggleConnection.UseVisualStyleBackColor = true;
            this.btnToggleConnection.Click += new EventHandler(this.btnToggleConnection_Click);
            
            this.grpConnection.Controls.Add(this.lblConnectionStatusLabel);
            this.grpConnection.Controls.Add(this.lblConnectionStatus);
            this.grpConnection.Controls.Add(this.lblHostPortLabel);
            this.grpConnection.Controls.Add(this.lblHostPort);
            this.grpConnection.Controls.Add(this.lblLastActivityLabel);
            this.grpConnection.Controls.Add(this.lblLastActivity);
            this.grpConnection.Controls.Add(this.btnToggleConnection);
            
            // Statistics Group
            this.grpStatistics.Text = "Message Statistics";
            this.grpStatistics.Location = new Point(300, 12);
            this.grpStatistics.Size = new Size(280, 120);
            
            this.lblMessagesReceivedLabel.Text = "Messages Received:";
            this.lblMessagesReceivedLabel.Location = new Point(15, 25);
            this.lblMessagesReceivedLabel.Size = new Size(120, 20);
            
            this.lblMessagesReceived.Text = "0";
            this.lblMessagesReceived.Location = new Point(140, 25);
            this.lblMessagesReceived.Size = new Size(80, 20);
            
            this.lblMessagesSentLabel.Text = "Messages Sent:";
            this.lblMessagesSentLabel.Location = new Point(15, 50);
            this.lblMessagesSentLabel.Size = new Size(100, 20);
            
            this.lblMessagesSent.Text = "0";
            this.lblMessagesSent.Location = new Point(140, 50);
            this.lblMessagesSent.Size = new Size(80, 20);
            
            this.lblBytesReceivedLabel.Text = "Bytes Received:";
            this.lblBytesReceivedLabel.Location = new Point(15, 75);
            this.lblBytesReceivedLabel.Size = new Size(100, 20);
            
            this.lblBytesReceived.Text = "0 B";
            this.lblBytesReceived.Location = new Point(140, 75);
            this.lblBytesReceived.Size = new Size(80, 20);
            
            this.lblBytesSentLabel.Text = "Bytes Sent:";
            this.lblBytesSentLabel.Location = new Point(15, 100);
            this.lblBytesSentLabel.Size = new Size(80, 20);
            
            this.lblBytesSent.Text = "0 B";
            this.lblBytesSent.Location = new Point(140, 100);
            this.lblBytesSent.Size = new Size(80, 20);
            
            this.btnClearStats.Text = "Clear";
            this.btnClearStats.Location = new Point(200, 75);
            this.btnClearStats.Size = new Size(60, 25);
            this.btnClearStats.UseVisualStyleBackColor = true;
            this.btnClearStats.Click += new EventHandler(this.btnClearStats_Click);
            
            this.grpStatistics.Controls.Add(this.lblMessagesReceivedLabel);
            this.grpStatistics.Controls.Add(this.lblMessagesReceived);
            this.grpStatistics.Controls.Add(this.lblMessagesSentLabel);
            this.grpStatistics.Controls.Add(this.lblMessagesSent);
            this.grpStatistics.Controls.Add(this.lblBytesReceivedLabel);
            this.grpStatistics.Controls.Add(this.lblBytesReceived);
            this.grpStatistics.Controls.Add(this.lblBytesSentLabel);
            this.grpStatistics.Controls.Add(this.lblBytesSent);
            this.grpStatistics.Controls.Add(this.btnClearStats);
            
            // Performance Group
            this.grpPerformance.Text = "Performance";
            this.grpPerformance.Location = new Point(12, 140);
            this.grpPerformance.Size = new Size(280, 80);
            
            this.lblLatencyLabel.Text = "Latency:";
            this.lblLatencyLabel.Location = new Point(15, 25);
            this.lblLatencyLabel.Size = new Size(60, 20);
            
            this.lblLatency.Text = "N/A";
            this.lblLatency.Location = new Point(80, 25);
            this.lblLatency.Size = new Size(80, 20);
            
            this.lblThroughputLabel.Text = "Throughput:";
            this.lblThroughputLabel.Location = new Point(15, 50);
            this.lblThroughputLabel.Size = new Size(80, 20);
            
            this.lblThroughput.Text = "N/A";
            this.lblThroughput.Location = new Point(100, 50);
            this.lblThroughput.Size = new Size(100, 20);
            
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.Location = new Point(200, 25);
            this.btnRefresh.Size = new Size(60, 25);
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new EventHandler(this.btnRefresh_Click);
            
            this.grpPerformance.Controls.Add(this.lblLatencyLabel);
            this.grpPerformance.Controls.Add(this.lblLatency);
            this.grpPerformance.Controls.Add(this.lblThroughputLabel);
            this.grpPerformance.Controls.Add(this.lblThroughput);
            this.grpPerformance.Controls.Add(this.btnRefresh);
            
            // Log Group
            this.grpLog.Text = "Recent Activity Log";
            this.grpLog.Location = new Point(12, 230);
            this.grpLog.Size = new Size(568, 250);
            
            this.txtLog.Location = new Point(15, 25);
            this.txtLog.Size = new Size(490, 190);
            this.txtLog.Multiline = true;
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = ScrollBars.Vertical;
            this.txtLog.Font = new Font("Consolas", 8.25F);
            this.txtLog.BackColor = Color.Black;
            this.txtLog.ForeColor = Color.Lime;
            
            this.btnClearLog.Text = "Clear Log";
            this.btnClearLog.Location = new Point(515, 25);
            this.btnClearLog.Size = new Size(75, 25);
            this.btnClearLog.UseVisualStyleBackColor = true;
            this.btnClearLog.Click += new EventHandler(this.btnClearLog_Click);
            
            this.grpLog.Controls.Add(this.txtLog);
            this.grpLog.Controls.Add(this.btnClearLog);
            
            // Add controls to form
            this.Controls.Add(this.grpConnection);
            this.Controls.Add(this.grpStatistics);
            this.Controls.Add(this.grpPerformance);
            this.Controls.Add(this.grpLog);
            
            this.grpConnection.ResumeLayout(false);
            this.grpStatistics.ResumeLayout(false);
            this.grpPerformance.ResumeLayout(false);
            this.grpLog.ResumeLayout(false);
            this.grpLog.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion
    }
}