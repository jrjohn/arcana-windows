using System.Collections.ObjectModel;
using System.Windows.Input;
using Arcana.Plugin.FlowChart.Models;
using Arcana.Plugin.FlowChart.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Arcana.Plugin.FlowChart.ViewModels;

/// <summary>
/// ViewModel for the FlowChart editor.
/// </summary>
public partial class FlowChartEditorViewModel : ObservableObject
{
    private readonly DiagramSerializer _serializer = new();
    private readonly Stack<string> _undoStack = new();
    private readonly Stack<string> _redoStack = new();
    private string? _currentFilePath;
    private bool _isModified;

    public FlowChartEditorViewModel()
    {
        Diagram = Diagram.CreateNew();
        AvailableShapes = Enum.GetValues<NodeShape>().ToList();
        SelectedShape = NodeShape.Rectangle;

        // Initialize commands
        NewCommand = new RelayCommand(NewDiagram);
        OpenCommand = new AsyncRelayCommand(OpenDiagramAsync);
        SaveCommand = new AsyncRelayCommand(SaveDiagramAsync);
        SaveAsCommand = new AsyncRelayCommand(SaveDiagramAsAsync);
        UndoCommand = new RelayCommand(Undo, CanUndo);
        RedoCommand = new RelayCommand(Redo, CanRedo);
        DeleteSelectedCommand = new RelayCommand(DeleteSelected, () => SelectedNode != null || SelectedEdge != null);
        DuplicateSelectedCommand = new RelayCommand(DuplicateSelected, () => SelectedNode != null);
        ZoomInCommand = new RelayCommand(() => ZoomLevel = Math.Min(ZoomLevel + 0.1, 3.0));
        ZoomOutCommand = new RelayCommand(() => ZoomLevel = Math.Max(ZoomLevel - 0.1, 0.1));
        ZoomResetCommand = new RelayCommand(() => ZoomLevel = 1.0);
        CreateSampleCommand = new RelayCommand(CreateSampleDiagram);
        AddNodeCommand = new RelayCommand<NodeShape?>(AddNode);
        BringToFrontCommand = new RelayCommand(BringToFront, () => SelectedNode != null);
        SendToBackCommand = new RelayCommand(SendToBack, () => SelectedNode != null);
    }

    #region Properties

    [ObservableProperty]
    private Diagram _diagram;

    [ObservableProperty]
    private DiagramNode? _selectedNode;

    [ObservableProperty]
    private DiagramEdge? _selectedEdge;

    [ObservableProperty]
    private NodeShape _selectedShape;

    [ObservableProperty]
    private double _zoomLevel = 1.0;

    [ObservableProperty]
    private bool _isConnectMode;

    [ObservableProperty]
    private bool _isPanMode;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string _title = "Untitled - FlowChart Editor";

    public List<NodeShape> AvailableShapes { get; }

    public bool IsModified
    {
        get => _isModified;
        private set
        {
            if (SetProperty(ref _isModified, value))
            {
                UpdateTitle();
            }
        }
    }

    public string? CurrentFilePath
    {
        get => _currentFilePath;
        private set
        {
            if (SetProperty(ref _currentFilePath, value))
            {
                UpdateTitle();
            }
        }
    }

    #endregion

    #region Commands

