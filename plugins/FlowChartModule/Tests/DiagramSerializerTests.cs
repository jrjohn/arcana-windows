using Arcana.Plugin.FlowChart.Models;
using Arcana.Plugin.FlowChart.Services;

namespace Arcana.Plugin.FlowChart.Tests;

/// <summary>
/// Unit tests for the DiagramSerializer service.
/// </summary>
public class DiagramSerializerTests
{
    private readonly DiagramSerializer _serializer;

    public DiagramSerializerTests()
    {
        _serializer = new DiagramSerializer();
    }

    [Fact]
    public void SerializeToJson_ReturnsValidJson()
    {
        // Arrange
        var diagram = Diagram.CreateSample();

        // Act
        var json = _serializer.SerializeToJson(diagram);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"name\":");
        json.Should().Contain("Sample Flowchart");
        json.Should().Contain("\"nodes\":");
        json.Should().Contain("\"edges\":");
    }

    [Fact]
    public void DeserializeFromJson_RestoresDiagram()
    {
        // Arrange
        var original = Diagram.CreateSample();
        var json = _serializer.SerializeToJson(original);

        // Act
        var restored = _serializer.DeserializeFromJson(json);

        // Assert
        restored.Should().NotBeNull();
        restored!.Name.Should().Be(original.Name);
        restored.Nodes.Should().HaveCount(original.Nodes.Count);
        restored.Edges.Should().HaveCount(original.Edges.Count);
    }

    [Fact]
    public void SerializeDeserialize_PreservesNodeProperties()
    {
        // Arrange
        var diagram = Diagram.CreateNew();
        var node = new DiagramNode
        {
            Shape = NodeShape.Diamond,
            X = 100,
            Y = 200,
            Width = 150,
            Height = 80,
            Text = "Test Node",
            FillColor = "#FF5500",
            StrokeColor = "#00FF00",
            StrokeWidth = 3,
            FontSize = 18,
            ZIndex = 5
        };
        diagram.AddNode(node);
        var json = _serializer.SerializeToJson(diagram);

        // Act
        var restored = _serializer.DeserializeFromJson(json);

        // Assert
        restored.Should().NotBeNull();
        var restoredNode = restored!.Nodes[0];
        restoredNode.Shape.Should().Be(NodeShape.Diamond);
        restoredNode.X.Should().Be(100);
        restoredNode.Y.Should().Be(200);
        restoredNode.Width.Should().Be(150);
        restoredNode.Height.Should().Be(80);
        restoredNode.Text.Should().Be("Test Node");
        restoredNode.FillColor.Should().Be("#FF5500");
        restoredNode.StrokeColor.Should().Be("#00FF00");
        restoredNode.StrokeWidth.Should().Be(3);
        restoredNode.FontSize.Should().Be(18);
        restoredNode.ZIndex.Should().Be(5);
    }

    [Fact]
    public void SerializeDeserialize_PreservesEdgeProperties()
    {
        // Arrange
        var diagram = Diagram.CreateNew();
        var node1 = new DiagramNode { Text = "Node 1" };
        var node2 = new DiagramNode { Text = "Node 2" };
        var edge = new DiagramEdge
        {
            SourceNodeId = node1.Id,
            TargetNodeId = node2.Id,
            Label = "Test Edge",
            Style = LineStyle.Dashed,
            Routing = RoutingStyle.Orthogonal,
            TargetArrow = ArrowType.Diamond,
            StrokeColor = "#FF0000"
        };
        diagram.AddNode(node1);
        diagram.AddNode(node2);
        diagram.AddEdge(edge);
        var json = _serializer.SerializeToJson(diagram);

        // Act
        var restored = _serializer.DeserializeFromJson(json);

        // Assert
        restored.Should().NotBeNull();
        var restoredEdge = restored!.Edges[0];
        restoredEdge.Label.Should().Be("Test Edge");
        restoredEdge.Style.Should().Be(LineStyle.Dashed);
        restoredEdge.Routing.Should().Be(RoutingStyle.Orthogonal);
        restoredEdge.TargetArrow.Should().Be(ArrowType.Diamond);
        restoredEdge.StrokeColor.Should().Be("#FF0000");
    }

    [Fact]
    public void SerializeToDrawIO_ReturnsValidXml()
    {
        // Arrange
        var diagram = Diagram.CreateSample();

        // Act
        var xml = _serializer.SerializeToDrawIO(diagram);

        // Assert
        xml.Should().NotBeNullOrEmpty();
        xml.Should().Contain("<mxfile");
        xml.Should().Contain("<mxGraphModel");
        xml.Should().Contain("<mxCell");
        xml.Should().Contain("Sample Flowchart");
    }

    [Fact]
    public void DeserializeFromDrawIO_RestoresDiagram()
    {
        // Arrange
        var original = Diagram.CreateSample();
        var xml = _serializer.SerializeToDrawIO(original);

        // Act
        var restored = _serializer.DeserializeFromDrawIO(xml);

        // Assert
        restored.Should().NotBeNull();
        restored!.Name.Should().Be(original.Name);
        restored.Nodes.Should().HaveCount(original.Nodes.Count);
        restored.Edges.Should().HaveCount(original.Edges.Count);
    }

    [Theory]
    [InlineData(".afc", DiagramSerializer.DiagramFormat.ArcanaJson)]
    [InlineData(".json", DiagramSerializer.DiagramFormat.Json)]
    [InlineData(".drawio", DiagramSerializer.DiagramFormat.DrawIO)]
    [InlineData(".xml", DiagramSerializer.DiagramFormat.DrawIO)]
    [InlineData(".unknown", DiagramSerializer.DiagramFormat.ArcanaJson)]
    public void GetFormatFromExtension_ReturnsCorrectFormat(string extension, DiagramSerializer.DiagramFormat expected)
    {
        // Act
        var format = DiagramSerializer.GetFormatFromExtension($"test{extension}");

        // Assert
        format.Should().Be(expected);
    }

    [Fact]
    public void DeserializeFromJson_ReturnsNullForInvalidJson()
    {
        // Act
        Action act = () => _serializer.DeserializeFromJson("not valid json");

        // Assert
        act.Should().Throw<System.Text.Json.JsonException>();
    }

    [Fact]
    public void DeserializeFromDrawIO_ReturnsNullForInvalidXml()
    {
        // Act
        var result = _serializer.DeserializeFromDrawIO("not valid xml");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveToFileAsync_CreatesFile()
    {
        // Arrange
        var diagram = Diagram.CreateSample();
        var tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.afc");

        try
        {
            // Act
            await _serializer.SaveToFileAsync(diagram, tempPath);

            // Assert
            File.Exists(tempPath).Should().BeTrue();
            var content = await File.ReadAllTextAsync(tempPath);
            content.Should().Contain("Sample Flowchart");
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task LoadFromFileAsync_LoadsSavedFile()
    {
        // Arrange
        var original = Diagram.CreateSample();
        var tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.afc");

        try
        {
            await _serializer.SaveToFileAsync(original, tempPath);

            // Act
            var loaded = await _serializer.LoadFromFileAsync(tempPath);

            // Assert
            loaded.Should().NotBeNull();
            loaded!.Name.Should().Be(original.Name);
            loaded.Nodes.Should().HaveCount(original.Nodes.Count);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task LoadFromFileAsync_ThrowsForMissingFile()
    {
        // Act
        Func<Task> act = () => _serializer.LoadFromFileAsync("/nonexistent/path.afc");

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }
}
