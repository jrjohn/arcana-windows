namespace Arcana.Plugin.FlowChart.Models;

/// <summary>
/// Represents a complete flowchart diagram.
/// </summary>
public class Diagram
{
    /// <summary>
    /// Unique identifier for the diagram.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Diagram name/title.
    /// </summary>
    public string Name { get; set; } = "Untitled Diagram";

    /// <summary>
    /// Diagram description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// File format version for compatibility.
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last modified timestamp.
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Author/creator name.
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// Canvas width.
    /// </summary>
    public double CanvasWidth { get; set; } = 2000;

    /// <summary>
    /// Canvas height.
    /// </summary>
    public double CanvasHeight { get; set; } = 2000;

    /// <summary>
    /// Background color (hex format).
    /// </summary>
    public string BackgroundColor { get; set; } = "#FFFFFF";

    /// <summary>
    /// Grid size for snapping.
    /// </summary>
    public double GridSize { get; set; } = 20;

    /// <summary>
    /// Whether to show grid.
    /// </summary>
    public bool ShowGrid { get; set; } = true;

    /// <summary>
    /// Whether to snap to grid.
    /// </summary>
    public bool SnapToGrid { get; set; } = true;

    /// <summary>
    /// All nodes in the diagram.
    /// </summary>
    public List<DiagramNode> Nodes { get; set; } = new();

    /// <summary>
    /// All edges/connections in the diagram.
    /// </summary>
    public List<DiagramEdge> Edges { get; set; } = new();

    /// <summary>
    /// Custom metadata/properties.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Gets a node by ID.
    /// </summary>
    public DiagramNode? GetNode(string id) => Nodes.FirstOrDefault(n => n.Id == id);

    /// <summary>
    /// Gets an edge by ID.
    /// </summary>
    public DiagramEdge? GetEdge(string id) => Edges.FirstOrDefault(e => e.Id == id);

    /// <summary>
    /// Gets all edges connected to a node.
    /// </summary>
    public IEnumerable<DiagramEdge> GetEdgesForNode(string nodeId)
    {
        return Edges.Where(e => e.SourceNodeId == nodeId || e.TargetNodeId == nodeId);
    }

    /// <summary>
    /// Adds a node to the diagram.
    /// </summary>
    public void AddNode(DiagramNode node)
    {
        Nodes.Add(node);
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Removes a node and all its connected edges.
    /// </summary>
    public void RemoveNode(string nodeId)
    {
        Nodes.RemoveAll(n => n.Id == nodeId);
        Edges.RemoveAll(e => e.SourceNodeId == nodeId || e.TargetNodeId == nodeId);
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds an edge to the diagram.
    /// </summary>
    public void AddEdge(DiagramEdge edge)
    {
        Edges.Add(edge);
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Removes an edge from the diagram.
    /// </summary>
    public void RemoveEdge(string edgeId)
    {
        Edges.RemoveAll(e => e.Id == edgeId);
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the next Z-index value.
    /// </summary>
    public int GetNextZIndex()
    {
        var maxNodeZ = Nodes.Count > 0 ? Nodes.Max(n => n.ZIndex) : 0;
        var maxEdgeZ = Edges.Count > 0 ? Edges.Max(e => e.ZIndex) : 0;
        return Math.Max(maxNodeZ, maxEdgeZ) + 1;
    }

    /// <summary>
    /// Creates a new empty diagram.
    /// </summary>
    public static Diagram CreateNew(string name = "Untitled Diagram")
    {
        return new Diagram
        {
            Name = name,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a sample flowchart diagram.
    /// </summary>
    public static Diagram CreateSample()
    {
        var diagram = new Diagram
        {
            Name = "Sample Flowchart",
            Description = "A sample flowchart demonstrating various shapes and connections"
        };

        // Start node
        var start = new DiagramNode
        {
            Shape = NodeShape.Ellipse,
            X = 400,
            Y = 50,
            Width = 100,
            Height = 50,
            Text = "Start",
            FillColor = "#90EE90",
            ZIndex = 1
        };
        diagram.AddNode(start);

        // Process 1
        var process1 = new DiagramNode
        {
            Shape = NodeShape.Rectangle,
            X = 375,
            Y = 150,
            Width = 150,
            Height = 60,
            Text = "Process Step 1",
            FillColor = "#87CEEB",
            ZIndex = 2
        };
        diagram.AddNode(process1);

        // Decision
        var decision = new DiagramNode
        {
            Shape = NodeShape.Diamond,
            X = 375,
            Y = 260,
            Width = 150,
            Height = 100,
            Text = "Decision?",
            FillColor = "#FFD700",
            ZIndex = 3
        };
        diagram.AddNode(decision);

        // Process 2 (Yes branch)
        var process2 = new DiagramNode
        {
            Shape = NodeShape.Rectangle,
            X = 200,
            Y = 410,
            Width = 150,
            Height = 60,
            Text = "Process Step 2",
            FillColor = "#87CEEB",
            ZIndex = 4
        };
        diagram.AddNode(process2);

        // Process 3 (No branch)
        var process3 = new DiagramNode
        {
            Shape = NodeShape.Rectangle,
            X = 550,
            Y = 410,
            Width = 150,
            Height = 60,
            Text = "Process Step 3",
            FillColor = "#87CEEB",
            ZIndex = 5
        };
        diagram.AddNode(process3);

        // End node
        var end = new DiagramNode
        {
            Shape = NodeShape.Ellipse,
            X = 400,
            Y = 520,
            Width = 100,
            Height = 50,
            Text = "End",
            FillColor = "#FFB6C1",
            ZIndex = 6
        };
        diagram.AddNode(end);

        // Connections
        diagram.AddEdge(new DiagramEdge
        {
            SourceNodeId = start.Id,
            TargetNodeId = process1.Id,
            SourcePoint = ConnectionPoint.Bottom,
            TargetPoint = ConnectionPoint.Top
        });

        diagram.AddEdge(new DiagramEdge
        {
            SourceNodeId = process1.Id,
            TargetNodeId = decision.Id,
            SourcePoint = ConnectionPoint.Bottom,
            TargetPoint = ConnectionPoint.Top
        });

        diagram.AddEdge(new DiagramEdge
        {
            SourceNodeId = decision.Id,
            TargetNodeId = process2.Id,
            SourcePoint = ConnectionPoint.Left,
            TargetPoint = ConnectionPoint.Top,
            Label = "Yes"
        });

        diagram.AddEdge(new DiagramEdge
        {
            SourceNodeId = decision.Id,
            TargetNodeId = process3.Id,
            SourcePoint = ConnectionPoint.Right,
            TargetPoint = ConnectionPoint.Top,
            Label = "No"
        });

        diagram.AddEdge(new DiagramEdge
        {
            SourceNodeId = process2.Id,
            TargetNodeId = end.Id,
            SourcePoint = ConnectionPoint.Bottom,
            TargetPoint = ConnectionPoint.Left
        });

        diagram.AddEdge(new DiagramEdge
        {
            SourceNodeId = process3.Id,
            TargetNodeId = end.Id,
            SourcePoint = ConnectionPoint.Bottom,
            TargetPoint = ConnectionPoint.Right
        });

        return diagram;
    }
}
