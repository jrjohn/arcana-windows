using Arcana.App.Navigation;
using Arcana.App.Services;
using Arcana.App.Views;
using Arcana.Core.Common;
using Arcana.Plugins.Contracts;
using Arcana.Sync;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Arcana.App;

/// <summary>
/// Main application window.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly INavigationService _navigationService;
    private readonly NetworkMonitor _networkMonitor;
    private readonly SyncService _syncService;
    private readonly IMenuRegistry _menuRegistry;
    private readonly CommandService _commandService;
    private readonly LocalizationService _localization;
    private readonly IViewRegistry _viewRegistry;
    private readonly ThemeService _themeService;
    private readonly DispatcherTimer _timer;

    public MainWindow()
    {
        this.InitializeComponent();

        // Set window size
        var appWindow = this.AppWindow;
        appWindow.Resize(new Windows.Graphics.SizeInt32(1400, 900));

        // Get services
        _navigationService = App.Services.GetRequiredService<INavigationService>();
        _networkMonitor = App.Services.GetRequiredService<NetworkMonitor>();
        _syncService = App.Services.GetRequiredService<SyncService>();
        _menuRegistry = App.Services.GetRequiredService<IMenuRegistry>();
        _commandService = App.Services.GetRequiredService<CommandService>();
        _localization = App.Services.GetRequiredService<LocalizationService>();
        _viewRegistry = App.Services.GetRequiredService<IViewRegistry>();
        _themeService = App.Services.GetRequiredService<ThemeService>();

        // Setup event handlers
        _networkMonitor.StatusChanged += OnNetworkStatusChanged;
        _syncService.StateChanged += OnSyncStateChanged;
        _menuRegistry.MenusChanged += OnMenusChanged;
        _localization.CultureChanged += OnCultureChanged;
        _themeService.ThemeChanged += OnThemeChanged;
        NavView.Loaded += OnNavViewLoaded;

        // Setup timer for clock
        _timer = new DispatcherTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (s, e) => UpdateClock();
        _timer.Start();

        // Initialize
        UpdateWindowTitle();
        UpdateNetworkStatus();
        UpdateSyncStatus();
        UpdateClock();
        BuildMenuBar();
        BuildQuickActionsMenu();
        BuildFunctionTree();
        UpdateNavigationItems();

        // Navigate to home page
        NavigateToPage("HomePage");
    }

    private void OnNavViewLoaded(object sender, RoutedEventArgs e)
    {
        // Update Settings item after NavigationView is fully loaded
        UpdateSettingsItem();
        UpdatePaneToggleButtonTooltip();

        // Listen for pane state changes to update tooltip
        NavView.PaneOpening += (s, args) => UpdatePaneToggleButtonTooltip();
        NavView.PaneClosing += (s, args) => UpdatePaneToggleButtonTooltip();
    }

    private void UpdatePaneToggleButtonTooltip()
    {
        // Find the pane toggle button in the visual tree and update its tooltip
        var toggleButton = FindVisualChild<Button>(NavView, "TogglePaneButton");
        if (toggleButton != null)
        {
            var tooltipKey = NavView.IsPaneOpen ? "tooltip.closeNavigation" : "tooltip.openNavigation";
            ToolTipService.SetToolTip(toggleButton, _localization.Get(tooltipKey));
        }
    }

    private static T? FindVisualChild<T>(DependencyObject parent, string name) where T : FrameworkElement
    {
        var count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T element && element.Name == name)
            {
                return element;
            }

            var result = FindVisualChild<T>(child, name);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }

    private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            var backgroundBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(e.NewTheme.BackgroundColor);
            var isCustomTheme = e.NewTheme.Id != "System" && e.NewTheme.Id != "Light" && e.NewTheme.Id != "Dark";

            // Apply theme to all tab contents
            foreach (var item in TabViewMain.TabItems)
            {
                if (item is TabViewItem tab && tab.Content is Frame frame && frame.Content is FrameworkElement page)
                {
                    // Set RequestedTheme for Light/Dark base theme
                    page.RequestedTheme = e.NewTheme.BaseTheme;

                    // Apply or clear background
                    ApplyThemeToElement(page, backgroundBrush, isCustomTheme);
                }
            }
        });
    }

    private static void ApplyThemeToElement(FrameworkElement element, Microsoft.UI.Xaml.Media.Brush backgroundBrush, bool isCustomTheme)
    {
        if (isCustomTheme)
        {
            // For custom themes, apply background directly
            if (element is Page page)
            {
                page.Background = backgroundBrush;
                if (page.Content is Panel contentPanel)
                {
                    contentPanel.Background = backgroundBrush;
                }
            }
            else if (element is Panel panel)
            {
                panel.Background = backgroundBrush;
            }
            else if (element is Control control)
            {
                control.Background = backgroundBrush;
            }
        }
        else
        {
            // For standard themes, clear custom background to use theme resource
            if (element is Page page)
            {
                page.ClearValue(Page.BackgroundProperty);
                if (page.Content is Panel contentPanel)
                {
                    contentPanel.ClearValue(Panel.BackgroundProperty);
                }
            }
            else if (element is Panel panel)
            {
                panel.ClearValue(Panel.BackgroundProperty);
            }
            else if (element is Control control)
            {
                control.ClearValue(Control.BackgroundProperty);
            }
        }
    }

    private void OnCultureChanged(object? sender, CultureChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            UpdateWindowTitle();
            UpdateNetworkStatus();
            UpdateSyncStatus();
            BuildMenuBar();
            BuildQuickActionsMenu();
            BuildFunctionTree();
            UpdateNavigationItems();
            UpdateSettingsItem();
            UpdateTabHeaders();
            UpdatePaneToggleButtonTooltip();
        });
    }

    private void UpdateTabHeaders()
    {
        foreach (var item in TabViewMain.TabItems)
        {
            if (item is TabViewItem tab && tab.Tag is string tag)
            {
                // Handle OrderDetailPage with orderId suffix
                if (tag.StartsWith("OrderDetailPage_"))
                {
                    var orderIdStr = tag.Replace("OrderDetailPage_", "");
                    if (int.TryParse(orderIdStr, out var orderId))
                    {
                        tab.Header = $"{_localization.Get("order.title")} #{orderId}";
                    }
                }
                else
                {
                    tab.Header = GetPageTitle(tag);
                }
            }
        }
    }

    private void UpdateWindowTitle()
    {
        AppWindow.Title = _localization.Get("app.title");
    }

    private void UpdateNavigationItems()
    {
        // Update navigation menu items by name
        NavHome.Content = _localization.Get("nav.home");
        NavHeaderBusiness.Content = _localization.Get("nav.header.business");
        NavCustomers.Content = _localization.Get("nav.customers");
        NavProducts.Content = _localization.Get("nav.products");
        NavOrders.Content = _localization.Get("nav.orders");
        NavHeaderReports.Content = _localization.Get("nav.header.reports");
        NavReports.Content = _localization.Get("nav.reports");

        // Update footer items
        NavPlugins.Content = _localization.Get("nav.plugins");
        NavSync.Content = _localization.Get("nav.sync");

        // Update search box and add tab button
        SearchBox.PlaceholderText = _localization.Get("search.placeholder");
        ToolTipService.SetToolTip(AddTabButton, _localization.Get("tooltip.newTab"));

        // Update status bar
        StatusMessage.Text = _localization.Get("status.ready");
    }

    private void UpdateSettingsItem()
    {
        // Update built-in Settings item - must be called after NavView is loaded
        if (NavView.SettingsItem is NavigationViewItem settingsItem)
        {
            settingsItem.Content = _localization.Get("nav.settings");
        }
    }

    private async void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked)
        {
            NavigateToPage("SettingsPage");
            return;
        }

        if (args.InvokedItemContainer is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();
            if (!string.IsNullOrEmpty(tag))
            {
                // Check if it's a command (for dynamic plugin items)
                if (_commandService.HasCommand(tag))
                {
                    try
                    {
                        await _commandService.ExecuteAsync(tag);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Command execution failed: {tag} - {ex.Message}");
                    }
                }
                else
                {
                    NavigateToPage(tag);
                }
            }
        }
    }

    private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        if (TabViewMain.SelectedItem is TabViewItem tab && tab.Content is Frame frame && frame.CanGoBack)
        {
            frame.GoBack();
        }
    }

    private void TabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        if (sender.TabItems.Count > 1)
        {
            sender.TabItems.Remove(args.Tab);
        }
    }

    private void NavigateToPage(string pageTag, object? parameter = null)
    {
        // For pages that can have multiple instances (like OrderDetailPage with different IDs), always create new tab
        var canHaveMultiple = pageTag == "OrderDetailPage";

        if (!canHaveMultiple)
        {
            // Check if tab already exists
            foreach (var item in TabViewMain.TabItems)
            {
                if (item is TabViewItem existingTab && existingTab.Tag?.ToString() == pageTag)
                {
                    TabViewMain.SelectedItem = existingTab;
                    return;
                }
            }
        }

        // Create new tab
        var pageName = GetPageTitle(pageTag);
        AddNewTab(pageTag, pageName, parameter);
    }

    private void AddNewTab(string pageTag, string title, object? parameter = null)
    {
        var pageType = GetPageType(pageTag);
        if (pageType == null)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Unknown page type for: {pageTag}");
            return; // Unknown page, don't create tab
        }

        System.Diagnostics.Debug.WriteLine($"[MainWindow] Creating tab for: {pageTag} ({pageType.Name})");
        var frame = new Frame();

        if (!SafeNavigate(frame, pageType, parameter))
        {
            return;
        }

        // Apply current theme to the new page
        ApplyCurrentThemeToPage(frame);

        // For OrderDetailPage with parameter, update title
        var tabTitle = title;
        var tabTag = pageTag;
        if (pageTag == "OrderDetailPage" && parameter is int orderId)
        {
            tabTitle = $"{_localization.Get("order.title")} #{orderId}";
            tabTag = $"{pageTag}_{orderId}";
        }

        var tab = new TabViewItem
        {
            Header = tabTitle,
            Tag = tabTag,
            Content = frame,
            IconSource = GetPageIcon(pageTag)
        };

        TabViewMain.TabItems.Add(tab);
        TabViewMain.SelectedItem = tab;
    }

    private void ApplyCurrentThemeToPage(Frame frame)
    {
        if (frame.Content is FrameworkElement page)
        {
            var currentTheme = _themeService.CurrentTheme;
            page.RequestedTheme = currentTheme.BaseTheme;

            var isCustomTheme = currentTheme.Id != "System" && currentTheme.Id != "Light" && currentTheme.Id != "Dark";
            var backgroundBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(currentTheme.BackgroundColor);
            ApplyThemeToElement(page, backgroundBrush, isCustomTheme);
        }
    }

    /// <summary>
    /// Safely navigates to a page, handling both built-in and external plugin pages.
    /// External plugin pages are instantiated directly to avoid XAML type info lookup issues.
    /// </summary>
    private bool SafeNavigate(Frame frame, Type pageType, object? parameter = null)
    {
        var isExternalPlugin = pageType.Assembly != typeof(MainWindow).Assembly;

        if (isExternalPlugin)
        {
            try
            {
                // For external plugin pages, we need to ensure all dependencies are loaded
                // in a way that WinUI's XAML parser can resolve them.
                var actualType = EnsurePluginDependenciesLoaded(pageType);

                // Create instance directly - this triggers InitializeComponent()
                var pageInstance = Activator.CreateInstance(actualType);
                if (pageInstance is Page page)
                {
                    frame.Content = page;

                    // Set localization service if the page supports it
                    var setLocMethod = actualType.GetMethod("SetLocalizationService");
                    if (setLocMethod != null)
                    {
                        setLocMethod.Invoke(page, new object[] { _localization });
                    }

                    // Pass navigation parameter if the page supports it
                    if (parameter != null)
                    {
                        var handleMethod = actualType.GetMethod("HandleNavigationParameters");
                        handleMethod?.Invoke(page, new[] { parameter });
                    }
                    return true;
                }
            }
            catch (System.Reflection.TargetInvocationException tex)
            {
                // Log the inner exception for better debugging
                var innerEx = tex.InnerException ?? tex;
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Failed to create plugin page: {innerEx.GetType().Name}: {innerEx.Message}");
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Inner stack trace: {innerEx.StackTrace}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Failed to create plugin page: {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Stack trace: {ex.StackTrace}");
                return false;
            }
        }
        else
        {
            frame.Navigate(pageType, parameter);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Ensures that plugin and its dependencies are loaded in a way that WinUI's XAML parser can find them.
    /// Returns the Type from the default context for proper XAML type resolution.
    /// This is necessary because plugins loaded in custom AssemblyLoadContexts can't have their XAML types
    /// resolved by the WinUI XAML parser which only looks in the default context.
    /// </summary>
    private static Type EnsurePluginDependenciesLoaded(Type pageType)
    {
        var pluginAssembly = pageType.Assembly;
        System.Reflection.Assembly? defaultContextAssembly = null;

        try
        {
            var pluginDir = Path.GetDirectoryName(pluginAssembly.Location);
            if (string.IsNullOrEmpty(pluginDir)) return pageType;

            // First, ensure the plugin assembly itself is loaded in the default context
            // This is critical for XAML type resolution of plugin-defined controls
            var pluginName = pluginAssembly.GetName().Name;
            defaultContextAssembly = System.Runtime.Loader.AssemblyLoadContext.Default.Assemblies
                .FirstOrDefault(a => a.GetName().Name == pluginName);

            if (defaultContextAssembly == null)
            {
                var pluginPath = pluginAssembly.Location;
                if (File.Exists(pluginPath))
                {
                    try
                    {
                        defaultContextAssembly = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(pluginPath);
                        System.Diagnostics.Debug.WriteLine($"[MainWindow] Loaded plugin in default context: {pluginName}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainWindow] Could not load plugin in default context: {ex.Message}");
                    }
                }
            }

            // Get all referenced assemblies and load them
            var referencedAssemblies = pluginAssembly.GetReferencedAssemblies();

            foreach (var assemblyName in referencedAssemblies)
            {
                try
                {
                    // Skip framework assemblies
                    if (assemblyName.Name?.StartsWith("System.") == true ||
                        assemblyName.Name?.StartsWith("Microsoft.") == true ||
                        assemblyName.Name == "netstandard" ||
                        assemblyName.Name?.StartsWith("Arcana.Plugins") == true)
                    {
                        continue;
                    }

                    // Try to load in default context if not already loaded
                    var loaded = System.Runtime.Loader.AssemblyLoadContext.Default.Assemblies
                        .FirstOrDefault(a => a.GetName().Name == assemblyName.Name);

                    if (loaded == null)
                    {
                        var assemblyPath = Path.Combine(pluginDir, $"{assemblyName.Name}.dll");
                        if (File.Exists(assemblyPath))
                        {
                            System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
                            System.Diagnostics.Debug.WriteLine($"[MainWindow] Loaded plugin dependency: {assemblyName.Name}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainWindow] Could not load dependency {assemblyName.Name}: {ex.Message}");
                }
            }

            // Also try to load CommunityToolkit assemblies explicitly if they exist
            var toolkitAssemblies = new[]
            {
                "CommunityToolkit.WinUI.UI.Controls",
                "CommunityToolkit.WinUI.UI.Controls.Core",
                "CommunityToolkit.WinUI.UI.Controls.Layout"
            };

            foreach (var toolkitName in toolkitAssemblies)
            {
                try
                {
                    var loaded = System.Runtime.Loader.AssemblyLoadContext.Default.Assemblies
                        .FirstOrDefault(a => a.GetName().Name == toolkitName);

                    if (loaded == null)
                    {
                        var toolkitPath = Path.Combine(pluginDir, $"{toolkitName}.dll");
                        if (File.Exists(toolkitPath))
                        {
                            System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(toolkitPath);
                            System.Diagnostics.Debug.WriteLine($"[MainWindow] Loaded CommunityToolkit: {toolkitName}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainWindow] Could not load {toolkitName}: {ex.Message}");
                }
            }

            // Return the type from the default context assembly if we loaded it there
            if (defaultContextAssembly != null)
            {
                var typeFromDefault = defaultContextAssembly.GetType(pageType.FullName!);
                if (typeFromDefault != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainWindow] Using type from default context: {typeFromDefault.FullName}");
                    return typeFromDefault;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Error loading plugin dependencies: {ex.Message}");
        }

        // Fallback to original type if we couldn't load in default context
        return pageType;
    }

    /// <summary>
    /// Public method for navigation service to use
    /// </summary>
    public void NavigateToPageWithParameter(string pageTag, object? parameter = null)
    {
        DispatcherQueue.TryEnqueue(() => NavigateToPage(pageTag, parameter));
    }

    /// <summary>
    /// Navigates within an existing parent module tab. If the parent tab doesn't exist, creates it first.
    /// For modules with nested tabs (like OrderModulePage), opens a new document tab within the module.
    /// </summary>
    public void NavigateWithinTab(string parentViewId, string viewId, object? parameter = null)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            // Find existing parent tab
            TabViewItem? parentTab = null;
            foreach (var item in TabViewMain.TabItems)
            {
                if (item is TabViewItem tab && tab.Tag?.ToString() == parentViewId)
                {
                    parentTab = tab;
                    break;
                }
            }

            // If parent tab doesn't exist, create it first
            if (parentTab == null)
            {
                var parentPageType = GetPageType(parentViewId);
                if (parentPageType == null) return;

                var parentFrame = new Frame();
                if (!SafeNavigate(parentFrame, parentPageType))
                {
                    return;
                }
                ApplyCurrentThemeToPage(parentFrame);

                parentTab = new TabViewItem
                {
                    Header = GetPageTitle(parentViewId),
                    Tag = parentViewId,
                    Content = parentFrame,
                    IconSource = GetPageIcon(parentViewId)
                };
                TabViewMain.TabItems.Add(parentTab);
            }

            // Select the parent tab
            TabViewMain.SelectedItem = parentTab;

            // For module pages with nested tabs, delegate to the module page
            if (parentTab.Content is Frame frame && frame.Content is OrderModulePage orderModule)
            {
                if (viewId == "OrderDetailPage")
                {
                    if (parameter is int orderId)
                    {
                        orderModule.OpenOrder(orderId);
                    }
                    else
                    {
                        orderModule.OpenNewOrder(parameter);
                    }
                }
            }
            else if (parentTab.Content is Frame legacyFrame)
            {
                // Legacy navigation for non-module pages
                var childPageType = GetPageType(viewId);
                if (childPageType == null) return;

                if (legacyFrame.Content?.GetType() == childPageType && parameter == null)
                {
                    return; // Already on page, do nothing
                }

                SafeNavigate(legacyFrame, childPageType, parameter);
            }
        });
    }

    /// <summary>
    /// Opens an order in a new document tab within the Order module
    /// </summary>
    public void OpenOrderInModule(int orderId)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            // Find or create the Order module tab
            NavigateWithinTab("OrderListPage", "OrderDetailPage", orderId);
        });
    }

    /// <summary>
    /// Opens a new order in the Order module
    /// </summary>
    public void OpenNewOrderInModule(object? parameter = null)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            NavigateWithinTab("OrderListPage", "OrderDetailPage", parameter);
        });
    }

    /// <summary>
    /// Selects a module tab in the main TabView
    /// </summary>
    public void SelectModuleTab(string moduleTag)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            foreach (var item in TabViewMain.TabItems)
            {
                if (item is TabViewItem tab && tab.Tag?.ToString() == moduleTag)
                {
                    TabViewMain.SelectedItem = tab;
                    return;
                }
            }
        });
    }

    private Type? GetPageType(string pageTag)
    {
        // First check built-in pages
        var builtInType = pageTag switch
        {
            "HomePage" => typeof(HomePage),
            "OrderListPage" => typeof(OrderModulePage), // Use module page with nested tabs
            "OrderModulePage" => typeof(OrderModulePage),
            "OrderDetailPage" => typeof(OrderDetailPage),
            "CustomerListPage" => typeof(CustomerListPage),
            "ProductListPage" => typeof(ProductListPage),
            "SalesReportPage" => typeof(HomePage), // TODO: Create SalesReportPage
            "SyncPage" => typeof(HomePage), // TODO: Create SyncPage
            "PluginManagerPage" => typeof(PluginManagerPage),
            "SettingsPage" => typeof(SettingsPage),
            _ => (Type?)null
        };

        if (builtInType != null)
            return builtInType;

        // Check ViewRegistry for dynamic plugin views
        var view = _viewRegistry.GetView(pageTag);
        return view?.ViewClass;
    }

    private string GetPageTitle(string pageTag)
    {
        // First check built-in pages
        var builtInTitle = pageTag switch
        {
            "HomePage" => _localization.Get("nav.home"),
            "OrderListPage" => _localization.Get("order.list"),
            "OrderDetailPage" => _localization.Get("order.detail"),
            "CustomerListPage" => _localization.Get("customer.list"),
            "ProductListPage" => _localization.Get("product.list"),
            "SalesReportPage" => _localization.Get("nav.reports"),
            "SyncPage" => _localization.Get("nav.sync"),
            "PluginManagerPage" => _localization.Get("nav.plugins"),
            "SettingsPage" => _localization.Get("settings.title"),
            _ => (string?)null
        };

        if (builtInTitle != null)
            return builtInTitle;

        // Check ViewRegistry for dynamic plugin views
        var view = _viewRegistry.GetView(pageTag);
        return view?.Title ?? pageTag;
    }

    private IconSource? GetPageIcon(string pageTag)
    {
        // First check built-in pages
        var builtInGlyph = pageTag switch
        {
            "HomePage" => "\uE80F",           // Home
            "OrderListPage" => "\uE7C3",      // List
            "CustomerListPage" => "\uE716",   // Contact
            "ProductListPage" => "\uE719",    // Shop
            "SalesReportPage" => "\uE9F9",    // Chart
            "SyncPage" => "\uE895",           // Sync
            "PluginManagerPage" => "\uEA86",  // Extension
            "SettingsPage" => "\uE713",       // Settings
            _ => (string?)null
        };

        string? glyph = builtInGlyph;

        // Check ViewRegistry for dynamic plugin views
        if (glyph == null)
        {
            var view = _viewRegistry.GetView(pageTag);
            glyph = !string.IsNullOrEmpty(view?.Icon) ? view.Icon : "\uE7C3"; // Default icon
        }

        return new FontIconSource
        {
            Glyph = glyph,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons,Segoe MDL2 Assets")
        };
    }

    private void OnNetworkStatusChanged(object? sender, NetworkStatusChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() => UpdateNetworkStatus());
    }

    private void OnSyncStateChanged(object? sender, SyncStateChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() => UpdateSyncStatus());
    }

    private void UpdateNetworkStatus()
    {
        var isOnline = _networkMonitor.IsOnline;
        NetworkStatus.Text = isOnline ? _localization.Get("status.online") : _localization.Get("status.offline");
        NetworkIcon.Glyph = isOnline ? "\uE701" : "\uEB5E";
    }

    private void UpdateSyncStatus()
    {
        var state = _syncService.State;
        SyncStatus.Text = state switch
        {
            SyncState.Idle => _localization.Get("status.synced"),
            SyncState.Syncing => _localization.Get("status.syncing"),
            SyncState.Error => _localization.Get("status.syncError"),
            SyncState.Offline => _localization.Get("status.offline"),
            _ => "Unknown"
        };

        SyncBadge.Value = _syncService.PendingCount;
        SyncBadge.Visibility = _syncService.PendingCount > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateClock()
    {
        CurrentTime.Text = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
    }

    #region Dynamic Menu Building

    private void OnMenusChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            BuildMenuBar();
            BuildQuickActionsMenu();
            BuildFunctionTree();
        });
    }

    private readonly List<object> _dynamicNavItems = new();

    private void BuildFunctionTree()
    {
        // Remove previously added dynamic items (including separators)
        foreach (var item in _dynamicNavItems)
        {
            NavView.MenuItems.Remove(item);
        }
        _dynamicNavItems.Clear();

        // Get existing tags from built-in navigation items
        var existingTags = new HashSet<string>();
        foreach (var item in NavView.MenuItems)
        {
            if (item is NavigationViewItem navItem && navItem.Tag is string tag)
            {
                existingTags.Add(tag);
            }
        }
        foreach (var item in NavView.FooterMenuItems)
        {
            if (item is NavigationViewItem navItem && navItem.Tag is string tag)
            {
                existingTags.Add(tag);
            }
        }

        // Get FunctionTree items from menu registry, filtering out duplicates
        var functionTreeItems = _menuRegistry.GetMenuItems(MenuLocation.FunctionTree)
            .Where(m => !existingTags.Contains(m.Command ?? m.Id)) // Skip if tag already exists
            .OrderBy(m => m.Order)
            .ToList();

        if (functionTreeItems.Count == 0)
            return;

        // Find the position to insert (before the reports section)
        var insertIndex = NavView.MenuItems.Count;
        for (var i = 0; i < NavView.MenuItems.Count; i++)
        {
            if (NavView.MenuItems[i] is NavigationViewItem navItem && navItem.Tag?.ToString() == "SalesReportPage")
            {
                // Insert before the separator that precedes reports
                insertIndex = i > 0 ? i - 1 : i;
                break;
            }
        }

        // Add separator before dynamic items if needed
        if (functionTreeItems.Count > 0 && insertIndex > 0)
        {
            var separator = new NavigationViewItemSeparator();
            NavView.MenuItems.Insert(insertIndex, separator);
            _dynamicNavItems.Add(separator); // Track the separator for removal
            insertIndex++;
        }

        // Add dynamic FunctionTree items
        foreach (var menuItem in functionTreeItems)
        {
            var navItem = new NavigationViewItem
            {
                Content = GetLocalizedMenuTitle(menuItem),
                Tag = menuItem.Command ?? menuItem.Id
            };

            if (!string.IsNullOrEmpty(menuItem.Icon))
            {
                navItem.Icon = new FontIcon { Glyph = menuItem.Icon };
            }

            NavView.MenuItems.Insert(insertIndex, navItem);
            _dynamicNavItems.Add(navItem);
            insertIndex++;
        }
    }

    private void BuildQuickActionsMenu()
    {
        QuickActionsFlyout.Items.Clear();

        // Get quick access items from menu registry
        var quickAccessItems = _menuRegistry.GetMenuItems(MenuLocation.QuickAccess)
            .OrderBy(m => m.Order)
            .ToList();

        string? lastGroup = null;
        foreach (var item in quickAccessItems)
        {
            // Add separator between groups
            if (!string.IsNullOrEmpty(item.Group) && item.Group != lastGroup && lastGroup != null)
            {
                QuickActionsFlyout.Items.Add(new MenuFlyoutSeparator());
            }
            lastGroup = item.Group;

            var menuItem = new MenuFlyoutItem
            {
                Text = GetLocalizedMenuTitle(item),
                Tag = item.Command
            };

            if (!string.IsNullOrEmpty(item.Icon))
            {
                menuItem.Icon = new FontIcon { Glyph = item.Icon };
            }

            menuItem.Click += OnQuickActionClick;
            QuickActionsFlyout.Items.Add(menuItem);
        }
    }

    private async void OnQuickActionClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuItem && menuItem.Tag is string commandId && !string.IsNullOrEmpty(commandId))
        {
            try
            {
                await _commandService.ExecuteAsync(commandId);
            }
            catch (InvalidOperationException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Quick action command not found: {commandId} - {ex.Message}");
            }
        }
    }

    private void BuildMenuBar()
    {
        MainMenuBar.Items.Clear();

        // Get all menu items for lookup
        var allMenuItems = _menuRegistry.GetAllMenuItems().ToList();

        // Get top-level menu items (no parent)
        var mainMenuItems = allMenuItems
            .Where(m => m.Location == MenuLocation.MainMenu && string.IsNullOrEmpty(m.ParentId))
            .OrderBy(m => m.Order)
            .ToList();

        foreach (var menuDef in mainMenuItems)
        {
            // Dynamically resolve title using localization
            var title = GetLocalizedMenuTitle(menuDef);
            var menuBarItem = new MenuBarItem { Title = title };

            // Build child items recursively
            BuildMenuChildren(menuBarItem.Items, menuDef.Id, allMenuItems);

            MainMenuBar.Items.Add(menuBarItem);
        }

        // Add built-in View menu items (sidebar/statusbar toggles)
        AddBuiltInViewMenuItems();
    }

    private string GetLocalizedMenuTitle(MenuItemDefinition menuDef)
    {
        // Try to get localized string from all sources (core + plugins)
        var localized = _localization.GetFromAnyPlugin(menuDef.Id);
        // If the key returns itself (not found), use the original title
        return localized != menuDef.Id ? localized : menuDef.Title;
    }

    private void BuildMenuChildren(IList<MenuFlyoutItemBase> parentItems, string parentId, List<MenuItemDefinition> allMenuItems)
    {
        var childItems = allMenuItems
            .Where(m => m.ParentId == parentId)
            .OrderBy(m => m.Order)
            .ToList();

        foreach (var childDef in childItems)
        {
            if (childDef.IsSeparator)
            {
                parentItems.Add(new MenuFlyoutSeparator());
            }
            else
            {
                // Check if this item has children (submenu)
                var hasChildren = allMenuItems.Any(m => m.ParentId == childDef.Id);
                var title = GetLocalizedMenuTitle(childDef);

                if (hasChildren)
                {
                    // Create submenu
                    var subItem = new MenuFlyoutSubItem
                    {
                        Text = title
                    };

                    // Add icon if specified
                    if (!string.IsNullOrEmpty(childDef.Icon))
                    {
                        subItem.Icon = new FontIcon { Glyph = childDef.Icon };
                    }

                    // Recursively build children
                    BuildMenuChildren(subItem.Items, childDef.Id, allMenuItems);

                    parentItems.Add(subItem);
                }
                else
                {
                    // Create menu item
                    var menuItem = new MenuFlyoutItem
                    {
                        Text = title,
                        Tag = childDef.Command
                    };

                    // Add icon if specified
                    if (!string.IsNullOrEmpty(childDef.Icon))
                    {
                        menuItem.Icon = new FontIcon { Glyph = childDef.Icon };
                    }

                    // Handle click - execute command
                    menuItem.Click += OnMenuItemClick;

                    parentItems.Add(menuItem);
                }
            }
        }
    }

    private void AddBuiltInViewMenuItems()
    {
        // Find View menu by looking for menu.view localization key
        MenuBarItem? viewMenu = null;
        var viewMenuTitle = _localization.Get("menu.view");
        foreach (var item in MainMenuBar.Items)
        {
            if (item is MenuBarItem mbi && mbi.Title == viewMenuTitle)
            {
                viewMenu = mbi;
                break;
            }
        }

        if (viewMenu != null)
        {
            viewMenu.Items.Add(new MenuFlyoutSeparator());

            var sidebarToggle = new ToggleMenuFlyoutItem
            {
                Text = _localization.Get("menu.view.sidebar"),
                IsChecked = NavView.IsPaneOpen
            };
            sidebarToggle.Click += (s, e) => NavView.IsPaneOpen = !NavView.IsPaneOpen;
            viewMenu.Items.Add(sidebarToggle);

            var statusBarToggle = new ToggleMenuFlyoutItem
            {
                Text = _localization.Get("menu.view.statusbar"),
                IsChecked = StatusBar.Visibility == Visibility.Visible
            };
            statusBarToggle.Click += (s, e) =>
            {
                StatusBar.Visibility = StatusBar.Visibility == Visibility.Visible
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            };
            viewMenu.Items.Add(statusBarToggle);
        }
    }

    private async void OnMenuItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuItem && menuItem.Tag is string commandId && !string.IsNullOrEmpty(commandId))
        {
            try
            {
                // Execute the command through CommandService
                await _commandService.ExecuteAsync(commandId);
            }
            catch (InvalidOperationException ex)
            {
                // Command not found - log or show error
                System.Diagnostics.Debug.WriteLine($"Command not found: {commandId} - {ex.Message}");
            }
        }
    }

    #endregion
}
