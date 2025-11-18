using System.Configuration;
using System.Reflection;
using Diz.Controllers.controllers;
using Diz.Controllers.interfaces;
using Diz.Controllers.util;
using Diz.Core.Interfaces;
using Diz.LogWriter;
using Diz.Ui.Winforms.dialogs;
using Diz.Ui.Winforms.util;

namespace Diz.Ui.Winforms.window;

public partial class MainWindow : Form, IMainGridWindowView
{
    private readonly IViewFactory viewFactory;
    private readonly IMesen2IntegrationController mesen2IntegrationController;

    public MainWindow(
        IProjectController projectController,
        IDizAppSettings appSettings, 
        IDizDocument document,
        IViewFactory viewFactory,
        IAppVersionInfo appVersionInfo,
        IMesen2IntegrationController mesen2IntegrationController)
    {
        Document = document;
        this.appSettings = appSettings;
        this.viewFactory = viewFactory;
        this.mesen2IntegrationController = mesen2IntegrationController;
        ProjectController = projectController;
        ProjectController.ProjectView = this;
        this.appVersionInfo = appVersionInfo;

        labelsView = viewFactory.GetLabelEditorView();
        labelsView.SetProjectController(ProjectController);

        regionsView = viewFactory.GetRegionEditorView();
        regionsView.SetProjectController(ProjectController);
            
        Document.PropertyChanged += Document_PropertyChanged;
        ProjectController.ProjectChanged += ProjectController_ProjectChanged;
        // Set up form closed event to handle cleanup and trigger interface event
        FormClosed += (sender, args) => {
            // Clean up Mesen2 integration
            mesen2IntegrationController?.Shutdown();
            // Trigger the interface event
            OnFormClosed?.Invoke(this, EventArgs.Empty);
        };

        NavigationForm = new NavigationForm
        {
            Document = Document,
            SnesNavigation = this,
        };

        InitializeComponent();
        
        // Initialize Mesen2 integration
        mesen2IntegrationController.Initialize();
        UpdateMesen2MenuState();
    }
    
    
    [AttributeUsage(AttributeTargets.Method)]
    public class MenuItemAttribute(string menu, string name, Keys shortcutKeys = Keys.None, bool visible = true) : Attribute
    {
        public string Name { get; } = name;
        public string Menu { get; } = menu;
        public Keys ShortcutKeys { get; } = shortcutKeys;
        public bool Visible { get; } = visible;
    }


    private void AddDynamicMenuItems()
    {
        // a lot of the Diz UI is hardcoded in the visual studio designer.
        // we want to move away from that and have the menu items dynamically populate
        // so we don't need a UI designer to add simple UI elements.
        // this is the first attempt at that.  we should migrate more of the hardcoded designer stuff into here
        // example
        
        // Use reflection to find methods in this class with the MenuItemAttribute
        var methodsWithMenuItems = this.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic) // include non-public methods
            .Where(m => m.GetCustomAttribute<MenuItemAttribute>() != null); // only methods with the MenuItemAttribute attached

