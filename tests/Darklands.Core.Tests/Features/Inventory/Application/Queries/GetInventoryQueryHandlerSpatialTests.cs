using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Inventory.Application.Queries;
using Darklands.Core.Features.Inventory.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Darklands.Core.Tests.Features.Inventory.Application.Queries;

[Trait("Category", "Phase2")]
[Trait("Category", "Unit")]
public class GetInventoryQueryHandlerSpatialTests
{
    [Fact]
    public async Task Handle_SpatialInventory_ShouldIncludeGridData()
    {
        // VERIFY: Enhanced DTO includes spatial fields

        // Arrange
        var actorId = ActorId.NewId();
        var repository = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);
        var handler = new GetInventoryQueryHandler(
            repository,
            NullLogger<GetInventoryQueryHandler>.Instance);

        // Create spatial inventory (default 20 capacity → 5×4 grid)
        var inventory = await repository.GetByActorIdAsync(actorId);

        var query = new GetInventoryQuery(actorId);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.GridWidth.Should().Be(5);
        result.Value.GridHeight.Should().Be(4);
        result.Value.Capacity.Should().Be(20);
        result.Value.ContainerType.Should().Be(ContainerType.General);
        result.Value.ItemPlacements.Should().NotBeNull();
        result.Value.ItemPlacements.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_InventoryWithItems_ShouldIncludeItemPlacements()
    {
        // VERIFY: ItemPlacements dictionary populated correctly

        // Arrange
        var actorId = ActorId.NewId();
        var itemId1 = ItemId.NewId();
        var itemId2 = ItemId.NewId();
        var pos1 = new GridPosition(0, 0);
        var pos2 = new GridPosition(3, 2);

        var repository = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);

        // Place items
        var inventory = await repository.GetByActorIdAsync(actorId);
        inventory.Value.PlaceItemAt(itemId1, pos1);
        inventory.Value.PlaceItemAt(itemId2, pos2);
        await repository.SaveAsync(inventory.Value, default);

        var handler = new GetInventoryQueryHandler(
            repository,
            NullLogger<GetInventoryQueryHandler>.Instance);

        var query = new GetInventoryQuery(actorId);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().Be(2);
        result.Value.ItemPlacements.Should().ContainKey(itemId1);
        result.Value.ItemPlacements[itemId1].Should().Be(pos1);
        result.Value.ItemPlacements[itemId2].Should().Be(pos2);

        // BACKWARD COMPAT: Items list still works
        result.Value.Items.Should().Contain(itemId1);
        result.Value.Items.Should().Contain(itemId2);
    }
}
