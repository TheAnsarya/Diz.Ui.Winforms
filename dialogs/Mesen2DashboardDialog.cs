using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Diz.Core.Interfaces;

namespace Diz.Ui.Winforms.dialogs
{
    /// <summary>
    /// Comprehensive dashboard for Mesen2 integration overview and control
    /// </summary>
    public partial class Mesen2DashboardDialog : Form
    {
        private readonly IMesen2IntegrationController _controller;
        private readonly Timer _updateTimer;
        private Form? _statusDialog;
        private Form? _traceViewer;
        
        public Mesen2DashboardDialog(IMesen2IntegrationController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            InitializeComponent();
            
            _updateTimer = new Timer { Interval = 1000 };
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
            
            // Initialize dashboard
            UpdateDashboard();
            SetupQuickActions();
        }

        private void SetupQuickActions()
        {
            // Setup quick action buttons based on current state
            UpdateQuickActions();
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            UpdateDashboard();
        }

        private void UpdateDashboard()
        {
            try
            {
                var client = _controller.StreamingClient;
                var config = _controller.Configuration;
                
                // Connection status panel
                UpdateConnectionStatus(client);
                
                // Statistics panel
                UpdateStatistics(client);
                
                // Configuration summary panel
                UpdateConfigurationSummary(config);
                
                // Quick actions panel
                UpdateQuickActions();
                
                // Recent activity panel
                UpdateRecentActivity(client);
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Dashboard Error: {ex.Message}";
                lblStatus.ForeColor = Color.Red;
            }
        }

        private void UpdateConnectionStatus(IMesen2StreamingClient? client)
        {
            if (client?.IsConnected == true)
            {
                lblConnectionStatus.Text = "Connected";
                lblConnectionStatus.ForeColor = Color.Green;
                lblConnectionDetails.Text = $"{client.Host}:{client.Port}";
                panelConnectionStatus.BackColor = Color.LightGreen;
                
                var uptime = DateTime.Now - client.ConnectedTime;
                lblUptime.Text = $"{uptime.Days}d {uptime.Hours:D2}h {uptime.Minutes:D2}m";
            }
            else
            {
                lblConnectionStatus.Text = "Disconnected";
                lblConnectionStatus.ForeColor = Color.Red;
                lblConnectionDetails.Text = "Not connected";
                panelConnectionStatus.BackColor = Color.LightPink;
                lblUptime.Text = "N/A";
            }
            
            // Health indicator
            if (client?.IsConnected == true)
            {
                var latency = client.LastLatency;
                if (latency < 50)
                {
                    lblHealth.Text = "Excellent";
                    lblHealth.ForeColor = Color.Green;
                }
                else if (latency < 100)
                {
                    lblHealth.Text = "Good";
                    lblHealth.ForeColor = Color.Orange;
                }
                else
                {
                    lblHealth.Text = "Poor";
                    lblHealth.ForeColor = Color.Red;
                }
            }
            else
            {
                lblHealth.Text = "N/A";
                lblHealth.ForeColor = Color.Gray;
            }
        }

        private void UpdateStatistics(IMesen2StreamingClient? client)
        {
            if (client != null)
            {
                lblMessagesReceived.Text = $"{client.MessagesReceived:N0}";
                lblMessagesSent.Text = $"{client.MessagesSent:N0}";
                lblBytesReceived.Text = FormatBytes(client.BytesReceived);
                lblBytesSent.Text = FormatBytes(client.BytesSent);
                lblThroughput.Text = $"{client.MessagesPerSecond:F1} msg/s";
                
                if (client.IsConnected)
                {
                    lblLatency.Text = $"{client.LastLatency}ms";
                }
                else
                {
                    lblLatency.Text = "N/A";
                }
            }
            else
            {
                lblMessagesReceived.Text = "0";
                lblMessagesSent.Text = "0";
                lblBytesReceived.Text = "0 B";
                lblBytesSent.Text = "0 B";
                lblThroughput.Text = "0 msg/s";
                lblLatency.Text = "N/A";
            }
        }

