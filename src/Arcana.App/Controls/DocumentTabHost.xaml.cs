using Arcana.App.Services;
using Arcana.Plugins.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Arcana.App.Controls;

/// <summary>
/// A document tab host control that provides nested tabs within a module.
/// Supports multiple document instances, pop-out to floating windows, and dock-back.
/// </summary>
public sealed partial class DocumentTabHost : UserControl
{
    private readonly ILocalizationService _localization;
    private readonly IDocumentManager _documentManager;

    public event EventHandler<DocumentEventArgs>? DocumentOpened;
    public event EventHandler<DocumentEventArgs>? DocumentClosed;
    public event EventHandler<DocumentEventArgs>? DocumentSelected;
    public event EventHandler<DocumentPopOutEventArgs>? DocumentPopOutRequested;
    public event EventHandler? CreateNewRequested;

    /// <summary>
    /// The module ID this host belongs to (e.g., "OrderModule", "CustomerModule")
    /// </summary>
    public string ModuleId { get; set; } = string.Empty;

    /// <summary>
    /// The default page type for new documents
    /// </summary>
    public Type? DefaultDocumentType { get; set; }

    /// <summary>
    /// The list page type (shown when no documents are open)
    /// </summary>
    public Type? ListPageType { get; set; }

    /// <summary>
    /// Whether to show the list page as a permanent first tab
    /// </summary>
    public bool ShowListAsTab { get; set; } = true;

    /// <summary>
    /// The empty state message key for localization
    /// </summary>
    public string EmptyStateMessageKey { get; set; } = "common.noDocuments";

    /// <summary>
    /// The create new button text key for localization
    /// </summary>
    public string CreateNewTextKey { get; set; } = "common.createNew";

    public DocumentTabHost()
    {
        this.InitializeComponent();
        _localization = App.Services.GetRequiredService<ILocalizationService>();
        _documentManager = App.Services.GetRequiredService<IDocumentManager>();
        _localization.CultureChanged += OnCultureChanged;

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyLocalization();
        UpdateUI();
    }

    private void OnCultureChanged(object? sender, CultureChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(ApplyLocalization);
    }

    private void ApplyLocalization()
    {
        EmptyStateText.Text = _localization.Get(EmptyStateMessageKey);
        CreateNewText.Text = _localization.Get(CreateNewTextKey);
        UpdateDocumentCount();
    }

