using System.Globalization;

namespace Darklands.Core.Domain.Determinism;

/// <summary>
/// Fixed-point number for deterministic arithmetic following ADR-004.
/// Uses 16.16 format (16 bits integer, 16 bits fraction) for consistent cross-platform math.
/// 
/// Replaces floating-point operations in gameplay logic to ensure:
/// - Identical results across all platforms and hardware
/// - No floating-point precision drift over time  
/// - Deterministic save/load behavior
/// - Predictable combat and game mechanics
/// 
/// Range: -32768.0 to 32767.99998 (approximately)
/// Precision: 1/65536 ≈ 0.0000152587890625
/// </summary>
public readonly struct Fixed : IEquatable<Fixed>, IComparable<Fixed>
{
    private readonly int _raw;

    // Constants for 16.16 fixed-point format
    private const int FractionBits = 16;
    private const int FractionMask = (1 << FractionBits) - 1; // 0x0000FFFF
    private const int OneRaw = 1 << FractionBits; // 0x00010000 = 65536

    // Private constructor from raw value
    private Fixed(int raw) => _raw = raw;

    // Factory methods for safe construction
    public static Fixed FromInt(int value) => new(value << FractionBits);
    public static Fixed FromRaw(int raw) => new(raw);
    public static Fixed FromFloat(float value) => new((int)(value * OneRaw));
    public static Fixed FromDouble(double value) => new((int)(value * OneRaw));

    // Common constants
    public static readonly Fixed Zero = new(0);
    public static readonly Fixed One = new(OneRaw);
    public static readonly Fixed Half = new(OneRaw >> 1);
    public static readonly Fixed MinValue = new(int.MinValue);
    public static readonly Fixed MaxValue = new(int.MaxValue);

    // Conversion methods
    public int ToInt() => _raw >> FractionBits;
    public float ToFloat() => _raw / (float)OneRaw;
    public double ToDouble() => _raw / (double)OneRaw;
    public int ToRaw() => _raw;

    // Arithmetic operators with overflow checking
    public static Fixed operator +(Fixed a, Fixed b)
    {
        var result = (long)a._raw + b._raw;
        if (result > int.MaxValue || result < int.MinValue)
            throw new OverflowException($"Fixed addition overflow: {a} + {b}");
        return new((int)result);
    }

    public static Fixed operator -(Fixed a, Fixed b)
    {
        var result = (long)a._raw - b._raw;
        if (result > int.MaxValue || result < int.MinValue)
            throw new OverflowException($"Fixed subtraction overflow: {a} - {b}");
        return new((int)result);
    }

    public static Fixed operator *(Fixed a, Fixed b)
    {
        var result = (long)a._raw * b._raw >> FractionBits;
        if (result > int.MaxValue || result < int.MinValue)
            throw new OverflowException($"Fixed multiplication overflow: {a} * {b}");
        return new((int)result);
    }

    public static Fixed operator /(Fixed a, Fixed b)
    {
        if (b._raw == 0)
            throw new DivideByZeroException("Fixed division by zero");

        var result = ((long)a._raw << FractionBits) / b._raw;
        if (result > int.MaxValue || result < int.MinValue)
            throw new OverflowException($"Fixed division overflow: {a} / {b}");
        return new((int)result);
    }

    public static Fixed operator -(Fixed a) => new(-a._raw);

    // Comparison operators (deterministic ordering)
    public static bool operator >(Fixed a, Fixed b) => a._raw > b._raw;
    public static bool operator <(Fixed a, Fixed b) => a._raw < b._raw;
    public static bool operator >=(Fixed a, Fixed b) => a._raw >= b._raw;
    public static bool operator <=(Fixed a, Fixed b) => a._raw <= b._raw;
    public static bool operator ==(Fixed a, Fixed b) => a._raw == b._raw;
    public static bool operator !=(Fixed a, Fixed b) => a._raw != b._raw;

    // Implicit conversions from integers
    public static implicit operator Fixed(int value) => FromInt(value);
    public static implicit operator Fixed(short value) => FromInt(value);
    public static implicit operator Fixed(byte value) => FromInt(value);

    // Explicit conversions to prevent accidental precision loss
    public static explicit operator int(Fixed value) => value.ToInt();
    public static explicit operator float(Fixed value) => value.ToFloat();
    public static explicit operator double(Fixed value) => value.ToDouble();

    // Interface implementations
    public bool Equals(Fixed other) => _raw == other._raw;

    public override bool Equals(object? obj) => obj is Fixed other && Equals(other);

    public override int GetHashCode() => _raw;

    public int CompareTo(Fixed other) => _raw.CompareTo(other._raw);

    // String representation with deterministic formatting
    public override string ToString() => ToDouble().ToString("F5", CultureInfo.InvariantCulture);

    public string ToString(string format) => ToDouble().ToString(format, CultureInfo.InvariantCulture);

    // Useful mathematical functions
    public Fixed Abs() => _raw < 0 ? (_raw == int.MinValue ? MaxValue : new(-_raw)) : this;

    public Fixed Clamp(Fixed min, Fixed max)
    {
        if (this < min) return min;
        if (this > max) return max;
        return this;
    }

    public Fixed Min(Fixed other) => this < other ? this : other;
    public Fixed Max(Fixed other) => this > other ? this : other;

    // Linear interpolation (useful for smooth gameplay transitions)
    public static Fixed Lerp(Fixed a, Fixed b, Fixed t)
    {
        // Clamp t to [0, 1] range
        t = t.Clamp(Zero, One);
        return a + (b - a) * t;
    }

    // Integer square root (useful for distance calculations)
    public Fixed Sqrt()
    {
        if (_raw < 0)
            throw new ArgumentException("Cannot take square root of negative number");

        if (_raw == 0)
            return Zero;

        // Newton-Raphson method adapted for fixed-point
        var x = new Fixed(_raw);
        var prev = Zero;

        while (x != prev)
        {
            prev = x;
            x = (x + this / x) * Half;
        }

        return x;
    }
}
