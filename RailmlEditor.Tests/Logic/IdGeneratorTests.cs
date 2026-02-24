using System.Collections.Generic;
using RailmlEditor.Logic;
using RailmlEditor.ViewModels;
using RailmlEditor.ViewModels.Elements;
using Xunit;

namespace RailmlEditor.Tests.Logic
{
    public class IdGeneratorTests
    {
        [Fact]
        public void GetNextId_EmptyList_ReturnsPrefixWith1()
        {
            // Arrange
            var generator = new IdGenerator();
            var elements = new List<BaseElementViewModel>();

            // Act
            string result = generator.GetNextId(elements, "tr");

            // Assert
            Assert.Equal("tr1", result);
        }

        [Fact]
        public void GetNextId_ExistingIdentifiers_ReturnsNextHighest()
        {
            // Arrange
            var generator = new IdGenerator();
            var elements = new List<BaseElementViewModel>
            {
                new TrackViewModel { Id = "tr1" },
                new TrackViewModel { Id = "tr5" },
                new SwitchViewModel { Id = "sw1" }
            };

            // Act
            string result = generator.GetNextId(elements, "tr");

            // Assert
            Assert.Equal("tr6", result);
        }

        [Fact]
        public void GetNextId_WithPrefixOnly_IgnoresOtherPrefixes()
        {
             // Arrange
            var generator = new IdGenerator();
            var elements = new List<BaseElementViewModel>
            {
                new SwitchViewModel { Id = "sw1" },
                new SwitchViewModel { Id = "sw10" }
            };

            // Act
            string result = generator.GetNextId(elements, "tr");

            // Assert
            Assert.Equal("tr1", result);
        }
        
        [Fact]
        public void GetNextId_InvalidFormat_IgnoresInvalidNumbers()
        {
            // Arrange
            var generator = new IdGenerator();
            var elements = new List<BaseElementViewModel>
            {
                new TrackViewModel { Id = "tr1" },
                new TrackViewModel { Id = "trABC" },
                new TrackViewModel { Id = "tr2" }
            };

            // Act
            string result = generator.GetNextId(elements, "tr");

            // Assert
            Assert.Equal("tr3", result);
        }
    }
}
