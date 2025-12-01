using Arcana.Plugin.FlowChart.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;
using XamlPath = Microsoft.UI.Xaml.Shapes.Path;

namespace Arcana.Plugin.FlowChart.Controls;

/// <summary>
/// Custom canvas control for rendering and interacting with flowchart diagrams.
/// </summary>
public sealed class FlowChartCanvas : Canvas
{
    private readonly Dictionary<string, FrameworkElement> _nodeElements = new();
    private readonly Dictionary<string, XamlPath> _edgeElements = new();
    private DiagramNode? _draggedNode;
    private Windows.Foundation.Point _dragOffset;
    private bool _isConnecting;
    private string? _connectionSourceNodeId;
    private ConnectionPoint _connectionSourcePoint;
    private Line? _connectionPreviewLine;

    #region Dependency Properties

    public static readonly DependencyProperty DiagramProperty =
        DependencyProperty.Register(
            nameof(Diagram),
            typeof(Diagram),
            typeof(FlowChartCanvas),
            new PropertyMetadata(null, OnDiagramChanged));

    public static readonly DependencyProperty ZoomLevelProperty =
        DependencyProperty.Register(
            nameof(ZoomLevel),
            typeof(double),
            typeof(FlowChartCanvas),
            new PropertyMetadata(1.0, OnZoomLevelChanged));

    public static readonly DependencyProperty IsConnectModeProperty =
        DependencyProperty.Register(
            nameof(IsConnectMode),
            typeof(bool),
            typeof(FlowChartCanvas),
            new PropertyMetadata(false));

    public static readonly DependencyProperty SelectedNodeProperty =
        DependencyProperty.Register(
            nameof(SelectedNode),
            typeof(DiagramNode),
            typeof(FlowChartCanvas),
            new PropertyMetadata(null, OnSelectedNodeChanged));

    public static readonly DependencyProperty SelectedEdgeProperty =
        DependencyProperty.Register(
            nameof(SelectedEdge),
            typeof(DiagramEdge),
            typeof(FlowChartCanvas),
            new PropertyMetadata(null, OnSelectedEdgeChanged));

    public Diagram? Diagram
    {
        get => (Diagram?)GetValue(DiagramProperty);
        set => SetValue(DiagramProperty, value);
    }

    public double ZoomLevel
    {
        get => (double)GetValue(ZoomLevelProperty);
        set => SetValue(ZoomLevelProperty, value);
    }

    public bool IsConnectMode
    {
        get => (bool)GetValue(IsConnectModeProperty);
        set => SetValue(IsConnectModeProperty, value);
    }

    public DiagramNode? SelectedNode
    {
        get => (DiagramNode?)GetValue(SelectedNodeProperty);
        set => SetValue(SelectedNodeProperty, value);
    }

    public DiagramEdge? SelectedEdge
    {
        get => (DiagramEdge?)GetValue(SelectedEdgeProperty);
        set => SetValue(SelectedEdgeProperty, value);
    }

    #endregion

    #region Events

    public event EventHandler<NodeSelectedEventArgs>? NodeSelected;
    public event EventHandler<EdgeSelectedEventArgs>? EdgeSelected;
    public event EventHandler<NodeMovedEventArgs>? NodeMoved;
    public event EventHandler<ConnectionCreatedEventArgs>? ConnectionCreated;

    #endregion

    public FlowChartCanvas()
    {
        Background = new SolidColorBrush(Colors.White);
        PointerPressed += OnCanvasPointerPressed;
        PointerMoved += OnCanvasPointerMoved;
        PointerReleased += OnCanvasPointerReleased;
    }

    #region Property Changed Handlers