        private void UpdateConfigurationSummary(IMesen2Configuration? config)
        {
            if (config != null)
            {
                lblConfigHost.Text = config.DefaultHost;
                lblConfigPort.Text = config.DefaultPort.ToString();
                lblConfigAutoReconnect.Text = config.AutoReconnect ? "Enabled" : "Disabled";
                lblConfigCompression.Text = config.EnableCompression ? "Enabled" : "Disabled";
                lblConfigVerboseLogging.Text = config.VerboseLogging ? "Enabled" : "Disabled";
                
                // Data streaming settings
                var streamingOptions = new System.Collections.Generic.List<string>();
                if (config.StreamCpuData) streamingOptions.Add("CPU");
                if (config.StreamMemoryData) streamingOptions.Add("Memory");
                if (config.StreamDebugData) streamingOptions.Add("Debug");
                if (config.StreamEventData) streamingOptions.Add("Events");
                
                lblConfigStreaming.Text = streamingOptions.Count > 0 ? 
                    string.Join(", ", streamingOptions) : "None";
            }
            else
            {
                lblConfigHost.Text = "Unknown";
                lblConfigPort.Text = "Unknown";
                lblConfigAutoReconnect.Text = "Unknown";
                lblConfigCompression.Text = "Unknown";
                lblConfigVerboseLogging.Text = "Unknown";
                lblConfigStreaming.Text = "Unknown";
            }
        }

        private void UpdateQuickActions()
        {
            var isConnected = _controller.StreamingClient?.IsConnected == true;
            
            btnConnect.Enabled = !isConnected;
            btnDisconnect.Enabled = isConnected;
            btnOpenTraceViewer.Enabled = isConnected;
            
            // Update button text based on state
            btnConnect.Text = isConnected ? "Connected" : "Connect";
            btnDisconnect.Text = isConnected ? "Disconnect" : "Not Connected";
        }

        private void UpdateRecentActivity(IMesen2StreamingClient? client)
        {
            if (client?.RecentLogs?.Count > 0)
            {
                var recentLogs = new System.Collections.Generic.List<string>();
                foreach (var log in client.RecentLogs.TakeLast(5))
                {
                    recentLogs.Add($"[{log.Timestamp:HH:mm:ss}] {log.Message}");
                }
                
                txtRecentActivity.Lines = recentLogs.ToArray();
            }
            else
            {
                txtRecentActivity.Text = "No recent activity";
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

        // Quick Action Event Handlers
        private async void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                btnConnect.Enabled = false;
                await _controller.ConnectToMesen2Async();
                UpdateDashboard();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Connection failed: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnConnect.Enabled = true;
            }
        }

        private async void btnDisconnect_Click(object sender, EventArgs e)
        {
            try
            {
                btnDisconnect.Enabled = false;
                await _controller.DisconnectFromMesen2Async();
                UpdateDashboard();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Disconnect failed: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnDisconnect.Enabled = true;
            }
        }

