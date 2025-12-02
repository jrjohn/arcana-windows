using Arcana.Plugin.FlowChart.Models;
using Arcana.Plugin.FlowChart.Services;
using Arcana.Plugins.Contracts.Mvvm;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Arcana.Plugin.FlowChart.ViewModels;

/// <summary>
/// ViewModel for the FlowChart editor using UDF (Unidirectional Data Flow) pattern.
/// </summary>
public partial class FlowChartEditorViewModel : ReactiveViewModelBase
{
    // ============ Dependencies ============
    private readonly DiagramSerializer _serializer = new();
    private readonly Stack<string> _undoStack = new();
    private readonly Stack<string> _redoStack = new();

    // ============ Private State ============

    [ObservableProperty]
    private Diagram _diagram = Diagram.CreateNew();

    [ObservableProperty]
    private DiagramNode? _selectedNode;

    [ObservableProperty]
    private DiagramEdge? _selectedEdge;

    [ObservableProperty]
    private NodeShape _selectedShape = NodeShape.Rectangle;

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

    [ObservableProperty]
    private string? _currentFilePath;

    [ObservableProperty]
    private bool _isModified;

    // ============ Input/Output/Effect ============
    private Input? _input;
    private Output? _output;
    private Effect? _effect;

    public Input In => _input ??= new Input(this);
    public Output Out => _output ??= new Output(this);
    public Effect Fx => _effect ??= new Effect();

    // ============ Constructor ============
    public FlowChartEditorViewModel()
    {
    }

    // ============ State Change Handlers ============
    partial void OnIsModifiedChanged(bool value) => UpdateTitle();
    partial void OnCurrentFilePathChanged(string? value) => UpdateTitle();

    // ============ Internal Actions ============

    private void NewDiagram()
    {
        SaveUndoState();
        Diagram = Diagram.CreateNew();
        CurrentFilePath = null;
        IsModified = false;
        ClearSelectionInternal();
        StatusMessage = "New diagram created";
        Fx.DiagramChanged.Emit();
    }

    private async Task OpenDiagramAsync()
    {
        StatusMessage = "Opening diagram...";
        Fx.RequestFileOpen.Emit();
    }