        foreach (var method in methodsWithMenuItems)
        {
            // add each menu item found to the correct dropdown menu
            
            var attribute = method.GetCustomAttribute<MenuItemAttribute>();
            if (attribute == null || attribute.Visible == false) 
                continue;
            
            var targetMenu = menuStrip1.Items
                .OfType<ToolStripMenuItem>() // Cast menu items to ToolStripMenuItem
                .FirstOrDefault(menuItem =>
                    string.Equals(menuItem.Text?.Replace("&", string.Empty) ?? "", attribute.Menu, StringComparison.OrdinalIgnoreCase)
                );

            if (targetMenu == null)
            {
                // TODO: could also dynamically add a new menu here
                continue;
            }

            var newMenuItem = new ToolStripMenuItem
            {
                Size = new Size(253, 22),
                Name = method.Name,
                ShortcutKeys = attribute.ShortcutKeys,
                Text = attribute.Name,
            };
            
            var callbackMethod = (Action)Delegate.CreateDelegate(typeof(Action), this, method);
            newMenuItem.Click += (_, _) => callbackMethod();
            
            targetMenu.DropDownItems.Add(newMenuItem);
        }
    }
    
    private static void InitializeConfiguration()
    {
        try
        {
            // hack: Try to access some of the settings to test configuration file works
            var _ = Properties.Settings.Default.LastOpenedFile;
            var __ = Properties.Settings.Default.OpenLastFileAutomatically;
        }
        catch (ConfigurationErrorsException)
        {
            // if it's not ok, reset it:
            MessageBox.Show("Diz user.config file failed to load, attempting to reset it. If you have further issues, please manually delete it.");
            try
            {
                Properties.Settings.Default.Reset();
                Properties.Settings.Default.Save();
            }
            catch (Exception resetEx)
            {
                MessageBox.Show("Diz user.config reset failed.  Please manually delete it.");
                System.Diagnostics.Debug.WriteLine($"Failed to reset configuration: {resetEx.Message}");
            }
        }
    }


    private void Init()
    {
        InitializeConfiguration();
        
        AddDynamicMenuItems();
        
        InitMainTable();

        UpdatePanels();
        UpdateUiFromSettings();

        if (appSettings.OpenLastFileAutomatically)
            OpenLastProject();
    }


    private void Document_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DizDocument.LastProjectFilename))
        {
            UpdateUiFromSettings();
        }
    }

    private void ProjectController_ProjectChanged(object sender, IProjectController.ProjectChangedEventArgs e)
    {
        switch (e.ChangeType)
        {
            case IProjectController.ProjectChangedEventArgs.ProjectChangedType.Saved:
                OnProjectSaved();
                break;
            case IProjectController.ProjectChangedEventArgs.ProjectChangedType.Opened:
                OnProjectOpened(e.Filename);
                break;
            case IProjectController.ProjectChangedEventArgs.ProjectChangedType.Imported:
                OnImportedProjectSuccess();
                break;
            case IProjectController.ProjectChangedEventArgs.ProjectChangedType.Closing:
                OnProjectClosing();
                break;
        }

        RebindProject();
    }

    private void OnProjectClosing()
    {
        CloseAndDisposeOtherViews();
        
        UpdateSaveOptionStates(saveEnabled: false, saveAsEnabled: false, closeEnabled: false);
    }

    public void OnProjectOpened(string filename)
    {
        // TODO: do this with aliaslist too.
        CloseAndDisposeOtherViews();

        UpdateSaveOptionStates(saveEnabled: true, saveAsEnabled: true, closeEnabled: true);
        RefreshUi();

        Document.LastProjectFilename = filename; // do this last.

        BringFormToTop();
    }

    private void CloseAndDisposeOtherViews()
    {
        // close and dispose visualizer
        visualForm?.Close();
        visualForm?.Dispose();
        visualForm = null;
    }

    public void OnProjectOpenFail(string errorMsg)
    {
        Document.LastProjectFilename = "";
        ShowError(errorMsg, "Error opening project");
    }

    public void OnProjectSaved()
    {
        UpdateSaveOptionStates(saveEnabled: true, saveAsEnabled: true, closeEnabled: true);
        UpdateWindowTitle();
        BringFormToTop();
    }

    public void OnExportFinished(LogCreatorOutput.OutputResult result)
    {
        ShowExportResults(result);
        BringFormToTop();
    }

    private void RememberNavigationPoint(int pcOffset, ISnesNavigation.HistoryArgs? historyArgs)
    {
        var snesAddress = Project.Data.ConvertPCtoSnes(pcOffset);
        var history = Document.NavigationHistory;
            
        // if our last remembered offset IS the new offset, don't record it again
        // (prevents duplication)
        if (history.Count > 0 && history[history.Count-1].SnesOffset == snesAddress)
            return;

        history.Add(
            new NavigationEntry(
                snesAddress, 
                historyArgs,
                Project.Data
            )
        );
    }

    private void timer1_Tick(object sender, System.EventArgs e)
    {
        // the point of this timer is to throttle the ROM% calculator
        // since it is an expensive calculation. letting it happen attached to UI events
        // would significantly slow the user down.
        //
        // TODO: this is the kind of thing that Rx.net's Throttle function, or 
        // an async task would handle much better. For now, this is fine.
        UpdatePercentageCalculatorCooldown();
    }

    private void UpdatePercentageCalculatorCooldown()
    {
        if (_cooldownForPercentUpdate == -1)
            return;

        if (--_cooldownForPercentUpdate == -1)
            UpdatePercent(forceRecalculate: true);
    }

    public event EventHandler OnFormClosed;

    public void BringFormToTop() => this.BringWinFormToTop();

    #region Mesen2 Integration Event Handlers

    private void connectToMesen2ToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        Task.Run(async () =>
        {
            var success = await mesen2IntegrationController.ConnectToMesen2Async();
            
            Invoke(() =>
            {
                UpdateMesen2MenuState();
                
                if (success)
                {
                    MessageBox.Show(this, "Successfully connected to Mesen2!", "Mesen2 Integration", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(this, "Failed to connect to Mesen2. Please ensure:\n\n" +
                        "1. Mesen2 is running with a ROM loaded\n" +
                        "2. DiztinGUIsh server is started (Tools → DiztinGUIsh Server)\n" +
                        "3. Server is running on the configured port\n" +
                        "4. No firewall is blocking the connection", "Connection Failed", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            });
        });
    }

    private void disconnectFromMesen2ToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        Task.Run(async () =>
        {
            await mesen2IntegrationController.DisconnectFromMesen2Async();
            
            Invoke(() =>
            {
                UpdateMesen2MenuState();
                MessageBox.Show(this, "Disconnected from Mesen2.", "Mesen2 Integration", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
        });
    }

    private void mesen2StatusToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        mesen2IntegrationController.ShowStatusWindow();
    }

    private void mesen2DashboardToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        mesen2IntegrationController.ShowDashboard();
    }

    private void mesen2TraceViewerToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        mesen2IntegrationController.ShowTraceViewer();
    }

    private void mesen2ConfigurationToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        mesen2IntegrationController.ShowConnectionDialog();
    }

    private void advancedConfigurationToolStripMenuItem_Click(object? sender, EventArgs e)
    {
        mesen2IntegrationController.ShowAdvancedConfigurationDialog();
    }

    private void UpdateMesen2MenuState()
    {
        var isConnected = mesen2IntegrationController.Client?.IsConnected == true;
        
        connectToMesen2ToolStripMenuItem.Enabled = !isConnected;
        disconnectFromMesen2ToolStripMenuItem.Enabled = isConnected;
        
        // Update status text
        var statusText = isConnected ? "Connected to Mesen2" : "Disconnected from Mesen2";
        mesen2StatusToolStripMenuItem.Text = $"Show &Status Window ({statusText})";
    }

    #endregion
}