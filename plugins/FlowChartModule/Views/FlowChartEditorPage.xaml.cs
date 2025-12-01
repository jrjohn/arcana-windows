using Arcana.Plugin.FlowChart.Controls;
using Arcana.Plugin.FlowChart.Models;
using Arcana.Plugin.FlowChart.Services;
using Arcana.Plugin.FlowChart.ViewModels;
using Arcana.Plugins.Contracts;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Storage.Pickers;
using Windows.UI;

namespace Arcana.Plugin.FlowChart.Views;

/// <summary>
/// FlowChart editor page - built programmatically for plugin compatibility.
/// WinUI 3 cannot load XAML resources from dynamically loaded assemblies.
/// </summary>
public sealed class FlowChartEditorPage : Page
{
    private ILocalizationService? _localization;
    private readonly FlowChartEditorViewModel _viewModel;
    private bool _isUpdatingProperties;

    // UI Elements - Toolbar
    private AppBarButton NewButton = null!;
    private AppBarButton OpenButton = null!;
    private AppBarButton SaveButton = null!;
    private AppBarButton SaveAsButton = null!;
    private AppBarButton UndoButton = null!;
    private AppBarButton RedoButton = null!;
    private AppBarButton DeleteButton = null!;
    private AppBarButton DuplicateButton = null!;
    private AppBarButton ZoomInButton = null!;
    private AppBarButton ZoomOutButton = null!;
    private AppBarButton ZoomResetButton = null!;
    private AppBarToggleButton ConnectModeButton = null!;
    private AppBarButton SampleButton = null!;

    // UI Elements - Panels
    private TextBlock ShapesTitle = null!;
    private ItemsControl ShapesList = null!;
    private TextBlock PropertiesTitle = null!;

    // UI Elements - Node Properties
    private StackPanel NodePropertiesPanel = null!;
    private TextBlock NodeTextLabel = null!;
    private TextBox NodeTextBox = null!;
    private TextBlock NodeFillLabel = null!;
    private ColorPicker NodeFillColorPicker = null!;
    private TextBlock NodeStrokeLabel = null!;
    private ColorPicker NodeStrokeColorPicker = null!;
    private TextBlock NodeSizeLabel = null!;
    private NumberBox NodeWidthBox = null!;
    private NumberBox NodeHeightBox = null!;
    private TextBlock NodeFontLabel = null!;
    private NumberBox NodeFontSizeBox = null!;
    private Button BringToFrontButton = null!;
    private Button SendToBackButton = null!;

    // UI Elements - Edge Properties
    private StackPanel EdgePropertiesPanel = null!;
    private TextBlock EdgeLabelTitle = null!;
    private TextBox EdgeLabelBox = null!;
    private TextBlock EdgeStyleLabel = null!;
    private ComboBox EdgeStyleComboBox = null!;
    private TextBlock EdgeRoutingLabel = null!;
    private ComboBox EdgeRoutingComboBox = null!;
    private TextBlock EdgeArrowLabel = null!;
    private ComboBox EdgeArrowComboBox = null!;
    private TextBlock EdgeColorLabel = null!;
    private ColorPicker EdgeColorPicker = null!;

    // UI Elements - Diagram Properties
    private StackPanel DiagramPropertiesPanel = null!;
    private TextBlock DiagramNameLabel = null!;
    private TextBox DiagramNameBox = null!;
    private TextBlock DiagramDescLabel = null!;
    private TextBox DiagramDescBox = null!;
    private CheckBox ShowGridCheckBox = null!;
    private CheckBox SnapToGridCheckBox = null!;
    private TextBlock GridSizeLabel = null!;
    private NumberBox GridSizeBox = null!;

    // UI Elements - Canvas and Status
    private FlowChartCanvas DiagramCanvas = null!;
    private TextBlock StatusText = null!;
    private TextBlock ZoomText = null!;
    private TextBlock ElementCountText = null!;

    public FlowChartEditorPage()
    {
        _viewModel = new FlowChartEditorViewModel();
        BuildUI();

        // Bind canvas to diagram
        DiagramCanvas.Diagram = _viewModel.Diagram;

        Loaded += OnLoaded;
    }