    private void UpdateUI()
    {
        var hasDocuments = DocumentTabs.TabItems.Count > 0;
        DocumentTabs.Visibility = hasDocuments ? Visibility.Visible : Visibility.Collapsed;
        EmptyState.Visibility = hasDocuments ? Visibility.Collapsed : Visibility.Visible;
        PopOutButton.Visibility = hasDocuments && DocumentTabs.TabItems.Count > 0
            ? Visibility.Visible : Visibility.Collapsed;
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

    /// <summary>
    /// Opens a new document in the tab host
    /// </summary>
    public void OpenDocument(DocumentInfo document)
    {
        // Check if document already exists
        foreach (var item in DocumentTabs.TabItems)
        {
            if (item is TabViewItem existingTab &&
                existingTab.Tag is DocumentInfo existingDoc &&
                existingDoc.Id == document.Id)
            {
                // Document already open, select it
                DocumentTabs.SelectedItem = existingTab;
                return;
            }
        }

        // Create new tab for document
        var frame = new Frame();
        frame.Navigate(document.PageType, document.Parameter);

        var tab = new TabViewItem
        {
            Header = document.Title,
            Tag = document,
            Content = frame,
            IconSource = new FontIconSource { Glyph = document.IconGlyph ?? "\uE7C3" },
            IsClosable = document.IsClosable
        };

        // Add context menu for pop-out
        var flyout = new MenuFlyout();
        var popOutItem = new MenuFlyoutItem
        {
            Text = _localization.Get("common.popOut"),
            Icon = new FontIcon { Glyph = "\uE8A7" }
        };
        popOutItem.Click += (s, e) => RequestPopOut(tab);
        flyout.Items.Add(popOutItem);

        if (document.IsClosable)
        {
            flyout.Items.Add(new MenuFlyoutSeparator());
            var closeItem = new MenuFlyoutItem
            {
                Text = _localization.Get("common.close"),
                Icon = new FontIcon { Glyph = "\uE711" }
            };
            closeItem.Click += (s, e) => CloseDocument(document.Id);
            flyout.Items.Add(closeItem);
        }

        tab.ContextFlyout = flyout;

        DocumentTabs.TabItems.Add(tab);
        DocumentTabs.SelectedItem = tab;

        // Register with document manager
        _documentManager.RegisterDocument(ModuleId, document);

        DocumentOpened?.Invoke(this, new DocumentEventArgs(document));
        UpdateUI();
    }

    /// <summary>
    /// Opens or creates a document
    /// </summary>
    public void OpenOrCreateDocument(string documentId, string title, Type pageType, object? parameter = null, string? iconGlyph = null)
    {
        var doc = new DocumentInfo
        {
            Id = documentId,
            Title = title,
            PageType = pageType,
            Parameter = parameter,
            IconGlyph = iconGlyph ?? "\uE7C3",
            IsClosable = true
        };
        OpenDocument(doc);
    }

    /// <summary>
    /// Closes a document by ID
    /// </summary>
    public bool CloseDocument(string documentId)
    {
        TabViewItem? tabToRemove = null;
        DocumentInfo? docInfo = null;

        foreach (var item in DocumentTabs.TabItems)
        {
            if (item is TabViewItem tab &&
                tab.Tag is DocumentInfo doc &&
                doc.Id == documentId)
            {
                tabToRemove = tab;
                docInfo = doc;
                break;
            }
        }

        if (tabToRemove != null && docInfo != null)
        {
            DocumentTabs.TabItems.Remove(tabToRemove);
            _documentManager.UnregisterDocument(ModuleId, documentId);
            DocumentClosed?.Invoke(this, new DocumentEventArgs(docInfo));
            UpdateUI();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets all open documents
    /// </summary>
    public IEnumerable<DocumentInfo> GetOpenDocuments()
    {
        foreach (var item in DocumentTabs.TabItems)
        {
            if (item is TabViewItem tab && tab.Tag is DocumentInfo doc)
            {
                yield return doc;
            }
        }
    }

    /// <summary>
    /// Navigates within the current document's frame
    /// </summary>
    public bool NavigateInCurrentDocument(Type pageType, object? parameter = null)
    {
        if (DocumentTabs.SelectedItem is TabViewItem tab && tab.Content is Frame frame)
        {
            return frame.Navigate(pageType, parameter);
        }
        return false;
    }

    /// <summary>
    /// Gets the currently selected document
    /// </summary>
    public DocumentInfo? GetSelectedDocument()
    {
        if (DocumentTabs.SelectedItem is TabViewItem tab && tab.Tag is DocumentInfo doc)
        {
            return doc;
        }
        return null;
    }

    private void RequestPopOut(TabViewItem tab)
    {
        if (tab.Tag is DocumentInfo doc)
        {
            DocumentPopOutRequested?.Invoke(this, new DocumentPopOutEventArgs(doc, tab));
        }
    }

    private void DocumentTabs_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        if (args.Tab.Tag is DocumentInfo doc)
        {
            CloseDocument(doc.Id);
        }
    }

    private void DocumentTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DocumentTabs.SelectedItem is TabViewItem tab && tab.Tag is DocumentInfo doc)
        {
            DocumentSelected?.Invoke(this, new DocumentEventArgs(doc));
        }
    }

    private void PopOutButton_Click(object sender, RoutedEventArgs e)
    {
        if (DocumentTabs.SelectedItem is TabViewItem tab)
        {
            RequestPopOut(tab);
        }
    }

    private void CreateNewButton_Click(object sender, RoutedEventArgs e)
    {
        CreateNewRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Docks a document back from a floating window
    /// </summary>
    public void DockDocument(DocumentInfo document, FrameworkElement content)
    {
        var tab = new TabViewItem
        {
            Header = document.Title,
            Tag = document,
            Content = content,
            IconSource = new FontIconSource { Glyph = document.IconGlyph ?? "\uE7C3" },
            IsClosable = document.IsClosable
        };

        DocumentTabs.TabItems.Add(tab);
        DocumentTabs.SelectedItem = tab;

        _documentManager.RegisterDocument(ModuleId, document);
        UpdateUI();
    }
}

/// <summary>
/// Information about a document in the tab host
/// </summary>
public class DocumentInfo
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public Type PageType { get; set; } = typeof(Page);
    public object? Parameter { get; set; }
    public string? IconGlyph { get; set; }
    public bool IsClosable { get; set; } = true;
    public bool IsDirty { get; set; }
}

/// <summary>
/// Event args for document events
/// </summary>
public class DocumentEventArgs : EventArgs
{
    public DocumentInfo Document { get; }

    public DocumentEventArgs(DocumentInfo document)
    {
        Document = document;
    }
}

/// <summary>
/// Event args for pop-out request
/// </summary>
public class DocumentPopOutEventArgs : DocumentEventArgs
{
    public TabViewItem Tab { get; }

    public DocumentPopOutEventArgs(DocumentInfo document, TabViewItem tab) : base(document)
    {
        Tab = tab;
    }
}
