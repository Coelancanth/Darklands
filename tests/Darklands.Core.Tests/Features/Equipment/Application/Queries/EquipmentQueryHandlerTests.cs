using Darklands.Core.Domain.Common;
using Darklands.Core.Domain.Entities;
using Darklands.Core.Features.Equipment.Application.Queries;
using Darklands.Core.Features.Equipment.Domain;
using Darklands.Core.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Darklands.Core.Tests.Features.Equipment.Application.Queries;

/// <summary>
/// Tests for equipment query handlers.
/// Verifies queries return correct data for UI display and combat system.
/// </summary>
[Trait("Category", "Equipment")]
[Trait("Category", "Unit")]
public class EquipmentQueryHandlerTests
{
    #region GetEquippedItemsQuery Tests

    [Fact]
    public async Task GetEquippedItems_ActorWithEquipment_ShouldReturnAllEquippedItems()
    {
        // WHY: Core query - UI needs all equipped items to display equipment panel

        // Arrange
        var actors = new InMemoryActorRepository();
        var actor = new Actor(ActorId.NewId(), "ACTOR_TEST");
        var equipment = new EquipmentComponent(actor.Id);

        var sword = ItemId.NewId();
        var shield = ItemId.NewId();
        var helmet = ItemId.NewId();

        equipment.EquipItem(EquipmentSlot.MainHand, sword);
        equipment.EquipItem(EquipmentSlot.OffHand, shield);
        equipment.EquipItem(EquipmentSlot.Head, helmet);

        actor.AddComponent<IEquipmentComponent>(equipment);
        await actors.AddActorAsync(actor);

        var handler = new GetEquippedItemsQueryHandler(actors, NullLogger<GetEquippedItemsQueryHandler>.Instance);

        // Act
        var result = await handler.Handle(new GetEquippedItemsQuery(actor.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value[EquipmentSlot.MainHand].Should().Be(sword);
        result.Value[EquipmentSlot.OffHand].Should().Be(shield);
        result.Value[EquipmentSlot.Head].Should().Be(helmet);
    }

    [Fact]
    public async Task GetEquippedItems_ActorWithNoEquipment_ShouldReturnEmptyDictionary()
    {
        // WHY: Actor without equipment component should return empty (not error)

        // Arrange
        var actors = new InMemoryActorRepository();
        var actor = new Actor(ActorId.NewId(), "ACTOR_TEST");
        await actors.AddActorAsync(actor);

        var handler = new GetEquippedItemsQueryHandler(actors, NullLogger<GetEquippedItemsQueryHandler>.Instance);

        // Act
        var result = await handler.Handle(new GetEquippedItemsQuery(actor.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEquippedItems_ActorWithEmptySlots_ShouldReturnOnlyOccupiedSlots()
    {
        // WHY: Empty slots not included in dictionary - UI checks with TryGetValue

        // Arrange
        var actors = new InMemoryActorRepository();
        var actor = new Actor(ActorId.NewId(), "ACTOR_TEST");
        var equipment = new EquipmentComponent(actor.Id);

        var sword = ItemId.NewId();
        equipment.EquipItem(EquipmentSlot.MainHand, sword); // Only MainHand equipped

        actor.AddComponent<IEquipmentComponent>(equipment);
        await actors.AddActorAsync(actor);

        var handler = new GetEquippedItemsQueryHandler(actors, NullLogger<GetEquippedItemsQueryHandler>.Instance);

        // Act
        var result = await handler.Handle(new GetEquippedItemsQuery(actor.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.Should().ContainKey(EquipmentSlot.MainHand);
        result.Value.Should().NotContainKey(EquipmentSlot.OffHand); // Empty slot not in dictionary
        result.Value.Should().NotContainKey(EquipmentSlot.Head);
        result.Value.Should().NotContainKey(EquipmentSlot.Torso);
        result.Value.Should().NotContainKey(EquipmentSlot.Legs);
    }

    [Fact]
    public async Task GetEquippedItems_TwoHandedWeapon_ShouldReturnSameItemInBothHands()
    {
        // WHY: UI detects two-handed weapons by checking MainHand.ItemId == OffHand.ItemId

        // Arrange
        var actors = new InMemoryActorRepository();
        var actor = new Actor(ActorId.NewId(), "ACTOR_TEST");
        var equipment = new EquipmentComponent(actor.Id);

        var greatsword = ItemId.NewId();
        equipment.EquipItem(EquipmentSlot.MainHand, greatsword, isTwoHanded: true);

        actor.AddComponent<IEquipmentComponent>(equipment);
        await actors.AddActorAsync(actor);

        var handler = new GetEquippedItemsQueryHandler(actors, NullLogger<GetEquippedItemsQueryHandler>.Instance);

        // Act
        var result = await handler.Handle(new GetEquippedItemsQuery(actor.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2); // MainHand + OffHand
        result.Value[EquipmentSlot.MainHand].Should().Be(greatsword);
        result.Value[EquipmentSlot.OffHand].Should().Be(greatsword);
        result.Value[EquipmentSlot.MainHand].Should().Be(result.Value[EquipmentSlot.OffHand]); // Same item
    }

    [Fact]
    public async Task GetEquippedItems_ActorNotFound_ShouldReturnFailure()
    {
        // WHY: Invalid ActorId should return clear error

        // Arrange
        var actors = new InMemoryActorRepository();
        var handler = new GetEquippedItemsQueryHandler(actors, NullLogger<GetEquippedItemsQueryHandler>.Instance);

        // Act
        var result = await handler.Handle(new GetEquippedItemsQuery(ActorId.NewId()), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    #endregion

    #region GetEquippedWeaponQuery Tests

    [Fact]
    public async Task GetEquippedWeapon_ActorWithWeapon_ShouldReturnWeaponId()
    {
        // WHY: Combat system queries MainHand weapon for attack

        // Arrange
        var actors = new InMemoryActorRepository();
        var actor = new Actor(ActorId.NewId(), "ACTOR_TEST");
        var equipment = new EquipmentComponent(actor.Id);

        var sword = ItemId.NewId();
        equipment.EquipItem(EquipmentSlot.MainHand, sword);

        actor.AddComponent<IEquipmentComponent>(equipment);
        await actors.AddActorAsync(actor);

        var handler = new GetEquippedWeaponQueryHandler(actors, NullLogger<GetEquippedWeaponQueryHandler>.Instance);

        // Act
        var result = await handler.Handle(new GetEquippedWeaponQuery(actor.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(sword);
    }

    [Fact]
    public async Task GetEquippedWeapon_ActorWithNoWeapon_ShouldReturnFailure()
    {
        // WHY: Combat interprets failure as "cannot attack" or "unarmed"

        // Arrange
        var actors = new InMemoryActorRepository();
        var actor = new Actor(ActorId.NewId(), "ACTOR_TEST");
        var equipment = new EquipmentComponent(actor.Id);
        // MainHand is empty

        actor.AddComponent<IEquipmentComponent>(equipment);
        await actors.AddActorAsync(actor);

        var handler = new GetEquippedWeaponQueryHandler(actors, NullLogger<GetEquippedWeaponQueryHandler>.Instance);

        // Act
        var result = await handler.Handle(new GetEquippedWeaponQuery(actor.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("NO_WEAPON_EQUIPPED");
    }

    [Fact]
    public async Task GetEquippedWeapon_ActorWithNoEquipmentComponent_ShouldReturnFailure()
    {
        // WHY: Actor without equipment component cannot have weapon

        // Arrange
        var actors = new InMemoryActorRepository();
        var actor = new Actor(ActorId.NewId(), "ACTOR_TEST");
        await actors.AddActorAsync(actor);

        var handler = new GetEquippedWeaponQueryHandler(actors, NullLogger<GetEquippedWeaponQueryHandler>.Instance);

        // Act
        var result = await handler.Handle(new GetEquippedWeaponQuery(actor.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("NO_WEAPON_EQUIPPED");
    }

    [Fact]
    public async Task GetEquippedWeapon_TwoHandedWeapon_ShouldReturnWeaponId()
    {
        // WHY: Combat doesn't care if weapon is two-handed - just needs ItemId

        // Arrange
        var actors = new InMemoryActorRepository();
        var actor = new Actor(ActorId.NewId(), "ACTOR_TEST");
        var equipment = new EquipmentComponent(actor.Id);

        var greatsword = ItemId.NewId();
        equipment.EquipItem(EquipmentSlot.MainHand, greatsword, isTwoHanded: true);

        actor.AddComponent<IEquipmentComponent>(equipment);
        await actors.AddActorAsync(actor);

        var handler = new GetEquippedWeaponQueryHandler(actors, NullLogger<GetEquippedWeaponQueryHandler>.Instance);

        // Act
        var result = await handler.Handle(new GetEquippedWeaponQuery(actor.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(greatsword);
    }

    [Fact]
    public async Task GetEquippedWeapon_ActorNotFound_ShouldReturnFailure()
    {
        // WHY: Invalid ActorId should return clear error

        // Arrange
        var actors = new InMemoryActorRepository();
        var handler = new GetEquippedWeaponQueryHandler(actors, NullLogger<GetEquippedWeaponQueryHandler>.Instance);

        // Act
        var result = await handler.Handle(new GetEquippedWeaponQuery(ActorId.NewId()), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    #endregion
}