    private void BuildUI()
    {
        Background = (Brush)Application.Current.Resources["ApplicationPageBackgroundThemeBrush"];

        var mainGrid = new Grid();
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250) });

        // Toolbar
        var commandBar = BuildToolbar();
        Grid.SetRow(commandBar, 0);
        Grid.SetColumnSpan(commandBar, 3);
        mainGrid.Children.Add(commandBar);

        // Shape Palette
        var shapePalette = BuildShapePalette();
        Grid.SetRow(shapePalette, 1);
        Grid.SetColumn(shapePalette, 0);
        mainGrid.Children.Add(shapePalette);

        // Canvas Area
        var canvasArea = BuildCanvasArea();
        Grid.SetRow(canvasArea, 1);
        Grid.SetColumn(canvasArea, 1);
        mainGrid.Children.Add(canvasArea);

        // Properties Panel
        var propertiesPanel = BuildPropertiesPanel();
        Grid.SetRow(propertiesPanel, 1);
        Grid.SetColumn(propertiesPanel, 2);
        mainGrid.Children.Add(propertiesPanel);

        // Status Bar
        var statusBar = BuildStatusBar();
        Grid.SetRow(statusBar, 2);
        Grid.SetColumnSpan(statusBar, 3);
        mainGrid.Children.Add(statusBar);

        Content = mainGrid;
    }

    private CommandBar BuildToolbar()
    {
        var commandBar = new CommandBar { DefaultLabelPosition = CommandBarDefaultLabelPosition.Right };

        NewButton = new AppBarButton { Icon = new SymbolIcon(Symbol.Page2), Label = "New" };
        NewButton.Click += OnNewClick;
        commandBar.PrimaryCommands.Add(NewButton);

        OpenButton = new AppBarButton { Icon = new SymbolIcon(Symbol.OpenFile), Label = "Open" };
        OpenButton.Click += OnOpenClick;
        commandBar.PrimaryCommands.Add(OpenButton);

        SaveButton = new AppBarButton { Icon = new SymbolIcon(Symbol.Save), Label = "Save" };
        SaveButton.Click += OnSaveClick;
        commandBar.PrimaryCommands.Add(SaveButton);

        SaveAsButton = new AppBarButton { Icon = new SymbolIcon(Symbol.SaveLocal), Label = "Save As" };
        SaveAsButton.Click += OnSaveAsClick;
        commandBar.PrimaryCommands.Add(SaveAsButton);

        commandBar.PrimaryCommands.Add(new AppBarSeparator());

        UndoButton = new AppBarButton { Icon = new SymbolIcon(Symbol.Undo), Label = "Undo" };
        UndoButton.Click += OnUndoClick;
        commandBar.PrimaryCommands.Add(UndoButton);

        RedoButton = new AppBarButton { Icon = new SymbolIcon(Symbol.Redo), Label = "Redo" };
        RedoButton.Click += OnRedoClick;
        commandBar.PrimaryCommands.Add(RedoButton);

        commandBar.PrimaryCommands.Add(new AppBarSeparator());

        DeleteButton = new AppBarButton { Icon = new SymbolIcon(Symbol.Delete), Label = "Delete" };
        DeleteButton.Click += OnDeleteClick;
        commandBar.PrimaryCommands.Add(DeleteButton);

        DuplicateButton = new AppBarButton { Icon = new SymbolIcon(Symbol.Copy), Label = "Duplicate" };
        DuplicateButton.Click += OnDuplicateClick;
        commandBar.PrimaryCommands.Add(DuplicateButton);

        commandBar.PrimaryCommands.Add(new AppBarSeparator());

        ZoomInButton = new AppBarButton { Icon = new SymbolIcon(Symbol.ZoomIn), Label = "Zoom In" };
        ZoomInButton.Click += OnZoomInClick;
        commandBar.PrimaryCommands.Add(ZoomInButton);

        ZoomOutButton = new AppBarButton { Icon = new SymbolIcon(Symbol.ZoomOut), Label = "Zoom Out" };
        ZoomOutButton.Click += OnZoomOutClick;
        commandBar.PrimaryCommands.Add(ZoomOutButton);

        ZoomResetButton = new AppBarButton { Icon = new FontIcon { Glyph = "\uE71E" }, Label = "100%" };
        ZoomResetButton.Click += OnZoomResetClick;
        commandBar.PrimaryCommands.Add(ZoomResetButton);

        commandBar.PrimaryCommands.Add(new AppBarSeparator());

        ConnectModeButton = new AppBarToggleButton { Icon = new FontIcon { Glyph = "\uE8FB" }, Label = "Connect" };
        ConnectModeButton.Click += OnConnectModeClick;
        commandBar.PrimaryCommands.Add(ConnectModeButton);

        SampleButton = new AppBarButton { Icon = new FontIcon { Glyph = "\uE8F1" }, Label = "Create Sample" };
        SampleButton.Click += OnSampleClick;
        commandBar.SecondaryCommands.Add(SampleButton);

        return commandBar;
    }

    private Border BuildShapePalette()
    {
        var border = new Border
        {
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(0, 0, 1, 0)
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        ShapesTitle = new TextBlock
        {
            Text = "Shapes",
            Style = (Style)Application.Current.Resources["SubtitleTextBlockStyle"],
            Margin = new Thickness(16, 12, 16, 12)
        };
        Grid.SetRow(ShapesTitle, 0);
        grid.Children.Add(ShapesTitle);

        var scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };

        // Create shape buttons directly - ItemsControl with DataTemplate doesn't work well in code
        var shapesStack = new StackPanel { Margin = new Thickness(8), Spacing = 4 };
        ShapesList = new ItemsControl(); // Keep reference for compatibility but use shapesStack

        foreach (var shape in _viewModel.AvailableShapes)
        {
            var btn = new Button
            {
                Content = shape.ToString(),
                Tag = shape,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 2, 0, 2)
            };
            btn.Click += OnShapeClick;
            shapesStack.Children.Add(btn);
        }

        scrollViewer.Content = shapesStack;

        Grid.SetRow(scrollViewer, 1);
        grid.Children.Add(scrollViewer);

        border.Child = grid;
        return border;
    }

    private Border BuildCanvasArea()
    {
        var border = new Border
        {
            Background = (Brush)Application.Current.Resources["LayerFillColorDefaultBrush"]
        };

        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            ZoomMode = ZoomMode.Disabled
        };

        DiagramCanvas = new FlowChartCanvas
        {
            Width = 2000,
            Height = 2000
        };
        DiagramCanvas.NodeSelected += OnNodeSelected;
        DiagramCanvas.EdgeSelected += OnEdgeSelected;
        DiagramCanvas.NodeMoved += OnNodeMoved;
        DiagramCanvas.ConnectionCreated += OnConnectionCreated;

        scrollViewer.Content = DiagramCanvas;
        border.Child = scrollViewer;
        return border;
    }

    private Border BuildPropertiesPanel()
    {
        var border = new Border
        {
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1, 0, 0, 0)
        };

        var scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var mainStack = new StackPanel { Margin = new Thickness(16), Spacing = 12 };

        PropertiesTitle = new TextBlock
        {
            Text = "Properties",
            Style = (Style)Application.Current.Resources["SubtitleTextBlockStyle"]
        };
        mainStack.Children.Add(PropertiesTitle);

        // Node Properties Panel
        NodePropertiesPanel = BuildNodePropertiesPanel();
        mainStack.Children.Add(NodePropertiesPanel);

        // Edge Properties Panel
        EdgePropertiesPanel = BuildEdgePropertiesPanel();
        mainStack.Children.Add(EdgePropertiesPanel);

        // Diagram Properties Panel
        DiagramPropertiesPanel = BuildDiagramPropertiesPanel();
        mainStack.Children.Add(DiagramPropertiesPanel);

        scrollViewer.Content = mainStack;
        border.Child = scrollViewer;
        return border;
    }

    private StackPanel BuildNodePropertiesPanel()
    {
        var panel = new StackPanel { Visibility = Visibility.Collapsed, Spacing = 8 };

        NodeTextLabel = new TextBlock { Text = "Text", Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"] };
        panel.Children.Add(NodeTextLabel);

        NodeTextBox = new TextBox { PlaceholderText = "Enter text..." };
        NodeTextBox.TextChanged += OnNodeTextChanged;
        panel.Children.Add(NodeTextBox);

        NodeFillLabel = new TextBlock { Text = "Fill Color", Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"] };
        panel.Children.Add(NodeFillLabel);

        NodeFillColorPicker = new ColorPicker
        {
            ColorSpectrumShape = ColorSpectrumShape.Ring,
            IsMoreButtonVisible = false,
            IsColorSliderVisible = true,
            IsColorChannelTextInputVisible = false,
            IsHexInputVisible = true,
            IsAlphaEnabled = false
        };
        NodeFillColorPicker.ColorChanged += OnNodeFillColorChanged;
        panel.Children.Add(NodeFillColorPicker);

        NodeStrokeLabel = new TextBlock { Text = "Border Color", Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"] };
        panel.Children.Add(NodeStrokeLabel);

        NodeStrokeColorPicker = new ColorPicker
        {
            ColorSpectrumShape = ColorSpectrumShape.Ring,
            IsMoreButtonVisible = false,
            IsColorSliderVisible = true,
            IsColorChannelTextInputVisible = false,
            IsHexInputVisible = true,
            IsAlphaEnabled = false
        };
        NodeStrokeColorPicker.ColorChanged += OnNodeStrokeColorChanged;
        panel.Children.Add(NodeStrokeColorPicker);

        NodeSizeLabel = new TextBlock { Text = "Size", Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"] };
        panel.Children.Add(NodeSizeLabel);

        var sizeGrid = new Grid();
        sizeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        sizeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        sizeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        NodeWidthBox = new NumberBox { Header = "Width", Minimum = 40, Maximum = 500, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact };
        NodeWidthBox.ValueChanged += OnNodeSizeChanged;
        Grid.SetColumn(NodeWidthBox, 0);
        sizeGrid.Children.Add(NodeWidthBox);

        NodeHeightBox = new NumberBox { Header = "Height", Minimum = 30, Maximum = 500, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact };
        NodeHeightBox.ValueChanged += OnNodeSizeChanged;
        Grid.SetColumn(NodeHeightBox, 2);
        sizeGrid.Children.Add(NodeHeightBox);

        panel.Children.Add(sizeGrid);

        NodeFontLabel = new TextBlock { Text = "Font Size", Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"] };
        panel.Children.Add(NodeFontLabel);

        NodeFontSizeBox = new NumberBox { Minimum = 8, Maximum = 72, Value = 14, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact };
        NodeFontSizeBox.ValueChanged += OnNodeFontSizeChanged;
        panel.Children.Add(NodeFontSizeBox);

        var buttonsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        BringToFrontButton = new Button { Content = "Bring to Front" };
        BringToFrontButton.Click += OnBringToFrontClick;
        buttonsPanel.Children.Add(BringToFrontButton);

        SendToBackButton = new Button { Content = "Send to Back" };
        SendToBackButton.Click += OnSendToBackClick;
        buttonsPanel.Children.Add(SendToBackButton);

        panel.Children.Add(buttonsPanel);

        return panel;
    }

    private StackPanel BuildEdgePropertiesPanel()
    {
        var panel = new StackPanel { Visibility = Visibility.Collapsed, Spacing = 8 };

        EdgeLabelTitle = new TextBlock { Text = "Label", Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"] };
        panel.Children.Add(EdgeLabelTitle);

        EdgeLabelBox = new TextBox { PlaceholderText = "Enter label..." };
        EdgeLabelBox.TextChanged += OnEdgeLabelChanged;
        panel.Children.Add(EdgeLabelBox);

        EdgeStyleLabel = new TextBlock { Text = "Line Style", Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"] };
        panel.Children.Add(EdgeStyleLabel);

        EdgeStyleComboBox = new ComboBox();
        EdgeStyleComboBox.Items.Add(new ComboBoxItem { Content = "Solid", Tag = "Solid" });
        EdgeStyleComboBox.Items.Add(new ComboBoxItem { Content = "Dashed", Tag = "Dashed" });
        EdgeStyleComboBox.Items.Add(new ComboBoxItem { Content = "Dotted", Tag = "Dotted" });
        EdgeStyleComboBox.Items.Add(new ComboBoxItem { Content = "Dash-Dot", Tag = "DashDot" });
        EdgeStyleComboBox.SelectionChanged += OnEdgeStyleChanged;
        panel.Children.Add(EdgeStyleComboBox);

        EdgeRoutingLabel = new TextBlock { Text = "Routing", Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"] };
        panel.Children.Add(EdgeRoutingLabel);

        EdgeRoutingComboBox = new ComboBox();
        EdgeRoutingComboBox.Items.Add(new ComboBoxItem { Content = "Direct", Tag = "Direct" });
        EdgeRoutingComboBox.Items.Add(new ComboBoxItem { Content = "Orthogonal", Tag = "Orthogonal" });
        EdgeRoutingComboBox.Items.Add(new ComboBoxItem { Content = "Curved", Tag = "Curved" });
        EdgeRoutingComboBox.SelectionChanged += OnEdgeRoutingChanged;
        panel.Children.Add(EdgeRoutingComboBox);

        EdgeArrowLabel = new TextBlock { Text = "Target Arrow", Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"] };
        panel.Children.Add(EdgeArrowLabel);

        EdgeArrowComboBox = new ComboBox();
        EdgeArrowComboBox.Items.Add(new ComboBoxItem { Content = "None", Tag = "None" });
        EdgeArrowComboBox.Items.Add(new ComboBoxItem { Content = "Arrow", Tag = "Arrow" });
        EdgeArrowComboBox.Items.Add(new ComboBoxItem { Content = "Open Arrow", Tag = "OpenArrow" });
        EdgeArrowComboBox.Items.Add(new ComboBoxItem { Content = "Diamond", Tag = "Diamond" });
        EdgeArrowComboBox.Items.Add(new ComboBoxItem { Content = "Circle", Tag = "Circle" });
        EdgeArrowComboBox.SelectionChanged += OnEdgeArrowChanged;
        panel.Children.Add(EdgeArrowComboBox);

        EdgeColorLabel = new TextBlock { Text = "Color", Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"] };
        panel.Children.Add(EdgeColorLabel);

        EdgeColorPicker = new ColorPicker
        {
            ColorSpectrumShape = ColorSpectrumShape.Ring,
            IsMoreButtonVisible = false,
            IsColorSliderVisible = true,
            IsColorChannelTextInputVisible = false,
            IsHexInputVisible = true,
            IsAlphaEnabled = false
        };
        EdgeColorPicker.ColorChanged += OnEdgeColorChanged;
        panel.Children.Add(EdgeColorPicker);

        return panel;
    }

    private StackPanel BuildDiagramPropertiesPanel()
    {
        var panel = new StackPanel { Visibility = Visibility.Visible, Spacing = 8 };

        DiagramNameLabel = new TextBlock { Text = "Diagram Name", Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"] };
        panel.Children.Add(DiagramNameLabel);

        DiagramNameBox = new TextBox { PlaceholderText = "Untitled Diagram" };
        DiagramNameBox.TextChanged += OnDiagramNameChanged;
        panel.Children.Add(DiagramNameBox);

        DiagramDescLabel = new TextBlock { Text = "Description", Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"] };
        panel.Children.Add(DiagramDescLabel);

        DiagramDescBox = new TextBox { TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, Height = 80 };
        DiagramDescBox.TextChanged += OnDiagramDescChanged;
        panel.Children.Add(DiagramDescBox);

        ShowGridCheckBox = new CheckBox { Content = "Show Grid", IsChecked = true };
        ShowGridCheckBox.Click += OnShowGridChanged;
        panel.Children.Add(ShowGridCheckBox);

        SnapToGridCheckBox = new CheckBox { Content = "Snap to Grid", IsChecked = true };
        SnapToGridCheckBox.Click += OnSnapToGridChanged;
        panel.Children.Add(SnapToGridCheckBox);

        GridSizeLabel = new TextBlock { Text = "Grid Size", Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"] };
        panel.Children.Add(GridSizeLabel);

        GridSizeBox = new NumberBox { Minimum = 5, Maximum = 50, Value = 20, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact };
        GridSizeBox.ValueChanged += OnGridSizeChanged;
        panel.Children.Add(GridSizeBox);

        return panel;
    }

    private Border BuildStatusBar()
    {
        var border = new Border
        {
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(16, 8, 16, 8)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        StatusText = new TextBlock { Text = "Ready", VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(StatusText, 0);
        grid.Children.Add(StatusText);

        ZoomText = new TextBlock { Text = "100%", Margin = new Thickness(16, 0, 16, 0), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(ZoomText, 1);
        grid.Children.Add(ZoomText);

        ElementCountText = new TextBlock { Text = "0 nodes, 0 edges", VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(ElementCountText, 2);
        grid.Children.Add(ElementCountText);

        border.Child = grid;
        return border;
    }

    /// <summary>
    /// Sets the localization service (called by plugin framework).
    /// </summary>
    public void SetLocalizationService(ILocalizationService localization)
    {
        _localization = localization;
        _localization.CultureChanged += OnCultureChanged;
        ApplyLocalization();
    }

    /// <summary>
    /// Handles navigation parameters.
    /// </summary>
    public void HandleNavigationParameters(Dictionary<string, object>? parameters)
    {
        if (parameters == null) return;

        if (parameters.TryGetValue("action", out var action))
        {
            switch (action.ToString())
            {
                case "open":
                    _ = OpenDiagramAsync();
                    break;
                case "sample":
                    _viewModel.CreateSampleCommand.Execute(null);
                    RefreshCanvas();
                    break;
                case "load" when parameters.TryGetValue("filePath", out var filePath):
                    _ = LoadDiagramFromFileAsync(filePath.ToString()!);
                    break;
            }
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyLocalization();
        UpdateStatusBar();
        UpdatePropertiesPanel();
    }

    private void OnCultureChanged(object? sender, CultureChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(ApplyLocalization);
    }

    private void ApplyLocalization()
    {
        if (_localization == null) return;

        // Toolbar - using GetFromAnyPlugin to search plugin resources
        NewButton.Label = _localization.GetFromAnyPlugin("flowchart.action.new");
        OpenButton.Label = _localization.GetFromAnyPlugin("flowchart.action.open");
        SaveButton.Label = _localization.GetFromAnyPlugin("flowchart.action.save");
        SaveAsButton.Label = _localization.GetFromAnyPlugin("flowchart.action.saveas");
        UndoButton.Label = _localization.GetFromAnyPlugin("flowchart.action.undo");
        RedoButton.Label = _localization.GetFromAnyPlugin("flowchart.action.redo");
        DeleteButton.Label = _localization.GetFromAnyPlugin("flowchart.action.delete");
        DuplicateButton.Label = _localization.GetFromAnyPlugin("flowchart.action.duplicate");
        ZoomInButton.Label = _localization.GetFromAnyPlugin("flowchart.action.zoomin");
        ZoomOutButton.Label = _localization.GetFromAnyPlugin("flowchart.action.zoomout");
        ConnectModeButton.Label = _localization.GetFromAnyPlugin("flowchart.action.connect");
        SampleButton.Label = _localization.GetFromAnyPlugin("flowchart.action.sample");

        // Panels
        ShapesTitle.Text = _localization.GetFromAnyPlugin("flowchart.panel.shapes");
        PropertiesTitle.Text = _localization.GetFromAnyPlugin("flowchart.panel.properties");

        // Node properties
        NodeTextLabel.Text = _localization.GetFromAnyPlugin("flowchart.property.text");
        NodeFillLabel.Text = _localization.GetFromAnyPlugin("flowchart.property.fillcolor");
        NodeStrokeLabel.Text = _localization.GetFromAnyPlugin("flowchart.property.strokecolor");
        NodeSizeLabel.Text = _localization.GetFromAnyPlugin("flowchart.property.size");
        NodeFontLabel.Text = _localization.GetFromAnyPlugin("flowchart.property.fontsize");
        BringToFrontButton.Content = _localization.GetFromAnyPlugin("flowchart.action.bringtofront");
        SendToBackButton.Content = _localization.GetFromAnyPlugin("flowchart.action.sendtoback");

        // Edge properties
        EdgeLabelTitle.Text = _localization.GetFromAnyPlugin("flowchart.property.label");
        EdgeStyleLabel.Text = _localization.GetFromAnyPlugin("flowchart.property.linestyle");
        EdgeRoutingLabel.Text = _localization.GetFromAnyPlugin("flowchart.property.routing");
        EdgeArrowLabel.Text = _localization.GetFromAnyPlugin("flowchart.property.arrow");
        EdgeColorLabel.Text = _localization.GetFromAnyPlugin("flowchart.property.color");

        // Diagram properties
        DiagramNameLabel.Text = _localization.GetFromAnyPlugin("flowchart.property.name");
        DiagramDescLabel.Text = _localization.GetFromAnyPlugin("flowchart.property.description");
        ShowGridCheckBox.Content = _localization.GetFromAnyPlugin("flowchart.property.showgrid");
        SnapToGridCheckBox.Content = _localization.GetFromAnyPlugin("flowchart.property.snaptogrid");
        GridSizeLabel.Text = _localization.GetFromAnyPlugin("flowchart.property.gridsize");
    }

    #region Toolbar Commands

    private void OnNewClick(object sender, RoutedEventArgs e)
    {
        _viewModel.NewCommand.Execute(null);
        RefreshCanvas();
        UpdateStatusBar();
    }

    private async void OnOpenClick(object sender, RoutedEventArgs e)
    {
        await OpenDiagramAsync();
    }

    private async Task OpenDiagramAsync()
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".afc");
        picker.FileTypeFilter.Add(".drawio");
        picker.FileTypeFilter.Add(".json");

        var windowHandle = GetWindowHandle();
        if (windowHandle != IntPtr.Zero)
        {
            WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);
        }

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            await LoadDiagramFromFileAsync(file.Path);
        }
    }

    private IntPtr GetWindowHandle()
    {
        if (XamlRoot?.Content is FrameworkElement)
        {
            var appWindow = Microsoft.UI.Xaml.Window.Current;
            if (appWindow != null)
            {
                return WinRT.Interop.WindowNative.GetWindowHandle(appWindow);
            }
        }
        return IntPtr.Zero;
    }

    private async Task LoadDiagramFromFileAsync(string filePath)
    {
        await _viewModel.LoadFromFileAsync(filePath);
        RefreshCanvas();
        UpdateStatusBar();
        UpdatePropertiesPanel();
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_viewModel.CurrentFilePath))
        {
            await SaveDiagramAsAsync();
        }
        else
        {
            await _viewModel.SaveToFileAsync(_viewModel.CurrentFilePath);
            UpdateStatusBar();
        }
    }

    private async void OnSaveAsClick(object sender, RoutedEventArgs e)
    {
        await SaveDiagramAsAsync();
    }

    private async Task SaveDiagramAsAsync()
    {
        var picker = new FileSavePicker();
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeChoices.Add("Arcana FlowChart", new[] { ".afc" });
        picker.FileTypeChoices.Add("Draw.io", new[] { ".drawio" });
        picker.FileTypeChoices.Add("JSON", new[] { ".json" });
        picker.SuggestedFileName = _viewModel.Diagram.Name;

        var windowHandle = GetWindowHandle();
        if (windowHandle != IntPtr.Zero)
        {
            WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);
        }

        var file = await picker.PickSaveFileAsync();
        if (file != null)
        {
            await _viewModel.SaveToFileAsync(file.Path);
            UpdateStatusBar();
        }
    }

    private void OnUndoClick(object sender, RoutedEventArgs e)
    {
        _viewModel.UndoCommand.Execute(null);
        RefreshCanvas();
        UpdateStatusBar();
    }

    private void OnRedoClick(object sender, RoutedEventArgs e)
    {
        _viewModel.RedoCommand.Execute(null);
        RefreshCanvas();
        UpdateStatusBar();
    }

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        _viewModel.DeleteSelectedCommand.Execute(null);
        RefreshCanvas();
        UpdateStatusBar();
        UpdatePropertiesPanel();
    }

    private void OnDuplicateClick(object sender, RoutedEventArgs e)
    {
        _viewModel.DuplicateSelectedCommand.Execute(null);
        RefreshCanvas();
        UpdateStatusBar();
    }

    private void OnZoomInClick(object sender, RoutedEventArgs e)
    {
        _viewModel.ZoomInCommand.Execute(null);
        DiagramCanvas.ZoomLevel = _viewModel.ZoomLevel;
        UpdateStatusBar();
    }

    private void OnZoomOutClick(object sender, RoutedEventArgs e)
    {
        _viewModel.ZoomOutCommand.Execute(null);
        DiagramCanvas.ZoomLevel = _viewModel.ZoomLevel;
        UpdateStatusBar();
    }

    private void OnZoomResetClick(object sender, RoutedEventArgs e)
    {
        _viewModel.ZoomResetCommand.Execute(null);
        DiagramCanvas.ZoomLevel = _viewModel.ZoomLevel;
        UpdateStatusBar();
    }

    private void OnConnectModeClick(object sender, RoutedEventArgs e)
    {
        _viewModel.IsConnectMode = ConnectModeButton.IsChecked ?? false;
        DiagramCanvas.IsConnectMode = _viewModel.IsConnectMode;
        RefreshCanvas();
    }

    private void OnSampleClick(object sender, RoutedEventArgs e)
    {
        _viewModel.CreateSampleCommand.Execute(null);
        RefreshCanvas();
        UpdateStatusBar();
        UpdatePropertiesPanel();
    }

    #endregion

    #region Shape Palette

    private void OnShapeClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is NodeShape shape)
        {
            _viewModel.AddNodeCommand.Execute(shape);
            RefreshCanvas();
            UpdateStatusBar();
        }
    }

    #endregion

    #region Canvas Events

    private void OnNodeSelected(object sender, NodeSelectedEventArgs e)
    {
        _viewModel.SelectNode(e.Node?.Id);
        UpdatePropertiesPanel();
    }

    private void OnEdgeSelected(object sender, EdgeSelectedEventArgs e)
    {
        _viewModel.SelectEdge(e.Edge?.Id);
        UpdatePropertiesPanel();
    }

    private void OnNodeMoved(object sender, NodeMovedEventArgs e)
    {
        _viewModel.UpdateNodePosition(e.Node.Id, e.X, e.Y);
        UpdateStatusBar();
    }

    private void OnConnectionCreated(object sender, ConnectionCreatedEventArgs e)
    {
        _viewModel.AddConnection(e.SourceNodeId, e.TargetNodeId, e.SourcePoint, e.TargetPoint);
        RefreshCanvas();
        UpdateStatusBar();
    }

    #endregion

    #region Properties Panel

    private void UpdatePropertiesPanel()
    {
        _isUpdatingProperties = true;

        var selectedNode = _viewModel.SelectedNode;
        var selectedEdge = _viewModel.SelectedEdge;

        if (selectedNode != null)
        {
            NodePropertiesPanel.Visibility = Visibility.Visible;
            EdgePropertiesPanel.Visibility = Visibility.Collapsed;
            DiagramPropertiesPanel.Visibility = Visibility.Collapsed;

            NodeTextBox.Text = selectedNode.Text;
            NodeWidthBox.Value = selectedNode.Width;
            NodeHeightBox.Value = selectedNode.Height;
            NodeFontSizeBox.Value = selectedNode.FontSize;
            NodeFillColorPicker.Color = ParseColor(selectedNode.FillColor);
            NodeStrokeColorPicker.Color = ParseColor(selectedNode.StrokeColor);
        }
        else if (selectedEdge != null)
        {
            NodePropertiesPanel.Visibility = Visibility.Collapsed;
            EdgePropertiesPanel.Visibility = Visibility.Visible;
            DiagramPropertiesPanel.Visibility = Visibility.Collapsed;

            EdgeLabelBox.Text = selectedEdge.Label;
            EdgeStyleComboBox.SelectedIndex = (int)selectedEdge.Style;
            EdgeRoutingComboBox.SelectedIndex = (int)selectedEdge.Routing;
            EdgeArrowComboBox.SelectedIndex = (int)selectedEdge.TargetArrow;
            EdgeColorPicker.Color = ParseColor(selectedEdge.StrokeColor);
        }
        else
        {
            NodePropertiesPanel.Visibility = Visibility.Collapsed;
            EdgePropertiesPanel.Visibility = Visibility.Collapsed;
            DiagramPropertiesPanel.Visibility = Visibility.Visible;

            DiagramNameBox.Text = _viewModel.Diagram.Name;
            DiagramDescBox.Text = _viewModel.Diagram.Description;
            ShowGridCheckBox.IsChecked = _viewModel.Diagram.ShowGrid;
            SnapToGridCheckBox.IsChecked = _viewModel.Diagram.SnapToGrid;
            GridSizeBox.Value = _viewModel.Diagram.GridSize;
        }

        _isUpdatingProperties = false;
    }

    private void OnNodeTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingProperties || _viewModel.SelectedNode == null) return;
        _viewModel.SelectedNode.Text = NodeTextBox.Text;
        RefreshCanvas();
    }

    private void OnNodeFillColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        if (_isUpdatingProperties || _viewModel.SelectedNode == null) return;
        _viewModel.SelectedNode.FillColor = ColorToHex(args.NewColor);
        RefreshCanvas();
    }

    private void OnNodeStrokeColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        if (_isUpdatingProperties || _viewModel.SelectedNode == null) return;
        _viewModel.SelectedNode.StrokeColor = ColorToHex(args.NewColor);
        RefreshCanvas();
    }

    private void OnNodeSizeChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_isUpdatingProperties || _viewModel.SelectedNode == null) return;
        _viewModel.UpdateNodeSize(_viewModel.SelectedNode.Id, NodeWidthBox.Value, NodeHeightBox.Value);
        RefreshCanvas();
    }

    private void OnNodeFontSizeChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_isUpdatingProperties || _viewModel.SelectedNode == null) return;
        _viewModel.SelectedNode.FontSize = args.NewValue;
        RefreshCanvas();
    }

    private void OnBringToFrontClick(object sender, RoutedEventArgs e)
    {
        _viewModel.BringToFrontCommand.Execute(null);
        RefreshCanvas();
    }

    private void OnSendToBackClick(object sender, RoutedEventArgs e)
    {
        _viewModel.SendToBackCommand.Execute(null);
        RefreshCanvas();
    }

    private void OnEdgeLabelChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingProperties || _viewModel.SelectedEdge == null) return;
        _viewModel.SelectedEdge.Label = EdgeLabelBox.Text;
        RefreshCanvas();
    }

    private void OnEdgeStyleChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingProperties || _viewModel.SelectedEdge == null) return;
        _viewModel.SelectedEdge.Style = (LineStyle)EdgeStyleComboBox.SelectedIndex;
        RefreshCanvas();
    }

    private void OnEdgeRoutingChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingProperties || _viewModel.SelectedEdge == null) return;
        _viewModel.SelectedEdge.Routing = (RoutingStyle)EdgeRoutingComboBox.SelectedIndex;
        RefreshCanvas();
    }

    private void OnEdgeArrowChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingProperties || _viewModel.SelectedEdge == null) return;
        _viewModel.SelectedEdge.TargetArrow = (ArrowType)EdgeArrowComboBox.SelectedIndex;
        RefreshCanvas();
    }

    private void OnEdgeColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        if (_isUpdatingProperties || _viewModel.SelectedEdge == null) return;
        _viewModel.SelectedEdge.StrokeColor = ColorToHex(args.NewColor);
        RefreshCanvas();
    }

    private void OnDiagramNameChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingProperties) return;
        _viewModel.Diagram.Name = DiagramNameBox.Text;
    }

    private void OnDiagramDescChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingProperties) return;
        _viewModel.Diagram.Description = DiagramDescBox.Text;
    }

    private void OnShowGridChanged(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingProperties) return;
        _viewModel.Diagram.ShowGrid = ShowGridCheckBox.IsChecked ?? true;
        RefreshCanvas();
    }

    private void OnSnapToGridChanged(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingProperties) return;
        _viewModel.Diagram.SnapToGrid = SnapToGridCheckBox.IsChecked ?? true;
    }

    private void OnGridSizeChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_isUpdatingProperties) return;
        _viewModel.Diagram.GridSize = args.NewValue;
        RefreshCanvas();
    }

    #endregion

    #region Helpers

    private void RefreshCanvas()
    {
        DiagramCanvas.Diagram = _viewModel.Diagram;
        DiagramCanvas.RefreshDiagram();
    }

    private void UpdateStatusBar()
    {
        StatusText.Text = _viewModel.StatusMessage;
        ZoomText.Text = $"{_viewModel.ZoomLevel * 100:F0}%";
        ElementCountText.Text = $"{_viewModel.Diagram.Nodes.Count} nodes, {_viewModel.Diagram.Edges.Count} edges";
    }

    private static Color ParseColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
        {
            return Color.FromArgb(255,
                byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber));
        }
        return Colors.White;
    }

    private static string ColorToHex(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    #endregion
}
