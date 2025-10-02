using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Inventory.Application.Commands;
using Darklands.Core.Features.Inventory.Application.Queries;
using Darklands.Core.Features.Inventory.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Darklands.Core.Tests.Features.Inventory.Application.Queries;

[Trait("Category", "Phase2")]
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

        // Add 2 items
        await addHandler.Handle(new AddItemCommand(actorId, item1), default);
        await addHandler.Handle(new AddItemCommand(actorId, item2), default);

        // Act
        var result = await queryHandler.Handle(new GetInventoryQuery(actorId), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.ActorId.Should().Be(actorId);
        dto.Count.Should().Be(2);
        dto.Capacity.Should().Be(20); // Default capacity
        dto.IsFull.Should().BeFalse();
        dto.Items.Should().Contain(new[] { item1, item2 });
    }

    [Fact]
    public async Task Handle_NewActor_ShouldAutoCreateInventory()
    {
        // DESIGN DECISION: Auto-create inventory on first access

        // Arrange
        var actorId = ActorId.NewId();
        var repository = new InMemoryInventoryRepository(NullLogger<InMemoryInventoryRepository>.Instance);
        var queryHandler = new GetInventoryQueryHandler(repository, NullLogger<GetInventoryQueryHandler>.Instance);

        // Act
        var result = await queryHandler.Handle(new GetInventoryQuery(actorId), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.ActorId.Should().Be(actorId);
        dto.Count.Should().Be(0);
        dto.Capacity.Should().Be(20); // Default capacity
        dto.IsFull.Should().BeFalse();
        dto.Items.Should().BeEmpty();
    }
}
