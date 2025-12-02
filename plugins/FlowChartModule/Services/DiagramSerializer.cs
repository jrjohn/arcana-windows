using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Arcana.Plugin.FlowChart.Models;

namespace Arcana.Plugin.FlowChart.Services;

/// <summary>
/// Service for serializing and deserializing diagrams to various formats.
/// </summary>
public class DiagramSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Supported file formats.
    /// </summary>
    public enum DiagramFormat
    {
        /// <summary>
        /// Arcana FlowChart JSON format (.afc)
        /// </summary>
        ArcanaJson,

        /// <summary>
        /// Draw.io XML format (.drawio)
        /// </summary>
        DrawIO,

        /// <summary>
        /// Standard JSON format (.json)
        /// </summary>
        Json
    }

    #region JSON Serialization

    /// <summary>
    /// Serializes a diagram to JSON string.
    /// </summary>
    public string SerializeToJson(Diagram diagram)
    {
        return JsonSerializer.Serialize(diagram, JsonOptions);
    }

    /// <summary>
    /// Deserializes a diagram from JSON string.
    /// </summary>
    public Diagram? DeserializeFromJson(string json)
    {
        return JsonSerializer.Deserialize<Diagram>(json, JsonOptions);
    }

    #endregion

    #region File Operations

    /// <summary>
    /// Saves a diagram to a file.
    /// </summary>
    public async Task SaveToFileAsync(Diagram diagram, string filePath, DiagramFormat format = DiagramFormat.ArcanaJson)
    {
        string content = format switch
        {
            DiagramFormat.ArcanaJson or DiagramFormat.Json => SerializeToJson(diagram),
            DiagramFormat.DrawIO => SerializeToDrawIO(diagram),
            _ => throw new ArgumentException($"Unsupported format: {format}")
        };

        await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
    }

    /// <summary>
    /// Loads a diagram from a file.
    /// </summary>
    public async Task<Diagram?> LoadFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Diagram file not found", filePath);

        var content = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            ".afc" or ".json" => DeserializeFromJson(content),
            ".drawio" or ".xml" => DeserializeFromDrawIO(content),
            _ => DeserializeFromJson(content) // Default to JSON
        };
    }

    /// <summary>
    /// Gets the file filter for open/save dialogs.
    /// </summary>
    public static string GetFileFilter()
    {
        return "Arcana FlowChart (*.afc)|*.afc|Draw.io (*.drawio)|*.drawio|JSON (*.json)|*.json|All Files (*.*)|*.*";
    }

    /// <summary>
    /// Gets the format from file extension.
    /// </summary>
    public static DiagramFormat GetFormatFromExtension(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".afc" => DiagramFormat.ArcanaJson,
            ".drawio" or ".xml" => DiagramFormat.DrawIO,
            ".json" => DiagramFormat.Json,
            _ => DiagramFormat.ArcanaJson
        };
    }

    #endregion

    #region Draw.io Format

    /// <summary>
    /// Serializes a diagram to Draw.io XML format.
    /// </summary>
    public string SerializeToDrawIO(Diagram diagram)
    {
        var mxGraphModel = new XElement("mxGraphModel",
            new XAttribute("dx", "0"),
            new XAttribute("dy", "0"),
            new XAttribute("grid", diagram.ShowGrid ? "1" : "0"),
            new XAttribute("gridSize", diagram.GridSize),
            new XAttribute("guides", "1"),
            new XAttribute("tooltips", "1"),
            new XAttribute("connect", "1"),
            new XAttribute("arrows", "1"),
            new XAttribute("fold", "1"),
            new XAttribute("page", "1"),
            new XAttribute("pageScale", "1"),
            new XAttribute("pageWidth", diagram.CanvasWidth),
            new XAttribute("pageHeight", diagram.CanvasHeight),
            new XAttribute("background", diagram.BackgroundColor)
        );

        var root = new XElement("root");

        // Add default parent cells (required by draw.io)
        root.Add(new XElement("mxCell", new XAttribute("id", "0")));
        root.Add(new XElement("mxCell", new XAttribute("id", "1"), new XAttribute("parent", "0")));

        // Add nodes
        foreach (var node in diagram.Nodes)
        {
            var style = GetDrawIOStyle(node);
            var cell = new XElement("mxCell",
                new XAttribute("id", node.Id),
                new XAttribute("value", node.Text),
                new XAttribute("style", style),
                new XAttribute("vertex", "1"),
                new XAttribute("parent", "1"),
                new XElement("mxGeometry",
                    new XAttribute("x", node.X),
                    new XAttribute("y", node.Y),
                    new XAttribute("width", node.Width),
                    new XAttribute("height", node.Height),
                    new XAttribute("as", "geometry")
                )
            );
            root.Add(cell);
        }

        // Add edges
        foreach (var edge in diagram.Edges)
        {
            var style = GetDrawIOEdgeStyle(edge);
            var geometry = new XElement("mxGeometry",
                new XAttribute("relative", "1"),
                new XAttribute("as", "geometry")
            );

            // Add waypoints if present
            if (edge.Waypoints.Count > 0)
            {
                var points = new XElement("Array", new XAttribute("as", "points"));
                foreach (var wp in edge.Waypoints)
                {
                    points.Add(new XElement("mxPoint",
                        new XAttribute("x", wp.X),
                        new XAttribute("y", wp.Y)
                    ));
                }
                geometry.Add(points);
            }

            // Add exit/entry points
            var exitX = GetExitX(edge.SourcePoint);
            var exitY = GetExitY(edge.SourcePoint);
            var entryX = GetExitX(edge.TargetPoint);
            var entryY = GetExitY(edge.TargetPoint);

            style += $";exitX={exitX};exitY={exitY};entryX={entryX};entryY={entryY}";

            var cell = new XElement("mxCell",
                new XAttribute("id", edge.Id),
                new XAttribute("value", edge.Label),
                new XAttribute("style", style),
                new XAttribute("edge", "1"),
                new XAttribute("parent", "1"),
                new XAttribute("source", edge.SourceNodeId),
                new XAttribute("target", edge.TargetNodeId),
                geometry
            );
            root.Add(cell);
        }

        mxGraphModel.Add(root);

        var mxfile = new XElement("mxfile",
            new XAttribute("host", "Arcana.FlowChart"),
            new XAttribute("modified", diagram.ModifiedAt.ToString("O")),
            new XAttribute("version", diagram.Version),
            new XElement("diagram",
                new XAttribute("id", diagram.Id),
                new XAttribute("name", diagram.Name),
                mxGraphModel
            )
        );

        return mxfile.ToString();
    }

    private static double GetExitX(ConnectionPoint point) => point switch
    {
        ConnectionPoint.Left => 0,
        ConnectionPoint.Right => 1,
        ConnectionPoint.Top => 0.5,
        ConnectionPoint.Bottom => 0.5,
        ConnectionPoint.TopLeft => 0,
        ConnectionPoint.TopRight => 1,
        ConnectionPoint.BottomLeft => 0,
        ConnectionPoint.BottomRight => 1,
        ConnectionPoint.Center => 0.5,
        _ => 0.5
    };

    private static double GetExitY(ConnectionPoint point) => point switch
    {
        ConnectionPoint.Left => 0.5,
        ConnectionPoint.Right => 0.5,
        ConnectionPoint.Top => 0,
        ConnectionPoint.Bottom => 1,
        ConnectionPoint.TopLeft => 0,
        ConnectionPoint.TopRight => 0,
        ConnectionPoint.BottomLeft => 1,
        ConnectionPoint.BottomRight => 1,
        ConnectionPoint.Center => 0.5,
        _ => 0.5
    };

    /// <summary>
    /// Deserializes a diagram from Draw.io XML format.
    /// Supports both uncompressed and compressed (base64 + deflate) formats.
    /// </summary>
    public Diagram? DeserializeFromDrawIO(string xml)
    {
        try
        {
            var doc = XDocument.Parse(xml);
            var diagram = new Diagram();

            // Check for compressed content in diagram element
            var diagramElement = doc.Descendants("diagram").FirstOrDefault();
            if (diagramElement != null && !diagramElement.HasElements)
            {
                // Content might be compressed - try to decompress
                var compressedContent = diagramElement.Value?.Trim();
                if (!string.IsNullOrEmpty(compressedContent))
                {
                    var decompressed = DecompressDrawIOContent(compressedContent);
                    if (!string.IsNullOrEmpty(decompressed))
                    {
                        // Parse the decompressed content as mxGraphModel
                        var decompressedDoc = XDocument.Parse($"<root>{decompressed}</root>");
                        var mxGraphModel = decompressedDoc.Descendants("mxGraphModel").FirstOrDefault();
                        if (mxGraphModel != null)
                        {
                            diagramElement.RemoveNodes();
                            diagramElement.Add(mxGraphModel);
                            doc = XDocument.Parse(doc.ToString());
                        }
                    }
                }
            }

            // Parse mxfile metadata
            var mxfile = doc.Element("mxfile");
            if (mxfile != null)
            {
                var modified = mxfile.Attribute("modified")?.Value;
                if (DateTime.TryParse(modified, out var modifiedDate))
                    diagram.ModifiedAt = modifiedDate;
            }

            // Parse diagram element (reuse from above, may have been updated after decompression)
            diagramElement = doc.Descendants("diagram").FirstOrDefault();
            if (diagramElement != null)
            {
                diagram.Id = diagramElement.Attribute("id")?.Value ?? Guid.NewGuid().ToString();
                diagram.Name = diagramElement.Attribute("name")?.Value ?? "Imported Diagram";
            }

            // Parse mxGraphModel
            var graphModel = doc.Descendants("mxGraphModel").FirstOrDefault();
            if (graphModel != null)
            {
                diagram.ShowGrid = graphModel.Attribute("grid")?.Value == "1";
                if (double.TryParse(graphModel.Attribute("gridSize")?.Value, out var gridSize))
                    diagram.GridSize = gridSize;
                if (double.TryParse(graphModel.Attribute("pageWidth")?.Value, out var width))
                    diagram.CanvasWidth = width;
                if (double.TryParse(graphModel.Attribute("pageHeight")?.Value, out var height))
                    diagram.CanvasHeight = height;
                diagram.BackgroundColor = graphModel.Attribute("background")?.Value ?? "#FFFFFF";
            }

            // Parse cells
            var cells = doc.Descendants("mxCell").ToList();
            var edgeCells = new List<XElement>();

            foreach (var cell in cells)
            {
                var id = cell.Attribute("id")?.Value;
                if (id == "0" || id == "1") continue; // Skip default cells

                if (cell.Attribute("vertex")?.Value == "1")
                {
                    var node = ParseDrawIONode(cell);
                    if (node != null)
                        diagram.Nodes.Add(node);
                }
                else if (cell.Attribute("edge")?.Value == "1")
                {
                    edgeCells.Add(cell);
                }
            }

            // Parse edges after nodes
            foreach (var cell in edgeCells)
            {
                var edge = ParseDrawIOEdge(cell);
                if (edge != null)
                    diagram.Edges.Add(edge);
            }

            return diagram;
        }
        catch
        {
            return null;
        }
    }

    private static DiagramNode? ParseDrawIONode(XElement cell)
    {
        var geometry = cell.Element("mxGeometry");
        if (geometry == null) return null;

        var node = new DiagramNode
        {
            Id = cell.Attribute("id")?.Value ?? Guid.NewGuid().ToString(),
            Text = cell.Attribute("value")?.Value ?? string.Empty
        };

        if (double.TryParse(geometry.Attribute("x")?.Value, out var x)) node.X = x;
        if (double.TryParse(geometry.Attribute("y")?.Value, out var y)) node.Y = y;
        if (double.TryParse(geometry.Attribute("width")?.Value, out var w)) node.Width = w;
        if (double.TryParse(geometry.Attribute("height")?.Value, out var h)) node.Height = h;

        // Parse style
        var style = cell.Attribute("style")?.Value ?? string.Empty;
        node.Shape = ParseShapeFromStyle(style);
        node.FillColor = ParseStyleValue(style, "fillColor") ?? "#FFFFFF";
        node.StrokeColor = ParseStyleValue(style, "strokeColor") ?? "#000000";

        return node;
    }

    private static DiagramEdge? ParseDrawIOEdge(XElement cell)
    {
        var edge = new DiagramEdge
        {
            Id = cell.Attribute("id")?.Value ?? Guid.NewGuid().ToString(),
            SourceNodeId = cell.Attribute("source")?.Value ?? string.Empty,
            TargetNodeId = cell.Attribute("target")?.Value ?? string.Empty,
            Label = cell.Attribute("value")?.Value ?? string.Empty
        };

        var style = cell.Attribute("style")?.Value ?? string.Empty;
        edge.StrokeColor = ParseStyleValue(style, "strokeColor") ?? "#000000";

        if (style.Contains("dashed=1"))
            edge.Style = LineStyle.Dashed;
        if (style.Contains("dashPattern=1 2"))
            edge.Style = LineStyle.Dotted;
        if (style.Contains("dashPattern=3 1 1 1"))
            edge.Style = LineStyle.DashDot;

        // Parse routing style
        if (style.Contains("edgeStyle=orthogonalEdgeStyle"))
            edge.Routing = RoutingStyle.Orthogonal;
        else if (style.Contains("curved=1"))
            edge.Routing = RoutingStyle.Curved;
        else if (style.Contains("edgeStyle=entityRelationEdgeStyle"))
            edge.Routing = RoutingStyle.EntityRelation;

        // Parse arrow types
        edge.TargetArrow = ParseArrowType(ParseStyleValue(style, "endArrow"));
        edge.SourceArrow = ParseArrowType(ParseStyleValue(style, "startArrow"));

        // Parse connection points
        edge.SourcePoint = ParseConnectionPoint(
            ParseStyleValue(style, "exitX"),
            ParseStyleValue(style, "exitY"));
        edge.TargetPoint = ParseConnectionPoint(
            ParseStyleValue(style, "entryX"),
            ParseStyleValue(style, "entryY"));

        // Parse waypoints
        var geometry = cell.Element("mxGeometry");
        if (geometry != null)
        {
            var pointsArray = geometry.Element("Array");
            if (pointsArray != null && pointsArray.Attribute("as")?.Value == "points")
            {
                foreach (var point in pointsArray.Elements("mxPoint"))
                {
                    if (double.TryParse(point.Attribute("x")?.Value, out var x) &&
                        double.TryParse(point.Attribute("y")?.Value, out var y))
                    {
                        edge.Waypoints.Add(new Point(x, y));
                    }
                }
            }
        }

        return edge;
    }

    private static ArrowType ParseArrowType(string? arrowStyle)
    {
        return arrowStyle switch
        {
            "classic" => ArrowType.Arrow,
            "open" => ArrowType.OpenArrow,
            "diamond" => ArrowType.Diamond,
            "oval" => ArrowType.Circle,
            "block" => ArrowType.Square,
            _ => ArrowType.None
        };
    }

    private static ConnectionPoint ParseConnectionPoint(string? xValue, string? yValue)
    {
        if (!double.TryParse(xValue, out var x) || !double.TryParse(yValue, out var y))
            return ConnectionPoint.Center;

        // Map x,y coordinates to connection points
        return (x, y) switch
        {
            (0, 0.5) => ConnectionPoint.Left,
            (1, 0.5) => ConnectionPoint.Right,
            (0.5, 0) => ConnectionPoint.Top,
            (0.5, 1) => ConnectionPoint.Bottom,
            (0, 0) => ConnectionPoint.TopLeft,
            (1, 0) => ConnectionPoint.TopRight,
            (0, 1) => ConnectionPoint.BottomLeft,
            (1, 1) => ConnectionPoint.BottomRight,
            (0.5, 0.5) => ConnectionPoint.Center,
            _ => ConnectionPoint.Center
        };
    }

    private static NodeShape ParseShapeFromStyle(string style)
    {
        if (style.Contains("rhombus")) return NodeShape.Diamond;
        if (style.Contains("ellipse")) return NodeShape.Ellipse;
        if (style.Contains("rounded=1")) return NodeShape.RoundedRectangle;
        if (style.Contains("parallelogram")) return NodeShape.Parallelogram;
        if (style.Contains("hexagon")) return NodeShape.Hexagon;
        if (style.Contains("cylinder")) return NodeShape.Cylinder;
        if (style.Contains("document")) return NodeShape.Document;
        if (style.Contains("cloud")) return NodeShape.Cloud;
        if (style.Contains("triangle")) return NodeShape.Triangle;
        return NodeShape.Rectangle;
    }

    private static string? ParseStyleValue(string style, string key)
    {
        var parts = style.Split(';');
        foreach (var part in parts)
        {
            var kv = part.Split('=');
            if (kv.Length == 2 && kv[0] == key)
                return kv[1];
        }
        return null;
    }

    private static string GetDrawIOStyle(DiagramNode node)
    {
        var shapeStyle = node.Shape switch
        {
            NodeShape.Diamond => "rhombus",
            NodeShape.Ellipse => "ellipse",
            NodeShape.RoundedRectangle => "rounded=1",
            NodeShape.Parallelogram => "shape=parallelogram;perimeter=parallelogramPerimeter",
            NodeShape.Hexagon => "shape=hexagon;perimeter=hexagonPerimeter2",
            NodeShape.Cylinder => "shape=cylinder3;whiteSpace=wrap;boundedLbl=1;backgroundOutline=1;size=15",
            NodeShape.Document => "shape=document;whiteSpace=wrap;boundedLbl=1;backgroundOutline=1",
            NodeShape.Cloud => "ellipse;shape=cloud",
            NodeShape.Triangle => "triangle;whiteSpace=wrap",
            _ => ""
        };

        return $"{shapeStyle};whiteSpace=wrap;html=1;fillColor={node.FillColor};strokeColor={node.StrokeColor};strokeWidth={node.StrokeWidth};fontColor={node.TextColor};fontSize={node.FontSize}";
    }

    private static string GetDrawIOEdgeStyle(DiagramEdge edge)
    {
        var dashStyle = edge.Style switch
        {
            LineStyle.Dashed => "dashed=1;",
            LineStyle.Dotted => "dashed=1;dashPattern=1 2;",
            LineStyle.DashDot => "dashed=1;dashPattern=3 1 1 1;",
            _ => ""
        };

        var routing = edge.Routing switch
        {
            RoutingStyle.Orthogonal => "edgeStyle=orthogonalEdgeStyle;",
            RoutingStyle.Curved => "curved=1;",
            RoutingStyle.EntityRelation => "edgeStyle=entityRelationEdgeStyle;",
            _ => ""
        };

        var endArrow = edge.TargetArrow switch
        {
            ArrowType.Arrow => "endArrow=classic;",
            ArrowType.OpenArrow => "endArrow=open;",
            ArrowType.Diamond => "endArrow=diamond;",
            ArrowType.Circle => "endArrow=oval;",
            ArrowType.Square => "endArrow=block;",
            _ => "endArrow=none;"
        };

        var startArrow = edge.SourceArrow switch
        {
            ArrowType.Arrow => "startArrow=classic;",
            ArrowType.OpenArrow => "startArrow=open;",
            ArrowType.Diamond => "startArrow=diamond;",
            ArrowType.Circle => "startArrow=oval;",
            ArrowType.Square => "startArrow=block;",
            _ => "startArrow=none;"
        };

        return $"{routing}{dashStyle}{startArrow}{endArrow}strokeColor={edge.StrokeColor};strokeWidth={edge.StrokeWidth};html=1";
    }

    /// <summary>
    /// Decompresses Draw.io content (base64 + URL encoded + deflate).
    /// </summary>
    private static string? DecompressDrawIOContent(string compressed)
    {
        try
        {
            // Draw.io uses: base64 encode -> URL encode -> deflate
            // We reverse: base64 decode -> inflate -> URL decode
            var base64Decoded = Convert.FromBase64String(compressed);

            using var inputStream = new MemoryStream(base64Decoded);
            using var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress);
            using var outputStream = new MemoryStream();

            deflateStream.CopyTo(outputStream);
            var decompressed = Encoding.UTF8.GetString(outputStream.ToArray());

            // URL decode the result
            return Uri.UnescapeDataString(decompressed);
        }
        catch
        {
            // If decompression fails, the content might not be compressed
            return null;
        }
    }

    /// <summary>
    /// Compresses content for Draw.io format (deflate + URL encode + base64).
    /// </summary>
    private static string CompressDrawIOContent(string content)
    {
        // URL encode first
        var urlEncoded = Uri.EscapeDataString(content);
        var bytes = Encoding.UTF8.GetBytes(urlEncoded);

        using var outputStream = new MemoryStream();
        using (var deflateStream = new DeflateStream(outputStream, CompressionLevel.Optimal))
        {
            deflateStream.Write(bytes, 0, bytes.Length);
        }

        return Convert.ToBase64String(outputStream.ToArray());
    }

    #endregion
}
