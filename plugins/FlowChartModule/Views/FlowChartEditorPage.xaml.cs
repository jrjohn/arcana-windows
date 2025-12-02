using System.Reflection;
using Arcana.Plugin.FlowChart.Controls;
using Arcana.Plugin.FlowChart.Models;
using Arcana.Plugin.FlowChart.ViewModels;
using Arcana.Plugins.Contracts;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Storage.Pickers;
using Windows.UI;

namespace Arcana.Plugin.FlowChart.Views;

/// <summary>
/// FlowChart editor page - loads XAML at runtime using XamlReader.
/// </summary>
public sealed class FlowChartEditorPage : Page
{
    private ILocalizationService? _localization;
    private readonly FlowChartEditorViewModel _viewModel;
    private bool _isUpdatingProperties;
    private FlowChartCanvas? _flowChartCanvas;

    // UI Elements - found after XAML loading
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

    private TextBlock ShapesTitle = null!;
    private StackPanel ShapesPanel = null!;
    private Canvas DiagramCanvas = null!;
    private ScrollViewer? DiagramScrollViewer = null;  // Parent of DiagramCanvas
    private TextBlock PropertiesTitle = null!;

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

    private StackPanel DiagramPropertiesPanel = null!;
    private TextBlock DiagramNameLabel = null!;
    private TextBox DiagramNameBox = null!;
    private TextBlock DiagramDescLabel = null!;
    private TextBox DiagramDescBox = null!;
    private CheckBox ShowGridCheckBox = null!;
    private CheckBox SnapToGridCheckBox = null!;
    private TextBlock GridSizeLabel = null!;
    private NumberBox GridSizeBox = null!;

    private TextBlock StatusText = null!;
    private TextBlock ZoomText = null!;
    private TextBlock ElementCountText = null!;

