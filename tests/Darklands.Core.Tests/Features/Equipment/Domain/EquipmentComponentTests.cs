using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Equipment.Domain;
using FluentAssertions;
using Xunit;

namespace Darklands.Core.Tests.Features.Equipment.Domain;

[Trait("Category", "Equipment")]
[Trait("Category", "Unit")]
public class EquipmentComponentTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_ValidParameters_ShouldCreateComponent()
    {
        // Arrange
        var actorId = ActorId.NewId();

        // Act
        var component = new EquipmentComponent(actorId);

        // Assert
        component.OwnerId.Should().Be(actorId);
        component.IsSlotOccupied(EquipmentSlot.MainHand).Should().BeFalse();
        component.IsSlotOccupied(EquipmentSlot.OffHand).Should().BeFalse();
        component.IsSlotOccupied(EquipmentSlot.Head).Should().BeFalse();
        component.IsSlotOccupied(EquipmentSlot.Torso).Should().BeFalse();
        component.IsSlotOccupied(EquipmentSlot.Legs).Should().BeFalse();
    }

    [Fact]
    public void Constructor_EmptyActorId_ShouldThrowArgumentException()
    {
        // PROGRAMMER ERROR: Component must belong to a valid actor

        // Arrange
        var emptyId = new ActorId(Guid.Empty);

        // Act
        var act = () => new EquipmentComponent(emptyId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("ownerId");
    }

    #endregion

    #region EquipItem Tests

    [Fact]
    public void EquipItem_ToEmptySlot_ShouldSucceed()
    {
        // Arrange
        var component = new EquipmentComponent(ActorId.NewId());
        var itemId = ItemId.NewId();

        // Act
        var result = component.EquipItem(EquipmentSlot.MainHand, itemId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        component.IsSlotOccupied(EquipmentSlot.MainHand).Should().BeTrue();
        component.GetEquippedItem(EquipmentSlot.MainHand).Value.Should().Be(itemId);
    }

    [Fact]
    public void EquipItem_ToOccupiedSlot_ShouldReturnFailure()
    {
        // WHY: Cannot equip to occupied slot - must unequip first

        // Arrange
        var component = new EquipmentComponent(ActorId.NewId());
        var firstItem = ItemId.NewId();
        var secondItem = ItemId.NewId();
        component.EquipItem(EquipmentSlot.MainHand, firstItem);

        // Act
        var result = component.EquipItem(EquipmentSlot.MainHand, secondItem);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("ERROR_EQUIPMENT_SLOT_OCCUPIED");
        component.GetEquippedItem(EquipmentSlot.MainHand).Value.Should().Be(firstItem); // Original item unchanged
    }

    [Fact]
    public void EquipItem_TwoHandedToMainHand_WhenBothHandsEmpty_ShouldOccupyBothSlots()
    {
        // WHY: Two-handed weapons occupy both MainHand and OffHand simultaneously

        // Arrange
        var component = new EquipmentComponent(ActorId.NewId());
        var greatsword = ItemId.NewId();

        // Act
        var result = component.EquipItem(EquipmentSlot.MainHand, greatsword, isTwoHanded: true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        component.IsSlotOccupied(EquipmentSlot.MainHand).Should().BeTrue();
        component.IsSlotOccupied(EquipmentSlot.OffHand).Should().BeTrue();
        component.GetEquippedItem(EquipmentSlot.MainHand).Value.Should().Be(greatsword);
        component.GetEquippedItem(EquipmentSlot.OffHand).Value.Should().Be(greatsword); // Same item in both slots
    }

    [Fact]
    public void EquipItem_TwoHandedToMainHand_WhenMainHandOccupied_ShouldReturnFailure()
    {
        // WHY: Two-handed weapon requires BOTH hands to be empty

        // Arrange
        var component = new EquipmentComponent(ActorId.NewId());
        var sword = ItemId.NewId();
        var greatsword = ItemId.NewId();
        component.EquipItem(EquipmentSlot.MainHand, sword); // Occupy main hand

        // Act
        var result = component.EquipItem(EquipmentSlot.MainHand, greatsword, isTwoHanded: true);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("ERROR_EQUIPMENT_MAIN_HAND_OCCUPIED");
        component.GetEquippedItem(EquipmentSlot.MainHand).Value.Should().Be(sword); // Original item unchanged
    }

    [Fact]
    public void EquipItem_TwoHandedToMainHand_WhenOffHandOccupied_ShouldReturnFailure()
    {
        // WHY: Two-handed weapon requires BOTH hands to be empty

        // Arrange
        var component = new EquipmentComponent(ActorId.NewId());
        var shield = ItemId.NewId();
        var greatsword = ItemId.NewId();
        component.EquipItem(EquipmentSlot.OffHand, shield); // Occupy off hand

        // Act
        var result = component.EquipItem(EquipmentSlot.MainHand, greatsword, isTwoHanded: true);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("ERROR_EQUIPMENT_OFF_HAND_OCCUPIED");
        component.GetEquippedItem(EquipmentSlot.OffHand).Value.Should().Be(shield); // Original item unchanged
    }

    [Fact]
    public void EquipItem_TwoHandedToOffHand_ShouldReturnFailure()
    {
        // WHY: Two-handed weapons must be equipped to MainHand slot specifically

        // Arrange
        var component = new EquipmentComponent(ActorId.NewId());
        var greatsword = ItemId.NewId();

        // Act
        var result = component.EquipItem(EquipmentSlot.OffHand, greatsword, isTwoHanded: true);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("ERROR_EQUIPMENT_TWO_HANDED_MUST_USE_MAIN_HAND");
        component.IsSlotOccupied(EquipmentSlot.OffHand).Should().BeFalse();
    }

    [Fact]
    public void EquipItem_ToOffHand_WhenTwoHandedInMainHand_ShouldReturnFailure()
    {
        // WHY: Cannot equip to OffHand when a two-handed weapon is equipped

        // Arrange
        var component = new EquipmentComponent(ActorId.NewId());
        var greatsword = ItemId.NewId();
        var shield = ItemId.NewId();
        component.EquipItem(EquipmentSlot.MainHand, greatsword, isTwoHanded: true);

        // Act
        var result = component.EquipItem(EquipmentSlot.OffHand, shield);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("ERROR_EQUIPMENT_SLOT_OCCUPIED");
        component.GetEquippedItem(EquipmentSlot.OffHand).Value.Should().Be(greatsword); // Two-handed weapon still there
    }

    [Fact]
    public void EquipItem_MultipleItemsInDifferentSlots_ShouldSucceed()
    {
        // WHY: Actors can wear multiple items simultaneously in different slots

        // Arrange
        var component = new EquipmentComponent(ActorId.NewId());
        var sword = ItemId.NewId();
        var shield = ItemId.NewId();
        var helmet = ItemId.NewId();
        var armor = ItemId.NewId();
        var boots = ItemId.NewId();

        // Act
        var result1 = component.EquipItem(EquipmentSlot.MainHand, sword);
        var result2 = component.EquipItem(EquipmentSlot.OffHand, shield);
        var result3 = component.EquipItem(EquipmentSlot.Head, helmet);
        var result4 = component.EquipItem(EquipmentSlot.Torso, armor);
        var result5 = component.EquipItem(EquipmentSlot.Legs, boots);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result3.IsSuccess.Should().BeTrue();
        result4.IsSuccess.Should().BeTrue();
        result5.IsSuccess.Should().BeTrue();
        component.GetEquippedItem(EquipmentSlot.MainHand).Value.Should().Be(sword);
        component.GetEquippedItem(EquipmentSlot.OffHand).Value.Should().Be(shield);
        component.GetEquippedItem(EquipmentSlot.Head).Value.Should().Be(helmet);
        component.GetEquippedItem(EquipmentSlot.Torso).Value.Should().Be(armor);
        component.GetEquippedItem(EquipmentSlot.Legs).Value.Should().Be(boots);
    }

    #endregion

    #region UnequipItem Tests

    [Fact]
    public void UnequipItem_FromOccupiedSlot_ShouldReturnItemAndClearSlot()
    {
        // Arrange
        var component = new EquipmentComponent(ActorId.NewId());
        var itemId = ItemId.NewId();
        component.EquipItem(EquipmentSlot.MainHand, itemId);

        // Act
        var result = component.UnequipItem(EquipmentSlot.MainHand);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(itemId);
        component.IsSlotOccupied(EquipmentSlot.MainHand).Should().BeFalse();
    }

    [Fact]
    public void UnequipItem_FromEmptySlot_ShouldReturnFailure()
    {
        // WHY: Cannot unequip from empty slot

        // Arrange
        var component = new EquipmentComponent(ActorId.NewId());

        // Act
        var result = component.UnequipItem(EquipmentSlot.MainHand);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("ERROR_EQUIPMENT_SLOT_EMPTY");
    }

    [Fact]
    public void UnequipItem_TwoHandedFromMainHand_ShouldClearBothSlots()
    {
        // WHY: Unequipping two-handed weapon clears both hands atomically

        // Arrange
        var component = new EquipmentComponent(ActorId.NewId());
        var greatsword = ItemId.NewId();
        component.EquipItem(EquipmentSlot.MainHand, greatsword, isTwoHanded: true);

        // Act
        var result = component.UnequipItem(EquipmentSlot.MainHand);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(greatsword);
        component.IsSlotOccupied(EquipmentSlot.MainHand).Should().BeFalse();
        component.IsSlotOccupied(EquipmentSlot.OffHand).Should().BeFalse(); // Both slots cleared
    }

    [Fact]
    public void UnequipItem_TwoHandedFromOffHand_ShouldClearBothSlots()
    {
        // WHY: Unequipping two-handed weapon from EITHER hand clears both slots

        // Arrange
        var component = new EquipmentComponent(ActorId.NewId());
        var greatsword = ItemId.NewId();
        component.EquipItem(EquipmentSlot.MainHand, greatsword, isTwoHanded: true);

        // Act
        var result = component.UnequipItem(EquipmentSlot.OffHand); // Unequip from OffHand

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(greatsword);
        component.IsSlotOccupied(EquipmentSlot.MainHand).Should().BeFalse(); // Both slots cleared
        component.IsSlotOccupied(EquipmentSlot.OffHand).Should().BeFalse();
    }

    [Fact]
    public void EquipItem_AfterUnequip_ShouldSucceed()
    {
        // WHY: Slots can be reused after unequipping

        // Arrange
        var component = new EquipmentComponent(ActorId.NewId());
        var firstItem = ItemId.NewId();
        var secondItem = ItemId.NewId();
        component.EquipItem(EquipmentSlot.MainHand, firstItem);
        component.UnequipItem(EquipmentSlot.MainHand);

        // Act
        var result = component.EquipItem(EquipmentSlot.MainHand, secondItem);

        // Assert
        result.IsSuccess.Should().BeTrue();
        component.GetEquippedItem(EquipmentSlot.MainHand).Value.Should().Be(secondItem);
    }

    #endregion

    #region GetEquippedItem Tests

    [Fact]
    public void GetEquippedItem_FromOccupiedSlot_ShouldReturnItem()
    {
        // Arrange
        var component = new EquipmentComponent(ActorId.NewId());
        var itemId = ItemId.NewId();
        component.EquipItem(EquipmentSlot.Head, itemId);

        // Act
        var result = component.GetEquippedItem(EquipmentSlot.Head);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(itemId);
    }

    [Fact]
    public void GetEquippedItem_FromEmptySlot_ShouldReturnFailure()
    {
        // Arrange
        var component = new EquipmentComponent(ActorId.NewId());

        // Act
        var result = component.GetEquippedItem(EquipmentSlot.Head);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("ERROR_EQUIPMENT_SLOT_EMPTY");
    }

    [Fact]
    public void GetEquippedItem_TwoHandedWeapon_BothSlotsShouldReturnSameItem()
    {
        // WHY: Two-handed weapon is stored in both slots with same ItemId

        // Arrange
        var component = new EquipmentComponent(ActorId.NewId());
        var greatsword = ItemId.NewId();
        component.EquipItem(EquipmentSlot.MainHand, greatsword, isTwoHanded: true);

        // Act
        var mainHandResult = component.GetEquippedItem(EquipmentSlot.MainHand);
        var offHandResult = component.GetEquippedItem(EquipmentSlot.OffHand);

        // Assert
        mainHandResult.IsSuccess.Should().BeTrue();
        offHandResult.IsSuccess.Should().BeTrue();
        mainHandResult.Value.Should().Be(greatsword);
        offHandResult.Value.Should().Be(greatsword);
        mainHandResult.Value.Should().Be(offHandResult.Value); // Same ItemId in both slots
    }

    #endregion

    #region IsSlotOccupied Tests

    [Fact]
    public void IsSlotOccupied_EmptySlot_ShouldReturnFalse()
    {
        // Arrange
        var component = new EquipmentComponent(ActorId.NewId());

        // Act & Assert
        component.IsSlotOccupied(EquipmentSlot.MainHand).Should().BeFalse();
        component.IsSlotOccupied(EquipmentSlot.OffHand).Should().BeFalse();
        component.IsSlotOccupied(EquipmentSlot.Head).Should().BeFalse();
        component.IsSlotOccupied(EquipmentSlot.Torso).Should().BeFalse();
        component.IsSlotOccupied(EquipmentSlot.Legs).Should().BeFalse();
    }

    [Fact]
    public void IsSlotOccupied_OccupiedSlot_ShouldReturnTrue()
    {
        // Arrange
        var component = new EquipmentComponent(ActorId.NewId());
        var itemId = ItemId.NewId();
        component.EquipItem(EquipmentSlot.Torso, itemId);

        // Act & Assert
        component.IsSlotOccupied(EquipmentSlot.Torso).Should().BeTrue();
        component.IsSlotOccupied(EquipmentSlot.MainHand).Should().BeFalse(); // Other slots still empty
    }

    #endregion
}
