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

        // Setup event handlers
        _networkMonitor.StatusChanged += OnNetworkStatusChanged;
        _syncService.StateChanged += OnSyncStateChanged;

        // Setup timer for clock
        _timer = new DispatcherTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (s, e) => UpdateClock();
        _timer.Start();

        // Initialize
        UpdateNetworkStatus();
        UpdateSyncStatus();
        UpdateClock();

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

    private void NavigateToPage(string pageTag)
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

        // Create new tab
        var pageName = GetPageTitle(pageTag);
        AddNewTab(pageTag, pageName);
    }

    private void AddNewTab(string pageTag, string title)
    {
        var frame = new Frame();
        var pageType = GetPageType(pageTag);

        if (pageType != null)
        {
            frame.Navigate(pageType);
        }

        var tab = new TabViewItem
        {
            Header = title,
            Tag = pageTag,
            Content = frame,
            IconSource = GetPageIcon(pageTag)
        };

        TabViewMain.TabItems.Add(tab);
        TabViewMain.SelectedItem = tab;
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
            "PluginManagerPage" => typeof(PluginManagerPage),
            "SettingsPage" => typeof(SettingsPage),
            _ => typeof(HomePage)
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
            "PluginManagerPage" => "Plugin Manager",
            "SettingsPage" => "設定",
            _ => "頁面"
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
}
