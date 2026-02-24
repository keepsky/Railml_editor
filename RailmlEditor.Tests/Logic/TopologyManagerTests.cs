using System.Collections.Generic;
using RailmlEditor.Logic;
using RailmlEditor.ViewModels;
using RailmlEditor.ViewModels.Elements;
using Xunit;

namespace RailmlEditor.Tests.Logic
{
    public class TopologyManagerTests
    {
        [Fact]
        public void CheckConnections_ConnectsToNearbyTrack()
        {
            // Arrange
            var manager = new TopologyManager();
            var t1 = new TrackViewModel { Id = "tr1", X = 0, Y = 0, X2 = 100, Y2 = 0 };
            var t2 = new TrackViewModel { Id = "tr2", X = 100, Y = 0, X2 = 200, Y2 = 0 };
            
            var elements = new List<BaseElementViewModel> { t1, t2 };

            // Act
            // Check connections for t1
            manager.CheckConnections(t1, elements);

            // Assert
            Assert.Equal(TrackNodeType.Connection, t1.EndNode.NodeType);
            Assert.Equal("tr2", t1.EndNode.ConnectedTrackId);
            
            Assert.Equal(TrackNodeType.Connection, t2.BeginNode.NodeType);
            Assert.Equal("tr1", t2.BeginNode.ConnectedTrackId);
        }

        [Fact]
        public void CheckConnections_NoNearbyElements_DisconnectsNode()
        {
            // Arrange
            var manager = new TopologyManager();
            var t1 = new TrackViewModel { Id = "tr1", X = 0, Y = 0, X2 = 100, Y2 = 0 };
            t1.EndNode.NodeType = TrackNodeType.Connection;
            t1.EndNode.ConnectedTrackId = "trUnknown";
            
            var elements = new List<BaseElementViewModel> { t1 };

            // Act
            manager.CheckConnections(t1, elements);

            // Assert
            Assert.Equal(TrackNodeType.None, t1.EndNode.NodeType);
            Assert.Null(t1.EndNode.ConnectedTrackId);
        }
        
        [Fact]
        public void CheckConnections_ConnectsToNearbySwitch()
        {
            // Arrange
            var manager = new TopologyManager();
            var t1 = new TrackViewModel { Id = "tr1", X = 0, Y = 0, X2 = 100, Y2 = 0 };
            var sw = new SwitchViewModel { Id = "sw1", X = 100, Y = 0 };
            
            var elements = new List<BaseElementViewModel> { t1, sw };

            // Act
            manager.CheckConnections(t1, elements);

            // Assert
            Assert.Equal(TrackNodeType.Connection, t1.EndNode.NodeType);
            Assert.Equal("sw1", t1.EndNode.ConnectedTrackId);
        }
    }
}
