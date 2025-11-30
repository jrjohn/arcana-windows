using Arcana.App.Controls;
using Arcana.App.FloatingWindows;
using Arcana.App.Services;
using Arcana.Domain.Entities;
using Arcana.Plugins.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XamlNavigationEventArgs = Microsoft.UI.Xaml.Navigation.NavigationEventArgs;

namespace Arcana.App.Views;

/// <summary>
/// Order module page that contains nested document tabs.
/// The first tab is always the Order List, and additional tabs are opened for each order.
/// </summary>
public sealed partial class OrderModulePage : Page
{
    private const string ModuleId = "OrderModule";
    private const string ListTabId = "OrderList";

    private readonly ILocalizationService _localization;
    private readonly IDocumentManager _documentManager;
    private readonly IMenuRegistry _menuRegistry;
    private readonly ICommandService _commandService;
    private readonly ThemeService _themeService;
    private readonly List<FloatingDocumentWindow> _floatingWindows = [];

    public OrderModulePage()
    {
        this.InitializeComponent();
        _localization = App.Services.GetRequiredService<ILocalizationService>();
        _documentManager = App.Services.GetRequiredService<IDocumentManager>();
        _menuRegistry = App.Services.GetRequiredService<IMenuRegistry>();
        _commandService = App.Services.GetRequiredService<ICommandService>();
        _themeService = App.Services.GetRequiredService<ThemeService>();

        _localization.CultureChanged += OnCultureChanged;
        _menuRegistry.MenusChanged += OnMenusChanged;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyLocalization();
        BuildModuleQuickActionsMenu();

        // Ensure the Order List tab exists as the first (permanent) tab
        EnsureListTabExists();
        UpdateUI();
    }

