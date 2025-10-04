using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Inventory.Application.Commands;
using Darklands.Core.Features.Inventory.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Darklands.Core.Tests.Features.Inventory.Application.Commands;

[Trait("Category", "Inventory")]
[Trait("Category", "Unit")]
public class AddItemCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidItem_ShouldAddToInventory()
    {
        // Arrange
        var actorId = ActorId.NewId();
        var itemId = ItemId.NewId();
        var repository = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);
        var handler = new AddItemCommandHandler(repository, NullLogger<AddItemCommandHandler>.Instance);
        var command = new AddItemCommand(actorId, itemId);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var inventory = await repository.GetByActorIdAsync(actorId);
        inventory.Value.Contains(itemId).Should().BeTrue();
        inventory.Value.Count.Should().Be(1);
    }

    [Fact]
    public async Task Handle_FullInventory_ShouldFail()
    {
        // BUSINESS RULE: Cannot add items when inventory is full

        // Arrange
        var actorId = ActorId.NewId();
        var repository = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);
        var handler = new AddItemCommandHandler(repository, NullLogger<AddItemCommandHandler>.Instance);

        // Fill inventory (default capacity is 20)
        for (int i = 0; i < 20; i++)
        {
            await handler.Handle(new AddItemCommand(actorId, ItemId.NewId()), default);
        }

        // Act
        var result = await handler.Handle(new AddItemCommand(actorId, ItemId.NewId()), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("full");
    }

    [Fact]
    public async Task Handle_DuplicateItem_ShouldFail()
    {
        // BUSINESS RULE: Cannot add same item twice

        // Arrange
        var actorId = ActorId.NewId();
        var itemId = ItemId.NewId();
        var repository = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);
        var handler = new AddItemCommandHandler(repository, NullLogger<AddItemCommandHandler>.Instance);

        // Add item once
        await handler.Handle(new AddItemCommand(actorId, itemId), default);

        // Act
        var result = await handler.Handle(new AddItemCommand(actorId, itemId), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already in inventory");
    }
}
