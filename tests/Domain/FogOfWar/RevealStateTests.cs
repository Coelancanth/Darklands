using Darklands.Domain.Grid;
using Darklands.Domain.FogOfWar;
using Xunit;
using FluentAssertions;

namespace Darklands.Core.Tests.Domain.FogOfWar
{
    /// <summary>
    /// Unit tests for RevealState value object.
    /// Tests the simple state representation for actor reveal positions.
    /// </summary>
    [Trait("Category", "Domain")]
    [Trait("Category", "FogOfWar")]
    [Trait("Category", "Phase1")]
    public class RevealStateTests
    {
        [Fact]
        public void AtRest_CreatesCorrectState()
        {
            // Arrange
            var position = new Position(5, 3);

            // Act
            var state = RevealState.AtRest(position);

            // Assert
            state.CurrentRevealPosition.Should().Be(position);
            state.IsProgressing.Should().BeFalse();
            state.NextAdvanceTimeMs.Should().Be(0);
        }

        [Fact]
        public void Progressing_CreatesCorrectState()
        {
            // Arrange
            var position = new Position(2, 4);
            var nextAdvanceTime = 1500;

            // Act
            var state = RevealState.Progressing(position, nextAdvanceTime);

            // Assert
            state.CurrentRevealPosition.Should().Be(position);
            state.IsProgressing.Should().BeTrue();
            state.NextAdvanceTimeMs.Should().Be(nextAdvanceTime);
        }

        [Fact]
        public void ValueEquality_SameValues_AreEqual()
        {
            // Arrange
            var position = new Position(1, 1);
            var state1 = RevealState.AtRest(position);
            var state2 = RevealState.AtRest(position);

            // Act & Assert
            state1.Should().Be(state2);
            (state1 == state2).Should().BeTrue();
            state1.GetHashCode().Should().Be(state2.GetHashCode());
        }

        [Fact]
        public void ValueEquality_DifferentValues_AreNotEqual()
        {
            // Arrange
            var state1 = RevealState.AtRest(new Position(1, 1));
            var state2 = RevealState.AtRest(new Position(2, 2));

            // Act & Assert
            state1.Should().NotBe(state2);
            (state1 == state2).Should().BeFalse();
        }
    }
}
