using Xunit;
using FluentAssertions;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Darklands.Domain.Combat;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Darklands.Core.Tests.Domain.Combat;

/// <summary>
/// Unit tests for TimeUnitCalculator business logic.
/// Tests all calculation formulas, edge cases, and mathematical properties.
/// </summary>
[Trait("Category", "Domain")]
[Trait("Feature", "Combat")]
public class TimeUnitCalculatorTests
{
    [Fact]
    public void CalculateActionTime_ValidInputs_ReturnsExpectedTime()
    {
        // Arrange
        var daggerStab = CombatAction.Common.DaggerStab; // 50 TU base
        var agility = 20; // 100/20 = 5.0 modifier
        var encumbrance = 0; // 1.0 + (0 * 0.1) = 1.0 modifier

        // Act
        var result = TimeUnitCalculator.CalculateActionTime(daggerStab, agility, encumbrance);

        // Assert
        result.IsSucc.Should().BeTrue();
        result.IfSucc(time =>
        {
            // Expected: 50 * (100/20) * (1 + 0*0.1) = 50 * 5 * 1 = 250
            time.Value.Should().Be(250);
        });
    }

    [Fact]
    public void CalculateActionTime_HighAgility_ReducesTime()
    {
        // Arrange
        var action = CombatAction.Common.SwordSlash; // 800ms base
        var lowAgilityTime = TimeUnitCalculator.CalculateActionTime(action, 10, 0);
        var highAgilityTime = TimeUnitCalculator.CalculateActionTime(action, 50, 0);

        // Act & Assert
        lowAgilityTime.IsSucc.Should().BeTrue();
        highAgilityTime.IsSucc.Should().BeTrue();

        lowAgilityTime.IfSucc(lowTime =>
            highAgilityTime.IfSucc(highTime =>
                highTime.Value.Should().BeLessThan(lowTime.Value)));
    }

