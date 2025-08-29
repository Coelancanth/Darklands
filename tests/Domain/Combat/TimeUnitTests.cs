using Xunit;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Darklands.Core.Domain.Combat;

namespace Darklands.Core.Tests.Domain.Combat;

/// <summary>
/// Unit tests for TimeUnit value object.
/// Tests all operations, edge cases, and property-based invariants.
/// </summary>
[Trait("Category", "Domain")]
[Trait("Feature", "Combat")]
public class TimeUnitTests
{
    [Fact]
    public void Constructor_ValidValue_CreatesTimeUnit()
    {
        // Arrange
        var value = 1000;
        
        // Act
        var timeUnit = new TimeUnit(value);
        
        // Assert
        timeUnit.Value.Should().Be(value);
        timeUnit.IsValid.Should().BeTrue();
    }
    
    [Fact]
    public void Zero_ReturnsZeroTimeUnit()
    {
        // Act & Assert
        TimeUnit.Zero.Value.Should().Be(0);
        TimeUnit.Zero.IsValid.Should().BeTrue();
    }
    
    [Fact]
    public void Minimum_ReturnsOneMillisecond()
    {
        // Act & Assert
        TimeUnit.Minimum.Value.Should().Be(1);
        TimeUnit.Minimum.IsValid.Should().BeTrue();
    }
    
    [Fact]
    public void Maximum_ReturnsTenSeconds()
    {
        // Act & Assert
        TimeUnit.Maximum.Value.Should().Be(10_000);
        TimeUnit.Maximum.IsValid.Should().BeTrue();
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1000)]
    [InlineData(10_000)]
    public void IsValid_ValidValues_ReturnsTrue(int value)
    {
        // Act & Assert
        new TimeUnit(value).IsValid.Should().BeTrue();
    }
    
    [Theory]
    [InlineData(-1)]
    [InlineData(-1000)]
    [InlineData(10_001)]
    [InlineData(100_000)]
    public void IsValid_InvalidValues_ReturnsFalse(int value)
    {
        // Act & Assert
        new TimeUnit(value).IsValid.Should().BeFalse();
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1000)]
    [InlineData(10_000)]
    public void FromMilliseconds_ValidValues_ReturnsSuccess(int milliseconds)
    {
        // Act
        var result = TimeUnit.FromMilliseconds(milliseconds);
        
        // Assert
        result.IsSucc.Should().BeTrue();
        result.IfSucc(timeUnit => timeUnit.Value.Should().Be(milliseconds));
    }
    
    [Theory]
    [InlineData(-1)]
    [InlineData(-1000)]
    [InlineData(10_001)]
    [InlineData(100_000)]
    public void FromMilliseconds_InvalidValues_ReturnsFailure(int milliseconds)
    {
        // Act
        var result = TimeUnit.FromMilliseconds(milliseconds);
        
        // Assert
        result.IsFail.Should().BeTrue();
    }
    
    [Fact]
    public void AdditionOperator_ValidValues_AddsCorrectly()
    {
        // Arrange
        var timeA = new TimeUnit(300);
        var timeB = new TimeUnit(200);
        
        // Act
        var result = timeA + timeB;
        
        // Assert
        result.Value.Should().Be(500);
    }
    
    [Fact]
    public void AdditionOperator_ExceedsMaximum_ClampsToMaximum()
    {
        // Arrange
        var timeA = new TimeUnit(9_000);
        var timeB = new TimeUnit(2_000);
        
        // Act
        var result = timeA + timeB;
        
        // Assert
        result.Value.Should().Be(TimeUnit.Maximum.Value);
    }
    
    [Fact]
    public void SubtractionOperator_ValidValues_SubtractsCorrectly()
    {
        // Arrange
        var timeA = new TimeUnit(500);
        var timeB = new TimeUnit(200);
        
        // Act
        var result = timeA - timeB;
        
        // Assert
        result.Value.Should().Be(300);
    }
    
    [Fact]
    public void SubtractionOperator_ResultNegative_ClampsToZero()
    {
        // Arrange
        var timeA = new TimeUnit(200);
        var timeB = new TimeUnit(500);
        
        // Act
        var result = timeA - timeB;
        
        // Assert
        result.Value.Should().Be(0);
    }
    
    [Theory]
    [InlineData(1000, 2.0, 2000)]
    [InlineData(1000, 0.5, 500)]
    [InlineData(1000, 1.0, 1000)]
    public void MultiplicationOperator_ValidFactor_MultipliesCorrectly(int baseValue, double factor, int expected)
    {
        // Arrange
        var time = new TimeUnit(baseValue);
        
        // Act
        var result = time * factor;
        
        // Assert
        result.Value.Should().Be(expected);
    }
    
    [Fact]
    public void MultiplicationOperator_ExceedsMaximum_ClampsToMaximum()
    {
        // Arrange
        var time = new TimeUnit(9_000);
        var factor = 2.0;
        
        // Act
        var result = time * factor;
        
        // Assert
        result.Value.Should().Be(TimeUnit.Maximum.Value);
    }
    
    [Theory]
    [InlineData(100, 200, true)]
    [InlineData(200, 100, false)]
    [InlineData(100, 100, false)]
    public void LessThanOperator_CompareValues_ReturnsCorrectResult(int leftValue, int rightValue, bool expected)
    {
        // Arrange
        var left = new TimeUnit(leftValue);
        var right = new TimeUnit(rightValue);
        
        // Act & Assert
        (left < right).Should().Be(expected);
    }
    
    [Theory]
    [InlineData(200, 100, true)]
    [InlineData(100, 200, false)]
    [InlineData(100, 100, false)]
    public void GreaterThanOperator_CompareValues_ReturnsCorrectResult(int leftValue, int rightValue, bool expected)
    {
        // Arrange
        var left = new TimeUnit(leftValue);
        var right = new TimeUnit(rightValue);
        
        // Act & Assert
        (left > right).Should().Be(expected);
    }
    
    [Theory]
    [InlineData(500, "500ms")]
    [InlineData(1000, "1.0s")]
    [InlineData(1500, "1.5s")]
    [InlineData(60000, "1.0min")]
    [InlineData(90000, "1.5min")]
    public void ToString_VariousValues_ReturnsFormattedString(int value, string expected)
    {
        // Arrange
        var timeUnit = new TimeUnit(value);
        
        // Act & Assert
        timeUnit.ToString().Should().Be(expected);
    }
    
    // Property-based tests using FsCheck
    [Property]
    public bool Addition_IsCommutative(ushort a, ushort b)
    {
        var timeA = new TimeUnit(a);
        var timeB = new TimeUnit(b);
        
        return (timeA + timeB).Value == (timeB + timeA).Value;
    }
    
    [Property]
    public bool Addition_WithZero_IsIdentity(ushort value)
    {
        var time = new TimeUnit(value);
        return (time + TimeUnit.Zero).Value == time.Value;
    }
    
    [Property]
    public bool Subtraction_WithSelf_IsZero(ushort value)
    {
        var time = new TimeUnit(value);
        return (time - time).Value == 0;
    }
    
    [Property]
    public bool Multiplication_WithOne_IsIdentity(ushort value)
    {
        var time = new TimeUnit(value);
        return (time * 1.0).Value == time.Value;
    }
    
    [Property]
    public bool Multiplication_WithZero_IsZero(ushort value)
    {
        var time = new TimeUnit(value);
        return (time * 0.0).Value == 0;
    }
}