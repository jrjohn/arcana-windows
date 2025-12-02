using System.Linq;
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
    private readonly Dictionary<string, List<Ellipse>> _waypointHandles = new();
    private readonly Dictionary<string, Polygon> _arrowHeads = new();
    private DiagramNode? _draggedNode;
    private Windows.Foundation.Point _dragOffset;
    private bool _isConnecting;
    private string? _connectionSourceNodeId;
    private ConnectionPoint _connectionSourcePoint;
    private Line? _connectionPreviewLine;

    // Waypoint dragging state
    private bool _isDraggingWaypoint;
    private string? _draggingWaypointEdgeId;
    private int _draggingWaypointIndex;
    private Ellipse? _draggingWaypointHandle;

    // Node dragging state
    private FrameworkElement? _draggedNodeElement;
    private uint _dragPointerId;

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
    public event EventHandler<WaypointChangedEventArgs>? WaypointChanged;

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
        _waypointHandles.Clear();
        _arrowHeads.Clear();

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
        var nodePadding = IsConnectMode ? ConnectionPointPadding : 0;
        foreach (var node in Diagram.Nodes.OrderBy(n => n.ZIndex))
        {
            var nodeElement = CreateNodeElement(node);
            _nodeElements[node.Id] = nodeElement;
            Children.Add(nodeElement);
            // Offset position by padding so the shape stays at the correct visual position
            SetLeft(nodeElement, node.X - nodePadding);
            SetTop(nodeElement, node.Y - nodePadding);
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

    // Padding to accommodate connection point hit areas (28px diameter, centered on edge)
    private const double ConnectionPointPadding = 14;

    private FrameworkElement CreateNodeElement(DiagramNode node)
    {
        // When in connect mode, add padding to accommodate connection points that extend beyond node bounds
        var padding = IsConnectMode ? ConnectionPointPadding : 0;

        var grid = new Grid
        {
            Width = node.Width + (padding * 2),
            Height = node.Height + (padding * 2),
            Tag = node.Id
        };

        // Create a container for the shape and text, offset by padding
        var contentContainer = new Grid
        {
            Width = node.Width,
            Height = node.Height,
            Margin = new Thickness(padding)
        };

        var shape = CreateShape(node);
        contentContainer.Children.Add(shape);

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
        contentContainer.Children.Add(textBlock);

        grid.Children.Add(contentContainer);

        // Add connection point indicators when in connect mode
        if (IsConnectMode)
        {
            AddConnectionPoints(grid, node, padding);
        }

        // Only need PointerPressed - move/release handled at canvas level with pointer capture
        grid.PointerPressed += (s, e) => OnNodePointerPressed(node, grid, e);

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

    // Size constants for connection points
    private const double ConnectionPointVisualSize = 14;
    private const double ConnectionPointHitAreaSize = 28; // Larger hit area for easier clicking

    private void AddConnectionPoints(Grid grid, DiagramNode node, double padding)
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
            // Container with larger hit area
            var hitArea = new Grid
            {
                Width = ConnectionPointHitAreaSize,
                Height = ConnectionPointHitAreaSize,
                Background = new SolidColorBrush(Colors.Transparent),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(
                    padding + (node.Width * xRatio) - (ConnectionPointHitAreaSize / 2),
                    padding + (node.Height * yRatio) - (ConnectionPointHitAreaSize / 2),
                    0, 0),
                Tag = point
            };

            // Visual indicator (smaller, centered in hit area)
            var ellipse = new Ellipse
            {
                Width = ConnectionPointVisualSize,
                Height = ConnectionPointVisualSize,
                Fill = new SolidColorBrush(Colors.DodgerBlue),
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 2,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            hitArea.Children.Add(ellipse);

            hitArea.PointerEntered += (s, e) =>
            {
                ellipse.Fill = new SolidColorBrush(Colors.Orange);
                ellipse.Width = ConnectionPointVisualSize + 4;
                ellipse.Height = ConnectionPointVisualSize + 4;
            };

            hitArea.PointerExited += (s, e) =>
            {
                ellipse.Fill = new SolidColorBrush(Colors.DodgerBlue);
                ellipse.Width = ConnectionPointVisualSize;
                ellipse.Height = ConnectionPointVisualSize;
            };

            hitArea.PointerPressed += (s, e) =>
            {
                e.Handled = true;
                StartConnection(node.Id, point);
            };

            hitArea.PointerReleased += (s, e) =>
            {
                if (_isConnecting && _connectionSourceNodeId != node.Id)
                {
                    e.Handled = true;
                    CompleteConnection(node.Id, point);
                }
            };

            grid.Children.Add(hitArea);
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

        // Build list of all points for the path
        var allPoints = new List<Windows.Foundation.Point> { startPoint };

        // Check if edge has manual waypoints
        if (edge.Waypoints.Count > 0)
        {
            // Use manual waypoints
            foreach (var wp in edge.Waypoints)
            {
                var wpPoint = new Windows.Foundation.Point(wp.X, wp.Y);
                allPoints.Add(wpPoint);
                pathFigure.Segments.Add(new LineSegment { Point = wpPoint });
            }
            pathFigure.Segments.Add(new LineSegment { Point = endPoint });
            allPoints.Add(endPoint);
        }
        else if (edge.Routing == RoutingStyle.Orthogonal)
        {
            // Smart orthogonal routing based on connection point directions
            var routePoints = CalculateOrthogonalRoute(startPoint, endPoint, edge.SourcePoint, edge.TargetPoint);
            foreach (var pt in routePoints)
            {
                allPoints.Add(pt);
                pathFigure.Segments.Add(new LineSegment { Point = pt });
            }
        }
        else if (edge.Routing == RoutingStyle.Curved)
        {
            // Create bezier curve based on connection point directions
            var offset = 50.0;
            var controlPoint1 = GetOffsetPoint(startPoint, edge.SourcePoint, offset);
            var controlPoint2 = GetOffsetPoint(endPoint, edge.TargetPoint, offset);
            pathFigure.Segments.Add(new BezierSegment
            {
                Point1 = controlPoint1,
                Point2 = controlPoint2,
                Point3 = endPoint
            });
            allPoints.Add(endPoint);
        }
        else
        {
            // Direct line
            pathFigure.Segments.Add(new LineSegment { Point = endPoint });
            allPoints.Add(endPoint);
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
            StrokeThickness = edge.IsSelected ? edge.StrokeWidth + 2 : edge.StrokeWidth,
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

        // Add arrow at target - use the last segment direction for proper arrow orientation
        if (edge.TargetArrow != ArrowType.None)
        {
            var arrowStartPoint = startPoint;
            // For orthogonal/curved routing, get the second-to-last point for correct arrow direction
            if (pathFigure.Segments.Count >= 2)
            {
                var secondToLast = pathFigure.Segments[pathFigure.Segments.Count - 2];
                if (secondToLast is LineSegment lineSegment)
                {
                    arrowStartPoint = lineSegment.Point;
                }
            }
            AddArrowHead(edge.Id, arrowStartPoint, endPoint, edge.TargetArrow, strokeBrush);
        }

        // Handle edge click - select edge or add waypoint
        path.PointerPressed += (s, e) =>
        {
            e.Handled = true;
            var clickPoint = e.GetCurrentPoint(this).Position;

            if (edge.IsSelected)
            {
                // Edge already selected - add waypoint at click position
                AddWaypointToEdge(edge, clickPoint, allPoints);
            }
            else
            {
                // Select the edge
                EdgeSelected?.Invoke(this, new EdgeSelectedEventArgs(edge));
            }
        };

        // Show waypoint handles if edge is selected
        if (edge.IsSelected)
        {
            CreateWaypointHandles(edge);
        }

        return path;
    }

    /// <summary>
    /// Creates draggable waypoint handles for a selected edge.
    /// </summary>
    private void CreateWaypointHandles(DiagramEdge edge)
    {
        // Remove existing handles
        if (_waypointHandles.TryGetValue(edge.Id, out var existingHandles))
        {
            foreach (var handle in existingHandles)
            {
                Children.Remove(handle);
            }
        }

        var handles = new List<Ellipse>();

        for (int i = 0; i < edge.Waypoints.Count; i++)
        {
            var wp = edge.Waypoints[i];
            var waypointIndex = i;

            var handle = new Ellipse
            {
                Width = 12,
                Height = 12,
                Fill = new SolidColorBrush(Colors.White),
                Stroke = new SolidColorBrush(Colors.DodgerBlue),
                StrokeThickness = 2,
                Tag = new WaypointHandleTag { EdgeId = edge.Id, WaypointIndex = waypointIndex }
            };

            SetLeft(handle, wp.X - 6);
            SetTop(handle, wp.Y - 6);

            // Drag to move waypoint
            handle.PointerPressed += (s, e) =>
            {
                e.Handled = true;
                _isDraggingWaypoint = true;
                _draggingWaypointEdgeId = edge.Id;
                _draggingWaypointIndex = waypointIndex;
                _draggingWaypointHandle = handle;
                handle.Fill = new SolidColorBrush(Colors.DodgerBlue);
            };

            // Double-click to delete waypoint
            handle.DoubleTapped += (s, e) =>
            {
                e.Handled = true;
                RemoveWaypoint(edge, waypointIndex);
            };

            // Right-click context menu for delete
            var flyout = new MenuFlyout();
            var deleteItem = new MenuFlyoutItem { Text = "Delete Waypoint", Icon = new FontIcon { Glyph = "\uE74D" } };
            deleteItem.Click += (s, e) => RemoveWaypoint(edge, waypointIndex);
            flyout.Items.Add(deleteItem);
            handle.ContextFlyout = flyout;

            handles.Add(handle);
            Children.Add(handle);
        }

        _waypointHandles[edge.Id] = handles;
    }

    /// <summary>
    /// Adds a waypoint to an edge at the specified position.
    /// </summary>
    private void AddWaypointToEdge(DiagramEdge edge, Windows.Foundation.Point clickPoint, List<Windows.Foundation.Point> pathPoints)
    {
        // Find which segment was clicked
        int insertIndex = 0;
        double minDistance = double.MaxValue;

        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            var segmentStart = pathPoints[i];
            var segmentEnd = pathPoints[i + 1];
            var dist = DistanceToSegment(clickPoint, segmentStart, segmentEnd);

            if (dist < minDistance)
            {
                minDistance = dist;
                // Adjust insert index to account for waypoints vs path points
                // pathPoints[0] is start, pathPoints[1..n] are waypoints or auto-route points
                insertIndex = Math.Max(0, i - (pathPoints.Count - edge.Waypoints.Count - 2) + edge.Waypoints.Count);
                if (i <= edge.Waypoints.Count) insertIndex = i;
            }
        }

        // Insert waypoint
        var newWaypoint = new Models.Point(clickPoint.X, clickPoint.Y);
        if (insertIndex >= edge.Waypoints.Count)
        {
            edge.Waypoints.Add(newWaypoint);
        }
        else
        {
            edge.Waypoints.Insert(insertIndex, newWaypoint);
        }

        WaypointChanged?.Invoke(this, new WaypointChangedEventArgs(edge, WaypointChangeType.Added, insertIndex));
        RefreshEdge(edge);
    }

    /// <summary>
    /// Removes a waypoint from an edge.
    /// </summary>
    private void RemoveWaypoint(DiagramEdge edge, int waypointIndex)
    {
        if (waypointIndex >= 0 && waypointIndex < edge.Waypoints.Count)
        {
            edge.Waypoints.RemoveAt(waypointIndex);
            WaypointChanged?.Invoke(this, new WaypointChangedEventArgs(edge, WaypointChangeType.Removed, waypointIndex));
            RefreshEdge(edge);
        }
    }

    /// <summary>
    /// Calculates the distance from a point to a line segment.
    /// </summary>
    private static double DistanceToSegment(Windows.Foundation.Point p, Windows.Foundation.Point a, Windows.Foundation.Point b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var lengthSquared = dx * dx + dy * dy;

        if (lengthSquared == 0) return Math.Sqrt((p.X - a.X) * (p.X - a.X) + (p.Y - a.Y) * (p.Y - a.Y));

        var t = Math.Max(0, Math.Min(1, ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lengthSquared));
        var projX = a.X + t * dx;
        var projY = a.Y + t * dy;

        return Math.Sqrt((p.X - projX) * (p.X - projX) + (p.Y - projY) * (p.Y - projY));
    }

    /// <summary>
    /// Refreshes a single edge without redrawing everything.
    /// </summary>
    private void RefreshEdge(DiagramEdge edge)
    {
        // Remove old path
        if (_edgeElements.TryGetValue(edge.Id, out var oldPath))
        {
            Children.Remove(oldPath);
        }

        // Remove old arrow head
        if (_arrowHeads.TryGetValue(edge.Id, out var oldArrow))
        {
            Children.Remove(oldArrow);
            _arrowHeads.Remove(edge.Id);
        }

        // Remove old waypoint handles
        if (_waypointHandles.TryGetValue(edge.Id, out var oldHandles))
        {
            foreach (var h in oldHandles)
            {
                Children.Remove(h);
            }
            _waypointHandles.Remove(edge.Id);
        }

        // Create new path
        var newPath = CreateEdgePath(edge);
        _edgeElements[edge.Id] = newPath;

        // Insert at the beginning (below nodes)
        Children.Insert(0, newPath);
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

    /// <summary>
    /// Calculates orthogonal route points based on connection point directions.
    /// </summary>
    private static List<Windows.Foundation.Point> CalculateOrthogonalRoute(
        Windows.Foundation.Point start,
        Windows.Foundation.Point end,
        ConnectionPoint sourcePoint,
        ConnectionPoint targetPoint)
    {
        var points = new List<Windows.Foundation.Point>();
        const double minOffset = 30; // Minimum distance to extend from connection point

        // Determine routing strategy based on connection point directions
        bool srcHorizontal = sourcePoint == ConnectionPoint.Left || sourcePoint == ConnectionPoint.Right;
        bool tgtHorizontal = targetPoint == ConnectionPoint.Left || targetPoint == ConnectionPoint.Right;

        if (srcHorizontal && tgtHorizontal)
        {
            // Both horizontal (Left/Right to Left/Right)
            var srcRight = sourcePoint == ConnectionPoint.Right;
            var tgtRight = targetPoint == ConnectionPoint.Right;

            if (srcRight && !tgtRight && end.X > start.X)
            {
                // Right to Left, target is to the right - direct horizontal possible
                var midX = (start.X + end.X) / 2;
                points.Add(new Windows.Foundation.Point(midX, start.Y));
                points.Add(new Windows.Foundation.Point(midX, end.Y));
            }
            else if (!srcRight && tgtRight && end.X < start.X)
            {
                // Left to Right, target is to the left - direct horizontal possible
                var midX = (start.X + end.X) / 2;
                points.Add(new Windows.Foundation.Point(midX, start.Y));
                points.Add(new Windows.Foundation.Point(midX, end.Y));
            }
            else
            {
                // Need to go around
                var offsetX = srcRight ? Math.Max(start.X, end.X) + minOffset : Math.Min(start.X, end.X) - minOffset;
                points.Add(new Windows.Foundation.Point(offsetX, start.Y));
                points.Add(new Windows.Foundation.Point(offsetX, end.Y));
            }
        }
        else if (!srcHorizontal && !tgtHorizontal)
        {
            // Both vertical (Top/Bottom to Top/Bottom)
            var srcDown = sourcePoint == ConnectionPoint.Bottom;
            var tgtDown = targetPoint == ConnectionPoint.Bottom;

            if (srcDown && !tgtDown && end.Y > start.Y)
            {
                // Bottom to Top, target is below - can go mostly straight down
                if (Math.Abs(start.X - end.X) < 5)
                {
                    // Nearly aligned - direct line
                    // No intermediate points needed
                }
                else
                {
                    // Offset horizontally
                    var midY = (start.Y + end.Y) / 2;
                    points.Add(new Windows.Foundation.Point(start.X, midY));
                    points.Add(new Windows.Foundation.Point(end.X, midY));
                }
            }
            else if (!srcDown && tgtDown && end.Y < start.Y)
            {
                // Top to Bottom, target is above - can go mostly straight up
                if (Math.Abs(start.X - end.X) < 5)
                {
                    // Nearly aligned - direct line
                    // No intermediate points needed
                }
                else
                {
                    // Offset horizontally
                    var midY = (start.Y + end.Y) / 2;
                    points.Add(new Windows.Foundation.Point(start.X, midY));
                    points.Add(new Windows.Foundation.Point(end.X, midY));
                }
            }
            else
            {
                // Same direction or need to go around
                var offsetY = srcDown ? Math.Max(start.Y, end.Y) + minOffset : Math.Min(start.Y, end.Y) - minOffset;
                points.Add(new Windows.Foundation.Point(start.X, offsetY));
                points.Add(new Windows.Foundation.Point(end.X, offsetY));
            }
        }
        else
        {
            // Mixed: one horizontal, one vertical - L-shape route
            if (srcHorizontal)
            {
                // Source is horizontal (Left/Right), target is vertical (Top/Bottom)
                points.Add(new Windows.Foundation.Point(end.X, start.Y));
            }
            else
            {
                // Source is vertical (Top/Bottom), target is horizontal (Left/Right)
                points.Add(new Windows.Foundation.Point(start.X, end.Y));
            }
        }

        points.Add(end);
        return points;
    }

    /// <summary>
    /// Gets the direction vector for a connection point.
    /// </summary>
    private static (double dx, double dy) GetDirectionVector(ConnectionPoint point)
    {
        return point switch
        {
            ConnectionPoint.Top => (0, -1),
            ConnectionPoint.Bottom => (0, 1),
            ConnectionPoint.Left => (-1, 0),
            ConnectionPoint.Right => (1, 0),
            ConnectionPoint.TopLeft => (-1, -1),
            ConnectionPoint.TopRight => (1, -1),
            ConnectionPoint.BottomLeft => (-1, 1),
            ConnectionPoint.BottomRight => (1, 1),
            _ => (0, 0)
        };
    }

    /// <summary>
    /// Gets a point offset from the given point in the direction of the connection point.
    /// </summary>
    private static Windows.Foundation.Point GetOffsetPoint(Windows.Foundation.Point point, ConnectionPoint connectionPoint, double offset)
    {
        var (dx, dy) = GetDirectionVector(connectionPoint);
        return new Windows.Foundation.Point(point.X + dx * offset, point.Y + dy * offset);
    }

    private void AddArrowHead(string edgeId, Windows.Foundation.Point start, Windows.Foundation.Point end, ArrowType arrowType, SolidColorBrush brush)
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

        // Remove old arrow if exists
        if (_arrowHeads.TryGetValue(edgeId, out var oldArrow))
        {
            Children.Remove(oldArrow);
        }

        _arrowHeads[edgeId] = arrow;
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
            // Remove waypoint handles
            if (_waypointHandles.TryGetValue(oldEdge.Id, out var oldHandles))
            {
                foreach (var handle in oldHandles)
                {
                    Children.Remove(handle);
                }
                _waypointHandles.Remove(oldEdge.Id);
            }
            // Refresh the edge to update appearance
            RefreshEdge(oldEdge);
        }

        if (newEdge != null)
        {
            newEdge.IsSelected = true;
            // Refresh to show waypoint handles
            RefreshEdge(newEdge);
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
        var point = e.GetCurrentPoint(this).Position;

        // Handle node dragging
        if (_draggedNode != null && _draggedNodeElement != null)
        {
            var newX = point.X - _dragOffset.X;
            var newY = point.Y - _dragOffset.Y;

            var padding = IsConnectMode ? ConnectionPointPadding : 0;
            // Offset position by padding so the shape stays at the correct visual position
            SetLeft(_draggedNodeElement, newX - padding);
            SetTop(_draggedNodeElement, newY - padding);

            _draggedNode.X = newX;
            _draggedNode.Y = newY;

            // Refresh edges connected to this node
            RefreshConnectedEdges(_draggedNode.Id);
            return;
        }

        // Handle waypoint dragging
        if (_isDraggingWaypoint && _draggingWaypointEdgeId != null && _draggingWaypointHandle != null)
        {
            // Update handle position
            SetLeft(_draggingWaypointHandle, point.X - 6);
            SetTop(_draggingWaypointHandle, point.Y - 6);

            // Update waypoint in edge
            var edge = Diagram?.Edges.FirstOrDefault(ed => ed.Id == _draggingWaypointEdgeId);
            if (edge != null && _draggingWaypointIndex < edge.Waypoints.Count)
            {
                edge.Waypoints[_draggingWaypointIndex] = new Models.Point(point.X, point.Y);

                // Refresh the edge path (but not handles, to avoid flicker)
                if (_edgeElements.TryGetValue(edge.Id, out var oldPath))
                {
                    Children.Remove(oldPath);
                    var newPath = CreateEdgePathWithoutHandles(edge);
                    _edgeElements[edge.Id] = newPath;
                    Children.Insert(0, newPath);
                }
            }
            return;
        }

        // Handle connection preview
        if (_isConnecting && _connectionPreviewLine != null)
        {
            _connectionPreviewLine.X2 = point.X;
            _connectionPreviewLine.Y2 = point.Y;
        }
    }

    private void OnCanvasPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        // Finish node dragging
        if (_draggedNode != null)
        {
            NodeMoved?.Invoke(this, new NodeMovedEventArgs(_draggedNode, _draggedNode.X, _draggedNode.Y));

            // Release pointer capture
            if (_draggedNodeElement != null)
            {
                _draggedNodeElement.ReleasePointerCapture(e.Pointer);
            }

            _draggedNode = null;
            _draggedNodeElement = null;
            return;
        }

        // Finish waypoint dragging
        if (_isDraggingWaypoint && _draggingWaypointEdgeId != null)
        {
            var edge = Diagram?.Edges.FirstOrDefault(ed => ed.Id == _draggingWaypointEdgeId);
            if (edge != null)
            {
                WaypointChanged?.Invoke(this, new WaypointChangedEventArgs(edge, WaypointChangeType.Moved, _draggingWaypointIndex));
            }

            _isDraggingWaypoint = false;
            _draggingWaypointEdgeId = null;
            _draggingWaypointIndex = -1;
            if (_draggingWaypointHandle != null)
            {
                _draggingWaypointHandle.Fill = new SolidColorBrush(Colors.White);
                _draggingWaypointHandle = null;
            }
            return;
        }

        CancelConnection();
    }

    /// <summary>
    /// Creates edge path without creating waypoint handles (used during drag).
    /// </summary>
    private XamlPath CreateEdgePathWithoutHandles(DiagramEdge edge)
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

        // Use waypoints
        foreach (var wp in edge.Waypoints)
        {
            pathFigure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(wp.X, wp.Y) });
        }
        pathFigure.Segments.Add(new LineSegment { Point = endPoint });

        pathGeometry.Figures.Add(pathFigure);

        var strokeBrush = new SolidColorBrush(edge.IsSelected ? Colors.DodgerBlue : ParseColor(edge.StrokeColor));

        var path = new XamlPath
        {
            Data = pathGeometry,
            Stroke = strokeBrush,
            StrokeThickness = edge.IsSelected ? edge.StrokeWidth + 2 : edge.StrokeWidth,
            Tag = edge.Id
        };

        // Arrow
        if (edge.TargetArrow != ArrowType.None && pathFigure.Segments.Count >= 1)
        {
            Windows.Foundation.Point arrowStart = pathFigure.Segments.Count >= 2
                ? ((LineSegment)pathFigure.Segments[pathFigure.Segments.Count - 2]).Point
                : startPoint;
            AddArrowHead(edge.Id, arrowStart, endPoint, edge.TargetArrow, strokeBrush);
        }

        return path;
    }

    private void OnNodePointerPressed(DiagramNode node, FrameworkElement element, PointerRoutedEventArgs e)
    {
        e.Handled = true;

        if (IsConnectMode)
        {
            return; // Let connection point handlers handle it
        }

        NodeSelected?.Invoke(this, new NodeSelectedEventArgs(node));
        _draggedNode = node;
        _draggedNodeElement = element;

        // Capture pointer for smooth dragging even when mouse moves fast
        _dragPointerId = e.Pointer.PointerId;
        element.CapturePointer(e.Pointer);

        var point = e.GetCurrentPoint(this).Position;
        _dragOffset = new Windows.Foundation.Point(point.X - node.X, point.Y - node.Y);
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

public class WaypointChangedEventArgs : EventArgs
{
    public DiagramEdge Edge { get; }
    public WaypointChangeType ChangeType { get; }
    public int WaypointIndex { get; }

    public WaypointChangedEventArgs(DiagramEdge edge, WaypointChangeType changeType, int waypointIndex)
    {
        Edge = edge;
        ChangeType = changeType;
        WaypointIndex = waypointIndex;
    }
}

public enum WaypointChangeType
{
    Added,
    Moved,
    Removed
}

/// <summary>
/// Tag class for waypoint handles to track edge and index.
/// </summary>
internal class WaypointHandleTag
{
    public string EdgeId { get; set; } = string.Empty;
    public int WaypointIndex { get; set; }
}

#endregion
