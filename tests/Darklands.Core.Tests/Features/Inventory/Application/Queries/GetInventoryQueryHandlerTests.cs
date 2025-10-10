using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Inventory.Application.Commands;
using Darklands.Core.Features.Inventory.Application.Queries;
using Darklands.Core.Features.Inventory.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using InventoryEntity = Darklands.Core.Features.Inventory.Domain.Inventory;
using InventoryId = Darklands.Core.Features.Inventory.Domain.InventoryId;

namespace Darklands.Core.Tests.Features.Inventory.Application.Queries;

[Trait("Category", "Inventory")]
[Trait("Category", "Unit")]
public class GetInventoryQueryHandlerTests
{
    [Fact]
    public async Task Handle_ExistingInventory_ShouldReturnDto()
    {
        // Arrange
        var actorId = ActorId.NewId();
        var item1 = ItemId.NewId();
        var item2 = ItemId.NewId();
        var repository = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);
        var addHandler = new AddItemCommandHandler(repository, NullLogger<AddItemCommandHandler>.Instance);
        var queryHandler = new GetInventoryQueryHandler(repository, NullLogger<GetInventoryQueryHandler>.Instance);

        // TD_019: Create inventory explicitly with InventoryId and ownerId
        var inventory = InventoryEntity.Create(InventoryId.NewId(), capacity: 20, ownerId: actorId).Value;
        repository.RegisterInventory(inventory);

        // Add 2 items (using inventory.Id, not actorId)
        await addHandler.Handle(new AddItemCommand(inventory.Id, item1), default);
        await addHandler.Handle(new AddItemCommand(inventory.Id, item2), default);

        // Act (using inventory.Id, not actorId)
        var result = await queryHandler.Handle(new GetInventoryQuery(inventory.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.OwnerId.Should().Be(actorId); // TD_019: OwnerId is nullable
        dto.Count.Should().Be(2);
        dto.Capacity.Should().Be(20); // Default capacity
        dto.IsFull.Should().BeFalse();
        dto.Items.Should().Contain(new[] { item1, item2 });
    }

    [Fact]
    public async Task Handle_EmptyInventory_ShouldReturnEmptyDto()
    {
        // TD_019: Test renamed - no more auto-creation, inventories must be explicitly created

        // Arrange
        var actorId = ActorId.NewId();
        var repository = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);
        var queryHandler = new GetInventoryQueryHandler(repository, NullLogger<GetInventoryQueryHandler>.Instance);

        // TD_019: Create empty inventory explicitly
        var inventory = InventoryEntity.Create(InventoryId.NewId(), capacity: 20, ownerId: actorId).Value;
        repository.RegisterInventory(inventory);

        // Act (using inventory.Id)
        var result = await queryHandler.Handle(new GetInventoryQuery(inventory.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.OwnerId.Should().Be(actorId); // TD_019: OwnerId is nullable
        dto.Count.Should().Be(0);
        dto.Capacity.Should().Be(20); // Default capacity
        dto.IsFull.Should().BeFalse();
        dto.Items.Should().BeEmpty();
    }
}
