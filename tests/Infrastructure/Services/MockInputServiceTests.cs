using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Darklands.Application.Services;
using Darklands.Application.Infrastructure.Services;
using Darklands.Domain.Grid;
using System.Reactive.Linq;

namespace Darklands.Core.Tests.Infrastructure.Services;

[Trait("Category", "Phase2")]
public class MockInputServiceTests
{
    private readonly MockInputService _inputService;

    public MockInputServiceTests()
    {
        _inputService = new MockInputService();
    }

    [Fact]
    public void IsActionPressed_WithUnpressedAction_ShouldReturnFalse()
    {
        // Act & Assert
        _inputService.IsActionPressed(InputAction.Attack).Should().BeFalse();
        _inputService.IsActionJustPressed(InputAction.Attack).Should().BeFalse();
        _inputService.IsActionJustReleased(InputAction.Attack).Should().BeFalse();
    }

    [Fact]
    public void SimulatePressAction_WithNewAction_ShouldSetCorrectStates()
    {
        // Arrange
        var action = InputAction.MoveUp;

        // Act
        _inputService.SimulatePressAction(action, 100);

        // Assert
        _inputService.IsActionPressed(action).Should().BeTrue();
        _inputService.IsActionJustPressed(action).Should().BeTrue();
        _inputService.IsActionJustReleased(action).Should().BeFalse();
    }

    [Fact]
    public void SimulateReleaseAction_WithPressedAction_ShouldSetCorrectStates()
    {
        // Arrange
        var action = InputAction.Defend;
        _inputService.SimulatePressAction(action);

        // Act
        _inputService.SimulateReleaseAction(action, 200);

        // Assert
        _inputService.IsActionPressed(action).Should().BeFalse();
        _inputService.IsActionJustPressed(action).Should().BeFalse();
        _inputService.IsActionJustReleased(action).Should().BeTrue();
    }

    [Fact]
    public void Update_AfterFrameUpdate_ShouldClearJustPressedAndReleased()
    {
        // Arrange
        var action = InputAction.Confirm;
        _inputService.SimulatePressAction(action);

        // Verify initial state
        _inputService.IsActionJustPressed(action).Should().BeTrue();

        // Act
        _inputService.Update();

        // Assert
        _inputService.IsActionPressed(action).Should().BeTrue(); // Still pressed
        _inputService.IsActionJustPressed(action).Should().BeFalse(); // Just-pressed cleared
        _inputService.IsActionJustReleased(action).Should().BeFalse(); // Not released
    }

    [Fact]
    public void GetMousePosition_WithSetPosition_ShouldReturnCorrectPosition()
    {
        // Arrange
        var expectedPosition = new Position(100, 200);
        _inputService.MousePosition = expectedPosition;

        // Act
        var actualPosition = _inputService.GetMousePosition();

        // Assert
        actualPosition.Should().Be(expectedPosition);
    }

    [Fact]
    public void GetWorldMousePosition_WithSetPosition_ShouldReturnCorrectPosition()
    {
        // Arrange
        var expectedWorldPosition = new Position(5, 8);
        _inputService.WorldMousePosition = expectedWorldPosition;

        // Act
        var actualPosition = _inputService.GetWorldMousePosition();

        // Assert
        actualPosition.Should().Be(expectedWorldPosition);
    }

    [Fact]
    public void SimulateMousePress_ShouldEmitMouseInputEvent()
    {
        // Arrange
        var receivedEvents = new List<InputEvent>();
        _inputService.InputEvents.Subscribe(receivedEvents.Add);

        var position = new Position(50, 75);
        var button = MouseButton.Left;

        // Act
        _inputService.SimulateMousePress(button, position, 300);

        // Assert
        receivedEvents.Should().HaveCount(1);
        var mouseEvent = receivedEvents[0].Should().BeOfType<MouseInputEvent>().Subject;
        mouseEvent.Position.Should().Be(position);
        mouseEvent.Button.Should().Be(button);
        mouseEvent.IsPressed.Should().BeTrue();
        mouseEvent.Timestamp.Should().Be(300);
    }

    [Fact]
    public void SimulateMouseRelease_ShouldEmitMouseInputEvent()
    {
        // Arrange
        var receivedEvents = new List<InputEvent>();
        _inputService.InputEvents.Subscribe(receivedEvents.Add);

        var position = new Position(25, 35);
        var button = MouseButton.Right;

        // Act
        _inputService.SimulateMouseRelease(button, position, 400);

        // Assert
        receivedEvents.Should().HaveCount(1);
        var mouseEvent = receivedEvents[0].Should().BeOfType<MouseInputEvent>().Subject;
        mouseEvent.Position.Should().Be(position);
        mouseEvent.Button.Should().Be(button);
        mouseEvent.IsPressed.Should().BeFalse();
        mouseEvent.Timestamp.Should().Be(400);
    }