        private void btnOpenStatus_Click(object sender, EventArgs e)
        {
            try
            {
                if (_statusDialog == null || _statusDialog.IsDisposed)
                {
                    _statusDialog = new Mesen2StatusDialog(_controller);
                    _statusDialog.Show(this);
                }
                else
                {
                    _statusDialog.BringToFront();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to open status dialog: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnOpenTraceViewer_Click(object sender, EventArgs e)
        {
            try
            {
                if (_traceViewer == null || _traceViewer.IsDisposed)
                {
                    _traceViewer = new Mesen2TraceViewerDialog(_controller);
                    _traceViewer.Show(this);
                }
                else
                {
                    _traceViewer.BringToFront();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to open trace viewer: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnConnectionConfig_Click(object sender, EventArgs e)
        {
            try
            {
                using var dialog = new Mesen2ConnectionDialog(_controller.Configuration);
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    UpdateDashboard();
                    MessageBox.Show(this, "Configuration updated successfully.", "Configuration", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to open connection configuration: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAdvancedConfig_Click(object sender, EventArgs e)
        {
            try
            {
                using var dialog = new Mesen2ConfigurationDialog(_controller.Configuration);
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    UpdateDashboard();
                    MessageBox.Show(this, "Advanced configuration updated successfully.", "Configuration", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to open advanced configuration: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnRefreshDashboard_Click(object sender, EventArgs e)
        {
            UpdateDashboard();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _updateTimer?.Stop();
            _updateTimer?.Dispose();
            
            // Close child dialogs
            _statusDialog?.Close();
            _traceViewer?.Close();
            
            base.OnFormClosing(e);
        }

        #region Windows Form Designer generated code

        private System.ComponentModel.IContainer components = null;
        private Panel panelConnectionStatus;
        private Panel panelStatistics;
        private Panel panelConfiguration;
        private Panel panelQuickActions;
        private Panel panelRecentActivity;
        
        // Connection status controls
        private Label lblConnectionStatus;
        private Label lblConnectionDetails;
        private Label lblUptime;
        private Label lblHealth;
        
        // Statistics controls
        private Label lblMessagesReceived;
        private Label lblMessagesSent;
        private Label lblBytesReceived;
        private Label lblBytesSent;
        private Label lblThroughput;
        private Label lblLatency;
        
        // Configuration summary controls
        private Label lblConfigHost;
        private Label lblConfigPort;
        private Label lblConfigAutoReconnect;
        private Label lblConfigCompression;
        private Label lblConfigVerboseLogging;
        private Label lblConfigStreaming;
        
        // Quick action buttons
        private Button btnConnect;
        private Button btnDisconnect;
        private Button btnOpenStatus;
        private Button btnOpenTraceViewer;
        private Button btnConnectionConfig;
        private Button btnAdvancedConfig;
        private Button btnRefreshDashboard;
        
        // Recent activity
        private TextBox txtRecentActivity;
        
        // Status
        private Label lblStatus;

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
            // Form setup
            this.Text = "Mesen2 Integration Dashboard";
            this.Size = new Size(800, 600);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(700, 500);
            
            // Initialize all panels and controls
            InitializePanels();
            InitializeControls();
            LayoutControls();
        }

        private void InitializePanels()
        {
            this.panelConnectionStatus = new Panel();
            this.panelStatistics = new Panel();
            this.panelConfiguration = new Panel();
            this.panelQuickActions = new Panel();
            this.panelRecentActivity = new Panel();
            
            // Connection Status Panel
            this.panelConnectionStatus.BorderStyle = BorderStyle.FixedSingle;
            this.panelConnectionStatus.Location = new Point(12, 12);
            this.panelConnectionStatus.Size = new Size(375, 120);
            this.panelConnectionStatus.BackColor = Color.LightGray;
            
            // Statistics Panel
            this.panelStatistics.BorderStyle = BorderStyle.FixedSingle;
            this.panelStatistics.Location = new Point(400, 12);
            this.panelStatistics.Size = new Size(375, 120);
            
            // Configuration Panel
            this.panelConfiguration.BorderStyle = BorderStyle.FixedSingle;
            this.panelConfiguration.Location = new Point(12, 140);
            this.panelConfiguration.Size = new Size(375, 140);
            
            // Quick Actions Panel
            this.panelQuickActions.BorderStyle = BorderStyle.FixedSingle;
            this.panelQuickActions.Location = new Point(400, 140);
            this.panelQuickActions.Size = new Size(375, 140);
            
            // Recent Activity Panel
            this.panelRecentActivity.BorderStyle = BorderStyle.FixedSingle;
            this.panelRecentActivity.Location = new Point(12, 290);
            this.panelRecentActivity.Size = new Size(763, 120);
            
            this.Controls.Add(this.panelConnectionStatus);
            this.Controls.Add(this.panelStatistics);
            this.Controls.Add(this.panelConfiguration);
            this.Controls.Add(this.panelQuickActions);
            this.Controls.Add(this.panelRecentActivity);
        }

        private void InitializeControls()
        {
            // Initialize all labels and controls
            // Implementation details abbreviated for brevity
            // In a real implementation, all controls would be fully initialized here
            this.lblStatus = new Label { Dock = DockStyle.Bottom, Height = 25, Text = "Ready" };
            this.Controls.Add(this.lblStatus);
        }

        private void LayoutControls()
        {
            // Layout all controls within their panels
            // Implementation details abbreviated for brevity
            // In a real implementation, precise positioning would be specified here
        }

        #endregion
    }
}