    private async Task SaveDiagramAsync()
    {
        if (string.IsNullOrEmpty(CurrentFilePath))
        {
            Fx.RequestFileSaveAs.Emit();
            return;
        }

        try
        {
            var format = DiagramSerializer.GetFormatFromExtension(CurrentFilePath);
            await _serializer.SaveToFileAsync(Diagram, CurrentFilePath, format);
            IsModified = false;
            StatusMessage = $"Saved to {Path.GetFileName(CurrentFilePath)}";
            Fx.ShowSuccess.Emit($"Saved to {Path.GetFileName(CurrentFilePath)}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving file: {ex.Message}";
            Fx.ShowError.Emit(ex.Message);
        }
    }

    private async Task SaveDiagramAsAsync()
    {
        Fx.RequestFileSaveAs.Emit();
    }

    internal async Task LoadFromFileAsync(string filePath)
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
                ClearSelectionInternal();
                StatusMessage = $"Loaded {Path.GetFileName(filePath)}";
                Fx.DiagramChanged.Emit();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading file: {ex.Message}";
            Fx.ShowError.Emit(ex.Message);
        }
    }

    internal async Task SaveToFileAsync(string filePath)
    {
        try
        {
            var format = DiagramSerializer.GetFormatFromExtension(filePath);
            await _serializer.SaveToFileAsync(Diagram, filePath, format);
            CurrentFilePath = filePath;
            IsModified = false;
            StatusMessage = $"Saved to {Path.GetFileName(filePath)}";
            Fx.ShowSuccess.Emit($"Saved to {Path.GetFileName(filePath)}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving file: {ex.Message}";
            Fx.ShowError.Emit(ex.Message);
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
            ClearSelectionInternal();
            StatusMessage = "Undo";
            Fx.DiagramChanged.Emit();
        }
    }

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
            ClearSelectionInternal();
            StatusMessage = "Redo";
            Fx.DiagramChanged.Emit();
        }
    }

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
        Fx.DiagramChanged.Emit();
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
        Fx.DiagramChanged.Emit();
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
        Fx.DiagramChanged.Emit();
    }

    private void BringToFront()
    {
        if (SelectedNode == null) return;

        SaveUndoState();
        SelectedNode.ZIndex = Diagram.GetNextZIndex();
        IsModified = true;
        Fx.DiagramChanged.Emit();
    }

    private void SendToBack()
    {
        if (SelectedNode == null) return;

        SaveUndoState();
        var minZ = Diagram.Nodes.Min(n => n.ZIndex);
        SelectedNode.ZIndex = minZ - 1;
        IsModified = true;
        Fx.DiagramChanged.Emit();
    }

    private void CreateSampleDiagram()
    {
        SaveUndoState();
        Diagram = Diagram.CreateSample();
        CurrentFilePath = null;
        IsModified = true;
        ClearSelectionInternal();
        StatusMessage = "Sample diagram created";
        Fx.DiagramChanged.Emit();
    }

    internal void AddConnection(string sourceNodeId, string targetNodeId,
        ConnectionPoint sourcePoint, ConnectionPoint targetPoint)
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
        Fx.DiagramChanged.Emit();
    }

    internal void UpdateNodePosition(string nodeId, double x, double y)
    {
        var node = Diagram.GetNode(nodeId);
        if (node == null) return;

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

    internal void UpdateNodeSize(string nodeId, double width, double height)
    {
        var node = Diagram.GetNode(nodeId);
        if (node == null) return;

        node.Width = Math.Max(40, width);
        node.Height = Math.Max(30, height);
        Diagram.ModifiedAt = DateTime.UtcNow;
        IsModified = true;
    }

    internal void SelectNode(string? nodeId)
    {
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

    internal void SelectEdge(string? edgeId)
    {
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

    private void ClearSelectionInternal()
    {
        SelectNode(null);
        SelectEdge(null);
    }

    // ============ Private Helpers ============

    private void SaveUndoState()
    {
        var state = _serializer.SerializeToJson(Diagram);
        _undoStack.Push(state);
        _redoStack.Clear();

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

    // ============ Disposal ============
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _effect?.Dispose();
        }
        base.Dispose(disposing);
    }

    // ============================================================
    // NESTED CLASSES: Input, Output, Effect
    // ============================================================

    #region Input

    /// <summary>
    /// Input actions - the ONLY entry point for state changes.
    /// </summary>
    public sealed class Input : IViewModelInput
    {
        private readonly FlowChartEditorViewModel _vm;

        internal Input(FlowChartEditorViewModel vm) => _vm = vm;

        // File operations
        public void NewDiagram() => _vm.NewDiagram();
        public Task OpenDiagram() => _vm.OpenDiagramAsync();
        public Task SaveDiagram() => _vm.SaveDiagramAsync();
        public Task SaveDiagramAs() => _vm.SaveDiagramAsAsync();
        public Task LoadFromFile(string filePath) => _vm.LoadFromFileAsync(filePath);
        public Task SaveToFile(string filePath) => _vm.SaveToFileAsync(filePath);

        // Edit operations
        public void Undo() => _vm.Undo();
        public void Redo() => _vm.Redo();
        public void DeleteSelected() => _vm.DeleteSelected();
        public void DuplicateSelected() => _vm.DuplicateSelected();

        // Node operations
        public void AddNode(NodeShape? shape = null) => _vm.AddNode(shape);
        public void BringToFront() => _vm.BringToFront();
        public void SendToBack() => _vm.SendToBack();
        public void UpdateNodePosition(string nodeId, double x, double y) => _vm.UpdateNodePosition(nodeId, x, y);
        public void UpdateNodeSize(string nodeId, double width, double height) => _vm.UpdateNodeSize(nodeId, width, height);

        // Connection operations
        public void AddConnection(string sourceId, string targetId,
            ConnectionPoint sourcePoint = ConnectionPoint.Right,
            ConnectionPoint targetPoint = ConnectionPoint.Left)
            => _vm.AddConnection(sourceId, targetId, sourcePoint, targetPoint);

        // Selection
        public void SelectNode(string? nodeId) => _vm.SelectNode(nodeId);
        public void SelectEdge(string? edgeId) => _vm.SelectEdge(edgeId);
        public void ClearSelection() => _vm.ClearSelectionInternal();

        // View operations
        public void ZoomIn() => _vm.ZoomLevel = Math.Min(_vm.ZoomLevel + 0.1, 3.0);
        public void ZoomOut() => _vm.ZoomLevel = Math.Max(_vm.ZoomLevel - 0.1, 0.1);
        public void ZoomReset() => _vm.ZoomLevel = 1.0;
        public void SetZoom(double level) => _vm.ZoomLevel = Math.Clamp(level, 0.1, 3.0);

        // Mode toggles
        public void ToggleConnectMode() => _vm.IsConnectMode = !_vm.IsConnectMode;
        public void TogglePanMode() => _vm.IsPanMode = !_vm.IsPanMode;
        public void SetConnectMode(bool enabled) => _vm.IsConnectMode = enabled;
        public void SetPanMode(bool enabled) => _vm.IsPanMode = enabled;

        // Shape selection
        public void SetSelectedShape(NodeShape shape) => _vm.SelectedShape = shape;

        // Sample
        public void CreateSampleDiagram() => _vm.CreateSampleDiagram();
    }

    #endregion

    #region Output

    /// <summary>
    /// Output state - read-only reactive state exposed to View.
    /// </summary>
    public sealed class Output : IViewModelOutput
    {
        private readonly FlowChartEditorViewModel _vm;

        internal Output(FlowChartEditorViewModel vm) => _vm = vm;

        // Diagram state
        public Diagram Diagram => _vm.Diagram;
        public DiagramNode? SelectedNode => _vm.SelectedNode;
        public DiagramEdge? SelectedEdge => _vm.SelectedEdge;

        // View state
        public double ZoomLevel => _vm.ZoomLevel;
        public bool IsConnectMode => _vm.IsConnectMode;
        public bool IsPanMode => _vm.IsPanMode;
        public NodeShape SelectedShape => _vm.SelectedShape;
        public List<NodeShape> AvailableShapes => Enum.GetValues<NodeShape>().ToList();

        // File state
        public string Title => _vm.Title;
        public string? CurrentFilePath => _vm.CurrentFilePath;
        public bool IsModified => _vm.IsModified;
        public string StatusMessage => _vm.StatusMessage;

        // Computed state
        public bool CanUndo => _vm._undoStack.Count > 0;
        public bool CanRedo => _vm._redoStack.Count > 0;
        public bool HasSelection => _vm.SelectedNode != null || _vm.SelectedEdge != null;
        public bool HasNodeSelection => _vm.SelectedNode != null;
        public bool HasEdgeSelection => _vm.SelectedEdge != null;
    }

    #endregion

    #region Effect

    /// <summary>
    /// Effect - one-time events for side effects.
    /// </summary>
    public sealed class Effect : IViewModelEffect, IDisposable
    {
        private bool _disposed;

        // File dialogs
        public EffectSubject RequestFileOpen { get; } = new();
        public EffectSubject RequestFileSaveAs { get; } = new();

        // Notifications
        public EffectSubject<string> ShowError { get; } = new();
        public EffectSubject<string> ShowSuccess { get; } = new();
        public EffectSubject<string> ShowInfo { get; } = new();

        // Events
        public EffectSubject DiagramChanged { get; } = new();

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            RequestFileOpen.Dispose();
            RequestFileSaveAs.Dispose();
            ShowError.Dispose();
            ShowSuccess.Dispose();
            ShowInfo.Dispose();
            DiagramChanged.Dispose();
        }
    }

    #endregion
}
