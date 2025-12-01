using System.Text.Json.Serialization;

namespace Arcana.Plugin.FlowChart.Models;

/// <summary>
/// Represents a node (shape) in the flowchart diagram.
/// </summary>
public class DiagramNode
{
    /// <summary>
    /// Unique identifier for the node.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The type of shape.
    /// </summary>
    public NodeShape Shape { get; set; } = NodeShape.Rectangle;

    /// <summary>
    /// X position on the canvas.
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y position on the canvas.
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Width of the node.
    /// </summary>
    public double Width { get; set; } = 120;

    /// <summary>
    /// Height of the node.
    /// </summary>
    public double Height { get; set; } = 60;

    /// <summary>
    /// Text label displayed in the node.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Fill color (hex format, e.g., "#FFFFFF").
    /// </summary>
    public string FillColor { get; set; } = "#FFFFFF";

    /// <summary>
    /// Border/stroke color (hex format).
    /// </summary>
    public string StrokeColor { get; set; } = "#000000";

    /// <summary>
    /// Border/stroke width.
    /// </summary>
    public double StrokeWidth { get; set; } = 2;

    /// <summary>
    /// Text color (hex format).
    /// </summary>
    public string TextColor { get; set; } = "#000000";

    /// <summary>
    /// Font size for the text.
    /// </summary>
    public double FontSize { get; set; } = 14;

    /// <summary>
    /// Z-index for layering.
    /// </summary>
    public int ZIndex { get; set; }

    /// <summary>
    /// Whether the node is currently selected.
    /// </summary>
    [JsonIgnore]
    public bool IsSelected { get; set; }

    /// <summary>
    /// Custom metadata/properties.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Creates a deep copy of this node.
    /// </summary>
    public DiagramNode Clone()
    {
        return new DiagramNode
        {
            Id = Guid.NewGuid().ToString(),
            Shape = Shape,
            X = X + 20,
            Y = Y + 20,
            Width = Width,
            Height = Height,
            Text = Text,
            FillColor = FillColor,
            StrokeColor = StrokeColor,
            StrokeWidth = StrokeWidth,
            TextColor = TextColor,
            FontSize = FontSize,
            ZIndex = ZIndex,
            Metadata = new Dictionary<string, string>(Metadata)
        };
    }
}

/// <summary>
/// Available node shapes for flowcharts.
/// </summary>
public enum NodeShape
{
    /// <summary>
    /// Rectangle - Process step.
    /// </summary>
    Rectangle,

    /// <summary>
    /// Rounded rectangle - Alternative process.
    /// </summary>
    RoundedRectangle,

    /// <summary>
    /// Diamond - Decision point.
    /// </summary>
    Diamond,

    /// <summary>
    /// Ellipse/Oval - Start/End terminal.
    /// </summary>
    Ellipse,

    /// <summary>
    /// Parallelogram - Input/Output.
    /// </summary>
    Parallelogram,

    /// <summary>
    /// Hexagon - Preparation.
    /// </summary>
    Hexagon,

    /// <summary>
    /// Cylinder - Database/Storage.
    /// </summary>
    Cylinder,

    /// <summary>
    /// Document shape.
    /// </summary>
    Document,

    /// <summary>
    /// Cloud shape.
    /// </summary>
    Cloud,

    /// <summary>
    /// Triangle.
    /// </summary>
    Triangle,

    /// <summary>
    /// Pentagon.
    /// </summary>
    Pentagon,

    /// <summary>
    /// Star shape.
    /// </summary>
    Star,

    /// <summary>
    /// Arrow shape.
    /// </summary>
    Arrow,

    /// <summary>
    /// Callout/Comment.
    /// </summary>
    Callout
}
