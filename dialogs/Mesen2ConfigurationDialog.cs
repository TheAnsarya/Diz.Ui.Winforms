using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Diz.Core.Interfaces;

namespace Diz.Ui.Winforms.dialogs
{
    /// <summary>
    /// Advanced configuration editor for Mesen2 integration
    /// </summary>
    public partial class Mesen2ConfigurationDialog : Form
    {
        private readonly IMesen2Configuration _configuration;
        
        public Mesen2ConfigurationDialog(IMesen2Configuration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            InitializeComponent();
            LoadConfiguration();
            SetupValidation();
        }

        private void LoadConfiguration()
        {
            // Connection settings
            txtHost.Text = _configuration.DefaultHost;
            numPort.Value = _configuration.DefaultPort;
            numTimeout.Value = _configuration.ConnectionTimeoutMs;
            
            // Auto-reconnection settings
            chkAutoReconnect.Checked = _configuration.AutoReconnect;
            numMaxRetries.Value = _configuration.MaxReconnectAttempts;
            numRetryDelay.Value = _configuration.AutoReconnectDelayMs;
            
            // Streaming settings
            chkEnableCompression.Checked = _configuration.EnableCompression;
            numBufferSize.Value = _configuration.BufferSize;
            numBatchSize.Value = _configuration.BatchSize;
            
            // Data filtering
            chkCpuData.Checked = _configuration.StreamCpuData;
            chkMemoryData.Checked = _configuration.StreamMemoryData;
            chkDebugData.Checked = _configuration.StreamDebugData;
            chkEventData.Checked = _configuration.StreamEventData;
            
            // Performance settings
            numUpdateInterval.Value = _configuration.UpdateIntervalMs;
            numMaxQueueSize.Value = _configuration.MaxQueueSize;
            
            // Logging
            chkVerboseLogging.Checked = _configuration.VerboseLogging;
            chkLogToFile.Checked = _configuration.LogToFile;
            txtLogPath.Text = _configuration.LogFilePath ?? "";
            
            // Security
            chkSecureConnection.Checked = _configuration.UseSecureConnection;
            txtApiKey.Text = _configuration.ApiKey ?? "";
            
            // Advanced
            chkStrictProtocol.Checked = _configuration.StrictProtocolValidation;
            numKeepAlive.Value = _configuration.KeepAliveIntervalMs;
            chkBinaryProtocol.Checked = _configuration.UseBinaryProtocol;
        }

        private void SaveConfiguration()
        {
            // Connection settings
            _configuration.DefaultHost = txtHost.Text.Trim();
            _configuration.DefaultPort = (int)numPort.Value;
            _configuration.ConnectionTimeoutMs = (int)numTimeout.Value;
            
            // Auto-reconnection settings
            _configuration.AutoReconnect = chkAutoReconnect.Checked;
            _configuration.MaxReconnectAttempts = (int)numMaxRetries.Value;
            _configuration.AutoReconnectDelayMs = (int)numRetryDelay.Value;
            
            // Streaming settings
            _configuration.EnableCompression = chkEnableCompression.Checked;
            _configuration.BufferSize = (int)numBufferSize.Value;
            _configuration.BatchSize = (int)numBatchSize.Value;
            
            // Data filtering
            _configuration.StreamCpuData = chkCpuData.Checked;
            _configuration.StreamMemoryData = chkMemoryData.Checked;
            _configuration.StreamDebugData = chkDebugData.Checked;
            _configuration.StreamEventData = chkEventData.Checked;
            
            // Performance settings
            _configuration.UpdateIntervalMs = (int)numUpdateInterval.Value;
            _configuration.MaxQueueSize = (int)numMaxQueueSize.Value;
            
            // Logging
            _configuration.VerboseLogging = chkVerboseLogging.Checked;
            _configuration.LogToFile = chkLogToFile.Checked;
            _configuration.LogFilePath = txtLogPath.Text.Trim();
            
            // Security
            _configuration.UseSecureConnection = chkSecureConnection.Checked;
            _configuration.ApiKey = txtApiKey.Text.Trim();
            
            // Advanced
            _configuration.StrictProtocolValidation = chkStrictProtocol.Checked;
            _configuration.KeepAliveIntervalMs = (int)numKeepAlive.Value;
            _configuration.UseBinaryProtocol = chkBinaryProtocol.Checked;
            
            _configuration.Save();
        }

        private void SetupValidation()
        {
            // Add validation event handlers
            txtHost.Validating += ValidateHost;
            numPort.Validating += ValidatePort;
            txtLogPath.Validating += ValidateLogPath;
            txtApiKey.Validating += ValidateApiKey;
        }

