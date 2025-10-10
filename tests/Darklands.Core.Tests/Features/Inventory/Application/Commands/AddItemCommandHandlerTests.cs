using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Inventory.Application.Commands;
using Darklands.Core.Features.Inventory.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using InventoryId = Darklands.Core.Features.Inventory.Domain.InventoryId;

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

        // TD_019: Use obsolete GetByActorIdAsync for test setup (auto-creates inventory)
        #pragma warning disable CS0618 // Type or member is obsolete
        var invResult = await repository.GetByActorIdAsync(actorId);
        #pragma warning restore CS0618
        var inventoryId = invResult.Value.Id;
        var command = new AddItemCommand(inventoryId, itemId);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var inventory = await repository.GetByIdAsync(inventoryId);
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

        // TD_019: Use obsolete GetByActorIdAsync for test setup (auto-creates inventory)
        #pragma warning disable CS0618 // Type or member is obsolete
        var invResult = await repository.GetByActorIdAsync(actorId);
        #pragma warning restore CS0618
        var inventoryId = invResult.Value.Id;

        // Fill inventory (default capacity is 20)
        for (int i = 0; i < 20; i++)
        {
            await handler.Handle(new AddItemCommand(inventoryId, ItemId.NewId()), default);
        }

        // Act
        var result = await handler.Handle(new AddItemCommand(inventoryId, ItemId.NewId()), default);

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

        // TD_019: Use obsolete GetByActorIdAsync for test setup (auto-creates inventory)
        #pragma warning disable CS0618 // Type or member is obsolete
        var invResult = await repository.GetByActorIdAsync(actorId);
        #pragma warning restore CS0618
        var inventoryId = invResult.Value.Id;

        // Add item once
        await handler.Handle(new AddItemCommand(inventoryId, itemId), default);

        // Act
        var result = await handler.Handle(new AddItemCommand(inventoryId, itemId), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already in inventory");
    }
}
