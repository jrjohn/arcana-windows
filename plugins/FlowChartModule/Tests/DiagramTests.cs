using Arcana.Plugin.FlowChart.Models;

namespace Arcana.Plugin.FlowChart.Tests;

/// <summary>
/// Unit tests for the Diagram model class.
/// </summary>
public class DiagramTests
{
    [Fact]
    public void CreateNew_ReturnsEmptyDiagram()
    {
        // Act
        var diagram = Diagram.CreateNew("Test Diagram");

        // Assert
        diagram.Name.Should().Be("Test Diagram");
        diagram.Nodes.Should().BeEmpty();
        diagram.Edges.Should().BeEmpty();
        diagram.ShowGrid.Should().BeTrue();
        diagram.SnapToGrid.Should().BeTrue();
    }

    [Fact]
    public void CreateSample_ReturnsPopulatedDiagram()
    {
        // Act
        var diagram = Diagram.CreateSample();

        // Assert
        diagram.Name.Should().Be("Sample Flowchart");
        diagram.Nodes.Should().HaveCount(6);
        diagram.Edges.Should().HaveCount(6);
    }

    [Fact]
    public void AddNode_AddsNodeToList()
    {
        // Arrange
        var diagram = Diagram.CreateNew();
        var node = new DiagramNode
        {
            Text = "Test Node",
            Shape = NodeShape.Rectangle
        };

        // Act
        diagram.AddNode(node);

        // Assert
        diagram.Nodes.Should().HaveCount(1);
        diagram.Nodes[0].Text.Should().Be("Test Node");
    }

    [Fact]
    public void RemoveNode_RemovesNodeAndConnectedEdges()
    {
        // Arrange
        var diagram = Diagram.CreateNew();
        var node1 = new DiagramNode { Text = "Node 1" };
        var node2 = new DiagramNode { Text = "Node 2" };
        var edge = new DiagramEdge
        {
            SourceNodeId = node1.Id,
            TargetNodeId = node2.Id
        };

        diagram.AddNode(node1);
        diagram.AddNode(node2);
        diagram.AddEdge(edge);

        // Act
        diagram.RemoveNode(node1.Id);

        // Assert
        diagram.Nodes.Should().HaveCount(1);
        diagram.Nodes[0].Text.Should().Be("Node 2");
        diagram.Edges.Should().BeEmpty(); // Edge should be removed too
    }

    [Fact]
    public void GetNode_ReturnsCorrectNode()
    {
        // Arrange
        var diagram = Diagram.CreateNew();
        var node = new DiagramNode { Text = "Find Me" };
        diagram.AddNode(node);

        // Act
        var found = diagram.GetNode(node.Id);

        // Assert
        found.Should().NotBeNull();
        found!.Text.Should().Be("Find Me");
    }

    [Fact]
    public void GetNode_ReturnsNullForNonexistent()
    {
        // Arrange
        var diagram = Diagram.CreateNew();

        // Act
        var found = diagram.GetNode("nonexistent-id");

        // Assert
        found.Should().BeNull();
    }

    [Fact]
    public void GetEdgesForNode_ReturnsConnectedEdges()
    {
        // Arrange
        var diagram = Diagram.CreateNew();
        var node1 = new DiagramNode { Text = "Node 1" };
        var node2 = new DiagramNode { Text = "Node 2" };
        var node3 = new DiagramNode { Text = "Node 3" };
        var edge1 = new DiagramEdge { SourceNodeId = node1.Id, TargetNodeId = node2.Id };
        var edge2 = new DiagramEdge { SourceNodeId = node2.Id, TargetNodeId = node3.Id };
        var edge3 = new DiagramEdge { SourceNodeId = node1.Id, TargetNodeId = node3.Id };

        diagram.AddNode(node1);
        diagram.AddNode(node2);
        diagram.AddNode(node3);
        diagram.AddEdge(edge1);
        diagram.AddEdge(edge2);
        diagram.AddEdge(edge3);

        // Act
        var edges = diagram.GetEdgesForNode(node1.Id).ToList();

        // Assert
        edges.Should().HaveCount(2);
        edges.Should().Contain(e => e.Id == edge1.Id);
        edges.Should().Contain(e => e.Id == edge3.Id);
    }

    [Fact]
    public void GetNextZIndex_ReturnsIncrementingValue()
    {
        // Arrange
        var diagram = Diagram.CreateNew();
        var node1 = new DiagramNode { ZIndex = 5 };
        var node2 = new DiagramNode { ZIndex = 3 };
        diagram.AddNode(node1);
        diagram.AddNode(node2);

        // Act
        var nextZ = diagram.GetNextZIndex();

        // Assert
        nextZ.Should().Be(6);
    }

    [Fact]
    public void AddNode_UpdatesModifiedAt()
    {
        // Arrange
        var diagram = Diagram.CreateNew();
        var originalModified = diagram.ModifiedAt;
        Thread.Sleep(10); // Ensure time difference

        // Act
        diagram.AddNode(new DiagramNode());

        // Assert
        diagram.ModifiedAt.Should().BeAfter(originalModified);
    }
}
