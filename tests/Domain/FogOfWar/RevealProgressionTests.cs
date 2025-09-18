using System;
using System.Collections.Generic;
using System.Linq;
using Darklands.Domain.Grid;
using Darklands.Domain.FogOfWar;
using Darklands.Core.Tests.TestUtilities;
using Xunit;
using FluentAssertions;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Darklands.Core.Tests.Domain.FogOfWar
{
    /// <summary>
    /// Unit tests for RevealProgression value object.
    /// Tests the core business logic for progressive FOV revelation.
    /// </summary>
    [Trait("Category", "Domain")]
    [Trait("Category", "FogOfWar")]
    [Trait("Category", "Phase1")]
    public class RevealProgressionTests
    {
        private readonly ActorId _testActorId = ActorId.NewId(TestIdGenerator.Instance);
        private readonly List<Position> _testPath = new()
        {
            new Position(0, 0),
            new Position(1, 0),
            new Position(2, 0),
            new Position(3, 0)
        };

        [Fact]
        public void Create_ValidInput_Success()
        {
            // Act
            var result = RevealProgression.Create(_testActorId, _testPath, 200, 0);

            // Assert
            result.IsSucc.Should().BeTrue();
            result.Match(
                Succ: progression =>
                {
                    progression.ActorId.Should().Be(_testActorId);
                    progression.Path.Should().Equal(_testPath);
                    progression.CurrentIndex.Should().Be(0);
                    progression.MillisecondsPerStep.Should().Be(200);
                    progression.NextAdvanceTimeMs.Should().Be(200);
                    progression.CurrentRevealPosition.Should().Be(new Position(0, 0));
                    progression.HasMoreSteps.Should().BeTrue();
                },
                Fail: _ => throw new Exception("Expected success")
            );
        }

        [Fact]
        public void Create_EmptyPath_Fails()
        {
            // Act
            var result = RevealProgression.Create(_testActorId, new List<Position>());

            // Assert
            result.IsFail.Should().BeTrue();
            result.Match(
                Succ: _ => throw new Exception("Expected failure"),
                Fail: error => error.Message.Should().Contain("Path cannot be empty")
            );
        }

        [Fact]
        public void Create_InvalidTimingParameter_Fails()
        {
            // Act
            var result = RevealProgression.Create(_testActorId, _testPath, 0); // Invalid timing

            // Assert
            result.IsFail.Should().BeTrue();
            result.Match(
                Succ: _ => throw new Exception("Expected failure"),
                Fail: error => error.Message.Should().Contain("MillisecondsPerStep must be positive")
            );
        }

        [Fact]
        public void TryAdvance_NotEnoughTime_NoAdvancement()
        {
            // Arrange
            var progression = RevealProgression.Create(_testActorId, _testPath, 200, 100).Match(
                Succ: p => p,
                Fail: _ => throw new Exception("Test setup failed")
            );

            // Act - Try to advance before enough time has passed
            var (newProgression, advancementEvent) = progression.TryAdvance(299, 1); // 299 < 300

            // Assert
            newProgression.Should().Be(progression); // No change
            advancementEvent.IsNone.Should().BeTrue();
        }

        [Fact]
        public void TryAdvance_EnoughTime_AdvancesCorrectly()
        {
            // Arrange
            var progression = RevealProgression.Create(_testActorId, _testPath, 200, 100).Match(
                Succ: p => p,
                Fail: _ => throw new Exception("Test setup failed")
            );

            // Act - Advance with enough time
            var (newProgression, advancementEvent) = progression.TryAdvance(300, 1); // 300 >= 300

            // Assert
            newProgression.Should().NotBe(progression);
            newProgression.CurrentIndex.Should().Be(1);
            newProgression.CurrentRevealPosition.Should().Be(new Position(1, 0));
            newProgression.NextAdvanceTimeMs.Should().Be(500); // 300 + 200

            advancementEvent.IsSome.Should().BeTrue();
            advancementEvent.Match(
                Some: evt =>
                {
                    evt.ActorId.Should().Be(_testActorId);
                    evt.NewRevealPosition.Should().Be(new Position(1, 0));
                    evt.PreviousPosition.Should().Be(new Position(0, 0));
                    evt.Turn.Should().Be(1);
                },
                None: () => throw new Exception("Expected advancement event")
            );
        }

        [Fact]
        public void TryAdvance_AtEndOfPath_NoAdvancement()
        {
            // Arrange - Create progression at end of path
            var progression = RevealProgression.Create(_testActorId, _testPath, 200, 0).Match(
                Succ: p => p,
                Fail: _ => throw new Exception("Test setup failed")
            );

            // Move to end
            var finalProgression = progression;
            for (int i = 0; i < _testPath.Count - 1; i++)
            {
                var (newProg, _) = finalProgression.TryAdvance(finalProgression.NextAdvanceTimeMs, 1);
                finalProgression = newProg;
            }

            // Act - Try to advance beyond end
            var (resultProgression, advancementEvent) = finalProgression.TryAdvance(finalProgression.NextAdvanceTimeMs + 1000, 1);

            // Assert
            resultProgression.Should().Be(finalProgression); // No change
            advancementEvent.IsNone.Should().BeTrue();
            finalProgression.HasMoreSteps.Should().BeFalse();
        }

        [Fact]
        public void TryCreateCompletionEvent_NotComplete_ReturnsNone()
        {
            // Arrange
            var progression = RevealProgression.Create(_testActorId, _testPath, 200, 0).Match(
                Succ: p => p,
                Fail: _ => throw new Exception("Test setup failed")
            );

            // Act
            var completionEvent = progression.TryCreateCompletionEvent(1);

            // Assert
            completionEvent.IsNone.Should().BeTrue();
        }

        [Fact]
        public void TryCreateCompletionEvent_Complete_ReturnsEvent()
        {
            // Arrange - Create progression and advance to completion
            var progression = RevealProgression.Create(_testActorId, _testPath, 200, 0).Match(
                Succ: p => p,
                Fail: _ => throw new Exception("Test setup failed")
            );

            // Advance to end
            var finalProgression = progression;
            for (int i = 0; i < _testPath.Count - 1; i++)
            {
                var (newProg, _) = finalProgression.TryAdvance(finalProgression.NextAdvanceTimeMs, 1);
                finalProgression = newProg;
            }

            // Act
            var completionEvent = finalProgression.TryCreateCompletionEvent(2);

            // Assert
            completionEvent.IsSome.Should().BeTrue();
            completionEvent.Match(
                Some: evt =>
                {
                    evt.ActorId.Should().Be(_testActorId);
                    evt.FinalPosition.Should().Be(new Position(3, 0)); // Last position in path
                    evt.Turn.Should().Be(2);
                },
                None: () => throw new Exception("Expected completion event")
            );
        }

        [Fact]
        public void FullProgressionFlow_MultipleAdvances_WorksCorrectly()
        {
            // Arrange
            var progression = RevealProgression.Create(_testActorId, _testPath, 100, 0).Match(
                Succ: p => p,
                Fail: _ => throw new Exception("Test setup failed")
            );
            var currentTime = 0;
            var events = new List<RevealPositionAdvanced>();

            // Act - Simulate full progression
            var currentProgression = progression;
            while (currentProgression.HasMoreSteps)
            {
                currentTime += 100; // Advance time
                var (newProgression, advancementEvent) = currentProgression.TryAdvance(currentTime, 1);

                advancementEvent.Match(
                    Some: evt => events.Add(evt),
                    None: () => { }
                );

                currentProgression = newProgression;
            }

            // Assert
            events.Should().HaveCount(3); // 4 positions, 3 advancements
            events[0].NewRevealPosition.Should().Be(new Position(1, 0));
            events[1].NewRevealPosition.Should().Be(new Position(2, 0));
            events[2].NewRevealPosition.Should().Be(new Position(3, 0));

            // Final state
            currentProgression.HasMoreSteps.Should().BeFalse();
            currentProgression.CurrentRevealPosition.Should().Be(new Position(3, 0));

            // Completion event available
            var completionEvent = currentProgression.TryCreateCompletionEvent(1);
            completionEvent.IsSome.Should().BeTrue();
        }
    }
}
