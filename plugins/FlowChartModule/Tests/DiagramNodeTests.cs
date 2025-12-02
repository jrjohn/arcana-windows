using Arcana.Plugin.FlowChart.Models;

namespace Arcana.Plugin.FlowChart.Tests;

/// <summary>
/// Unit tests for the DiagramNode model class.
/// </summary>
public class DiagramNodeTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var node = new DiagramNode();

        // Assert
        node.Id.Should().NotBeNullOrEmpty();
        node.Shape.Should().Be(NodeShape.Rectangle);
        node.Width.Should().Be(120);
        node.Height.Should().Be(60);
        node.FillColor.Should().Be("#FFFFFF");
        node.StrokeColor.Should().Be("#000000");
        node.StrokeWidth.Should().Be(2);
        node.FontSize.Should().Be(14);
        node.Text.Should().BeEmpty();
    }

    [Fact]
    public void Clone_CreatesDeepCopy()
    {
        // Arrange
        var original = new DiagramNode
        {
            Shape = NodeShape.Diamond,
            X = 100,
            Y = 200,
            Width = 150,
            Height = 80,
            Text = "Original Node",
            FillColor = "#FF0000",
            StrokeColor = "#00FF00",
            StrokeWidth = 3,
            FontSize = 16,
            ZIndex = 5
        };
        original.Metadata["key1"] = "value1";

        // Act
        var clone = original.Clone();

        // Assert
        clone.Id.Should().NotBe(original.Id); // New ID
        clone.Shape.Should().Be(NodeShape.Diamond);
        clone.X.Should().Be(120); // Offset by 20
        clone.Y.Should().Be(220); // Offset by 20
        clone.Width.Should().Be(150);
        clone.Height.Should().Be(80);
        clone.Text.Should().Be("Original Node");
        clone.FillColor.Should().Be("#FF0000");
        clone.StrokeColor.Should().Be("#00FF00");
        clone.StrokeWidth.Should().Be(3);
        clone.FontSize.Should().Be(16);
        clone.ZIndex.Should().Be(5);
        clone.Metadata.Should().ContainKey("key1");
        clone.Metadata["key1"].Should().Be("value1");
    }

    [Fact]
    public void Clone_MetadataIsIndependent()
    {
        // Arrange
        var original = new DiagramNode();
        original.Metadata["key1"] = "original";

        // Act
        var clone = original.Clone();
        clone.Metadata["key1"] = "modified";
        clone.Metadata["key2"] = "new";

        // Assert
        original.Metadata["key1"].Should().Be("original");
        original.Metadata.Should().NotContainKey("key2");
    }

    [Theory]
    [InlineData(NodeShape.Rectangle)]
    [InlineData(NodeShape.Diamond)]
    [InlineData(NodeShape.Ellipse)]
    [InlineData(NodeShape.Parallelogram)]
    [InlineData(NodeShape.Cylinder)]
    [InlineData(NodeShape.Cloud)]
    [InlineData(NodeShape.Document)]
    [InlineData(NodeShape.Hexagon)]
    public void Shape_CanBeSetToAnyValue(NodeShape shape)
    {
        // Arrange & Act
        var node = new DiagramNode { Shape = shape };

        // Assert
        node.Shape.Should().Be(shape);
    }

    [Fact]
    public void IsSelected_DefaultsFalse()
    {
        // Act
        var node = new DiagramNode();

        // Assert
        node.IsSelected.Should().BeFalse();
    }

    [Fact]
    public void Id_IsUniquePerInstance()
    {
        // Act
        var node1 = new DiagramNode();
        var node2 = new DiagramNode();

        // Assert
        node1.Id.Should().NotBe(node2.Id);
    }
}
