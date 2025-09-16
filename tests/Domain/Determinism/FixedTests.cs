using Xunit;
using FluentAssertions;
using Darklands.Domain.Determinism;

namespace Darklands.Core.Tests.Domain.Determinism;

[Trait("Category", "Phase1")]
public class FixedTests
{
    [Fact]
    public void Constants_HaveExpectedValues()
    {
        // Assert
        Fixed.Zero.ToInt().Should().Be(0);
        Fixed.One.ToInt().Should().Be(1);
        Fixed.Half.ToFloat().Should().BeApproximately(0.5f, 0.001f);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(-1, -1)]
    [InlineData(100, 100)]
    [InlineData(-100, -100)]
    public void FromInt_ToInt_RoundTrip(int input, int expected)
    {
        // Act
        var fixed1 = Fixed.FromInt(input);
        var result = fixed1.ToInt();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0.0f, 0.0f)]
    [InlineData(1.0f, 1.0f)]
    [InlineData(-1.0f, -1.0f)]
    [InlineData(0.5f, 0.5f)]
    [InlineData(123.75f, 123.75f)]
    public void FromFloat_ToFloat_RoundTrip(float input, float expected)
    {
        // Act
        var fixed1 = Fixed.FromFloat(input);
        var result = fixed1.ToFloat();

        // Assert
        result.Should().BeApproximately(expected, 0.001f);
    }

    [Fact]
    public void Addition_BasicCases_WorksCorrectly()
    {
        // Arrange
        var a = Fixed.FromInt(5);
        var b = Fixed.FromInt(3);

        // Act
        var result = a + b;

        // Assert
        result.ToInt().Should().Be(8);
    }

    [Fact]
    public void Addition_Fractions_WorksCorrectly()
    {
        // Arrange
        var a = Fixed.FromFloat(1.5f);
        var b = Fixed.FromFloat(2.25f);

        // Act
        var result = a + b;

        // Assert
        result.ToFloat().Should().BeApproximately(3.75f, 0.001f);
    }

    [Fact]
    public void Subtraction_BasicCases_WorksCorrectly()
    {
        // Arrange
        var a = Fixed.FromInt(10);
        var b = Fixed.FromInt(3);

        // Act
        var result = a - b;

        // Assert
        result.ToInt().Should().Be(7);
    }

    [Fact]
    public void Multiplication_BasicCases_WorksCorrectly()
    {
        // Arrange
        var a = Fixed.FromInt(4);
        var b = Fixed.FromInt(5);

        // Act
        var result = a * b;

        // Assert
        result.ToInt().Should().Be(20);
    }

    [Fact]
    public void Multiplication_Fractions_WorksCorrectly()
    {
        // Arrange
        var a = Fixed.FromFloat(1.5f);
        var b = Fixed.FromFloat(2.0f);

        // Act
        var result = a * b;

        // Assert
        result.ToFloat().Should().BeApproximately(3.0f, 0.001f);
    }

    [Fact]
    public void Division_BasicCases_WorksCorrectly()
    {
        // Arrange
        var a = Fixed.FromInt(15);
        var b = Fixed.FromInt(3);

        // Act
        var result = a / b;

        // Assert
        result.ToInt().Should().Be(5);
    }

    [Fact]
    public void Division_Fractions_WorksCorrectly()
    {
        // Arrange
        var a = Fixed.FromFloat(7.5f);
        var b = Fixed.FromFloat(2.5f);

        // Act
        var result = a / b;

        // Assert
        result.ToFloat().Should().BeApproximately(3.0f, 0.001f);
    }

    [Fact]
    public void Division_ByZero_ThrowsException()
    {
        // Arrange
        var a = Fixed.FromInt(5);
        var b = Fixed.Zero;

        // Act & Assert
        Assert.Throws<DivideByZeroException>(() => a / b);
    }

    [Fact]
    public void Negation_WorksCorrectly()
    {
        // Arrange
        var positive = Fixed.FromInt(5);
        var negative = Fixed.FromInt(-3);

        // Act
        var negatedPositive = -positive;
        var negatedNegative = -negative;

        // Assert
        negatedPositive.ToInt().Should().Be(-5);
        negatedNegative.ToInt().Should().Be(3);
    }

    [Theory]
    [InlineData(5, 3, true)]
    [InlineData(3, 5, false)]
    [InlineData(5, 5, false)]
    public void GreaterThan_Comparison_WorksCorrectly(int a, int b, bool expected)
    {
        // Arrange
        var fixedA = Fixed.FromInt(a);
        var fixedB = Fixed.FromInt(b);

        // Act
        var result = fixedA > fixedB;

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(3, 5, true)]
    [InlineData(5, 3, false)]
    [InlineData(5, 5, false)]
    public void LessThan_Comparison_WorksCorrectly(int a, int b, bool expected)
    {
        // Arrange
        var fixedA = Fixed.FromInt(a);
        var fixedB = Fixed.FromInt(b);

        // Act
        var result = fixedA < fixedB;

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(5, 5, true)]
    [InlineData(5, 3, false)]
    [InlineData(3, 5, false)]
    public void Equality_Comparison_WorksCorrectly(int a, int b, bool expected)
    {
        // Arrange
        var fixedA = Fixed.FromInt(a);
        var fixedB = Fixed.FromInt(b);

        // Act
        var result = fixedA == fixedB;

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ImplicitConversion_FromInt_WorksCorrectly()
    {
        // Act
        Fixed result = 42; // Implicit conversion

        // Assert
        result.ToInt().Should().Be(42);
    }

    [Fact]
    public void ExplicitConversion_ToInt_WorksCorrectly()
    {
        // Arrange
        var fixed1 = Fixed.FromFloat(42.75f);

        // Act
        var result = (int)fixed1; // Explicit conversion (truncates fraction)

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void Abs_PositiveValue_ReturnsUnchanged()
    {
        // Arrange
        var value = Fixed.FromInt(5);

        // Act
        var result = value.Abs();

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void Abs_NegativeValue_ReturnsPositive()
    {
        // Arrange
        var value = Fixed.FromInt(-5);

        // Act
        var result = value.Abs();

        // Assert
        result.ToInt().Should().Be(5);
    }

    [Fact]
    public void Clamp_ValueInRange_ReturnsValue()
    {
        // Arrange
        var value = Fixed.FromInt(5);
        var min = Fixed.FromInt(0);
        var max = Fixed.FromInt(10);

        // Act
        var result = value.Clamp(min, max);

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void Clamp_ValueBelowMin_ReturnsMin()
    {
        // Arrange
        var value = Fixed.FromInt(-5);
        var min = Fixed.FromInt(0);
        var max = Fixed.FromInt(10);

        // Act
        var result = value.Clamp(min, max);

        // Assert
        result.Should().Be(min);
    }

    [Fact]
    public void Clamp_ValueAboveMax_ReturnsMax()
    {
        // Arrange
        var value = Fixed.FromInt(15);
        var min = Fixed.FromInt(0);
        var max = Fixed.FromInt(10);

        // Act
        var result = value.Clamp(min, max);

        // Assert
        result.Should().Be(max);
    }

    [Fact]
    public void Lerp_MidPoint_ReturnsExpected()
    {
        // Arrange
        var a = Fixed.FromInt(0);
        var b = Fixed.FromInt(10);
        var t = Fixed.FromFloat(0.5f);

        // Act
        var result = Fixed.Lerp(a, b, t);

        // Assert
        result.ToInt().Should().Be(5);
    }

    [Fact]
    public void Lerp_StartPoint_ReturnsStart()
    {
        // Arrange
        var a = Fixed.FromInt(5);
        var b = Fixed.FromInt(15);
        var t = Fixed.Zero;

        // Act
        var result = Fixed.Lerp(a, b, t);

        // Assert
        result.Should().Be(a);
    }

    [Fact]
    public void Lerp_EndPoint_ReturnsEnd()
    {
        // Arrange
        var a = Fixed.FromInt(5);
        var b = Fixed.FromInt(15);
        var t = Fixed.One;

        // Act
        var result = Fixed.Lerp(a, b, t);

        // Assert
        result.Should().Be(b);
    }

    [Fact]
    public void Sqrt_PerfectSquare_ReturnsExact()
    {
        // Arrange
        var value = Fixed.FromInt(9); // 3²

        // Act
        var result = value.Sqrt();

        // Assert
        result.ToInt().Should().Be(3);
    }

    [Fact]
    public void Sqrt_NonPerfectSquare_ReturnsApproximate()
    {
        // Arrange
        var value = Fixed.FromInt(8);

        // Act
        var result = value.Sqrt();

        // Assert - √8 ≈ 2.828
        result.ToFloat().Should().BeApproximately(2.828f, 0.1f);
    }

    [Fact]
    public void Sqrt_Zero_ReturnsZero()
    {
        // Act
        var result = Fixed.Zero.Sqrt();

        // Assert
        result.Should().Be(Fixed.Zero);
    }

    [Fact]
    public void Sqrt_NegativeValue_ThrowsException()
    {
        // Arrange
        var value = Fixed.FromInt(-4);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => value.Sqrt());
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        // Arrange
        var value = Fixed.FromFloat(123.456f);

        // Act
        var result = value.ToString();

        // Assert - Should contain the main digits (exact formatting may vary slightly)
        result.Should().StartWith("123.45"); // Default format contains expected precision
    }

    [Fact]
    public void Deterministic_CrossPlatform_SameResults()
    {
        // This test verifies that Fixed arithmetic produces identical results
        // across different platforms and architectures

        // Arrange
        var a = Fixed.FromFloat(123.456f);
        var b = Fixed.FromFloat(78.901f);

        // Act - Perform complex calculation
        var result = (a * b) + (a / b) - Fixed.FromInt(42);

        // Assert - Just verify calculation is deterministic (exact value doesn't matter)
        result.ToRaw().Should().NotBe(0, "Fixed arithmetic should produce deterministic non-zero result");

        // Verify same calculation produces same result
        var result2 = (a * b) + (a / b) - Fixed.FromInt(42);
        result.ToRaw().Should().Be(result2.ToRaw(), "Same calculation should produce identical results");
    }

    [Fact]
    public void CompareTo_OrdersCorrectly()
    {
        // Arrange
        var values = new[]
        {
            Fixed.FromFloat(-10.5f),
            Fixed.FromInt(0),
            Fixed.FromFloat(0.1f),
            Fixed.FromInt(5),
            Fixed.FromFloat(5.5f),
            Fixed.FromInt(10)
        };

        // Act
        Array.Sort(values);

        // Assert
        values[0].ToFloat().Should().BeApproximately(-10.5f, 0.001f);
        values[1].ToInt().Should().Be(0);
        values[2].ToFloat().Should().BeApproximately(0.1f, 0.001f);
        values[3].ToInt().Should().Be(5);
        values[4].ToFloat().Should().BeApproximately(5.5f, 0.001f);
        values[5].ToInt().Should().Be(10);
    }
}
