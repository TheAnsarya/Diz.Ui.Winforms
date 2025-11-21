using Diz.Cpu._65816;
using Diz.Import.bsnes.tracelog;
using Diz.Import.mesen.tracelog;
using Diz.Ui.Winforms.dialogs;

namespace Diz.Ui.Winforms.window;

public partial class MainWindow
{
    private void ImportBizhawkCDL()
    {
        var filename = PromptOpenBizhawkCDLFile();
        if (filename != null && filename == "") return;
        ImportBizHawkCdl(filename);
        UpdateSomeUI2();
    }

    private void ImportBizHawkCdl(string filename)
    {
        try
        {
            ProjectController.ImportBizHawkCdl(filename);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message, "Error");
        }
    }

    private void ImportBsnesTraceLogText()
    {
        if (!PromptForImportBSNESTraceLogFile())
            return;

        var (numModifiedFlags, numFiles) = ImportBsnesTraceLogs();

        RefreshUi();
        ReportNumberFlagsModified(numModifiedFlags, numFiles);
    }

    private void UiImportBsnesUsageMap()
    {
        if (openUsageMapFile.ShowDialog() != DialogResult.OK)
            return;

        var numModifiedFlags = ProjectController.ImportBsnesUsageMap(openUsageMapFile.FileName);

        RefreshUi();
        ShowInfo($"Modified total {numModifiedFlags} flags!", "Done");
    }

    private (long numBytesModified, int numFiles) ImportBsnesTraceLogs()
    {
        var numBytesModified = ProjectController.ImportBsnesTraceLogs(openTraceLogDialog.FileNames);
        return (numBytesModified, openTraceLogDialog.FileNames.Length);
    }

    private void ImportBsnesBinaryTraceLog()
    {
        var snesData = Project.Data.GetSnesApi();
        if (snesData == null)
            return;

        var captureController = new BsnesTraceLogCaptureController(snesData);
        new BsnesTraceLogBinaryMonitorForm(captureController).ShowDialog();

        RefreshUi();
    }

    private async void ImportMesenLiveTrace()
    {
        var snesData = Project.Data.GetSnesApi();
        if (snesData == null)
        {
            ShowError("No project loaded!", "Error");
            return;
        }

        // Simple connection dialog
        using var form = new Form();
        form.Text = "Connect to Mesen2";
        form.Size = new Size(300, 150);
        form.StartPosition = FormStartPosition.CenterParent;

        var hostLabel = new Label { Text = "Host:", Location = new Point(10, 20), Size = new Size(40, 20) };
        var hostTextBox = new TextBox { Text = "localhost", Location = new Point(60, 18), Size = new Size(100, 20) };

        var portLabel = new Label { Text = "Port:", Location = new Point(170, 20), Size = new Size(30, 20) };
        var portNumeric = new NumericUpDown
        {
            Minimum = 1024,
            Maximum = 65535,
            Value = 9998,
            Location = new Point(210, 18),
            Size = new Size(60, 20)
        };

        var connectButton = new Button { Text = "Connect", Location = new Point(60, 60), Size = new Size(75, 25) };
        var cancelButton = new Button { Text = "Cancel", Location = new Point(150, 60), Size = new Size(75, 25) };

        connectButton.Click += (s, e) => { form.DialogResult = DialogResult.OK; form.Close(); };
        cancelButton.Click += (s, e) => { form.DialogResult = DialogResult.Cancel; form.Close(); };

        form.Controls.AddRange(new Control[] { hostLabel, hostTextBox, portLabel, portNumeric, connectButton, cancelButton });

        if (form.ShowDialog() != DialogResult.OK)
            return;

        var host = hostTextBox.Text;
        var port = (int)portNumeric.Value;

        // Create and show streaming dialog
        var streamForm = new Form();
        streamForm.Text = $"Mesen2 Live Streaming - {host}:{port}";
        streamForm.Size = new Size(500, 300);
        streamForm.StartPosition = FormStartPosition.CenterParent;

        var statusLabel = new Label
        {
            Text = "Connecting...",
            Location = new Point(10, 10),
            Size = new Size(460, 20)
        };

        var statsLabel = new Label
        {
            Text = "",
            Location = new Point(10, 40),
            Size = new Size(460, 100)
        };

        var stopButton = new Button
        {
            Text = "Stop Streaming",
            Location = new Point(200, 200),
            Size = new Size(100, 30)
        };

        streamForm.Controls.AddRange(new Control[] { statusLabel, statsLabel, stopButton });

        var cancellationTokenSource = new CancellationTokenSource();
        stopButton.Click += (s, e) =>
        {
            cancellationTokenSource.Cancel();
            streamForm.Close();
        };

        streamForm.FormClosing += (s, e) => cancellationTokenSource.Cancel();

        try
        {
            streamForm.Show();

            // Start streaming in background
            var bytesModified = await ProjectController.ImportMesenTraceLive(host, port, cancellationTokenSource.Token);

            streamForm.Close();
            RefreshUi();
            ShowInfo($"Streaming completed. Modified {bytesModified:N0} ROM bytes.", "Mesen2 Live Streaming");
        }
        catch (Exception ex)
        {
            streamForm.Close();
            
            // Provide detailed troubleshooting based on error type
            string troubleshootingSteps;
            if (ex.Message.Contains("refused") || ex.Message.Contains("target machine"))
            {
                troubleshootingSteps = $@"
🔧 Connection Refused - Server Not Running:

1. ✅ Launch Mesen2 emulator
2. ✅ Load a ROM in Mesen2
3. ✅ Open Lua Console in Mesen2
4. ✅ Run: emu.startDiztinguishServer({port})
5. ✅ Start emulation (make sure it's not paused)

💡 Test your setup with: ~docs\test_connection_lifecycle.ps1";
            }
            else if (ex.Message.Contains("timeout"))
            {
                troubleshootingSteps = $@"
🔧 Connection Timeout:

1. ✅ Check if Mesen2 DiztinGUIsh server is running
2. ✅ Verify port {port} is not blocked by firewall
3. ✅ Try restarting: emu.stopDiztinguishServer() then emu.startDiztinguishServer({port})";
            }
            else
            {
                troubleshootingSteps = $@"
🔧 Connection Error:

1. ✅ Mesen2 is running
2. ✅ A ROM is loaded in Mesen2
3. ✅ DiztinGUIsh server enabled: emu.startDiztinguishServer({port})
4. ✅ Emulation is active (not paused)";
            }
            
            ShowError($"Failed to connect to Mesen2 at {host}:{port}.\n\nError: {ex.Message}\n{troubleshootingSteps}", "Mesen2 Connection Error");
        }
    }

    private void OnImportedProjectSuccess()
    {
        UpdateSaveOptionStates(saveEnabled: false, saveAsEnabled: true, closeEnabled: true);
        RefreshUi();
    }
}