    private void OnCultureChanged(object? sender, CultureChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            ApplyLocalization();
            BuildModuleQuickActionsMenu();
            UpdateTabHeaders();
        });
    }

    private void OnMenusChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(BuildModuleQuickActionsMenu);
    }

    private void BuildModuleQuickActionsMenu()
    {
        ModuleQuickActionsFlyout.Items.Clear();

        // Get module-level quick access items for this module
        var quickAccessItems = _menuRegistry.GetMenuItems(MenuLocation.ModuleQuickAccess, ModuleId)
            .OrderBy(m => m.Order)
            .ToList();

        string? lastGroup = null;
        foreach (var item in quickAccessItems)
        {
            // Add separator between groups
            if (!string.IsNullOrEmpty(item.Group) && item.Group != lastGroup && lastGroup != null)
            {
                ModuleQuickActionsFlyout.Items.Add(new MenuFlyoutSeparator());
            }
            lastGroup = item.Group;

            var menuItem = new MenuFlyoutItem
            {
                Text = GetLocalizedTitle(item),
                Tag = item.Command
            };

            if (!string.IsNullOrEmpty(item.Icon))
            {
                menuItem.Icon = new FontIcon { Glyph = item.Icon };
            }

            menuItem.Click += OnModuleQuickActionClick;
            ModuleQuickActionsFlyout.Items.Add(menuItem);
        }
    }

    private string GetLocalizedTitle(MenuItemDefinition item)
    {
        // Try to get localized string
        var localized = _localization.GetFromAnyPlugin(item.Id);
        return localized != item.Id ? localized : item.Title;
    }

    private async void OnModuleQuickActionClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuItem && menuItem.Tag is string commandId && !string.IsNullOrEmpty(commandId))
        {
            // Handle module-specific commands locally
            switch (commandId)
            {
                case "module.order.list":
                    SelectListTab();
                    break;
                case "module.order.new":
                case "order.new":
                    OpenNewOrder();
                    break;
                default:
                    // For other commands, use the command service
                    try
                    {
                        await _commandService.ExecuteAsync(commandId);
                    }
                    catch (InvalidOperationException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Module quick action command not found: {commandId} - {ex.Message}");
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Selects the Order List tab
    /// </summary>
    public void SelectListTab()
    {
        foreach (var item in DocumentTabs.TabItems)
        {
            if (item is TabViewItem tab && tab.Tag is DocumentInfo doc && doc.Id == ListTabId)
            {
                DocumentTabs.SelectedItem = tab;
                return;
            }
        }
    }

    private void ApplyLocalization()
    {
        ToolTipService.SetToolTip(ModuleQuickActionsButton, _localization.Get("tooltip.newTab"));
        ToolTipService.SetToolTip(PopOutButton, _localization.Get("common.popOut"));
        UpdateDocumentCount();
    }

    private void UpdateTabHeaders()
    {
        foreach (var item in DocumentTabs.TabItems)
        {
            if (item is TabViewItem tab && tab.Tag is DocumentInfo doc)
            {
                if (doc.Id == ListTabId)
                {
                    tab.Header = _localization.Get("order.list");
                }
                else if (doc.Id.StartsWith("Order_"))
                {
                    // Update order tab header
                    var orderId = doc.Id.Replace("Order_", "");
                    tab.Header = $"{_localization.Get("order.title")} #{orderId}";
                }
                else if (doc.Id == "NewOrder")
                {
                    tab.Header = _localization.Get("order.new");
                }
            }
        }
    }

    private void AddListTab()
    {
        var frame = new Frame();
        frame.Navigate(typeof(OrderListPage));
        ApplyThemeToFrame(frame);

        // Wire up navigation from list page
        if (frame.Content is OrderListPage listPage)
        {
            // Subscribe to list page events for opening orders
        }

        var listDoc = new DocumentInfo
        {
            Id = ListTabId,
            Title = _localization.Get("order.list"),
            PageType = typeof(OrderListPage),
            IconGlyph = "\uE8A5",
            IsClosable = false
        };

        var tab = new TabViewItem
        {
            Header = listDoc.Title,
            Tag = listDoc,
            Content = frame,
            IconSource = new FontIconSource { Glyph = listDoc.IconGlyph },
            IsClosable = false
        };

        DocumentTabs.TabItems.Add(tab);
        DocumentTabs.SelectedItem = tab;

        _documentManager.RegisterDocument(ModuleId, listDoc);
    }

    /// <summary>
    /// Ensures the Order List tab exists
    /// </summary>
    private void EnsureListTabExists()
    {
        // Check if list tab already exists
        foreach (var item in DocumentTabs.TabItems)
        {
            if (item is TabViewItem tab && tab.Tag is DocumentInfo doc && doc.Id == ListTabId)
            {
                return; // Already exists
            }
        }

        // Add the list tab
        AddListTab();
    }

    /// <summary>
    /// Opens an order in a new document tab
    /// </summary>
    public void OpenOrder(int orderId)
    {
        // Ensure the list tab exists first
        EnsureListTabExists();

        var docId = $"Order_{orderId}";

        // Check if already open
        foreach (var item in DocumentTabs.TabItems)
        {
            if (item is TabViewItem existingTab && existingTab.Tag is DocumentInfo doc && doc.Id == docId)
            {
                DocumentTabs.SelectedItem = existingTab;
                return;
            }
        }

        // Create new tab
        var frame = new Frame();
        frame.Navigate(typeof(OrderDetailPage), orderId);
        ApplyThemeToFrame(frame);

        var orderDoc = new DocumentInfo
        {
            Id = docId,
            Title = $"{_localization.Get("order.title")} #{orderId}",
            PageType = typeof(OrderDetailPage),
            Parameter = orderId,
            IconGlyph = "\uE7C3",
            IsClosable = true
        };

        var tab = new TabViewItem
        {
            Header = orderDoc.Title,
            Tag = orderDoc,
            Content = frame,
            IconSource = new FontIconSource { Glyph = orderDoc.IconGlyph },
            IsClosable = true
        };

        // Add context menu
        AddTabContextMenu(tab, orderDoc);

        DocumentTabs.TabItems.Add(tab);
        DocumentTabs.SelectedItem = tab;

        _documentManager.RegisterDocument(ModuleId, orderDoc);
        UpdateUI();
    }

    /// <summary>
    /// Opens a new order document tab
    /// </summary>
    public void OpenNewOrder(object? parameter = null)
    {
        // Ensure the list tab exists first
        EnsureListTabExists();

        // Generate unique ID for new order tab
        var docId = parameter is OrderCopyParameter ? $"NewOrder_{Guid.NewGuid():N}" : "NewOrder";

        // For new orders (not copy), check if already open
        if (parameter == null)
        {
            foreach (var item in DocumentTabs.TabItems)
            {
                if (item is TabViewItem existingTab && existingTab.Tag is DocumentInfo doc && doc.Id == docId)
                {
                    DocumentTabs.SelectedItem = existingTab;
                    return;
                }
            }
        }

        var frame = new Frame();
        frame.Navigate(typeof(OrderDetailPage), parameter);
        ApplyThemeToFrame(frame);

        var title = parameter is OrderCopyParameter copyParam
            ? $"{_localization.Get("order.copy")} - {_localization.Get("order.copiedFrom")} #{copyParam.SourceOrderId}"
            : _localization.Get("order.new");

        var orderDoc = new DocumentInfo
        {
            Id = docId,
            Title = title,
            PageType = typeof(OrderDetailPage),
            Parameter = parameter,
            IconGlyph = "\uE710",
            IsClosable = true
        };

        var tab = new TabViewItem
        {
            Header = orderDoc.Title,
            Tag = orderDoc,
            Content = frame,
            IconSource = new FontIconSource { Glyph = orderDoc.IconGlyph },
            IsClosable = true
        };

        // Add context menu
        AddTabContextMenu(tab, orderDoc);

        DocumentTabs.TabItems.Add(tab);
        DocumentTabs.SelectedItem = tab;

        _documentManager.RegisterDocument(ModuleId, orderDoc);
        UpdateUI();
    }

    private void AddTabContextMenu(TabViewItem tab, DocumentInfo doc)
    {
        var flyout = new MenuFlyout();

        var popOutItem = new MenuFlyoutItem
        {
            Text = _localization.Get("common.popOut"),
            Icon = new FontIcon { Glyph = "\uE8A7" }
        };
        popOutItem.Click += (s, e) => PopOutTab(tab, doc);
        flyout.Items.Add(popOutItem);

        if (doc.IsClosable)
        {
            flyout.Items.Add(new MenuFlyoutSeparator());
            var closeItem = new MenuFlyoutItem
            {
                Text = _localization.Get("common.close"),
                Icon = new FontIcon { Glyph = "\uE711" }
            };
            closeItem.Click += (s, e) => CloseTab(tab, doc);
            flyout.Items.Add(closeItem);
        }

        tab.ContextFlyout = flyout;
    }

    private void PopOutTab(TabViewItem tab, DocumentInfo doc)
    {
        // Get the content from the tab
        var content = tab.Content;
        tab.Content = null;

        // Remove from tabs
        DocumentTabs.TabItems.Remove(tab);
        _documentManager.UnregisterDocument(ModuleId, doc.Id);

        // Create floating window
        var floatingWindow = new FloatingDocumentWindow(ModuleId, doc, content as FrameworkElement);
        floatingWindow.DockBackRequested += OnDockBackRequested;
        floatingWindow.Closed += (s, e) =>
        {
            _floatingWindows.Remove(floatingWindow);
            floatingWindow.DockBackRequested -= OnDockBackRequested;
        };

        _floatingWindows.Add(floatingWindow);
        floatingWindow.Activate();

        UpdateUI();
    }

    private void OnDockBackRequested(object? sender, DockBackEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            // First, ensure the main window switches to this module's tab
            if (App.MainWindow is MainWindow mainWindow)
            {
                mainWindow.SelectModuleTab("OrderListPage");
            }

            // Then dock the document
            DockDocument(e.Document, e.Content);
        });
    }

    private void DockDocument(DocumentInfo doc, FrameworkElement? content)
    {
        // Always create new content - cannot reuse content from another window
        // as it's still attached to the floating window's visual tree
        var newContent = CreateContentForDocument(doc);

        // Create new tab with the content
        var tab = new TabViewItem
        {
            Header = doc.Title,
            Tag = doc,
            Content = newContent,
            IconSource = new FontIconSource { Glyph = doc.IconGlyph ?? "\uE7C3" },
            IsClosable = doc.IsClosable
        };

        // Add context menu
        AddTabContextMenu(tab, doc);

        DocumentTabs.TabItems.Add(tab);
        DocumentTabs.SelectedItem = tab;

        _documentManager.RegisterDocument(ModuleId, doc);
        UpdateUI();
    }

    private FrameworkElement CreateContentForDocument(DocumentInfo doc)
    {
        var frame = new Frame();
        frame.Navigate(doc.PageType, doc.Parameter);
        ApplyThemeToFrame(frame);
        return frame;
    }

    private void CloseTab(TabViewItem tab, DocumentInfo doc)
    {
        DocumentTabs.TabItems.Remove(tab);
        _documentManager.UnregisterDocument(ModuleId, doc.Id);
        UpdateUI();
    }

    private void ApplyThemeToFrame(Frame frame)
    {
        if (frame.Content is FrameworkElement page)
        {
            var currentTheme = _themeService.CurrentTheme;
            page.RequestedTheme = currentTheme.BaseTheme;

            var isCustomTheme = currentTheme.Id != "System" && currentTheme.Id != "Light" && currentTheme.Id != "Dark";
            if (isCustomTheme && page is Page p)
            {
                p.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(currentTheme.BackgroundColor);
            }
        }
    }

    private void UpdateUI()
    {
        // Update pop-out button visibility (only show if selected tab is closable)
        if (DocumentTabs.SelectedItem is TabViewItem tab && tab.Tag is DocumentInfo doc)
        {
            PopOutButton.Visibility = doc.IsClosable ? Visibility.Visible : Visibility.Collapsed;
        }
        else
        {
            PopOutButton.Visibility = Visibility.Collapsed;
        }

        UpdateDocumentCount();
    }

    private void UpdateDocumentCount()
    {
        var count = DocumentTabs.TabItems.Count;
        if (count > 1)
        {
            DocumentCountText.Text = string.Format(_localization.Get("common.documentCount"), count);
            DocumentCountText.Visibility = Visibility.Visible;
        }
        else
        {
            DocumentCountText.Visibility = Visibility.Collapsed;
        }
    }

    private void DocumentTabs_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        if (args.Tab.Tag is DocumentInfo doc && doc.IsClosable)
        {
            CloseTab(args.Tab, doc);
        }
    }

    private void DocumentTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateUI();
    }

    private void PopOutButton_Click(object sender, RoutedEventArgs e)
    {
        if (DocumentTabs.SelectedItem is TabViewItem tab && tab.Tag is DocumentInfo doc && doc.IsClosable)
        {
            PopOutTab(tab, doc);
        }
    }

    protected override void OnNavigatedTo(XamlNavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // Handle navigation parameters
        if (e.Parameter is int orderId)
        {
            // Open the specified order
            OpenOrder(orderId);
        }
        else if (e.Parameter is string action && action == "new")
        {
            // Open new order tab
            OpenNewOrder();
        }
        else if (e.Parameter is OrderCopyParameter copyParam)
        {
            // Open new order with copy data
            OpenNewOrder(copyParam);
        }
    }
}
