using Arcana.App.Navigation;
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
/// 主應用程式視窗
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly INavigationService _navigationService;
    private readonly INetworkMonitor _networkMonitor;
    private readonly ISyncService _syncService;
    private readonly IMenuRegistry _menuRegistry;
    private readonly ICommandService _commandService;
    private readonly DispatcherTimer _timer;

    public MainWindow()
    {
        this.InitializeComponent();

        // Set window size
        var appWindow = this.AppWindow;
        appWindow.Resize(new Windows.Graphics.SizeInt32(1400, 900));
        appWindow.Title = "Arcana - 企業管理系統";

        // Get services
        _navigationService = App.Services.GetRequiredService<INavigationService>();
        _networkMonitor = App.Services.GetRequiredService<INetworkMonitor>();
        _syncService = App.Services.GetRequiredService<ISyncService>();
        _menuRegistry = App.Services.GetRequiredService<IMenuRegistry>();
        _commandService = App.Services.GetRequiredService<ICommandService>();

        // Setup event handlers
        _networkMonitor.StatusChanged += OnNetworkStatusChanged;
        _syncService.StateChanged += OnSyncStateChanged;
        _menuRegistry.MenusChanged += OnMenusChanged;

        // Setup timer for clock
        _timer = new DispatcherTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (s, e) => UpdateClock();
        _timer.Start();

        // Initialize
        UpdateNetworkStatus();
        UpdateSyncStatus();
        UpdateClock();
        BuildMenuBar();

        // Navigate to home page
        NavigateToPage("HomePage");
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            NavigateToPage("SettingsPage");
            return;
        }

        if (args.SelectedItem is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();
            if (!string.IsNullOrEmpty(tag))
            {
                NavigateToPage(tag);
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

    private void AddTab_Click(object sender, RoutedEventArgs e)
    {
        AddNewTab("HomePage", "首頁");
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
            return; // Unknown page, don't create tab
        }

        var frame = new Frame();
        frame.Navigate(pageType, parameter);

        // For OrderDetailPage with parameter, update title
        var tabTitle = title;
        var tabTag = pageTag;
        if (pageTag == "OrderDetailPage" && parameter is int orderId)
        {
            tabTitle = $"訂單 #{orderId}";
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

    /// <summary>
    /// Public method for navigation service to use
    /// </summary>
    public void NavigateToPageWithParameter(string pageTag, object? parameter = null)
    {
        DispatcherQueue.TryEnqueue(() => NavigateToPage(pageTag, parameter));
    }

    private static Type? GetPageType(string pageTag)
    {
        return pageTag switch
        {
            "HomePage" => typeof(HomePage),
            "OrderListPage" => typeof(OrderListPage),
            "OrderDetailPage" => typeof(OrderDetailPage),
            "CustomerListPage" => typeof(CustomerListPage),
            "ProductListPage" => typeof(ProductListPage),
            "SalesReportPage" => typeof(HomePage), // TODO: Create SalesReportPage
            "SyncPage" => typeof(HomePage), // TODO: Create SyncPage
            "PluginManagerPage" => typeof(PluginManagerPage),
            "SettingsPage" => typeof(SettingsPage),
            _ => null
        };
    }

    private static string GetPageTitle(string pageTag)
    {
        return pageTag switch
        {
            "HomePage" => "首頁",
            "OrderListPage" => "訂單管理",
            "OrderDetailPage" => "訂單明細",
            "CustomerListPage" => "客戶管理",
            "ProductListPage" => "產品管理",
            "SalesReportPage" => "銷售報表",
            "SyncPage" => "同步狀態",
            "PluginManagerPage" => "Plugin Manager",
            "SettingsPage" => "設定",
            _ => pageTag
        };
    }

    private static IconSource? GetPageIcon(string pageTag)
    {
        var glyph = pageTag switch
        {
            "HomePage" => "\uE80F",
            "OrderListPage" => "\uE7C3",
            "CustomerListPage" => "\uE716",
            "ProductListPage" => "\uE719",
            "SalesReportPage" => "\uE9F9",
            "SyncPage" => "\uE895",
            "PluginManagerPage" => "\uEA86",
            "SettingsPage" => "\uE713",
            _ => "\uE7C3"
        };

        return new FontIconSource { Glyph = glyph };
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
        NetworkStatus.Text = isOnline ? "線上" : "離線";
        NetworkIcon.Glyph = isOnline ? "\uE701" : "\uEB5E";
    }

    private void UpdateSyncStatus()
    {
        var state = _syncService.State;
        SyncStatus.Text = state switch
        {
            SyncState.Idle => "已同步",
            SyncState.Syncing => "同步中...",
            SyncState.Error => "同步錯誤",
            SyncState.Offline => "離線",
            _ => "未知"
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
        DispatcherQueue.TryEnqueue(BuildMenuBar);
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
            var menuBarItem = new MenuBarItem { Title = menuDef.Title };

            // Build child items recursively
            BuildMenuChildren(menuBarItem.Items, menuDef.Id, allMenuItems);

            MainMenuBar.Items.Add(menuBarItem);
        }

        // Add built-in View menu items (sidebar/statusbar toggles)
        AddBuiltInViewMenuItems();
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

                if (hasChildren)
                {
                    // Create submenu
                    var subItem = new MenuFlyoutSubItem
                    {
                        Text = childDef.Title
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
                        Text = childDef.Title,
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
        // Find or create View menu
        MenuBarItem? viewMenu = null;
        foreach (var item in MainMenuBar.Items)
        {
            if (item is MenuBarItem mbi && mbi.Title == "檢視")
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
                Text = "側邊欄",
                IsChecked = NavView.IsPaneOpen
            };
            sidebarToggle.Click += (s, e) => NavView.IsPaneOpen = !NavView.IsPaneOpen;
            viewMenu.Items.Add(sidebarToggle);

            var statusBarToggle = new ToggleMenuFlyoutItem
            {
                Text = "狀態列",
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
                // Execute the command through ICommandService
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
