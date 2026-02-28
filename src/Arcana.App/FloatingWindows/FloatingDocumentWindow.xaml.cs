using Arcana.App.Controls;
using Arcana.App.Services;
using Arcana.Plugins.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Arcana.App.FloatingWindows;

/// <summary>
/// A floating window that hosts a single document.
/// Supports docking back to the main window.
/// </summary>
public sealed partial class FloatingDocumentWindow : Window
{
    private readonly LocalizationService _localization;
    private readonly DocumentManager _documentManager;
    private readonly ThemeService _themeService;

    public string WindowId { get; }
    public string ModuleId { get; }
    public DocumentInfo Document { get; }

    public event EventHandler<DockBackEventArgs>? DockBackRequested;

    public FloatingDocumentWindow(string moduleId, DocumentInfo document, FrameworkElement? existingContent = null)
    {
        this.InitializeComponent();

        WindowId = Guid.NewGuid().ToString();
        ModuleId = moduleId;
        Document = document;

        _localization = App.Services.GetRequiredService<LocalizationService>();
        _documentManager = App.Services.GetRequiredService<DocumentManager>();
        _themeService = App.Services.GetRequiredService<ThemeService>();

        // Set window properties
        var appWindow = this.AppWindow;
        appWindow.Resize(new Windows.Graphics.SizeInt32(900, 700));
        appWindow.Title = document.Title;

        // Setup event handlers
        _localization.CultureChanged += OnCultureChanged;
        _themeService.ThemeChanged += OnThemeChanged;

        // Initialize content
        if (existingContent != null)
        {
            ContentFrame.Content = existingContent;
        }
        else
        {
            ContentFrame.Navigate(document.PageType, document.Parameter);
        }

        // Apply current theme
        ApplyTheme(_themeService.CurrentTheme);

        // Update UI
        UpdateUI();

        // Register with document manager
        _documentManager.RegisterFloatingWindow(new FloatingWindowInfo
        {
            Id = WindowId,
            ModuleId = moduleId,
            Document = document,
            WindowReference = this
        });

        // Handle window closing
        Closed += OnWindowClosed;
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        _documentManager.UnregisterFloatingWindow(WindowId);
        _localization.CultureChanged -= OnCultureChanged;
        _themeService.ThemeChanged -= OnThemeChanged;
    }

    private void OnCultureChanged(object? sender, CultureChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(UpdateUI);
    }

    private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() => ApplyTheme(e.NewTheme));
    }

    private void ApplyTheme(ThemeDefinition theme)
    {
        var isCustomTheme = theme.Id != "System" && theme.Id != "Light" && theme.Id != "Dark";
        var backgroundBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(theme.BackgroundColor);

        if (ContentFrame.Content is FrameworkElement page)
        {
            page.RequestedTheme = theme.BaseTheme;

            if (isCustomTheme)
            {
                if (page is Microsoft.UI.Xaml.Controls.Page p)
                {
                    p.Background = backgroundBrush;
                }
            }
            else
            {
                if (page is Microsoft.UI.Xaml.Controls.Page p)
                {
                    p.ClearValue(Microsoft.UI.Xaml.Controls.Page.BackgroundProperty);
                }
            }
        }
    }

    private void UpdateUI()
    {
        DocumentTitle.Text = Document.Title;
        if (!string.IsNullOrEmpty(Document.IconGlyph))
        {
            DocumentIcon.Glyph = Document.IconGlyph;
        }
        DockBackText.Text = _localization.Get("common.dockBack");
        ToolTipService.SetToolTip(DockBackButton, _localization.Get("common.dockBackTooltip"));
    }

    private void DockBackButton_Click(object sender, RoutedEventArgs e)
    {
        // Raise dock back event
        DockBackRequested?.Invoke(this, new DockBackEventArgs(ModuleId, Document, ContentFrame.Content as FrameworkElement));

        // Close the floating window
        Close();
    }

    /// <summary>
    /// Gets the content frame for the floating window
    /// </summary>
    public FrameworkElement? GetContent()
    {
        return ContentFrame.Content as FrameworkElement;
    }
}

/// <summary>
/// Event args for dock back request
/// </summary>
public class DockBackEventArgs : EventArgs
{
    public string ModuleId { get; }
    public DocumentInfo Document { get; }
    public FrameworkElement? Content { get; }

    public DockBackEventArgs(string moduleId, DocumentInfo document, FrameworkElement? content)
    {
        ModuleId = moduleId;
        Document = document;
        Content = content;
    }
}
