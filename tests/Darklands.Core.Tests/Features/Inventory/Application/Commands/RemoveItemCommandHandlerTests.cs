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
public class RemoveItemCommandHandlerTests
{
    [Fact]
    public async Task Handle_ExistingItem_ShouldRemoveFromInventory()
    {
        // Arrange
        var actorId = ActorId.NewId();
        var itemId = ItemId.NewId();
        var repository = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);
        var addHandler = new AddItemCommandHandler(repository, NullLogger<AddItemCommandHandler>.Instance);
        var removeHandler = new RemoveItemCommandHandler(repository, NullLogger<RemoveItemCommandHandler>.Instance);

        var inventory = Darklands.Core.Features.Inventory.Domain.Inventory.Create(Darklands.Core.Features.Inventory.Domain.InventoryId.NewId(), 20, actorId).Value;
        repository.RegisterInventory(inventory);
        var inventoryId = inventory.Id;

        // Add item first
        await addHandler.Handle(new AddItemCommand(inventoryId, itemId), default);

        // Act
        var result = await removeHandler.Handle(new RemoveItemCommand(inventoryId, itemId), default);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var inventoryCheck = await repository.GetByIdAsync(inventoryId);
        inventoryCheck.Value.Contains(itemId).Should().BeFalse();
        inventoryCheck.Value.Count.Should().Be(0);
    }

    [Fact]
    public async Task Handle_NonExistentItem_ShouldFail()
    {
        // Arrange
        var actorId = ActorId.NewId();
        var itemId = ItemId.NewId();
        var repository = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);
        var handler = new RemoveItemCommandHandler(repository, NullLogger<RemoveItemCommandHandler>.Instance);

        var inventory = Darklands.Core.Features.Inventory.Domain.Inventory.Create(Darklands.Core.Features.Inventory.Domain.InventoryId.NewId(), 20, actorId).Value;
        repository.RegisterInventory(inventory);
        var inventoryId = inventory.Id;

        // Act
        var result = await handler.Handle(new RemoveItemCommand(inventoryId, itemId), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }
}