    public FlowChartEditorPage()
    {
        System.Diagnostics.Debug.WriteLine("[FlowChart] FlowChartEditorPage constructor starting...");
        try
        {
            _viewModel = new FlowChartEditorViewModel();
            System.Diagnostics.Debug.WriteLine("[FlowChart] ViewModel created");

            LoadXamlContent();
            System.Diagnostics.Debug.WriteLine("[FlowChart] LoadXamlContent completed");

            InitializeFlowChartCanvas();
            System.Diagnostics.Debug.WriteLine("[FlowChart] InitializeFlowChartCanvas completed");

            WireUpEvents();
            System.Diagnostics.Debug.WriteLine("[FlowChart] WireUpEvents completed");

            PopulateShapePalette();
            System.Diagnostics.Debug.WriteLine("[FlowChart] PopulateShapePalette completed");

            Loaded += OnLoaded;
            System.Diagnostics.Debug.WriteLine("[FlowChart] FlowChartEditorPage constructor completed successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FlowChart] Constructor failed: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[FlowChart] Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// Loads XAML content at runtime using XamlReader.
    /// </summary>
    private void LoadXamlContent()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[FlowChart] LoadXamlContent starting...");
            var xamlContent = GetEmbeddedXaml();
            System.Diagnostics.Debug.WriteLine($"[FlowChart] XAML content length: {xamlContent?.Length ?? 0}");

            var loadedContent = XamlReader.Load(xamlContent) as Page;
            System.Diagnostics.Debug.WriteLine($"[FlowChart] XamlReader.Load completed, loadedContent is null: {loadedContent == null}");

            if (loadedContent?.Content is UIElement rootElement)
            {
                System.Diagnostics.Debug.WriteLine("[FlowChart] Setting Content from loaded XAML");
                // Detach content from the loaded Page before attaching to this Page
                loadedContent.Content = null;
                Content = rootElement;
                FindNamedElements(rootElement);
                System.Diagnostics.Debug.WriteLine("[FlowChart] XAML load completed successfully");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[FlowChart] loadedContent.Content is not UIElement, using fallback UI");
                // Fallback to programmatic UI if XAML fails
                BuildFallbackUI();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FlowChart] XAML load failed: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[FlowChart] Stack trace: {ex.StackTrace}");
            BuildFallbackUI();
        }
    }

    /// <summary>
    /// Gets the embedded XAML content.
    /// </summary>
    private string GetEmbeddedXaml()
    {
        // Try to load from file first
        var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var xamlPath = Path.Combine(assemblyLocation ?? "", "Views", "FlowChartEditorPage.xaml");
        System.Diagnostics.Debug.WriteLine($"[FlowChart] Looking for XAML at: {xamlPath}");

        if (File.Exists(xamlPath))
        {
            System.Diagnostics.Debug.WriteLine("[FlowChart] Loading XAML from file");
            return File.ReadAllText(xamlPath);
        }

        System.Diagnostics.Debug.WriteLine("[FlowChart] XAML file not found, trying embedded resource");

        // Fallback: try embedded resource
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Arcana.Plugin.FlowChart.Views.FlowChartEditorPage.xaml";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream != null)
        {
            System.Diagnostics.Debug.WriteLine("[FlowChart] Loading XAML from embedded resource");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        System.Diagnostics.Debug.WriteLine("[FlowChart] Using inline XAML fallback");
        // Return inline XAML as last resort
        return GetInlineXaml();
    }

    /// <summary>
    /// Inline XAML fallback.
    /// </summary>
    private static string GetInlineXaml()
    {
        return """
            <Page
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="*"/>
                        <RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="200"/>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="250"/>
                    </Grid.ColumnDefinitions>

                    <CommandBar Grid.Row="0" Grid.ColumnSpan="3" DefaultLabelPosition="Right">
                        <AppBarButton Name="NewButton" Icon="Page2" Label="New"/>
                        <AppBarButton Name="OpenButton" Icon="OpenFile" Label="Open"/>
                        <AppBarButton Name="SaveButton" Icon="Save" Label="Save"/>
                        <AppBarButton Name="SaveAsButton" Icon="SaveLocal" Label="Save As"/>
                        <AppBarSeparator/>
                        <AppBarButton Name="UndoButton" Icon="Undo" Label="Undo"/>
                        <AppBarButton Name="RedoButton" Icon="Redo" Label="Redo"/>
                        <AppBarSeparator/>
                        <AppBarButton Name="DeleteButton" Icon="Delete" Label="Delete"/>
                        <AppBarButton Name="DuplicateButton" Icon="Copy" Label="Duplicate"/>
                        <AppBarSeparator/>
                        <AppBarButton Name="ZoomInButton" Icon="ZoomIn" Label="Zoom In"/>
                        <AppBarButton Name="ZoomOutButton" Icon="ZoomOut" Label="Zoom Out"/>
                        <AppBarButton Name="ZoomResetButton" Label="100%">
                            <AppBarButton.Icon><FontIcon Glyph="&#xE71E;"/></AppBarButton.Icon>
                        </AppBarButton>
                        <AppBarSeparator/>
                        <AppBarToggleButton Name="ConnectModeButton" Label="Connect">
                            <AppBarToggleButton.Icon><FontIcon Glyph="&#xE8FB;"/></AppBarToggleButton.Icon>
                        </AppBarToggleButton>
                        <CommandBar.SecondaryCommands>
                            <AppBarButton Name="SampleButton" Label="Create Sample">
                                <AppBarButton.Icon><FontIcon Glyph="&#xE8F1;"/></AppBarButton.Icon>
                            </AppBarButton>
                        </CommandBar.SecondaryCommands>
                    </CommandBar>

                    <Border Grid.Row="1" Grid.Column="0" Background="{ThemeResource CardBackgroundFillColorDefaultBrush}" BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}" BorderThickness="0,0,1,0">
                        <Grid>
                            <Grid.RowDefinitions>
                                <RowDefinition Height="Auto"/>
                                <RowDefinition Height="*"/>
                            </Grid.RowDefinitions>
                            <TextBlock Name="ShapesTitle" Text="Shapes" Style="{ThemeResource SubtitleTextBlockStyle}" Margin="16,12,16,12"/>
                            <ScrollViewer Grid.Row="1" VerticalScrollBarVisibility="Auto">
                                <StackPanel Name="ShapesPanel" Margin="8" Spacing="4"/>
                            </ScrollViewer>
                        </Grid>
                    </Border>

                    <Border Grid.Row="1" Grid.Column="1" Background="{ThemeResource LayerFillColorDefaultBrush}">
                        <ScrollViewer HorizontalScrollBarVisibility="Auto" VerticalScrollBarVisibility="Auto" ZoomMode="Disabled">
                            <Canvas Name="DiagramCanvas" Width="2000" Height="2000" Background="White"/>
                        </ScrollViewer>
                    </Border>

                    <Border Grid.Row="1" Grid.Column="2" Background="{ThemeResource CardBackgroundFillColorDefaultBrush}" BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}" BorderThickness="1,0,0,0">
                        <ScrollViewer VerticalScrollBarVisibility="Auto">
                            <StackPanel Margin="16" Spacing="12">
                                <TextBlock Name="PropertiesTitle" Text="Properties" Style="{ThemeResource SubtitleTextBlockStyle}"/>
                                <StackPanel Name="NodePropertiesPanel" Visibility="Collapsed" Spacing="8">
                                    <TextBlock Name="NodeTextLabel" Text="Text" Style="{ThemeResource CaptionTextBlockStyle}"/>
                                    <TextBox Name="NodeTextBox" PlaceholderText="Enter text..."/>
                                    <TextBlock Name="NodeFillLabel" Text="Fill Color" Style="{ThemeResource CaptionTextBlockStyle}"/>
                                    <ColorPicker Name="NodeFillColorPicker" ColorSpectrumShape="Ring" IsMoreButtonVisible="False" IsColorSliderVisible="True" IsColorChannelTextInputVisible="False" IsHexInputVisible="True" IsAlphaEnabled="False"/>
                                    <TextBlock Name="NodeStrokeLabel" Text="Border Color" Style="{ThemeResource CaptionTextBlockStyle}"/>
                                    <ColorPicker Name="NodeStrokeColorPicker" ColorSpectrumShape="Ring" IsMoreButtonVisible="False" IsColorSliderVisible="True" IsColorChannelTextInputVisible="False" IsHexInputVisible="True" IsAlphaEnabled="False"/>
                                    <TextBlock Name="NodeSizeLabel" Text="Size" Style="{ThemeResource CaptionTextBlockStyle}"/>
                                    <Grid>
                                        <Grid.ColumnDefinitions>
                                            <ColumnDefinition Width="*"/>
                                            <ColumnDefinition Width="8"/>
                                            <ColumnDefinition Width="*"/>
                                        </Grid.ColumnDefinitions>
                                        <NumberBox Name="NodeWidthBox" Header="Width" Minimum="40" Maximum="500" SpinButtonPlacementMode="Compact"/>
                                        <NumberBox Name="NodeHeightBox" Grid.Column="2" Header="Height" Minimum="30" Maximum="500" SpinButtonPlacementMode="Compact"/>
                                    </Grid>
                                    <TextBlock Name="NodeFontLabel" Text="Font Size" Style="{ThemeResource CaptionTextBlockStyle}"/>
                                    <NumberBox Name="NodeFontSizeBox" Minimum="8" Maximum="72" Value="14" SpinButtonPlacementMode="Compact"/>
                                    <StackPanel Orientation="Horizontal" Spacing="8">
                                        <Button Name="BringToFrontButton" Content="Bring to Front"/>
                                        <Button Name="SendToBackButton" Content="Send to Back"/>
                                    </StackPanel>
                                </StackPanel>
                                <StackPanel Name="EdgePropertiesPanel" Visibility="Collapsed" Spacing="8">
                                    <TextBlock Name="EdgeLabelTitle" Text="Label" Style="{ThemeResource CaptionTextBlockStyle}"/>
                                    <TextBox Name="EdgeLabelBox" PlaceholderText="Enter label..."/>
                                    <TextBlock Name="EdgeStyleLabel" Text="Line Style" Style="{ThemeResource CaptionTextBlockStyle}"/>
                                    <ComboBox Name="EdgeStyleComboBox" HorizontalAlignment="Stretch">
                                        <ComboBoxItem Content="Solid" Tag="Solid"/>
                                        <ComboBoxItem Content="Dashed" Tag="Dashed"/>
                                        <ComboBoxItem Content="Dotted" Tag="Dotted"/>
                                        <ComboBoxItem Content="Dash-Dot" Tag="DashDot"/>
                                    </ComboBox>
                                    <TextBlock Name="EdgeRoutingLabel" Text="Routing" Style="{ThemeResource CaptionTextBlockStyle}"/>
                                    <ComboBox Name="EdgeRoutingComboBox" HorizontalAlignment="Stretch">
                                        <ComboBoxItem Content="Direct" Tag="Direct"/>
                                        <ComboBoxItem Content="Orthogonal" Tag="Orthogonal"/>
                                        <ComboBoxItem Content="Curved" Tag="Curved"/>
                                    </ComboBox>
                                    <TextBlock Name="EdgeArrowLabel" Text="Target Arrow" Style="{ThemeResource CaptionTextBlockStyle}"/>
                                    <ComboBox Name="EdgeArrowComboBox" HorizontalAlignment="Stretch">
                                        <ComboBoxItem Content="None" Tag="None"/>
                                        <ComboBoxItem Content="Arrow" Tag="Arrow"/>
                                        <ComboBoxItem Content="Open Arrow" Tag="OpenArrow"/>
                                        <ComboBoxItem Content="Diamond" Tag="Diamond"/>
                                        <ComboBoxItem Content="Circle" Tag="Circle"/>
                                    </ComboBox>
                                    <TextBlock Name="EdgeColorLabel" Text="Color" Style="{ThemeResource CaptionTextBlockStyle}"/>
                                    <ColorPicker Name="EdgeColorPicker" ColorSpectrumShape="Ring" IsMoreButtonVisible="False" IsColorSliderVisible="True" IsColorChannelTextInputVisible="False" IsHexInputVisible="True" IsAlphaEnabled="False"/>
                                </StackPanel>
                                <StackPanel Name="DiagramPropertiesPanel" Visibility="Visible" Spacing="8">
                                    <TextBlock Name="DiagramNameLabel" Text="Diagram Name" Style="{ThemeResource CaptionTextBlockStyle}"/>
                                    <TextBox Name="DiagramNameBox" PlaceholderText="Untitled Diagram"/>
                                    <TextBlock Name="DiagramDescLabel" Text="Description" Style="{ThemeResource CaptionTextBlockStyle}"/>
                                    <TextBox Name="DiagramDescBox" TextWrapping="Wrap" AcceptsReturn="True" Height="80"/>
                                    <CheckBox Name="ShowGridCheckBox" Content="Show Grid" IsChecked="True"/>
                                    <CheckBox Name="SnapToGridCheckBox" Content="Snap to Grid" IsChecked="True"/>
                                    <TextBlock Name="GridSizeLabel" Text="Grid Size" Style="{ThemeResource CaptionTextBlockStyle}"/>
                                    <NumberBox Name="GridSizeBox" Minimum="5" Maximum="50" Value="20" SpinButtonPlacementMode="Compact"/>
                                </StackPanel>
                            </StackPanel>
                        </ScrollViewer>
                    </Border>

                    <Border Grid.Row="2" Grid.ColumnSpan="3" Background="{ThemeResource CardBackgroundFillColorDefaultBrush}" BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}" BorderThickness="0,1,0,0" Padding="16,8,16,8">
                        <Grid>
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="Auto"/>
                                <ColumnDefinition Width="Auto"/>
                            </Grid.ColumnDefinitions>
                            <TextBlock Name="StatusText" Text="Ready" VerticalAlignment="Center"/>
                            <TextBlock Name="ZoomText" Grid.Column="1" Text="100%" Margin="16,0" VerticalAlignment="Center"/>
                            <TextBlock Name="ElementCountText" Grid.Column="2" Text="0 nodes, 0 edges" VerticalAlignment="Center"/>
                        </Grid>
                    </Border>
                </Grid>
            </Page>
            """;
    }

    /// <summary>
    /// Finds all named elements in the logical tree.
    /// </summary>
    private void FindNamedElements(UIElement root)
    {
        var elements = new Dictionary<string, FrameworkElement>();
        FindAllNamedElements(root, elements);
        System.Diagnostics.Debug.WriteLine($"[FlowChart] FindNamedElements found {elements.Count} elements");

        // Toolbar
        NewButton = elements.GetValueOrDefault("NewButton") as AppBarButton ?? new AppBarButton();
        OpenButton = elements.GetValueOrDefault("OpenButton") as AppBarButton ?? new AppBarButton();
        SaveButton = elements.GetValueOrDefault("SaveButton") as AppBarButton ?? new AppBarButton();
        SaveAsButton = elements.GetValueOrDefault("SaveAsButton") as AppBarButton ?? new AppBarButton();
        UndoButton = elements.GetValueOrDefault("UndoButton") as AppBarButton ?? new AppBarButton();
        RedoButton = elements.GetValueOrDefault("RedoButton") as AppBarButton ?? new AppBarButton();
        DeleteButton = elements.GetValueOrDefault("DeleteButton") as AppBarButton ?? new AppBarButton();
        DuplicateButton = elements.GetValueOrDefault("DuplicateButton") as AppBarButton ?? new AppBarButton();
        ZoomInButton = elements.GetValueOrDefault("ZoomInButton") as AppBarButton ?? new AppBarButton();
        ZoomOutButton = elements.GetValueOrDefault("ZoomOutButton") as AppBarButton ?? new AppBarButton();
        ZoomResetButton = elements.GetValueOrDefault("ZoomResetButton") as AppBarButton ?? new AppBarButton();
        ConnectModeButton = elements.GetValueOrDefault("ConnectModeButton") as AppBarToggleButton ?? new AppBarToggleButton();
        SampleButton = elements.GetValueOrDefault("SampleButton") as AppBarButton ?? new AppBarButton();

        // Panels
        ShapesTitle = elements.GetValueOrDefault("ShapesTitle") as TextBlock ?? new TextBlock();
        ShapesPanel = elements.GetValueOrDefault("ShapesPanel") as StackPanel ?? new StackPanel();
        DiagramCanvas = elements.GetValueOrDefault("DiagramCanvas") as Canvas ?? new Canvas();
        DiagramScrollViewer = elements.GetValueOrDefault("__DiagramScrollViewer__") as ScrollViewer;
        PropertiesTitle = elements.GetValueOrDefault("PropertiesTitle") as TextBlock ?? new TextBlock();

        // Node Properties
        NodePropertiesPanel = elements.GetValueOrDefault("NodePropertiesPanel") as StackPanel ?? new StackPanel();
        NodeTextLabel = elements.GetValueOrDefault("NodeTextLabel") as TextBlock ?? new TextBlock();
        NodeTextBox = elements.GetValueOrDefault("NodeTextBox") as TextBox ?? new TextBox();
        NodeFillLabel = elements.GetValueOrDefault("NodeFillLabel") as TextBlock ?? new TextBlock();
        NodeFillColorPicker = elements.GetValueOrDefault("NodeFillColorPicker") as ColorPicker ?? new ColorPicker();
        NodeStrokeLabel = elements.GetValueOrDefault("NodeStrokeLabel") as TextBlock ?? new TextBlock();
        NodeStrokeColorPicker = elements.GetValueOrDefault("NodeStrokeColorPicker") as ColorPicker ?? new ColorPicker();
        NodeSizeLabel = elements.GetValueOrDefault("NodeSizeLabel") as TextBlock ?? new TextBlock();
        NodeWidthBox = elements.GetValueOrDefault("NodeWidthBox") as NumberBox ?? new NumberBox();
        NodeHeightBox = elements.GetValueOrDefault("NodeHeightBox") as NumberBox ?? new NumberBox();
        NodeFontLabel = elements.GetValueOrDefault("NodeFontLabel") as TextBlock ?? new TextBlock();
        NodeFontSizeBox = elements.GetValueOrDefault("NodeFontSizeBox") as NumberBox ?? new NumberBox();
        BringToFrontButton = elements.GetValueOrDefault("BringToFrontButton") as Button ?? new Button();
        SendToBackButton = elements.GetValueOrDefault("SendToBackButton") as Button ?? new Button();

        // Edge Properties
        EdgePropertiesPanel = elements.GetValueOrDefault("EdgePropertiesPanel") as StackPanel ?? new StackPanel();
        EdgeLabelTitle = elements.GetValueOrDefault("EdgeLabelTitle") as TextBlock ?? new TextBlock();
        EdgeLabelBox = elements.GetValueOrDefault("EdgeLabelBox") as TextBox ?? new TextBox();
        EdgeStyleLabel = elements.GetValueOrDefault("EdgeStyleLabel") as TextBlock ?? new TextBlock();
        EdgeStyleComboBox = elements.GetValueOrDefault("EdgeStyleComboBox") as ComboBox ?? new ComboBox();
        EdgeRoutingLabel = elements.GetValueOrDefault("EdgeRoutingLabel") as TextBlock ?? new TextBlock();
        EdgeRoutingComboBox = elements.GetValueOrDefault("EdgeRoutingComboBox") as ComboBox ?? new ComboBox();
        EdgeArrowLabel = elements.GetValueOrDefault("EdgeArrowLabel") as TextBlock ?? new TextBlock();
        EdgeArrowComboBox = elements.GetValueOrDefault("EdgeArrowComboBox") as ComboBox ?? new ComboBox();
        EdgeColorLabel = elements.GetValueOrDefault("EdgeColorLabel") as TextBlock ?? new TextBlock();
        EdgeColorPicker = elements.GetValueOrDefault("EdgeColorPicker") as ColorPicker ?? new ColorPicker();

        // Diagram Properties
        DiagramPropertiesPanel = elements.GetValueOrDefault("DiagramPropertiesPanel") as StackPanel ?? new StackPanel();
        DiagramNameLabel = elements.GetValueOrDefault("DiagramNameLabel") as TextBlock ?? new TextBlock();
        DiagramNameBox = elements.GetValueOrDefault("DiagramNameBox") as TextBox ?? new TextBox();
        DiagramDescLabel = elements.GetValueOrDefault("DiagramDescLabel") as TextBlock ?? new TextBlock();
        DiagramDescBox = elements.GetValueOrDefault("DiagramDescBox") as TextBox ?? new TextBox();
        ShowGridCheckBox = elements.GetValueOrDefault("ShowGridCheckBox") as CheckBox ?? new CheckBox();
        SnapToGridCheckBox = elements.GetValueOrDefault("SnapToGridCheckBox") as CheckBox ?? new CheckBox();
        GridSizeLabel = elements.GetValueOrDefault("GridSizeLabel") as TextBlock ?? new TextBlock();
        GridSizeBox = elements.GetValueOrDefault("GridSizeBox") as NumberBox ?? new NumberBox();

        // Status Bar
        StatusText = elements.GetValueOrDefault("StatusText") as TextBlock ?? new TextBlock();
        ZoomText = elements.GetValueOrDefault("ZoomText") as TextBlock ?? new TextBlock();
        ElementCountText = elements.GetValueOrDefault("ElementCountText") as TextBlock ?? new TextBlock();
    }

    /// <summary>
    /// Recursively finds all named elements by traversing the logical tree.
    /// Uses logical tree traversal instead of VisualTreeHelper since the visual tree
    /// may not be built yet when this is called.
    /// </summary>
    private static void FindAllNamedElements(DependencyObject parent, Dictionary<string, FrameworkElement> elements)
    {
        if (parent is FrameworkElement fe && !string.IsNullOrEmpty(fe.Name))
        {
            elements[fe.Name] = fe;
            System.Diagnostics.Debug.WriteLine($"[FlowChart] Found element: {fe.Name} ({fe.GetType().Name})");
        }

        // Traverse logical tree by checking different container types
        // Note: Order matters! More specific types must come before base types.
        // ScrollViewer extends ContentControl, so check ScrollViewer first.
        if (parent is Panel panel)
        {
            foreach (var child in panel.Children)
            {
                FindAllNamedElements(child, elements);
            }
        }
        else if (parent is ScrollViewer scrollViewer && scrollViewer.Content is DependencyObject svContent)
        {
            // Check if this ScrollViewer contains DiagramCanvas
            if (svContent is Canvas canvas && canvas.Name == "DiagramCanvas")
            {
                // Store reference to parent ScrollViewer for later use
                elements["__DiagramScrollViewer__"] = scrollViewer;
                System.Diagnostics.Debug.WriteLine("[FlowChart] Found DiagramCanvas parent ScrollViewer");
            }
            FindAllNamedElements(svContent, elements);
        }
        else if (parent is Border border && border.Child is DependencyObject borderChild)
        {
            FindAllNamedElements(borderChild, elements);
        }
        else if (parent is ContentControl contentControl && contentControl.Content is DependencyObject contentObj)
        {
            FindAllNamedElements(contentObj, elements);
        }
        else if (parent is ItemsControl itemsControl)
        {
            foreach (var item in itemsControl.Items)
            {
                if (item is DependencyObject itemObj)
                {
                    FindAllNamedElements(itemObj, elements);
                }
            }
        }

        // Also check CommandBar's commands which aren't in logical tree children
        if (parent is CommandBar commandBar)
        {
            foreach (var cmd in commandBar.PrimaryCommands)
            {
                if (cmd is FrameworkElement cmdFe && !string.IsNullOrEmpty(cmdFe.Name))
                {
                    elements[cmdFe.Name] = cmdFe;
                    System.Diagnostics.Debug.WriteLine($"[FlowChart] Found command: {cmdFe.Name} ({cmdFe.GetType().Name})");
                }
            }
            foreach (var cmd in commandBar.SecondaryCommands)
            {
                if (cmd is FrameworkElement cmdFe && !string.IsNullOrEmpty(cmdFe.Name))
                {
                    elements[cmdFe.Name] = cmdFe;
                    System.Diagnostics.Debug.WriteLine($"[FlowChart] Found secondary command: {cmdFe.Name} ({cmdFe.GetType().Name})");
                }
            }
        }
    }

    /// <summary>
    /// Initializes the FlowChartCanvas and replaces the placeholder Canvas.
    /// </summary>
    private void InitializeFlowChartCanvas()
    {
        _flowChartCanvas = new FlowChartCanvas
        {
            Width = 2000,
            Height = 2000,
            Diagram = _viewModel.Out.Diagram
        };

        // Use the stored DiagramScrollViewer reference (since Parent is null before visual tree is built)
        if (DiagramScrollViewer != null)
        {
            DiagramScrollViewer.Content = _flowChartCanvas;
            System.Diagnostics.Debug.WriteLine("[FlowChart] Replaced DiagramCanvas with FlowChartCanvas");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[FlowChart] DiagramScrollViewer is null, cannot initialize FlowChartCanvas");
        }
    }

    /// <summary>
    /// Wires up all event handlers.
    /// </summary>
    private void WireUpEvents()
    {
        // Toolbar events - with null checks for fallback UI scenario
        if (NewButton != null) NewButton.Click += OnNewClick;
        if (OpenButton != null) OpenButton.Click += OnOpenClick;
        if (SaveButton != null) SaveButton.Click += OnSaveClick;
        if (SaveAsButton != null) SaveAsButton.Click += OnSaveAsClick;
        if (UndoButton != null) UndoButton.Click += OnUndoClick;
        if (RedoButton != null) RedoButton.Click += OnRedoClick;
        if (DeleteButton != null) DeleteButton.Click += OnDeleteClick;
        if (DuplicateButton != null) DuplicateButton.Click += OnDuplicateClick;
        if (ZoomInButton != null) ZoomInButton.Click += OnZoomInClick;
        if (ZoomOutButton != null) ZoomOutButton.Click += OnZoomOutClick;
        if (ZoomResetButton != null) ZoomResetButton.Click += OnZoomResetClick;
        if (ConnectModeButton != null) ConnectModeButton.Click += OnConnectModeClick;
        if (SampleButton != null) SampleButton.Click += OnSampleClick;

        // Canvas events
        if (_flowChartCanvas != null)
        {
            _flowChartCanvas.NodeSelected += OnNodeSelected;
            _flowChartCanvas.EdgeSelected += OnEdgeSelected;
            _flowChartCanvas.NodeMoved += OnNodeMoved;
            _flowChartCanvas.ConnectionCreated += OnConnectionCreated;
        }

        // Node property events - with null checks
        if (NodeTextBox != null) NodeTextBox.TextChanged += OnNodeTextChanged;
        if (NodeFillColorPicker != null) NodeFillColorPicker.ColorChanged += OnNodeFillColorChanged;
        if (NodeStrokeColorPicker != null) NodeStrokeColorPicker.ColorChanged += OnNodeStrokeColorChanged;
        if (NodeWidthBox != null) NodeWidthBox.ValueChanged += OnNodeSizeChanged;
        if (NodeHeightBox != null) NodeHeightBox.ValueChanged += OnNodeSizeChanged;
        if (NodeFontSizeBox != null) NodeFontSizeBox.ValueChanged += OnNodeFontSizeChanged;
        if (BringToFrontButton != null) BringToFrontButton.Click += OnBringToFrontClick;
        if (SendToBackButton != null) SendToBackButton.Click += OnSendToBackClick;

        // Edge property events - with null checks
        if (EdgeLabelBox != null) EdgeLabelBox.TextChanged += OnEdgeLabelChanged;
        if (EdgeStyleComboBox != null) EdgeStyleComboBox.SelectionChanged += OnEdgeStyleChanged;
        if (EdgeRoutingComboBox != null) EdgeRoutingComboBox.SelectionChanged += OnEdgeRoutingChanged;
        if (EdgeArrowComboBox != null) EdgeArrowComboBox.SelectionChanged += OnEdgeArrowChanged;
        if (EdgeColorPicker != null) EdgeColorPicker.ColorChanged += OnEdgeColorChanged;

        // Diagram property events - with null checks
        if (DiagramNameBox != null) DiagramNameBox.TextChanged += OnDiagramNameChanged;
        if (DiagramDescBox != null) DiagramDescBox.TextChanged += OnDiagramDescChanged;
        if (ShowGridCheckBox != null) ShowGridCheckBox.Click += OnShowGridChanged;
        if (SnapToGridCheckBox != null) SnapToGridCheckBox.Click += OnSnapToGridChanged;
        if (GridSizeBox != null) GridSizeBox.ValueChanged += OnGridSizeChanged;
    }

    /// <summary>
    /// Populates the shape palette with buttons.
    /// </summary>
    private void PopulateShapePalette()
    {
        if (ShapesPanel == null)
        {
            System.Diagnostics.Debug.WriteLine("[FlowChart] ShapesPanel is null, cannot populate shape palette");
            return;
        }

        foreach (var shape in _viewModel.Out.AvailableShapes)
        {
            var btn = new Button
            {
                Content = shape.ToString(),
                Tag = shape,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 2, 0, 2)
            };
            btn.Click += OnShapeClick;
            ShapesPanel.Children.Add(btn);
        }
    }

    /// <summary>
    /// Fallback UI if XAML loading fails.
    /// </summary>
    private void BuildFallbackUI()
    {
        var errorText = new TextBlock
        {
            Text = "Failed to load FlowChart UI. Please check plugin installation.",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        Content = errorText;
    }

    /// <summary>
    /// Sets the localization service.
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
                    _viewModel.In.CreateSampleDiagram();
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

        ShapesTitle.Text = _localization.GetFromAnyPlugin("flowchart.panel.shapes");
        PropertiesTitle.Text = _localization.GetFromAnyPlugin("flowchart.panel.properties");

        NodeTextLabel.Text = _localization.GetFromAnyPlugin("flowchart.property.text");
        NodeFillLabel.Text = _localization.GetFromAnyPlugin("flowchart.property.fillcolor");
        NodeStrokeLabel.Text = _localization.GetFromAnyPlugin("flowchart.property.strokecolor");
        NodeSizeLabel.Text = _localization.GetFromAnyPlugin("flowchart.property.size");
        NodeFontLabel.Text = _localization.GetFromAnyPlugin("flowchart.property.fontsize");
        BringToFrontButton.Content = _localization.GetFromAnyPlugin("flowchart.action.bringtofront");
        SendToBackButton.Content = _localization.GetFromAnyPlugin("flowchart.action.sendtoback");

        EdgeLabelTitle.Text = _localization.GetFromAnyPlugin("flowchart.property.label");
        EdgeStyleLabel.Text = _localization.GetFromAnyPlugin("flowchart.property.linestyle");
        EdgeRoutingLabel.Text = _localization.GetFromAnyPlugin("flowchart.property.routing");
        EdgeArrowLabel.Text = _localization.GetFromAnyPlugin("flowchart.property.arrow");
        EdgeColorLabel.Text = _localization.GetFromAnyPlugin("flowchart.property.color");

        DiagramNameLabel.Text = _localization.GetFromAnyPlugin("flowchart.property.name");
        DiagramDescLabel.Text = _localization.GetFromAnyPlugin("flowchart.property.description");
        ShowGridCheckBox.Content = _localization.GetFromAnyPlugin("flowchart.property.showgrid");
        SnapToGridCheckBox.Content = _localization.GetFromAnyPlugin("flowchart.property.snaptogrid");
        GridSizeLabel.Text = _localization.GetFromAnyPlugin("flowchart.property.gridsize");
    }

    #region Toolbar Commands

    private void OnNewClick(object sender, RoutedEventArgs e)
    {
        _viewModel.In.NewDiagram();
        RefreshCanvas();
        UpdateStatusBar();
    }

    private async void OnOpenClick(object sender, RoutedEventArgs e)
    {
        await OpenDiagramAsync();
    }

    private async Task OpenDiagramAsync()
    {
        try
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FlowChart] Error opening file: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[FlowChart] Error opening file: {ex.Message}");
        }
    }

    private IntPtr GetWindowHandle()
    {
        return GetActiveWindow();
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    private async Task LoadDiagramFromFileAsync(string filePath)
    {
        await _viewModel.In.LoadFromFile(filePath);
        RefreshCanvas();
        UpdateStatusBar();
        UpdatePropertiesPanel();
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(_viewModel.Out.CurrentFilePath))
            {
                await SaveDiagramAsAsync();
            }
            else
            {
                await _viewModel.In.SaveToFile(_viewModel.Out.CurrentFilePath);
                UpdateStatusBar();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FlowChart] Error saving: {ex.Message}");
        }
    }

    private async void OnSaveAsClick(object sender, RoutedEventArgs e)
    {
        await SaveDiagramAsAsync();
    }

    private async Task SaveDiagramAsAsync()
    {
        try
        {
            var picker = new FileSavePicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add("Arcana FlowChart", new[] { ".afc" });
            picker.FileTypeChoices.Add("Draw.io", new[] { ".drawio" });
            picker.FileTypeChoices.Add("JSON", new[] { ".json" });
            picker.SuggestedFileName = _viewModel.Out.Diagram.Name;

            var windowHandle = GetWindowHandle();
            if (windowHandle != IntPtr.Zero)
            {
                WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);
            }

            var file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                await _viewModel.In.SaveToFile(file.Path);
                UpdateStatusBar();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FlowChart] Error saving file: {ex.Message}");
        }
    }

    private void OnUndoClick(object sender, RoutedEventArgs e)
    {
        _viewModel.In.Undo();
        RefreshCanvas();
        UpdateStatusBar();
    }

    private void OnRedoClick(object sender, RoutedEventArgs e)
    {
        _viewModel.In.Redo();
        RefreshCanvas();
        UpdateStatusBar();
    }

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        _viewModel.In.DeleteSelected();
        RefreshCanvas();
        UpdateStatusBar();
        UpdatePropertiesPanel();
    }

    private void OnDuplicateClick(object sender, RoutedEventArgs e)
    {
        _viewModel.In.DuplicateSelected();
        RefreshCanvas();
        UpdateStatusBar();
    }

    private void OnZoomInClick(object sender, RoutedEventArgs e)
    {
        _viewModel.In.ZoomIn();
        if (_flowChartCanvas != null)
            _flowChartCanvas.ZoomLevel = _viewModel.Out.ZoomLevel;
        UpdateStatusBar();
    }

    private void OnZoomOutClick(object sender, RoutedEventArgs e)
    {
        _viewModel.In.ZoomOut();
        if (_flowChartCanvas != null)
            _flowChartCanvas.ZoomLevel = _viewModel.Out.ZoomLevel;
        UpdateStatusBar();
    }

    private void OnZoomResetClick(object sender, RoutedEventArgs e)
    {
        _viewModel.In.ZoomReset();
        if (_flowChartCanvas != null)
            _flowChartCanvas.ZoomLevel = _viewModel.Out.ZoomLevel;
        UpdateStatusBar();
    }

    private void OnConnectModeClick(object sender, RoutedEventArgs e)
    {
        _viewModel.In.SetConnectMode(ConnectModeButton.IsChecked ?? false);
        if (_flowChartCanvas != null)
            _flowChartCanvas.IsConnectMode = _viewModel.Out.IsConnectMode;
        RefreshCanvas();
    }

    private void OnSampleClick(object sender, RoutedEventArgs e)
    {
        _viewModel.In.CreateSampleDiagram();
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
            _viewModel.In.AddNode(shape);
            RefreshCanvas();
            UpdateStatusBar();
        }
    }

    #endregion

    #region Canvas Events

    private void OnNodeSelected(object sender, NodeSelectedEventArgs e)
    {
        _viewModel.In.SelectNode(e.Node?.Id);
        UpdatePropertiesPanel();
    }

    private void OnEdgeSelected(object sender, EdgeSelectedEventArgs e)
    {
        _viewModel.In.SelectEdge(e.Edge?.Id);
        UpdatePropertiesPanel();
    }

    private void OnNodeMoved(object sender, NodeMovedEventArgs e)
    {
        _viewModel.In.UpdateNodePosition(e.Node.Id, e.X, e.Y);
        UpdateStatusBar();
    }

    private void OnConnectionCreated(object sender, ConnectionCreatedEventArgs e)
    {
        _viewModel.In.AddConnection(e.SourceNodeId, e.TargetNodeId, e.SourcePoint, e.TargetPoint);
        RefreshCanvas();
        UpdateStatusBar();
    }

    #endregion

    #region Properties Panel

    private void UpdatePropertiesPanel()
    {
        _isUpdatingProperties = true;

        var selectedNode = _viewModel.Out.SelectedNode;
        var selectedEdge = _viewModel.Out.SelectedEdge;

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

            DiagramNameBox.Text = _viewModel.Out.Diagram.Name;
            DiagramDescBox.Text = _viewModel.Out.Diagram.Description;
            ShowGridCheckBox.IsChecked = _viewModel.Out.Diagram.ShowGrid;
            SnapToGridCheckBox.IsChecked = _viewModel.Out.Diagram.SnapToGrid;
            GridSizeBox.Value = _viewModel.Out.Diagram.GridSize;
        }

        _isUpdatingProperties = false;
    }

    private void OnNodeTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingProperties || _viewModel.Out.SelectedNode == null) return;
        _viewModel.Out.SelectedNode.Text = NodeTextBox.Text;
        RefreshCanvas();
    }

    private void OnNodeFillColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        if (_isUpdatingProperties || _viewModel.Out.SelectedNode == null) return;
        _viewModel.Out.SelectedNode.FillColor = ColorToHex(args.NewColor);
        RefreshCanvas();
    }

    private void OnNodeStrokeColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        if (_isUpdatingProperties || _viewModel.Out.SelectedNode == null) return;
        _viewModel.Out.SelectedNode.StrokeColor = ColorToHex(args.NewColor);
        RefreshCanvas();
    }

    private void OnNodeSizeChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_isUpdatingProperties || _viewModel.Out.SelectedNode == null) return;
        _viewModel.In.UpdateNodeSize(_viewModel.Out.SelectedNode.Id, NodeWidthBox.Value, NodeHeightBox.Value);
        RefreshCanvas();
    }

    private void OnNodeFontSizeChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_isUpdatingProperties || _viewModel.Out.SelectedNode == null) return;
        _viewModel.Out.SelectedNode.FontSize = args.NewValue;
        RefreshCanvas();
    }

    private void OnBringToFrontClick(object sender, RoutedEventArgs e)
    {
        _viewModel.In.BringToFront();
        RefreshCanvas();
    }

    private void OnSendToBackClick(object sender, RoutedEventArgs e)
    {
        _viewModel.In.SendToBack();
        RefreshCanvas();
    }

    private void OnEdgeLabelChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingProperties || _viewModel.Out.SelectedEdge == null) return;
        _viewModel.Out.SelectedEdge.Label = EdgeLabelBox.Text;
        RefreshCanvas();
    }

    private void OnEdgeStyleChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingProperties || _viewModel.Out.SelectedEdge == null) return;
        _viewModel.Out.SelectedEdge.Style = (LineStyle)EdgeStyleComboBox.SelectedIndex;
        RefreshCanvas();
    }

    private void OnEdgeRoutingChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingProperties || _viewModel.Out.SelectedEdge == null) return;
        _viewModel.Out.SelectedEdge.Routing = (RoutingStyle)EdgeRoutingComboBox.SelectedIndex;
        RefreshCanvas();
    }

    private void OnEdgeArrowChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingProperties || _viewModel.Out.SelectedEdge == null) return;
        _viewModel.Out.SelectedEdge.TargetArrow = (ArrowType)EdgeArrowComboBox.SelectedIndex;
        RefreshCanvas();
    }

    private void OnEdgeColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        if (_isUpdatingProperties || _viewModel.Out.SelectedEdge == null) return;
        _viewModel.Out.SelectedEdge.StrokeColor = ColorToHex(args.NewColor);
        RefreshCanvas();
    }

    private void OnDiagramNameChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingProperties) return;
        _viewModel.Out.Diagram.Name = DiagramNameBox.Text;
    }

    private void OnDiagramDescChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingProperties) return;
        _viewModel.Out.Diagram.Description = DiagramDescBox.Text;
    }

    private void OnShowGridChanged(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingProperties) return;
        _viewModel.Out.Diagram.ShowGrid = ShowGridCheckBox.IsChecked ?? true;
        RefreshCanvas();
    }

    private void OnSnapToGridChanged(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingProperties) return;
        _viewModel.Out.Diagram.SnapToGrid = SnapToGridCheckBox.IsChecked ?? true;
    }

    private void OnGridSizeChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_isUpdatingProperties) return;
        _viewModel.Out.Diagram.GridSize = args.NewValue;
        RefreshCanvas();
    }

    #endregion

    #region Helpers

    private void RefreshCanvas()
    {
        if (_flowChartCanvas != null)
        {
            _flowChartCanvas.Diagram = _viewModel.Out.Diagram;
            _flowChartCanvas.RefreshDiagram();
        }
    }

    private void UpdateStatusBar()
    {
        StatusText.Text = _viewModel.Out.StatusMessage;
        ZoomText.Text = $"{_viewModel.Out.ZoomLevel * 100:F0}%";
        ElementCountText.Text = $"{_viewModel.Out.Diagram.Nodes.Count} nodes, {_viewModel.Out.Diagram.Edges.Count} edges";
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
