using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Diz.Core.Interfaces;

namespace Diz.Ui.Winforms.dialogs
{
    /// <summary>
    /// Connection dialog for Mesen2 integration
    /// </summary>
    public partial class Mesen2ConnectionDialog : Form
    {
        private readonly IMesen2Configuration _configuration;
        
        public Mesen2ConnectionDialog(IMesen2Configuration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            InitializeComponent();
            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            txtHost.Text = _configuration.DefaultHost;
            numPort.Value = _configuration.DefaultPort;
            numTimeout.Value = _configuration.ConnectionTimeoutMs;
            chkAutoReconnect.Checked = _configuration.AutoReconnect;
            numMaxRetries.Value = _configuration.MaxReconnectAttempts;
            numRetryDelay.Value = _configuration.AutoReconnectDelayMs;
            chkVerboseLogging.Checked = _configuration.VerboseLogging;
        }

        private void SaveConfiguration()
        {
            _configuration.DefaultHost = txtHost.Text.Trim();
            _configuration.DefaultPort = (int)numPort.Value;
            _configuration.ConnectionTimeoutMs = (int)numTimeout.Value;
            _configuration.AutoReconnect = chkAutoReconnect.Checked;
            _configuration.MaxReconnectAttempts = (int)numMaxRetries.Value;
            _configuration.AutoReconnectDelayMs = (int)numRetryDelay.Value;
            _configuration.VerboseLogging = chkVerboseLogging.Checked;
            
            _configuration.Save();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (ValidateInput())
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

        private void btnTestConnection_Click(object sender, EventArgs e)
        {
            try
            {
                using var client = new System.Net.Sockets.TcpClient();
                client.ReceiveTimeout = (int)numTimeout.Value;
                client.SendTimeout = (int)numTimeout.Value;
                
                var host = txtHost.Text.Trim();
                var port = (int)numPort.Value;
                
                client.Connect(host, port);
                client.Close();
                
                MessageBox.Show(this, $"Successfully connected to {host}:{port}!", "Connection Test", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Connection failed: {ex.Message}", "Connection Test", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnResetDefaults_Click(object sender, EventArgs e)
        {
            txtHost.Text = "127.0.0.1";
            numPort.Value = 9998;
            numTimeout.Value = 5000;
            chkAutoReconnect.Checked = true;
            numMaxRetries.Value = 5;
            numRetryDelay.Value = 2000;
            chkVerboseLogging.Checked = false;
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtHost.Text))
            {
                MessageBox.Show(this, "Host cannot be empty.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtHost.Focus();
                return false;
            }

            if (numPort.Value < 1 || numPort.Value > 65535)
            {
                MessageBox.Show(this, "Port must be between 1 and 65535.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                numPort.Focus();
                return false;
            }

            return true;
        }

        #region Windows Form Designer generated code

        private System.ComponentModel.IContainer components = null;
        private TextBox txtHost;
        private NumericUpDown numPort;
        private NumericUpDown numTimeout;
        private CheckBox chkAutoReconnect;
        private NumericUpDown numMaxRetries;
        private NumericUpDown numRetryDelay;
        private CheckBox chkVerboseLogging;
        private Button btnOK;
        private Button btnCancel;
        private Button btnTestConnection;
        private Button btnResetDefaults;
        private Label lblHost;
        private Label lblPort;
        private Label lblTimeout;
        private Label lblMaxRetries;
        private Label lblRetryDelay;
        private GroupBox grpConnection;
        private GroupBox grpReconnection;
        private GroupBox grpAdvanced;

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
            this.txtHost = new TextBox();
            this.numPort = new NumericUpDown();
            this.numTimeout = new NumericUpDown();
            this.chkAutoReconnect = new CheckBox();
            this.numMaxRetries = new NumericUpDown();
            this.numRetryDelay = new NumericUpDown();
            this.chkVerboseLogging = new CheckBox();
            this.btnOK = new Button();
            this.btnCancel = new Button();
            this.btnTestConnection = new Button();
            this.btnResetDefaults = new Button();
            this.lblHost = new Label();
            this.lblPort = new Label();
            this.lblTimeout = new Label();
            this.lblMaxRetries = new Label();
            this.lblRetryDelay = new Label();
            this.grpConnection = new GroupBox();
            this.grpReconnection = new GroupBox();
            this.grpAdvanced = new GroupBox();
            
            ((ISupportInitialize)(this.numPort)).BeginInit();
            ((ISupportInitialize)(this.numTimeout)).BeginInit();
            ((ISupportInitialize)(this.numMaxRetries)).BeginInit();
            ((ISupportInitialize)(this.numRetryDelay)).BeginInit();
            this.grpConnection.SuspendLayout();
            this.grpReconnection.SuspendLayout();
            this.grpAdvanced.SuspendLayout();
            this.SuspendLayout();
            
            // Form
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(420, 380);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Mesen2 Connection Configuration";
            
            // Connection Group
            this.grpConnection.Text = "Connection Settings";
            this.grpConnection.Location = new Point(12, 12);
            this.grpConnection.Size = new Size(385, 120);
            
            this.lblHost.Text = "Host:";
            this.lblHost.Location = new Point(15, 25);
            this.lblHost.Size = new Size(60, 23);
            
            this.txtHost.Location = new Point(80, 22);
            this.txtHost.Size = new Size(200, 23);
            
            this.lblPort.Text = "Port:";
            this.lblPort.Location = new Point(290, 25);
            this.lblPort.Size = new Size(30, 23);
            
            this.numPort.Location = new Point(325, 22);
            this.numPort.Size = new Size(70, 23);
            this.numPort.Minimum = 1;
            this.numPort.Maximum = 65535;
            
            this.lblTimeout.Text = "Timeout (ms):";
            this.lblTimeout.Location = new Point(15, 55);
            this.lblTimeout.Size = new Size(80, 23);
            
            this.numTimeout.Location = new Point(100, 52);
            this.numTimeout.Size = new Size(80, 23);
            this.numTimeout.Minimum = 1000;
            this.numTimeout.Maximum = 30000;
            
            this.btnTestConnection.Text = "Test Connection";
            this.btnTestConnection.Location = new Point(15, 85);
            this.btnTestConnection.Size = new Size(120, 25);
            this.btnTestConnection.UseVisualStyleBackColor = true;
            this.btnTestConnection.Click += new EventHandler(this.btnTestConnection_Click);
            
            this.grpConnection.Controls.Add(this.lblHost);
            this.grpConnection.Controls.Add(this.txtHost);
            this.grpConnection.Controls.Add(this.lblPort);
            this.grpConnection.Controls.Add(this.numPort);
            this.grpConnection.Controls.Add(this.lblTimeout);
            this.grpConnection.Controls.Add(this.numTimeout);
            this.grpConnection.Controls.Add(this.btnTestConnection);
            
            // Reconnection Group
            this.grpReconnection.Text = "Auto-Reconnection";
            this.grpReconnection.Location = new Point(12, 140);
            this.grpReconnection.Size = new Size(385, 100);
            
            this.chkAutoReconnect.Text = "Enable auto-reconnection";
            this.chkAutoReconnect.Location = new Point(15, 25);
            this.chkAutoReconnect.Size = new Size(160, 24);
            this.chkAutoReconnect.UseVisualStyleBackColor = true;
            
            this.lblMaxRetries.Text = "Max attempts:";
            this.lblMaxRetries.Location = new Point(15, 55);
            this.lblMaxRetries.Size = new Size(85, 23);
            
            this.numMaxRetries.Location = new Point(105, 52);
            this.numMaxRetries.Size = new Size(60, 23);
            this.numMaxRetries.Minimum = 1;
            this.numMaxRetries.Maximum = 100;
            
            this.lblRetryDelay.Text = "Delay (ms):";
            this.lblRetryDelay.Location = new Point(180, 55);
            this.lblRetryDelay.Size = new Size(70, 23);
            
            this.numRetryDelay.Location = new Point(255, 52);
            this.numRetryDelay.Size = new Size(80, 23);
            this.numRetryDelay.Minimum = 500;
            this.numRetryDelay.Maximum = 60000;
            
            this.grpReconnection.Controls.Add(this.chkAutoReconnect);
            this.grpReconnection.Controls.Add(this.lblMaxRetries);
            this.grpReconnection.Controls.Add(this.numMaxRetries);
            this.grpReconnection.Controls.Add(this.lblRetryDelay);
            this.grpReconnection.Controls.Add(this.numRetryDelay);
            
            // Advanced Group
            this.grpAdvanced.Text = "Advanced";
            this.grpAdvanced.Location = new Point(12, 248);
            this.grpAdvanced.Size = new Size(385, 60);
            
            this.chkVerboseLogging.Text = "Enable verbose logging";
            this.chkVerboseLogging.Location = new Point(15, 25);
            this.chkVerboseLogging.Size = new Size(160, 24);
            this.chkVerboseLogging.UseVisualStyleBackColor = true;
            
            this.grpAdvanced.Controls.Add(this.chkVerboseLogging);
            
            // Buttons
            this.btnOK.Text = "OK";
            this.btnOK.Location = new Point(160, 320);
            this.btnOK.Size = new Size(75, 25);
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new EventHandler(this.btnOK_Click);
            
            this.btnCancel.Text = "Cancel";
            this.btnCancel.Location = new Point(245, 320);
            this.btnCancel.Size = new Size(75, 25);
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.DialogResult = DialogResult.Cancel;
            this.btnCancel.Click += new EventHandler(this.btnCancel_Click);
            
            this.btnResetDefaults.Text = "Reset to Defaults";
            this.btnResetDefaults.Location = new Point(12, 320);
            this.btnResetDefaults.Size = new Size(110, 25);
            this.btnResetDefaults.UseVisualStyleBackColor = true;
            this.btnResetDefaults.Click += new EventHandler(this.btnResetDefaults_Click);
            
            // Add controls to form
            this.Controls.Add(this.grpConnection);
            this.Controls.Add(this.grpReconnection);
            this.Controls.Add(this.grpAdvanced);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnResetDefaults);
            
            this.AcceptButton = this.btnOK;
            this.CancelButton = this.btnCancel;
            
            ((ISupportInitialize)(this.numPort)).EndInit();
            ((ISupportInitialize)(this.numTimeout)).EndInit();
            ((ISupportInitialize)(this.numMaxRetries)).EndInit();
            ((ISupportInitialize)(this.numRetryDelay)).EndInit();
            this.grpConnection.ResumeLayout(false);
            this.grpConnection.PerformLayout();
            this.grpReconnection.ResumeLayout(false);
            this.grpReconnection.PerformLayout();
            this.grpAdvanced.ResumeLayout(false);
            this.grpAdvanced.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion
    }
}