        private void ValidateHost(object sender, CancelEventArgs e)
        {
            var host = txtHost.Text.Trim();
            if (string.IsNullOrEmpty(host))
            {
                errorProvider.SetError(txtHost, "Host cannot be empty");
                e.Cancel = true;
            }
            else
            {
                errorProvider.SetError(txtHost, "");
            }
        }

        private void ValidatePort(object sender, CancelEventArgs e)
        {
            if (numPort.Value < 1 || numPort.Value > 65535)
            {
                errorProvider.SetError(numPort, "Port must be between 1 and 65535");
                e.Cancel = true;
            }
            else
            {
                errorProvider.SetError(numPort, "");
            }
        }

        private void ValidateLogPath(object sender, CancelEventArgs e)
        {
            if (chkLogToFile.Checked && string.IsNullOrWhiteSpace(txtLogPath.Text))
            {
                errorProvider.SetError(txtLogPath, "Log file path is required when file logging is enabled");
                e.Cancel = true;
            }
            else
            {
                errorProvider.SetError(txtLogPath, "");
            }
        }

        private void ValidateApiKey(object sender, CancelEventArgs e)
        {
            if (chkSecureConnection.Checked && string.IsNullOrWhiteSpace(txtApiKey.Text))
            {
                errorProvider.SetError(txtApiKey, "API key is required for secure connections");
                e.Cancel = true;
            }
            else
            {
                errorProvider.SetError(txtApiKey, "");
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (ValidateChildren())
            {
                SaveConfiguration();
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btnResetDefaults_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(this, 
                "This will reset all settings to their default values. Are you sure?", 
                "Reset to Defaults", 
                MessageBoxButtons.YesNo, 
                MessageBoxIcon.Question);
                
            if (result == DialogResult.Yes)
            {
                _configuration.ResetToDefaults();
                LoadConfiguration();
            }
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            using var openDialog = new OpenFileDialog
            {
                Title = "Import Configuration",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                CheckFileExists = true
            };

            if (openDialog.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    _configuration.ImportFrom(openDialog.FileName);
                    LoadConfiguration();
                    MessageBox.Show(this, "Configuration imported successfully.", "Import", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Failed to import configuration: {ex.Message}", "Import Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            using var saveDialog = new SaveFileDialog
            {
                Title = "Export Configuration",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json"
            };

            if (saveDialog.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    SaveConfiguration(); // Ensure current UI state is saved
                    _configuration.ExportTo(saveDialog.FileName);
                    MessageBox.Show(this, "Configuration exported successfully.", "Export", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Failed to export configuration: {ex.Message}", "Export Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnBrowseLogPath_Click(object sender, EventArgs e)
        {
            using var saveDialog = new SaveFileDialog
            {
                Title = "Select Log File Location",
                Filter = "Log files (*.log)|*.log|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = "log",
                FileName = txtLogPath.Text
            };

            if (saveDialog.ShowDialog(this) == DialogResult.OK)
            {
                txtLogPath.Text = saveDialog.FileName;
            }
        }

        private void chkLogToFile_CheckedChanged(object sender, EventArgs e)
        {
            txtLogPath.Enabled = chkLogToFile.Checked;
            btnBrowseLogPath.Enabled = chkLogToFile.Checked;
            
            if (chkLogToFile.Checked && string.IsNullOrWhiteSpace(txtLogPath.Text))
            {
                txtLogPath.Text = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "DiztinGUIsh", "mesen2.log");
            }
        }

        private void chkSecureConnection_CheckedChanged(object sender, EventArgs e)
        {
            txtApiKey.Enabled = chkSecureConnection.Checked;
            lblApiKey.Enabled = chkSecureConnection.Checked;
        }

        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Update help text based on selected tab
            var tabControl = sender as TabControl;
            switch (tabControl?.SelectedIndex)
            {
                case 0: // Connection
                    lblHelp.Text = "Configure basic connection settings for Mesen2 integration.";
                    break;
                case 1: // Streaming
                    lblHelp.Text = "Adjust data streaming and performance settings.";
                    break;
                case 2: // Security
                    lblHelp.Text = "Configure security and logging options.";
                    break;
                case 3: // Advanced
                    lblHelp.Text = "Advanced protocol and debugging options.";
                    break;
                default:
                    lblHelp.Text = "Configure Mesen2 integration settings.";
                    break;
            }
        }

        #region Windows Form Designer generated code

        private System.ComponentModel.IContainer components = null;
        private TabControl tabControl;
        private TabPage tabConnection;
        private TabPage tabStreaming;
        private TabPage tabSecurity;
        private TabPage tabAdvanced;
        private ErrorProvider errorProvider;
        private Label lblHelp;
        
        // Connection tab controls
        private TextBox txtHost;
        private NumericUpDown numPort;
        private NumericUpDown numTimeout;
        private CheckBox chkAutoReconnect;
        private NumericUpDown numMaxRetries;
        private NumericUpDown numRetryDelay;
        
        // Streaming tab controls
        private CheckBox chkEnableCompression;
        private NumericUpDown numBufferSize;
        private NumericUpDown numBatchSize;
        private CheckBox chkCpuData;
        private CheckBox chkMemoryData;
        private CheckBox chkDebugData;
        private CheckBox chkEventData;
        private NumericUpDown numUpdateInterval;
        private NumericUpDown numMaxQueueSize;
        
        // Security tab controls
        private CheckBox chkVerboseLogging;
        private CheckBox chkLogToFile;
        private TextBox txtLogPath;
        private Button btnBrowseLogPath;
        private CheckBox chkSecureConnection;
        private TextBox txtApiKey;
        private Label lblApiKey;
        
        // Advanced tab controls
        private CheckBox chkStrictProtocol;
        private NumericUpDown numKeepAlive;
        private CheckBox chkBinaryProtocol;
        
        // Form buttons
        private Button btnOK;
        private Button btnCancel;
        private Button btnResetDefaults;
        private Button btnImport;
        private Button btnExport;

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
            this.components = new Container();
            this.errorProvider = new ErrorProvider(this.components);
            this.tabControl = new TabControl();
            this.tabConnection = new TabPage();
            this.tabStreaming = new TabPage();
            this.tabSecurity = new TabPage();
            this.tabAdvanced = new TabPage();
            this.lblHelp = new Label();
            
            // Initialize all controls
            InitializeConnectionControls();
            InitializeStreamingControls();
            InitializeSecurityControls();
            InitializeAdvancedControls();
            InitializeButtons();
            
            ((ISupportInitialize)(this.errorProvider)).BeginInit();
            this.tabControl.SuspendLayout();
            this.SuspendLayout();
            
            // Form
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(500, 450);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Mesen2 Configuration";
            
            // Tab control
            this.tabControl.Dock = DockStyle.Fill;
            this.tabControl.Location = new Point(0, 0);
            this.tabControl.Size = new Size(500, 400);
            this.tabControl.SelectedIndexChanged += new EventHandler(this.tabControl_SelectedIndexChanged);
            
            this.tabControl.Controls.Add(this.tabConnection);
            this.tabControl.Controls.Add(this.tabStreaming);
            this.tabControl.Controls.Add(this.tabSecurity);
            this.tabControl.Controls.Add(this.tabAdvanced);
            
            // Help label
            this.lblHelp.Dock = DockStyle.Bottom;
            this.lblHelp.Height = 30;
            this.lblHelp.Text = "Configure Mesen2 integration settings.";
            this.lblHelp.TextAlign = ContentAlignment.MiddleLeft;
            this.lblHelp.Padding = new Padding(10, 5, 10, 5);
            this.lblHelp.BackColor = SystemColors.Info;
            this.lblHelp.BorderStyle = BorderStyle.FixedSingle;
            
            // Buttons
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                Padding = new Padding(10)
            };
            
            buttonPanel.Controls.Add(this.btnOK);
            buttonPanel.Controls.Add(this.btnCancel);
            buttonPanel.Controls.Add(this.btnResetDefaults);
            buttonPanel.Controls.Add(this.btnImport);
            buttonPanel.Controls.Add(this.btnExport);
            
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.lblHelp);
            this.Controls.Add(buttonPanel);
            
            this.AcceptButton = this.btnOK;
            this.CancelButton = this.btnCancel;
            
            ((ISupportInitialize)(this.errorProvider)).EndInit();
            this.tabControl.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private void InitializeConnectionControls()
        {
            this.txtHost = new TextBox();
            this.numPort = new NumericUpDown();
            this.numPort.Minimum = 1;
            this.numPort.Maximum = 65535;
            this.numPort.Value = 9998;
            
            this.numTimeout = new NumericUpDown();
            this.numTimeout.Minimum = 1000;
            this.numTimeout.Maximum = 30000;
            this.numTimeout.Value = 5000;
            
            this.chkAutoReconnect = new CheckBox();
            
            this.numMaxRetries = new NumericUpDown();
            this.numMaxRetries.Minimum = 1;
            this.numMaxRetries.Maximum = 100;
            this.numMaxRetries.Value = 5;
            
            this.numRetryDelay = new NumericUpDown();
            this.numRetryDelay.Minimum = 1000;
            this.numRetryDelay.Maximum = 60000;
            this.numRetryDelay.Value = 2000;
            
            this.tabConnection.Text = "Connection";
            this.tabConnection.UseVisualStyleBackColor = true;
            
            // Add layout logic for connection tab controls
            // Implementation details omitted for brevity
        }

        private void InitializeStreamingControls()
        {
            this.chkEnableCompression = new CheckBox();
            
            this.numBufferSize = new NumericUpDown();
            this.numBufferSize.Minimum = 1024;
            this.numBufferSize.Maximum = 1048576; // 1MB
            this.numBufferSize.Value = 8192;
            
            this.numBatchSize = new NumericUpDown();
            this.numBatchSize.Minimum = 1;
            this.numBatchSize.Maximum = 10000;
            this.numBatchSize.Value = 100;
            
            this.chkCpuData = new CheckBox();
            this.chkMemoryData = new CheckBox();
            this.chkDebugData = new CheckBox();
            this.chkEventData = new CheckBox();
            
            this.numUpdateInterval = new NumericUpDown();
            this.numUpdateInterval.Minimum = 100;
            this.numUpdateInterval.Maximum = 10000;
            this.numUpdateInterval.Value = 1000;
            
            this.numMaxQueueSize = new NumericUpDown();
            this.numMaxQueueSize.Minimum = 100;
            this.numMaxQueueSize.Maximum = 100000;
            this.numMaxQueueSize.Value = 10000;
            
            this.tabStreaming.Text = "Streaming";
            this.tabStreaming.UseVisualStyleBackColor = true;
            
            // Add layout logic for streaming tab controls
            // Implementation details omitted for brevity
        }

        private void InitializeSecurityControls()
        {
            this.chkVerboseLogging = new CheckBox();
            this.chkLogToFile = new CheckBox();
            this.txtLogPath = new TextBox();
            this.btnBrowseLogPath = new Button();
            this.chkSecureConnection = new CheckBox();
            this.txtApiKey = new TextBox();
            this.lblApiKey = new Label();
            
            this.tabSecurity.Text = "Security & Logging";
            this.tabSecurity.UseVisualStyleBackColor = true;
            
            this.chkLogToFile.CheckedChanged += new EventHandler(this.chkLogToFile_CheckedChanged);
            this.chkSecureConnection.CheckedChanged += new EventHandler(this.chkSecureConnection_CheckedChanged);
            this.btnBrowseLogPath.Click += new EventHandler(this.btnBrowseLogPath_Click);
            
            // Add layout logic for security tab controls
            // Implementation details omitted for brevity
        }

        private void InitializeAdvancedControls()
        {
            this.chkStrictProtocol = new CheckBox();
            
            this.numKeepAlive = new NumericUpDown();
            this.numKeepAlive.Minimum = 1000;
            this.numKeepAlive.Maximum = 60000;
            this.numKeepAlive.Value = 30000;
            
            this.chkBinaryProtocol = new CheckBox();
            
            this.tabAdvanced.Text = "Advanced";
            this.tabAdvanced.UseVisualStyleBackColor = true;
            
            // Add layout logic for advanced tab controls
            // Implementation details omitted for brevity
        }

        private void InitializeButtons()
        {
            this.btnOK = new Button();
            this.btnCancel = new Button();
            this.btnResetDefaults = new Button();
            this.btnImport = new Button();
            this.btnExport = new Button();
            
            this.btnOK.Text = "OK";
            this.btnOK.Size = new Size(75, 25);
            this.btnOK.Location = new Point(315, 10);
            this.btnOK.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new EventHandler(this.btnOK_Click);
            
            this.btnCancel.Text = "Cancel";
            this.btnCancel.Size = new Size(75, 25);
            this.btnCancel.Location = new Point(405, 10);
            this.btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.DialogResult = DialogResult.Cancel;
            this.btnCancel.Click += new EventHandler(this.btnCancel_Click);
            
            this.btnResetDefaults.Text = "Reset";
            this.btnResetDefaults.Size = new Size(60, 25);
            this.btnResetDefaults.Location = new Point(10, 10);
            this.btnResetDefaults.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this.btnResetDefaults.UseVisualStyleBackColor = true;
            this.btnResetDefaults.Click += new EventHandler(this.btnResetDefaults_Click);
            
            this.btnImport.Text = "Import";
            this.btnImport.Size = new Size(60, 25);
            this.btnImport.Location = new Point(80, 10);
            this.btnImport.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this.btnImport.UseVisualStyleBackColor = true;
            this.btnImport.Click += new EventHandler(this.btnImport_Click);
            
            this.btnExport.Text = "Export";
            this.btnExport.Size = new Size(60, 25);
            this.btnExport.Location = new Point(150, 10);
            this.btnExport.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new EventHandler(this.btnExport_Click);
        }

        #endregion
    }
}