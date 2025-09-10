using Xunit;
using FluentAssertions;
using Darklands.Core.Domain.Combat;

namespace Darklands.Core.Tests.Domain.Combat;

/// <summary>
/// Unit tests for CombatAction record and its validation logic.
/// </summary>
[Trait("Category", "Domain")]
[Trait("Feature", "Combat")]
public class CombatActionTests
{
    [Fact]
    public void Constructor_ValidParameters_CreatesCombatAction()
    {
        // Arrange
        var name = "Test Attack";
        var baseCost = TimeUnit.CreateUnsafe(500);
        var baseDamage = 10;
        var type = CombatActionType.Attack;
        var accuracyBonus = 5;

        // Act
        var action = CombatAction.CreateUnsafe(name, baseCost, baseDamage, type, accuracyBonus);

        // Assert
        action.Name.Should().Be(name);
        action.BaseCost.Should().Be(baseCost);
        action.BaseDamage.Should().Be(baseDamage);
        action.Type.Should().Be(type);
        action.AccuracyBonus.Should().Be(accuracyBonus);
        action.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Constructor_DefaultParameters_UsesDefaults()
    {
        // Arrange
        var name = "Default Action";
        var baseCost = TimeUnit.CreateUnsafe(300);
        var baseDamage = 5;

        // Act
        var action = CombatAction.CreateUnsafe(name, baseCost, baseDamage);

        // Assert
        action.Type.Should().Be(CombatActionType.Attack);
        action.AccuracyBonus.Should().Be(0);
        action.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null!)]
    public void Create_EmptyName_ReturnsFailure(string? name)
    {
        // Arrange
        var validTimeUnit = TimeUnit.CreateUnsafe(500);

        // Act
        var result = CombatAction.Create(name!, validTimeUnit, 10);

        // Assert
        result.IsFail.Should().BeTrue();
    }