    public ICommand NewCommand { get; }
    public ICommand OpenCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand SaveAsCommand { get; }
    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }
    public ICommand DeleteSelectedCommand { get; }
    public ICommand DuplicateSelectedCommand { get; }
    public ICommand ZoomInCommand { get; }
    public ICommand ZoomOutCommand { get; }
    public ICommand ZoomResetCommand { get; }
    public ICommand CreateSampleCommand { get; }
    public ICommand AddNodeCommand { get; }
    public ICommand BringToFrontCommand { get; }
    public ICommand SendToBackCommand { get; }

    #endregion

    #region Command Implementations

    private void NewDiagram()
    {
        SaveUndoState();
        Diagram = Diagram.CreateNew();
        CurrentFilePath = null;
        IsModified = false;
        ClearSelection();
        StatusMessage = "New diagram created";
    }

    private async Task OpenDiagramAsync()
    {
        try
        {
            // In a real implementation, this would use a file picker dialog
            // For now, we'll use a placeholder that can be implemented with WinUI FileOpenPicker
            StatusMessage = "Opening diagram...";

            // TODO: Implement with FileOpenPicker
            // var picker = new FileOpenPicker();
            // picker.FileTypeFilter.Add(".afc");
            // picker.FileTypeFilter.Add(".drawio");
            // picker.FileTypeFilter.Add(".json");
            // var file = await picker.PickSingleFileAsync();

            StatusMessage = "Use File Picker to open diagram";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error opening file: {ex.Message}";
        }
    }

    private async Task SaveDiagramAsync()
    {
        if (string.IsNullOrEmpty(CurrentFilePath))
        {
            await SaveDiagramAsAsync();
            return;
        }

        try
        {
            var format = DiagramSerializer.GetFormatFromExtension(CurrentFilePath);
            await _serializer.SaveToFileAsync(Diagram, CurrentFilePath, format);
            IsModified = false;
            StatusMessage = $"Saved to {Path.GetFileName(CurrentFilePath)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving file: {ex.Message}";
        }
    }

    private async Task SaveDiagramAsAsync()
    {
        try
        {
            // TODO: Implement with FileSavePicker
            // var picker = new FileSavePicker();
            // picker.FileTypeChoices.Add("Arcana FlowChart", new[] { ".afc" });
            // picker.FileTypeChoices.Add("Draw.io", new[] { ".drawio" });
            // picker.FileTypeChoices.Add("JSON", new[] { ".json" });
            // picker.SuggestedFileName = Diagram.Name;
            // var file = await picker.PickSaveFileAsync();

            StatusMessage = "Use File Picker to save diagram";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving file: {ex.Message}";
        }
    }

    /// <summary>
    /// Loads a diagram from the specified file path.
    /// </summary>
    public async Task LoadFromFileAsync(string filePath)
    {
        try
        {
            SaveUndoState();
            var diagram = await _serializer.LoadFromFileAsync(filePath);
            if (diagram != null)
            {
                Diagram = diagram;
                CurrentFilePath = filePath;
                IsModified = false;
                ClearSelection();
                StatusMessage = $"Loaded {Path.GetFileName(filePath)}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading file: {ex.Message}";
        }
    }

    /// <summary>
    /// Saves the diagram to the specified file path.
    /// </summary>
    public async Task SaveToFileAsync(string filePath)
    {
        try
        {
            var format = DiagramSerializer.GetFormatFromExtension(filePath);
            await _serializer.SaveToFileAsync(Diagram, filePath, format);
            CurrentFilePath = filePath;
            IsModified = false;
            StatusMessage = $"Saved to {Path.GetFileName(filePath)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving file: {ex.Message}";
        }
    }

    private void Undo()
    {
        if (_undoStack.Count == 0) return;

        var currentState = _serializer.SerializeToJson(Diagram);
        _redoStack.Push(currentState);

        var previousState = _undoStack.Pop();
        var diagram = _serializer.DeserializeFromJson(previousState);
        if (diagram != null)
        {
            Diagram = diagram;
            ClearSelection();
            StatusMessage = "Undo";
        }
    }

    private bool CanUndo() => _undoStack.Count > 0;

    private void Redo()
    {
        if (_redoStack.Count == 0) return;

        var currentState = _serializer.SerializeToJson(Diagram);
        _undoStack.Push(currentState);

        var nextState = _redoStack.Pop();
        var diagram = _serializer.DeserializeFromJson(nextState);
        if (diagram != null)
        {
            Diagram = diagram;
            ClearSelection();
            StatusMessage = "Redo";
        }
    }

    private bool CanRedo() => _redoStack.Count > 0;

    private void DeleteSelected()
    {
        SaveUndoState();

        if (SelectedNode != null)
        {
            Diagram.RemoveNode(SelectedNode.Id);
            SelectedNode = null;
            StatusMessage = "Node deleted";
        }
        else if (SelectedEdge != null)
        {
            Diagram.RemoveEdge(SelectedEdge.Id);
            SelectedEdge = null;
            StatusMessage = "Connection deleted";
        }

        IsModified = true;
        OnPropertyChanged(nameof(Diagram));
    }

    private void DuplicateSelected()
    {
        if (SelectedNode == null) return;

        SaveUndoState();
        var clone = SelectedNode.Clone();
        clone.ZIndex = Diagram.GetNextZIndex();
        Diagram.AddNode(clone);
        SelectedNode = clone;
        IsModified = true;
        StatusMessage = "Node duplicated";
        OnPropertyChanged(nameof(Diagram));
    }

    private void AddNode(NodeShape? shape)
    {
        SaveUndoState();

        var actualShape = shape ?? SelectedShape;
        var node = new DiagramNode
        {
            Shape = actualShape,
            X = 100 + (Diagram.Nodes.Count * 20) % 400,
            Y = 100 + (Diagram.Nodes.Count * 20) % 300,
            Text = GetDefaultTextForShape(actualShape),
            ZIndex = Diagram.GetNextZIndex()
        };

        Diagram.AddNode(node);
        SelectedNode = node;
        IsModified = true;
        StatusMessage = $"Added {actualShape} node";
        OnPropertyChanged(nameof(Diagram));
    }

    private void BringToFront()
    {
        if (SelectedNode == null) return;

        SaveUndoState();
        SelectedNode.ZIndex = Diagram.GetNextZIndex();
        IsModified = true;
        OnPropertyChanged(nameof(Diagram));
    }

    private void SendToBack()
    {
        if (SelectedNode == null) return;

        SaveUndoState();
        var minZ = Diagram.Nodes.Min(n => n.ZIndex);
        SelectedNode.ZIndex = minZ - 1;
        IsModified = true;
        OnPropertyChanged(nameof(Diagram));
    }

    private void CreateSampleDiagram()
    {
        SaveUndoState();
        Diagram = Diagram.CreateSample();
        CurrentFilePath = null;
        IsModified = true;
        ClearSelection();
        StatusMessage = "Sample diagram created";
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Adds a connection between two nodes.
    /// </summary>
    public void AddConnection(string sourceNodeId, string targetNodeId,
        ConnectionPoint sourcePoint = ConnectionPoint.Right,
        ConnectionPoint targetPoint = ConnectionPoint.Left)
    {
        if (sourceNodeId == targetNodeId) return;

        SaveUndoState();
        var edge = new DiagramEdge
        {
            SourceNodeId = sourceNodeId,
            TargetNodeId = targetNodeId,
            SourcePoint = sourcePoint,
            TargetPoint = targetPoint,
            ZIndex = Diagram.GetNextZIndex()
        };

        Diagram.AddEdge(edge);
        SelectedEdge = edge;
        IsModified = true;
        StatusMessage = "Connection added";
        OnPropertyChanged(nameof(Diagram));
    }

    /// <summary>
    /// Updates a node's position.
    /// </summary>
    public void UpdateNodePosition(string nodeId, double x, double y)
    {
        var node = Diagram.GetNode(nodeId);
        if (node == null) return;

        // Snap to grid if enabled
        if (Diagram.SnapToGrid)
        {
            x = Math.Round(x / Diagram.GridSize) * Diagram.GridSize;
            y = Math.Round(y / Diagram.GridSize) * Diagram.GridSize;
        }

        node.X = x;
        node.Y = y;
        Diagram.ModifiedAt = DateTime.UtcNow;
        IsModified = true;
    }

    /// <summary>
    /// Updates a node's size.
    /// </summary>
    public void UpdateNodeSize(string nodeId, double width, double height)
    {
        var node = Diagram.GetNode(nodeId);
        if (node == null) return;

        node.Width = Math.Max(40, width);
        node.Height = Math.Max(30, height);
        Diagram.ModifiedAt = DateTime.UtcNow;
        IsModified = true;
    }

    /// <summary>
    /// Selects a node.
    /// </summary>
    public void SelectNode(string? nodeId)
    {
        // Deselect previous
        if (SelectedNode != null)
            SelectedNode.IsSelected = false;
        if (SelectedEdge != null)
            SelectedEdge.IsSelected = false;

        SelectedEdge = null;

        if (nodeId == null)
        {
            SelectedNode = null;
            return;
        }

        var node = Diagram.GetNode(nodeId);
        if (node != null)
        {
            node.IsSelected = true;
            SelectedNode = node;
        }
    }

    /// <summary>
    /// Selects an edge.
    /// </summary>
    public void SelectEdge(string? edgeId)
    {
        // Deselect previous
        if (SelectedNode != null)
            SelectedNode.IsSelected = false;
        if (SelectedEdge != null)
            SelectedEdge.IsSelected = false;

        SelectedNode = null;

        if (edgeId == null)
        {
            SelectedEdge = null;
            return;
        }

        var edge = Diagram.GetEdge(edgeId);
        if (edge != null)
        {
            edge.IsSelected = true;
            SelectedEdge = edge;
        }
    }

    /// <summary>
    /// Clears the current selection.
    /// </summary>
    public void ClearSelection()
    {
        SelectNode(null);
        SelectEdge(null);
    }

    #endregion

    #region Private Methods

    private void SaveUndoState()
    {
        var state = _serializer.SerializeToJson(Diagram);
        _undoStack.Push(state);
        _redoStack.Clear();

        // Limit undo history
        while (_undoStack.Count > 50)
        {
            var items = _undoStack.ToArray();
            _undoStack.Clear();
            for (int i = 0; i < items.Length - 1; i++)
                _undoStack.Push(items[i]);
        }
    }

    private void UpdateTitle()
    {
        var fileName = string.IsNullOrEmpty(CurrentFilePath)
            ? "Untitled"
            : Path.GetFileName(CurrentFilePath);
        var modified = IsModified ? " *" : "";
        Title = $"{fileName}{modified} - FlowChart Editor";
    }

    private static string GetDefaultTextForShape(NodeShape shape)
    {
        return shape switch
        {
            NodeShape.Ellipse => "Start/End",
            NodeShape.Diamond => "Decision",
            NodeShape.Parallelogram => "Input/Output",
            NodeShape.Document => "Document",
            NodeShape.Cylinder => "Database",
            NodeShape.Cloud => "Cloud",
            _ => "Process"
        };
    }

    #endregion
}
