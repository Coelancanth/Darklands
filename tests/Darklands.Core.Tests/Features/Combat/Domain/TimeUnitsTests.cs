using Darklands.Core.Features.Combat.Domain;
using FluentAssertions;
using Xunit;

namespace Darklands.Core.Tests.Features.Combat.Domain;

[Trait("Category", "Phase1")]
public class TimeUnitsTests
{
    [Fact]
    public void Create_WithZero_ShouldSucceed()
    {
        var result = TimeUnits.Create(0);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(0);
    }

    [Fact]
    public void Create_WithPositiveValue_ShouldSucceed()
    {
        var result = TimeUnits.Create(100);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(100);
    }

    [Fact]
    public void Create_WithNegativeValue_ShouldFail()
    {
        var result = TimeUnits.Create(-1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("cannot be negative");
    }

    [Fact]
    public void Zero_ShouldReturnZeroTimeUnits()
    {
        var zero = TimeUnits.Zero;

        zero.Value.Should().Be(0);
    }

    [Fact]
    public void MovementCost_ShouldReturn100TimeUnits()
    {
        var movementCost = TimeUnits.MovementCost;

        movementCost.Value.Should().Be(100);
    }

    [Fact]
    public void Add_ValidTimeUnits_ShouldSucceed()
    {
        var time1 = TimeUnits.Create(50).Value;
        var time2 = TimeUnits.Create(30).Value;

        var result = time1.Add(time2);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(80);
    }

    [Fact]
    public void Subtract_ResultingInPositive_ShouldSucceed()
    {
        var time1 = TimeUnits.Create(100).Value;
        var time2 = TimeUnits.Create(30).Value;

        var result = time1.Subtract(time2);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(70);
    }

    [Fact]
    public void Subtract_ResultingInNegative_ShouldFail()
    {
        var time1 = TimeUnits.Create(30).Value;
        var time2 = TimeUnits.Create(100).Value;

        var result = time1.Subtract(time2);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("cannot be negative");
    }

    [Fact]
    public void CompareTo_LessThan_ShouldReturnNegative()
    {
        var time1 = TimeUnits.Create(50).Value;
        var time2 = TimeUnits.Create(100).Value;

        var comparison = time1.CompareTo(time2);

        comparison.Should().BeLessThan(0);
    }

    [Fact]
    public void CompareTo_Equal_ShouldReturnZero()
    {
        var time1 = TimeUnits.Create(100).Value;
        var time2 = TimeUnits.Create(100).Value;

        var comparison = time1.CompareTo(time2);

        comparison.Should().Be(0);
    }

    [Fact]
    public void CompareTo_GreaterThan_ShouldReturnPositive()
    {
        var time1 = TimeUnits.Create(100).Value;
        var time2 = TimeUnits.Create(50).Value;

        var comparison = time1.CompareTo(time2);

        comparison.Should().BeGreaterThan(0);
    }

    [Fact]
    public void LessThanOperator_ShouldWorkCorrectly()
    {
        var time1 = TimeUnits.Create(50).Value;
        var time2 = TimeUnits.Create(100).Value;

        (time1 < time2).Should().BeTrue();
        (time2 < time1).Should().BeFalse();
    }

    [Fact]
    public void GreaterThanOperator_ShouldWorkCorrectly()
    {
        var time1 = TimeUnits.Create(100).Value;
        var time2 = TimeUnits.Create(50).Value;

        (time1 > time2).Should().BeTrue();
        (time2 > time1).Should().BeFalse();
    }

    [Fact]
    public void Equality_SameValue_ShouldBeEqual()
    {
        // WHY: Record struct provides value semantics automatically
        var time1 = TimeUnits.Create(100).Value;
        var time2 = TimeUnits.Create(100).Value;

        time1.Should().Be(time2);
        (time1 == time2).Should().BeTrue();
    }

    [Fact]
    public void ToString_ShouldIncludeValueAndUnits()
    {
        var time = TimeUnits.Create(150).Value;

        var result = time.ToString();

        result.Should().Contain("150");
        result.Should().Contain("time units");
    }
}