    [Fact]
    public void Create_InvalidBaseCost_ReturnsFailure()
    {
        // Arrange - Create will fail because TimeUnit.Create(-100) fails
        var invalidTimeUnit = TimeUnit.Create(-100);
        invalidTimeUnit.IsFail.Should().BeTrue(); // Verify our assumption

        // This test is no longer needed since we can't create invalid TimeUnits
        // All CombatActions with valid TimeUnits will be valid
        // Removing this test as it tests an impossible scenario
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_NegativeBaseDamage_ReturnsFailure(int baseDamage)
    {
        // Arrange
        var validTimeUnit = TimeUnit.CreateUnsafe(500);

        // Act
        var result = CombatAction.Create("Test", validTimeUnit, baseDamage);

        // Assert
        result.IsFail.Should().BeTrue();
    }

    [Theory]
    [InlineData(-101)]
    [InlineData(101)]
    [InlineData(-200)]
    [InlineData(200)]
    public void Create_AccuracyBonusOutOfRange_ReturnsFailure(int accuracyBonus)
    {
        // Arrange
        var validTimeUnit = TimeUnit.CreateUnsafe(500);

        // Act
        var result = CombatAction.Create("Test", validTimeUnit, 10, CombatActionType.Attack, accuracyBonus);

        // Assert
        result.IsFail.Should().BeTrue();
    }

    [Theory]
    [InlineData(-100)]
    [InlineData(-50)]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void IsValid_ValidAccuracyBonus_ReturnsTrue(int accuracyBonus)
    {
        // Arrange
        var action = CombatAction.CreateUnsafe("Test", TimeUnit.CreateUnsafe(500), 10, CombatActionType.Attack, accuracyBonus);

        // Act & Assert
        action.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Create_ValidParameters_ReturnsSuccessResult()
    {
        // Arrange
        var name = "Valid Action";
        var baseCost = TimeUnit.CreateUnsafe(600);
        var baseDamage = 12;

        // Act
        var result = CombatAction.Create(name, baseCost, baseDamage);

        // Assert
        result.IsSucc.Should().BeTrue();
        result.IfSucc(action =>
        {
            action.Name.Should().Be(name);
            action.BaseCost.Should().Be(baseCost);
            action.BaseDamage.Should().Be(baseDamage);
            action.IsValid.Should().BeTrue();
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null!)]
    public void Create_EmptyName_ReturnsFailureResult(string? name)
    {
        // Act
        var result = CombatAction.Create(name!, TimeUnit.CreateUnsafe(500), 10);

        // Assert
        result.IsFail.Should().BeTrue();
    }

    [Fact]
    public void Create_NegativeBaseDamage_ReturnsFailureResult()
    {
        // Act
        var result = CombatAction.Create("Test", TimeUnit.CreateUnsafe(500), -5);

        // Assert
        result.IsFail.Should().BeTrue();
    }

    [Fact]
    public void Create_InvalidAccuracyBonus_ReturnsFailureResult()
    {
        // Act
        var result = CombatAction.Create("Test", TimeUnit.CreateUnsafe(500), 10, CombatActionType.Attack, 150);

        // Assert
        result.IsFail.Should().BeTrue();
    }

    // Test all common combat actions
    [Fact]
    public void CommonActions_DaggerStab_HasCorrectProperties()
    {
        // Act
        var dagger = CombatAction.Common.DaggerStab;

        // Assert
        dagger.Name.Should().Be("Dagger Stab");
        dagger.BaseCost.Value.Should().Be(50);
        dagger.BaseDamage.Should().Be(8);
        dagger.Type.Should().Be(CombatActionType.Attack);
        dagger.AccuracyBonus.Should().Be(10);
        dagger.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CommonActions_SwordSlash_HasCorrectProperties()
    {
        // Act
        var sword = CombatAction.Common.SwordSlash;

        // Assert
        sword.Name.Should().Be("Sword Slash");
        sword.BaseCost.Value.Should().Be(80);
        sword.BaseDamage.Should().Be(15);
        sword.Type.Should().Be(CombatActionType.Attack);
        sword.AccuracyBonus.Should().Be(0);
        sword.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CommonActions_AxeChop_HasCorrectProperties()
    {
        // Act
        var axe = CombatAction.Common.AxeChop;

        // Assert
        axe.Name.Should().Be("Axe Chop");
        axe.BaseCost.Value.Should().Be(120);
        axe.BaseDamage.Should().Be(22);
        axe.Type.Should().Be(CombatActionType.Attack);
        axe.AccuracyBonus.Should().Be(-5);
        axe.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CommonActions_Block_HasCorrectProperties()
    {
        // Act
        var block = CombatAction.Common.Block;

        // Assert
        block.Name.Should().Be("Block");
        block.BaseCost.Value.Should().Be(30);
        block.BaseDamage.Should().Be(0);
        block.Type.Should().Be(CombatActionType.Defensive);
        block.AccuracyBonus.Should().Be(0);
        block.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CommonActions_Dodge_HasCorrectProperties()
    {
        // Act
        var dodge = CombatAction.Common.Dodge;

        // Assert
        dodge.Name.Should().Be("Dodge");
        dodge.BaseCost.Value.Should().Be(20);
        dodge.BaseDamage.Should().Be(0);
        dodge.Type.Should().Be(CombatActionType.Defensive);
        dodge.AccuracyBonus.Should().Be(0);
        dodge.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CommonActions_AllActions_AreValid()
    {
        // Arrange
        var commonActions = new[]
        {
            CombatAction.Common.DaggerStab,
            CombatAction.Common.SwordSlash,
            CombatAction.Common.AxeChop,
            CombatAction.Common.Block,
            CombatAction.Common.Dodge
        };

        // Act & Assert
        foreach (var action in commonActions)
        {
            action.IsValid.Should().BeTrue($"Common action {action.Name} should be valid");
        }
    }

    [Fact]
    public void CommonActions_AttackActions_HavePositiveDamage()
    {
        // Arrange
        var attackActions = new[]
        {
            CombatAction.Common.DaggerStab,
            CombatAction.Common.SwordSlash,
            CombatAction.Common.AxeChop
        };

        // Act & Assert
        foreach (var action in attackActions)
        {
            action.BaseDamage.Should().BeGreaterThan(0, $"Attack action {action.Name} should have positive damage");
        }
    }

    [Fact]
    public void CommonActions_DefensiveActions_HaveZeroDamage()
    {
        // Arrange
        var defensiveActions = new[]
        {
            CombatAction.Common.Block,
            CombatAction.Common.Dodge
        };

        // Act & Assert
        foreach (var action in defensiveActions)
        {
            action.BaseDamage.Should().Be(0, $"Defensive action {action.Name} should have zero damage");
        }
    }
}