    [Fact]
    public void SimulatePressAction_ShouldEmitKeyInputEvent()
    {
        // Arrange
        var receivedEvents = new List<InputEvent>();
        _inputService.InputEvents.Subscribe(receivedEvents.Add);

        var action = InputAction.SpecialAction;

        // Act
        _inputService.SimulatePressAction(action, 500);

        // Assert
        receivedEvents.Should().HaveCount(1);
        var keyEvent = receivedEvents[0].Should().BeOfType<KeyInputEvent>().Subject;
        keyEvent.Action.Should().Be(action);
        keyEvent.IsPressed.Should().BeTrue();
        keyEvent.Timestamp.Should().Be(500);
        keyEvent.Handled.Should().BeFalse();
    }

    [Fact]
    public void GetActionStates_ShouldReturnCurrentStateSnapshot()
    {
        // Arrange
        _inputService.SimulatePressAction(InputAction.MoveLeft);
        _inputService.SimulatePressAction(InputAction.Attack);

        // Act
        var states = _inputService.GetActionStates();

        // Assert
        states[InputAction.MoveLeft].Should().BeTrue();
        states[InputAction.Attack].Should().BeTrue();
        states[InputAction.MoveRight].Should().BeFalse();
        states[InputAction.Defend].Should().BeFalse();

        // Verify it's a snapshot (not live)
        _inputService.SimulateReleaseAction(InputAction.MoveLeft);
        states[InputAction.MoveLeft].Should().BeTrue(); // Snapshot unchanged
    }

    [Fact]
    public void Reset_ShouldClearAllInputStates()
    {
        // Arrange
        _inputService.MousePosition = new Position(999, 888);
        _inputService.WorldMousePosition = new Position(10, 20);
        _inputService.SimulatePressAction(InputAction.Menu);
        _inputService.SimulatePressAction(InputAction.Inventory);

        // Act
        _inputService.Reset();

        // Assert
        _inputService.MousePosition.Should().Be(Position.Zero);
        _inputService.WorldMousePosition.Should().Be(Position.Zero);

        var states = _inputService.GetActionStates();
        states.Values.Should().AllSatisfy(pressed => pressed.Should().BeFalse());
    }

    [Fact]
    public void SimulatePressAction_WhenAlreadyPressed_ShouldNotSetJustPressed()
    {
        // Arrange
        var action = InputAction.ZoomIn;
        _inputService.SimulatePressAction(action); // First press

        // Act
        _inputService.SimulatePressAction(action); // Second press while held

        // Assert
        _inputService.IsActionPressed(action).Should().BeTrue();
        _inputService.IsActionJustPressed(action).Should().BeFalse(); // Not just-pressed anymore
    }

    [Fact]
    public void SimulateReleaseAction_WhenNotPressed_ShouldNotSetJustReleased()
    {
        // Arrange
        var action = InputAction.ZoomOut;
        // Don't press the action first

        // Act
        _inputService.SimulateReleaseAction(action);

        // Assert
        _inputService.IsActionPressed(action).Should().BeFalse();
        _inputService.IsActionJustReleased(action).Should().BeFalse(); // Not just-released
    }

    [Fact]
    public void InputEvents_ShouldProvideReactiveStream()
    {
        // Arrange
        var eventCount = 0;
        var lastEvent = default(InputEvent);

        _inputService.InputEvents.Subscribe(e =>
        {
            eventCount++;
            lastEvent = e;
        });

        // Act
        _inputService.SimulatePressAction(InputAction.Cancel, 600);
        _inputService.SimulateMousePress(MouseButton.Middle, new Position(10, 10), 700);

        // Assert
        eventCount.Should().Be(2);
        lastEvent.Should().BeOfType<MouseInputEvent>();
        lastEvent!.Timestamp.Should().Be(700);
    }

    [Theory]
    [InlineData(InputAction.MoveUp)]
    [InlineData(InputAction.Attack)]
    [InlineData(InputAction.Menu)]
    [InlineData(InputAction.QuickSave)]
    public void AllInputActions_ShouldBeSupportedByMock(InputAction action)
    {
        // Act & Assert - Should not throw
        _inputService.SimulatePressAction(action);
        _inputService.IsActionPressed(action).Should().BeTrue();

        _inputService.SimulateReleaseAction(action);
        _inputService.IsActionPressed(action).Should().BeFalse();
    }

    [Fact]
    public void Dispose_ShouldCompleteInputEventStream()
    {
        // Arrange
        var completed = false;
        _inputService.InputEvents.Subscribe(
            onNext: _ => { },
            onCompleted: () => completed = true
        );

        // Act
        _inputService.Dispose();

        // Assert
        completed.Should().BeTrue();
    }
}