    private static void OnDiagramChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FlowChartCanvas canvas)
        {
            canvas.RefreshDiagram();
        }
    }

    private static void OnZoomLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FlowChartCanvas canvas)
        {
            canvas.ApplyZoom();
        }
    }

    private static void OnSelectedNodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FlowChartCanvas canvas)
        {
            canvas.UpdateNodeSelection(e.OldValue as DiagramNode, e.NewValue as DiagramNode);
        }
    }

    private static void OnSelectedEdgeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FlowChartCanvas canvas)
        {
            canvas.UpdateEdgeSelection(e.OldValue as DiagramEdge, e.NewValue as DiagramEdge);
        }
    }

    #endregion

    #region Rendering

    /// <summary>
    /// Refreshes the entire diagram display.
    /// </summary>
    public void RefreshDiagram()
    {
        Children.Clear();
        _nodeElements.Clear();
        _edgeElements.Clear();

        if (Diagram == null) return;

        // Draw grid if enabled
        if (Diagram.ShowGrid)
        {
            DrawGrid();
        }

        // Draw edges first (below nodes)
        foreach (var edge in Diagram.Edges.OrderBy(e => e.ZIndex))
        {
            var edgePath = CreateEdgePath(edge);
            _edgeElements[edge.Id] = edgePath;
            Children.Add(edgePath);
        }

        // Draw nodes
        foreach (var node in Diagram.Nodes.OrderBy(n => n.ZIndex))
        {
            var nodeElement = CreateNodeElement(node);
            _nodeElements[node.Id] = nodeElement;
            Children.Add(nodeElement);
            SetLeft(nodeElement, node.X);
            SetTop(nodeElement, node.Y);
        }

        ApplyZoom();
    }

    private void DrawGrid()
    {
        if (Diagram == null) return;

        var gridBrush = new SolidColorBrush(Color.FromArgb(50, 200, 200, 200));
        var gridSize = Diagram.GridSize;

        for (double x = 0; x < Diagram.CanvasWidth; x += gridSize)
        {
            var line = new Line
            {
                X1 = x,
                Y1 = 0,
                X2 = x,
                Y2 = Diagram.CanvasHeight,
                Stroke = gridBrush,
                StrokeThickness = 1
            };
            Children.Add(line);
        }

        for (double y = 0; y < Diagram.CanvasHeight; y += gridSize)
        {
            var line = new Line
            {
                X1 = 0,
                Y1 = y,
                X2 = Diagram.CanvasWidth,
                Y2 = y,
                Stroke = gridBrush,
                StrokeThickness = 1
            };
            Children.Add(line);
        }
    }

    private FrameworkElement CreateNodeElement(DiagramNode node)
    {
        var grid = new Grid
        {
            Width = node.Width,
            Height = node.Height,
            Tag = node.Id
        };

        var shape = CreateShape(node);
        grid.Children.Add(shape);

        var textBlock = new TextBlock
        {
            Text = node.Text,
            FontSize = node.FontSize,
            Foreground = new SolidColorBrush(ParseColor(node.TextColor)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(5)
        };
        grid.Children.Add(textBlock);

        // Add connection point indicators when in connect mode
        if (IsConnectMode)
        {
            AddConnectionPoints(grid, node);
        }

        grid.PointerPressed += (s, e) => OnNodePointerPressed(node, e);
        grid.PointerMoved += (s, e) => OnNodePointerMoved(node, e);
        grid.PointerReleased += (s, e) => OnNodePointerReleased(node, e);

        return grid;
    }

    private Shape CreateShape(DiagramNode node)
    {
        var fillBrush = new SolidColorBrush(ParseColor(node.FillColor));
        var strokeBrush = new SolidColorBrush(ParseColor(node.StrokeColor));

        Shape shape = node.Shape switch
        {
            NodeShape.Ellipse => new Ellipse(),
            NodeShape.Diamond => CreateDiamond(node.Width, node.Height),
            NodeShape.RoundedRectangle => new Rectangle { RadiusX = 10, RadiusY = 10 },
            NodeShape.Parallelogram => CreateParallelogram(node.Width, node.Height),
            NodeShape.Hexagon => CreateHexagon(node.Width, node.Height),
            NodeShape.Triangle => CreateTriangle(node.Width, node.Height),
            NodeShape.Cylinder => CreateCylinder(node.Width, node.Height),
            _ => new Rectangle()
        };

        shape.Fill = fillBrush;
        shape.Stroke = strokeBrush;
        shape.StrokeThickness = node.StrokeWidth;
        shape.Width = node.Width;
        shape.Height = node.Height;

        if (node.IsSelected)
        {
            shape.StrokeDashArray = new DoubleCollection { 4, 2 };
            shape.Stroke = new SolidColorBrush(Colors.DodgerBlue);
            shape.StrokeThickness = 3;
        }

        return shape;
    }

    private static Polygon CreateDiamond(double width, double height)
    {
        return new Polygon
        {
            Points = new PointCollection
            {
                new Windows.Foundation.Point(width / 2, 0),
                new Windows.Foundation.Point(width, height / 2),
                new Windows.Foundation.Point(width / 2, height),
                new Windows.Foundation.Point(0, height / 2)
            }
        };
    }

    private static Polygon CreateParallelogram(double width, double height)
    {
        var offset = width * 0.2;
        return new Polygon
        {
            Points = new PointCollection
            {
                new Windows.Foundation.Point(offset, 0),
                new Windows.Foundation.Point(width, 0),
                new Windows.Foundation.Point(width - offset, height),
                new Windows.Foundation.Point(0, height)
            }
        };
    }

    private static Polygon CreateHexagon(double width, double height)
    {
        var offset = width * 0.2;
        return new Polygon
        {
            Points = new PointCollection
            {
                new Windows.Foundation.Point(offset, 0),
                new Windows.Foundation.Point(width - offset, 0),
                new Windows.Foundation.Point(width, height / 2),
                new Windows.Foundation.Point(width - offset, height),
                new Windows.Foundation.Point(offset, height),
                new Windows.Foundation.Point(0, height / 2)
            }
        };
    }

    private static Polygon CreateTriangle(double width, double height)
    {
        return new Polygon
        {
            Points = new PointCollection
            {
                new Windows.Foundation.Point(width / 2, 0),
                new Windows.Foundation.Point(width, height),
                new Windows.Foundation.Point(0, height)
            }
        };
    }

    private static XamlPath CreateCylinder(double width, double height)
    {
        var ellipseHeight = height * 0.2;
        var geometry = new PathGeometry();

        // Top ellipse
        var topEllipse = new EllipseGeometry
        {
            Center = new Windows.Foundation.Point(width / 2, ellipseHeight / 2),
            RadiusX = width / 2,
            RadiusY = ellipseHeight / 2
        };

        // Body rectangle + bottom curve
        var pathFigure = new PathFigure
        {
            StartPoint = new Windows.Foundation.Point(0, ellipseHeight / 2),
            IsClosed = false
        };

        pathFigure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(0, height - ellipseHeight / 2) });
        pathFigure.Segments.Add(new ArcSegment
        {
            Point = new Windows.Foundation.Point(width, height - ellipseHeight / 2),
            Size = new Windows.Foundation.Size(width / 2, ellipseHeight / 2),
            SweepDirection = SweepDirection.Clockwise
        });
        pathFigure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(width, ellipseHeight / 2) });

        geometry.Figures.Add(pathFigure);

        var path = new XamlPath
        {
            Width = width,
            Height = height
        };

        // Use a combined geometry group
        var geometryGroup = new GeometryGroup();
        geometryGroup.Children.Add(topEllipse);
        geometryGroup.Children.Add(geometry);
        path.Data = geometryGroup;

        return path;
    }

    private void AddConnectionPoints(Grid grid, DiagramNode node)
    {
        var points = new[]
        {
            (ConnectionPoint.Top, 0.5, 0.0),
            (ConnectionPoint.Right, 1.0, 0.5),
            (ConnectionPoint.Bottom, 0.5, 1.0),
            (ConnectionPoint.Left, 0.0, 0.5)
        };

        foreach (var (point, xRatio, yRatio) in points)
        {
            var ellipse = new Ellipse
            {
                Width = 12,
                Height = 12,
                Fill = new SolidColorBrush(Colors.DodgerBlue),
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 2,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(
                    node.Width * xRatio - 6,
                    node.Height * yRatio - 6,
                    0, 0),
                Tag = point
            };

            ellipse.PointerPressed += (s, e) =>
            {
                e.Handled = true;
                StartConnection(node.Id, point);
            };

            ellipse.PointerReleased += (s, e) =>
            {
                if (_isConnecting && _connectionSourceNodeId != node.Id)
                {
                    e.Handled = true;
                    CompleteConnection(node.Id, point);
                }
            };

            grid.Children.Add(ellipse);
        }
    }

    private XamlPath CreateEdgePath(DiagramEdge edge)
    {
        var sourceNode = Diagram?.GetNode(edge.SourceNodeId);
        var targetNode = Diagram?.GetNode(edge.TargetNodeId);

        if (sourceNode == null || targetNode == null)
        {
            return new XamlPath();
        }

        var startPoint = GetConnectionPoint(sourceNode, edge.SourcePoint);
        var endPoint = GetConnectionPoint(targetNode, edge.TargetPoint);

        var pathGeometry = new PathGeometry();
        var pathFigure = new PathFigure { StartPoint = startPoint };

        if (edge.Routing == RoutingStyle.Orthogonal)
        {
            // Create orthogonal path with right angles
            var midX = (startPoint.X + endPoint.X) / 2;
            pathFigure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(midX, startPoint.Y) });
            pathFigure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(midX, endPoint.Y) });
            pathFigure.Segments.Add(new LineSegment { Point = endPoint });
        }
        else if (edge.Routing == RoutingStyle.Curved)
        {
            // Create bezier curve
            var controlPoint1 = new Windows.Foundation.Point(
                startPoint.X + (endPoint.X - startPoint.X) / 3,
                startPoint.Y);
            var controlPoint2 = new Windows.Foundation.Point(
                startPoint.X + 2 * (endPoint.X - startPoint.X) / 3,
                endPoint.Y);
            pathFigure.Segments.Add(new BezierSegment
            {
                Point1 = controlPoint1,
                Point2 = controlPoint2,
                Point3 = endPoint
            });
        }
        else
        {
            // Direct line
            pathFigure.Segments.Add(new LineSegment { Point = endPoint });
        }

        pathGeometry.Figures.Add(pathFigure);

        var strokeBrush = new SolidColorBrush(ParseColor(edge.StrokeColor));
        if (edge.IsSelected)
        {
            strokeBrush = new SolidColorBrush(Colors.DodgerBlue);
        }

        var path = new XamlPath
        {
            Data = pathGeometry,
            Stroke = strokeBrush,
            StrokeThickness = edge.StrokeWidth,
            Tag = edge.Id
        };

        // Apply line style
        path.StrokeDashArray = edge.Style switch
        {
            LineStyle.Dashed => new DoubleCollection { 6, 3 },
            LineStyle.Dotted => new DoubleCollection { 2, 2 },
            LineStyle.DashDot => new DoubleCollection { 6, 3, 2, 3 },
            _ => null
        };

        // Add arrow at target
        if (edge.TargetArrow != ArrowType.None)
        {
            AddArrowHead(path, startPoint, endPoint, edge.TargetArrow, strokeBrush);
        }

        path.PointerPressed += (s, e) =>
        {
            e.Handled = true;
            EdgeSelected?.Invoke(this, new EdgeSelectedEventArgs(edge));
        };

        return path;
    }

    private static Windows.Foundation.Point GetConnectionPoint(DiagramNode node, ConnectionPoint point)
    {
        return point switch
        {
            ConnectionPoint.Top => new Windows.Foundation.Point(node.X + node.Width / 2, node.Y),
            ConnectionPoint.Right => new Windows.Foundation.Point(node.X + node.Width, node.Y + node.Height / 2),
            ConnectionPoint.Bottom => new Windows.Foundation.Point(node.X + node.Width / 2, node.Y + node.Height),
            ConnectionPoint.Left => new Windows.Foundation.Point(node.X, node.Y + node.Height / 2),
            ConnectionPoint.TopLeft => new Windows.Foundation.Point(node.X, node.Y),
            ConnectionPoint.TopRight => new Windows.Foundation.Point(node.X + node.Width, node.Y),
            ConnectionPoint.BottomLeft => new Windows.Foundation.Point(node.X, node.Y + node.Height),
            ConnectionPoint.BottomRight => new Windows.Foundation.Point(node.X + node.Width, node.Y + node.Height),
            ConnectionPoint.Center => new Windows.Foundation.Point(node.X + node.Width / 2, node.Y + node.Height / 2),
            _ => new Windows.Foundation.Point(node.X + node.Width / 2, node.Y + node.Height / 2)
        };
    }

    private void AddArrowHead(XamlPath edgePath, Windows.Foundation.Point start, Windows.Foundation.Point end, ArrowType arrowType, SolidColorBrush brush)
    {
        var angle = Math.Atan2(end.Y - start.Y, end.X - start.X);
        var arrowSize = 12.0;

        var arrowPoints = new PointCollection();

        switch (arrowType)
        {
            case ArrowType.Arrow:
            case ArrowType.OpenArrow:
                arrowPoints.Add(end);
                arrowPoints.Add(new Windows.Foundation.Point(
                    end.X - arrowSize * Math.Cos(angle - Math.PI / 6),
                    end.Y - arrowSize * Math.Sin(angle - Math.PI / 6)));
                arrowPoints.Add(new Windows.Foundation.Point(
                    end.X - arrowSize * Math.Cos(angle + Math.PI / 6),
                    end.Y - arrowSize * Math.Sin(angle + Math.PI / 6)));
                break;
            case ArrowType.Diamond:
                var diamondMid = new Windows.Foundation.Point(
                    end.X - arrowSize / 2 * Math.Cos(angle),
                    end.Y - arrowSize / 2 * Math.Sin(angle));
                arrowPoints.Add(end);
                arrowPoints.Add(new Windows.Foundation.Point(
                    diamondMid.X - arrowSize / 2 * Math.Cos(angle - Math.PI / 2),
                    diamondMid.Y - arrowSize / 2 * Math.Sin(angle - Math.PI / 2)));
                arrowPoints.Add(new Windows.Foundation.Point(
                    end.X - arrowSize * Math.Cos(angle),
                    end.Y - arrowSize * Math.Sin(angle)));
                arrowPoints.Add(new Windows.Foundation.Point(
                    diamondMid.X + arrowSize / 2 * Math.Cos(angle - Math.PI / 2),
                    diamondMid.Y + arrowSize / 2 * Math.Sin(angle - Math.PI / 2)));
                break;
        }

        var arrow = new Polygon
        {
            Points = arrowPoints,
            Fill = arrowType == ArrowType.OpenArrow ? new SolidColorBrush(Colors.White) : brush,
            Stroke = brush,
            StrokeThickness = 1
        };

        Children.Add(arrow);
    }

    private void ApplyZoom()
    {
        RenderTransform = new ScaleTransform
        {
            ScaleX = ZoomLevel,
            ScaleY = ZoomLevel
        };
    }

    private void UpdateNodeSelection(DiagramNode? oldNode, DiagramNode? newNode)
    {
        if (oldNode != null)
        {
            oldNode.IsSelected = false;
            if (_nodeElements.TryGetValue(oldNode.Id, out var oldElement))
            {
                RefreshNodeElement(oldNode, oldElement);
            }
        }

        if (newNode != null)
        {
            newNode.IsSelected = true;
            if (_nodeElements.TryGetValue(newNode.Id, out var newElement))
            {
                RefreshNodeElement(newNode, newElement);
            }
        }
    }

    private void UpdateEdgeSelection(DiagramEdge? oldEdge, DiagramEdge? newEdge)
    {
        if (oldEdge != null)
        {
            oldEdge.IsSelected = false;
            if (_edgeElements.TryGetValue(oldEdge.Id, out var oldPath))
            {
                oldPath.Stroke = new SolidColorBrush(ParseColor(oldEdge.StrokeColor));
            }
        }

        if (newEdge != null)
        {
            newEdge.IsSelected = true;
            if (_edgeElements.TryGetValue(newEdge.Id, out var newPath))
            {
                newPath.Stroke = new SolidColorBrush(Colors.DodgerBlue);
            }
        }
    }

    private void RefreshNodeElement(DiagramNode node, FrameworkElement element)
    {
        if (element is Grid grid && grid.Children.Count > 0 && grid.Children[0] is Shape shape)
        {
            shape.Stroke = node.IsSelected
                ? new SolidColorBrush(Colors.DodgerBlue)
                : new SolidColorBrush(ParseColor(node.StrokeColor));
            shape.StrokeThickness = node.IsSelected ? 3 : node.StrokeWidth;
            shape.StrokeDashArray = node.IsSelected ? new DoubleCollection { 4, 2 } : null;
        }
    }

    #endregion

    #region Interaction

    private void OnCanvasPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        // Clicked on empty space - deselect
        NodeSelected?.Invoke(this, new NodeSelectedEventArgs(null));
        EdgeSelected?.Invoke(this, new EdgeSelectedEventArgs(null));
    }

    private void OnCanvasPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_isConnecting && _connectionPreviewLine != null)
        {
            var point = e.GetCurrentPoint(this).Position;
            _connectionPreviewLine.X2 = point.X;
            _connectionPreviewLine.Y2 = point.Y;
        }
    }

    private void OnCanvasPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        CancelConnection();
    }

    private void OnNodePointerPressed(DiagramNode node, PointerRoutedEventArgs e)
    {
        e.Handled = true;

        if (IsConnectMode)
        {
            return; // Let connection point handlers handle it
        }

        NodeSelected?.Invoke(this, new NodeSelectedEventArgs(node));
        _draggedNode = node;

        var point = e.GetCurrentPoint(this).Position;
        _dragOffset = new Windows.Foundation.Point(point.X - node.X, point.Y - node.Y);
    }

    private void OnNodePointerMoved(DiagramNode node, PointerRoutedEventArgs e)
    {
        if (_draggedNode == null || _draggedNode.Id != node.Id) return;

        var point = e.GetCurrentPoint(this).Position;
        var newX = point.X - _dragOffset.X;
        var newY = point.Y - _dragOffset.Y;

        if (_nodeElements.TryGetValue(node.Id, out var element))
        {
            SetLeft(element, newX);
            SetTop(element, newY);
        }

        node.X = newX;
        node.Y = newY;

        // Refresh edges connected to this node
        RefreshConnectedEdges(node.Id);
    }

    private void OnNodePointerReleased(DiagramNode node, PointerRoutedEventArgs e)
    {
        if (_draggedNode != null && _draggedNode.Id == node.Id)
        {
            NodeMoved?.Invoke(this, new NodeMovedEventArgs(node, node.X, node.Y));
            _draggedNode = null;
        }
    }

    private void RefreshConnectedEdges(string nodeId)
    {
        if (Diagram == null) return;

        foreach (var edge in Diagram.GetEdgesForNode(nodeId))
        {
            if (_edgeElements.TryGetValue(edge.Id, out var oldPath))
            {
                var index = Children.IndexOf(oldPath);
                Children.Remove(oldPath);

                var newPath = CreateEdgePath(edge);
                _edgeElements[edge.Id] = newPath;

                if (index >= 0)
                    Children.Insert(index, newPath);
                else
                    Children.Add(newPath);
            }
        }
    }

    private void StartConnection(string nodeId, ConnectionPoint point)
    {
        _isConnecting = true;
        _connectionSourceNodeId = nodeId;
        _connectionSourcePoint = point;

        var sourceNode = Diagram?.GetNode(nodeId);
        if (sourceNode != null)
        {
            var startPoint = GetConnectionPoint(sourceNode, point);
            _connectionPreviewLine = new Line
            {
                X1 = startPoint.X,
                Y1 = startPoint.Y,
                X2 = startPoint.X,
                Y2 = startPoint.Y,
                Stroke = new SolidColorBrush(Colors.DodgerBlue),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 }
            };
            Children.Add(_connectionPreviewLine);
        }
    }

    private void CompleteConnection(string targetNodeId, ConnectionPoint targetPoint)
    {
        if (_connectionSourceNodeId != null)
        {
            ConnectionCreated?.Invoke(this, new ConnectionCreatedEventArgs(
                _connectionSourceNodeId,
                targetNodeId,
                _connectionSourcePoint,
                targetPoint));
        }

        CancelConnection();
    }

    private void CancelConnection()
    {
        _isConnecting = false;
        _connectionSourceNodeId = null;

        if (_connectionPreviewLine != null)
        {
            Children.Remove(_connectionPreviewLine);
            _connectionPreviewLine = null;
        }
    }

    #endregion

    #region Helpers

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
        else if (hex.Length == 8)
        {
            return Color.FromArgb(
                byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber));
        }
        return Colors.Black;
    }

    #endregion
}

#region Event Args

public class NodeSelectedEventArgs : EventArgs
{
    public DiagramNode? Node { get; }
    public NodeSelectedEventArgs(DiagramNode? node) => Node = node;
}

public class EdgeSelectedEventArgs : EventArgs
{
    public DiagramEdge? Edge { get; }
    public EdgeSelectedEventArgs(DiagramEdge? edge) => Edge = edge;
}

public class NodeMovedEventArgs : EventArgs
{
    public DiagramNode Node { get; }
    public double X { get; }
    public double Y { get; }
    public NodeMovedEventArgs(DiagramNode node, double x, double y) => (Node, X, Y) = (node, x, y);
}

public class ConnectionCreatedEventArgs : EventArgs
{
    public string SourceNodeId { get; }
    public string TargetNodeId { get; }
    public ConnectionPoint SourcePoint { get; }
    public ConnectionPoint TargetPoint { get; }

    public ConnectionCreatedEventArgs(string sourceId, string targetId, ConnectionPoint sourcePoint, ConnectionPoint targetPoint)
    {
        SourceNodeId = sourceId;
        TargetNodeId = targetId;
        SourcePoint = sourcePoint;
        TargetPoint = targetPoint;
    }
}

#endregion
