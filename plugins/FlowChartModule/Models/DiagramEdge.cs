using System.Text.Json.Serialization;

namespace Arcana.Plugin.FlowChart.Models;

/// <summary>
/// Represents a connection/edge between two nodes in the diagram.
/// </summary>
public class DiagramEdge
{
    /// <summary>
    /// Unique identifier for the edge.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Source node ID.
    /// </summary>
    public string SourceNodeId { get; set; } = string.Empty;

    /// <summary>
    /// Target node ID.
    /// </summary>
    public string TargetNodeId { get; set; } = string.Empty;

    /// <summary>
    /// Source connection point on the node.
    /// </summary>
    public ConnectionPoint SourcePoint { get; set; } = ConnectionPoint.Right;

    /// <summary>
    /// Target connection point on the node.
    /// </summary>
    public ConnectionPoint TargetPoint { get; set; } = ConnectionPoint.Left;

    /// <summary>
    /// Line style.
    /// </summary>
    public LineStyle Style { get; set; } = LineStyle.Solid;

    /// <summary>
    /// Arrow type at the source.
    /// </summary>
    public ArrowType SourceArrow { get; set; } = ArrowType.None;

    /// <summary>
    /// Arrow type at the target.
    /// </summary>
    public ArrowType TargetArrow { get; set; } = ArrowType.Arrow;

    /// <summary>
    /// Routing style for the connection.
    /// </summary>
    public RoutingStyle Routing { get; set; } = RoutingStyle.Orthogonal;

    /// <summary>
    /// Stroke color (hex format).
    /// </summary>
    public string StrokeColor { get; set; } = "#000000";

    /// <summary>
    /// Stroke width.
    /// </summary>
    public double StrokeWidth { get; set; } = 2;

    /// <summary>
    /// Label text on the edge.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Label position (0.0 to 1.0, where 0.5 is center).
    /// </summary>
    public double LabelPosition { get; set; } = 0.5;

    /// <summary>
    /// Waypoints for curved or custom routing.
    /// </summary>
    public List<Point> Waypoints { get; set; } = new();

    /// <summary>
    /// Z-index for layering.
    /// </summary>
    public int ZIndex { get; set; }

    /// <summary>
    /// Whether the edge is currently selected.
    /// </summary>
    [JsonIgnore]
    public bool IsSelected { get; set; }

    /// <summary>
    /// Custom metadata/properties.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Connection points on a node.
/// </summary>
public enum ConnectionPoint
{
    Top,
    Right,
    Bottom,
    Left,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    Center
}

/// <summary>
/// Line styles for edges.
/// </summary>
public enum LineStyle
{
    Solid,
    Dashed,
    Dotted,
    DashDot
}

/// <summary>
/// Arrow types for edge endpoints.
/// </summary>
public enum ArrowType
{
    None,
    Arrow,
    OpenArrow,
    Diamond,
    Circle,
    Square
}

/// <summary>
/// Routing styles for edges.
/// </summary>
public enum RoutingStyle
{
    /// <summary>
    /// Straight line.
    /// </summary>
    Direct,

    /// <summary>
    /// Orthogonal (right-angle) routing.
    /// </summary>
    Orthogonal,

    /// <summary>
    /// Curved/bezier routing.
    /// </summary>
    Curved,

    /// <summary>
    /// Entity relationship style.
    /// </summary>
    EntityRelation
}

/// <summary>
/// Represents a 2D point.
/// </summary>
public struct Point
{
    public double X { get; set; }
    public double Y { get; set; }

    public Point(double x, double y)
    {
        X = x;
        Y = y;
    }
}