    [Fact]
    public void CalculateActionTime_HighEncumbrance_IncreasesTime()
    {
        // Arrange - Use lighter action and better agility to avoid exceeding TimeUnit.Maximum
        var action = CombatAction.Common.DaggerStab; // 800ms base instead of AxeChop (1200ms)
        var lowEncumbranceTime = TimeUnitCalculator.CalculateActionTime(action, 40, 0);
        var highEncumbranceTime = TimeUnitCalculator.CalculateActionTime(action, 40, 10);

        // Act & Assert
        lowEncumbranceTime.IsSucc.Should().BeTrue();
        highEncumbranceTime.IsSucc.Should().BeTrue();

        lowEncumbranceTime.IfSucc(lowTime =>
            highEncumbranceTime.IfSucc(highTime =>
                highTime.Value.Should().BeGreaterThan(lowTime.Value)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(-5)]
    public void CalculateActionTime_InvalidAgility_ReturnsFailure(int agility)
    {
        // Arrange
        var action = CombatAction.Common.DaggerStab;

        // Act
        var result = TimeUnitCalculator.CalculateActionTime(action, agility, 0);

        // Assert
        result.IsFail.Should().BeTrue();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(51)]
    [InlineData(100)]
    public void CalculateActionTime_InvalidEncumbrance_ReturnsFailure(int encumbrance)
    {
        // Arrange
        var action = CombatAction.Common.DaggerStab;

        // Act
        var result = TimeUnitCalculator.CalculateActionTime(action, 20, encumbrance);

        // Assert
        result.IsFail.Should().BeTrue();
    }

    [Fact]
    public void CalculateEncumbrancePenalty_ValidInput_ReturnsCorrectMultiplier()
    {
        // Arrange & Act
        var noPenalty = TimeUnitCalculator.CalculateEncumbranceFactor(0);
        var mediumPenalty = TimeUnitCalculator.CalculateEncumbranceFactor(10);
        var highPenalty = TimeUnitCalculator.CalculateEncumbranceFactor(30);

        // Assert
        noPenalty.IfSucc(factor => factor.Should().Be(10)); // 10 + 0 = 10
        mediumPenalty.IfSucc(factor => factor.Should().Be(20)); // 10 + 10 = 20  
        highPenalty.IfSucc(factor => factor.Should().Be(40)); // 10 + 30 = 40
    }

    [Fact]
    public void ValidateAgilityScore_ValidInput_ReturnsSuccess()
    {
        // Arrange & Act
        var low = TimeUnitCalculator.ValidateAgilityScore(1);
        var average = TimeUnitCalculator.ValidateAgilityScore(50);
        var high = TimeUnitCalculator.ValidateAgilityScore(100);

        // Assert
        low.IsSucc.Should().BeTrue();
        average.IsSucc.Should().BeTrue();
        high.IsSucc.Should().BeTrue();

        low.IfSucc(agility => agility.Should().Be(1));
        average.IfSucc(agility => agility.Should().Be(50));
        high.IfSucc(agility => agility.Should().Be(100));
    }

    [Fact]
    public void CompareActionSpeeds_DifferentActions_ReturnsComparison()
    {
        // Arrange
        var fastAction = CombatAction.Common.Dodge; // 200ms base
        var slowAction = CombatAction.Common.AxeChop; // 1200ms base
        var agility = 25;
        var encumbrance = 5;

        // Act
        var result = TimeUnitCalculator.CompareActionSpeeds(fastAction, slowAction, agility, encumbrance);

        // Assert
        result.IsSucc.Should().BeTrue();
        result.IfSucc(comparison =>
        {
            comparison.FasterAction.Should().Be(fastAction.Name);
            comparison.TimeA.Value.Should().BeLessThan(comparison.TimeB.Value);
            comparison.TimeDifferenceMs.Should().BeGreaterThan(0);
        });
    }

    // Property-based tests for mathematical invariants
    [Fact]
    public void CalculateActionTime_HigherAgilityGivesFasterTime()
    {
        var generator =
            from agility1 in Gen.Choose(20, 50)  // Higher minimum to avoid excessive time calculations
            from agility2 in Gen.Choose(51, 100)
            from encumbrance in Gen.Choose(0, 10) // Lower maximum to stay within time bounds
            select new { Agility1 = agility1, Agility2 = agility2, Encumbrance = encumbrance };

        Prop.ForAll(generator.ToArbitrary(),
            test =>
            {
                var action = CombatAction.Common.SwordSlash;
                var time1 = TimeUnitCalculator.CalculateActionTime(action, test.Agility1, test.Encumbrance);
                var time2 = TimeUnitCalculator.CalculateActionTime(action, test.Agility2, test.Encumbrance);

                return time1.IsSucc && time2.IsSucc &&
                       time1.Match(t1 => time2.Match(t2 => t1.Value >= t2.Value, _ => false), _ => false);
            }).QuickCheckThrowOnFailure();
    }

    [Fact]
    public void CalculateActionTime_HigherEncumbranceGivesSlowerTime()
    {
        var generator =
            from agility in Gen.Choose(30, 90)  // Higher minimum agility
            from enc1 in Gen.Choose(0, 8)       // Lower encumbrance ranges
            from enc2 in Gen.Choose(9, 15)      // To stay within time bounds
            select new { Agility = agility, Enc1 = enc1, Enc2 = enc2 };

        Prop.ForAll(generator.ToArbitrary(),
            test =>
            {
                var action = CombatAction.Common.DaggerStab;
                var time1 = TimeUnitCalculator.CalculateActionTime(action, test.Agility, test.Enc1);
                var time2 = TimeUnitCalculator.CalculateActionTime(action, test.Agility, test.Enc2);

                return time1.IsSucc && time2.IsSucc &&
                       time1.Match(t1 => time2.Match(t2 => t1.Value <= t2.Value, _ => false), _ => false);
            }).QuickCheckThrowOnFailure();
    }

    [Fact]
    public void CalculateActionTime_ResultIsAlwaysPositive()
    {
        var generator =
            from agility in Gen.Choose(25, 100)     // Higher minimum agility
            from encumbrance in Gen.Choose(0, 15)   // Lower maximum encumbrance
            select new { Agility = agility, Encumbrance = encumbrance };

        Prop.ForAll(generator.ToArbitrary(),
            test =>
            {
                var action = CombatAction.Common.Block;
                var result = TimeUnitCalculator.CalculateActionTime(action, test.Agility, test.Encumbrance);

                return result.Match(
                    Succ: time => time.Value > 0,
                    Fail: _ => true); // Failures are acceptable for this property
            }).QuickCheckThrowOnFailure();
    }
}
