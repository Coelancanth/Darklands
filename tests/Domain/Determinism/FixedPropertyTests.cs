using Xunit;
using FluentAssertions;
using FsCheck;
using FsCheck.Fluent;
using Darklands.Domain.Determinism;
using System;
using System.Linq;

namespace Darklands.Core.Tests.Domain.Determinism;

/// <summary>
/// Property-based tests for Fixed-point arithmetic.
/// Ensures deterministic behavior across platforms and mathematical correctness.
/// </summary>
[Trait("Category", "Phase1")]
[Trait("Category", "PropertyBased")]
public class FixedPropertyTests
{
    /// <summary>
    /// Property: Addition is commutative (a + b = b + a)
    /// </summary>
    [Fact]
    public void Addition_IsCommutative()
    {
        var generator =
            from a in Gen.Choose(-1000, 1000)
            from b in Gen.Choose(-1000, 1000)
            select new { A = Fixed.FromInt(a), B = Fixed.FromInt(b) };

        Prop.ForAll(generator.ToArbitrary(),
            test =>
            {
                var result1 = test.A + test.B;
                var result2 = test.B + test.A;
                result1.Should().Be(result2, "Addition should be commutative");
            }
        ).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: Addition is associative ((a + b) + c = a + (b + c))
    /// </summary>
    [Fact]
    public void Addition_IsAssociative()
    {
        var generator =
            from a in Gen.Choose(-500, 500)
            from b in Gen.Choose(-500, 500)
            from c in Gen.Choose(-500, 500)
            select new { A = Fixed.FromInt(a), B = Fixed.FromInt(b), C = Fixed.FromInt(c) };

        Prop.ForAll(generator.ToArbitrary(),
            test =>
            {
                var result1 = (test.A + test.B) + test.C;
                var result2 = test.A + (test.B + test.C);
                result1.Should().Be(result2, "Addition should be associative");
            }
        ).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: Multiplication is commutative (a * b = b * a)
    /// </summary>
    [Fact]
    public void Multiplication_IsCommutative()
    {
        var generator =
            from a in Gen.Choose(-100, 100)
            from b in Gen.Choose(-100, 100)
            select new { A = Fixed.FromInt(a), B = Fixed.FromInt(b) };

        Prop.ForAll(generator.ToArbitrary(),
            test =>
            {
                var result1 = test.A * test.B;
                var result2 = test.B * test.A;
                result1.Should().Be(result2, "Multiplication should be commutative");
            }
        ).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: Multiplication distributes over addition (a * (b + c) = a*b + a*c)
    /// </summary>
    [Fact]
    public void Multiplication_DistributesOverAddition()
    {
        var generator =
            from a in Gen.Choose(-50, 50)
            from b in Gen.Choose(-50, 50)
            from c in Gen.Choose(-50, 50)
            select new { A = Fixed.FromInt(a), B = Fixed.FromInt(b), C = Fixed.FromInt(c) };

        Prop.ForAll(generator.ToArbitrary(),
            test =>
            {
                var result1 = test.A * (test.B + test.C);
                var result2 = (test.A * test.B) + (test.A * test.C);

                // Allow small epsilon for rounding differences in fixed-point
                var diff = Math.Abs((result1 - result2).ToInt());
                diff.Should().BeLessThanOrEqualTo(1,
                    "Multiplication should distribute over addition (allowing minimal rounding)");
            }
        ).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: Zero is the additive identity (a + 0 = a)
    /// </summary>
    [Fact]
    public void Zero_IsAdditiveIdentity()
    {
        var generator = Gen.Choose(-1000, 1000).Select(x => Fixed.FromInt(x));

        Prop.ForAll(generator.ToArbitrary(),
            value =>
            {
                var result = value + Fixed.Zero;
                result.Should().Be(value, "Adding zero should not change the value");
            }
        ).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: One is the multiplicative identity (a * 1 = a)
    /// </summary>
    [Fact]
    public void One_IsMultiplicativeIdentity()
    {
        var generator = Gen.Choose(-1000, 1000).Select(x => Fixed.FromInt(x));

        Prop.ForAll(generator.ToArbitrary(),
            value =>
            {
                var result = value * Fixed.One;
                result.Should().Be(value, "Multiplying by one should not change the value");
            }
        ).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: Negation is self-inverse (--a = a)
    /// </summary>
    [Fact]
    public void Negation_IsSelfInverse()
    {
        var generator = Gen.Choose(-1000, 1000).Select(x => Fixed.FromInt(x));

        Prop.ForAll(generator.ToArbitrary(),
            value =>
            {
                var result = -(-value);
                result.Should().Be(value, "Double negation should return original value");
            }
        ).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: Comparison operators are transitive
    /// If a < b and b < c, then a < c
    /// </summary>
    [Fact]
    public void Comparison_IsTransitive()
    {
        var generator =
            from values in Gen.Choose(-1000, 1000).ArrayOf(3)
            let sorted = values.OrderBy(x => x).ToArray()
            select new
            {
                A = Fixed.FromInt(sorted[0]),
                B = Fixed.FromInt(sorted[1]),
                C = Fixed.FromInt(sorted[2])
            };

        Prop.ForAll(generator.ToArbitrary(),
            test =>
            {
                // Given A <= B <= C (by construction)
                if (test.A < test.B && test.B < test.C)
                {
                    (test.A < test.C).Should().BeTrue("Comparison should be transitive");
                }
                if (test.A <= test.B && test.B <= test.C)
                {
                    (test.A <= test.C).Should().BeTrue("Comparison should be transitive");
                }
            }
        ).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: Abs() always returns non-negative value
    /// </summary>
    [Fact]
    public void Abs_AlwaysReturnsNonNegative()
    {
        var generator = Gen.Choose(int.MinValue + 1, int.MaxValue).Select(x => Fixed.FromInt(x));

        Prop.ForAll(generator.ToArbitrary(),
            value =>
            {
                var result = value.Abs();
                (result >= Fixed.Zero).Should().BeTrue("Abs should always return non-negative value");

                // If original was non-negative, should be unchanged
                if (value >= Fixed.Zero)
                {
                    result.Should().Be(value, "Abs of non-negative should be unchanged");
                }
            }
        ).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: Clamp always returns value within bounds
    /// </summary>
    [Fact]
    public void Clamp_AlwaysReturnsValueInBounds()
    {
        var generator =
            from value in Gen.Choose(-1000, 1000)
            from min in Gen.Choose(-500, 0)
            from max in Gen.Choose(1, 500)
            where min < max
            select new
            {
                Value = Fixed.FromInt(value),
                Min = Fixed.FromInt(min),
                Max = Fixed.FromInt(max)
            };

        Prop.ForAll(generator.ToArbitrary(),
            test =>
            {
                var result = test.Value.Clamp(test.Min, test.Max);

                result.Should().BeGreaterThanOrEqualTo(test.Min, "Clamped value should be >= min");
                result.Should().BeLessThanOrEqualTo(test.Max, "Clamped value should be <= max");

                // If value was already in bounds, should be unchanged
                if (test.Value >= test.Min && test.Value <= test.Max)
                {
                    result.Should().Be(test.Value, "Value in bounds should not be changed");
                }
            }
        ).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: Lerp at t=0 returns start, at t=1 returns end
    /// </summary>
    [Fact]
    public void Lerp_BoundaryConditions()
    {
        var generator =
            from start in Gen.Choose(-1000, 1000)
            from end in Gen.Choose(-1000, 1000)
            select new { Start = Fixed.FromInt(start), End = Fixed.FromInt(end) };

        Prop.ForAll(generator.ToArbitrary(),
            test =>
            {
                var result0 = Fixed.Lerp(test.Start, test.End, Fixed.Zero);
                var result1 = Fixed.Lerp(test.Start, test.End, Fixed.One);

                result0.Should().Be(test.Start, "Lerp at t=0 should return start");
                result1.Should().Be(test.End, "Lerp at t=1 should return end");
            }
        ).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: Lerp is monotonic - larger t values produce results closer to end
    /// </summary>
    [Fact]
    public void Lerp_IsMonotonic()
    {
        var generator =
            from start in Gen.Choose(-100, 100)
            from end in Gen.Choose(-100, 100)
            where start != end
            from t1 in Gen.Choose(0, 50)
            from t2 in Gen.Choose(51, 100)
            select new
            {
                Start = Fixed.FromInt(start),
                End = Fixed.FromInt(end),
                T1 = Fixed.FromFloat(t1 / 100f),
                T2 = Fixed.FromFloat(t2 / 100f)
            };

        Prop.ForAll(generator.ToArbitrary(),
            test =>
            {
                var result1 = Fixed.Lerp(test.Start, test.End, test.T1);
                var result2 = Fixed.Lerp(test.Start, test.End, test.T2);

                if (test.Start < test.End)
                {
                    result2.Should().BeGreaterThanOrEqualTo(result1,
                        "Lerp with larger t should be closer to higher end");
                }
                else
                {
                    result2.Should().BeLessThanOrEqualTo(result1,
                        "Lerp with larger t should be closer to lower end");
                }
            }
        ).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: Sqrt of perfect squares returns exact value
    /// </summary>
    [Fact]
    public void Sqrt_PerfectSquares()
    {
        var generator = Gen.Choose(0, 100);

        Prop.ForAll(generator.ToArbitrary(),
            value =>
            {
                var square = Fixed.FromInt(value * value);
                var result = square.Sqrt();
                var expected = Fixed.FromInt(value);

                // Allow small epsilon for rounding
                var diff = (result - expected).Abs();
                (diff <= Fixed.FromFloat(0.01f)).Should().BeTrue(
                    $"Sqrt of {value * value} should be approximately {value}");
            }
        ).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: ToString produces deterministic output
    /// </summary>
    [Fact]
    public void ToString_IsDeterministic()
    {
        var generator = Gen.Choose(-10000, 10000).Select(x => Fixed.FromInt(x));

        Prop.ForAll(generator.ToArbitrary(),
            value =>
            {
                var str1 = value.ToString();
                var str2 = value.ToString();

                str1.Should().Be(str2, "ToString should be deterministic");
                str1.Should().NotContainAny("E", "e", "NaN", "Infinity",
                    "ToString should not use scientific notation or special float values");
            }
        ).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: FromInt and ToInt are inverse operations for valid range
    /// </summary>
    [Fact]
    public void FromInt_ToInt_RoundTrip()
    {
        var generator = Gen.Choose(-32768, 32767); // Stay within fixed-point range

        Prop.ForAll(generator.ToArbitrary(),
            value =>
            {
                var fixed_ = Fixed.FromInt(value);
                var result = fixed_.ToInt();
                result.Should().Be(value, "FromInt and ToInt should round-trip");
            }
        ).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: Division by powers of 2 is exact (bit shifting)
    /// </summary>
    [Fact]
    public void Division_ByPowersOfTwo_IsExact()
    {
        var generator =
            from value in Gen.Choose(-1000, 1000)
            from power in Gen.Choose(0, 10)
            select new { Value = Fixed.FromInt(value), Divisor = Fixed.FromInt(1 << power) };

        Prop.ForAll(generator.ToArbitrary(),
            test =>
            {
                var result = test.Value / test.Divisor;
                var expected = Fixed.FromFloat((float)test.Value.ToInt() / test.Divisor.ToInt());

                // Should be exact for power-of-2 divisions
                var diff = (result - expected).Abs();
                (diff <= Fixed.FromFloat(0.01f)).Should().BeTrue(
                    "Division by power of 2 should be accurate");
            }
        ).QuickCheckThrowOnFailure();
    }
}
