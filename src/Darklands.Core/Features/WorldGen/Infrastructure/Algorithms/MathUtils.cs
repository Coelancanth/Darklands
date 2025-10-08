using System;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;

/// <summary>
/// Mathematical utility functions for world generation algorithms.
/// Provides helpers matching WorldEngine/NumPy functionality.
/// </summary>
/// <remarks>
/// VS_025: Created for temperature simulation algorithm.
/// - Interp(): Matches numpy.interp (piecewise linear interpolation)
/// - SampleGaussian(): Box-Muller transform for normal distribution
/// </remarks>
public static class MathUtils
{
    /// <summary>
    /// Piecewise linear interpolation matching numpy.interp behavior.
    /// Maps input value x to output value based on control points (xp, fp).
    /// </summary>
    /// <param name="x">Input value to interpolate</param>
    /// <param name="xp">X-coordinates of control points (must be sorted ascending)</param>
    /// <param name="fp">Y-coordinates of control points (same length as xp)</param>
    /// <param name="left">Value to return when x < xp[0] (default: fp[0])</param>
    /// <param name="right">Value to return when x > xp[last] (default: fp[last])</param>
    /// <returns>Interpolated value</returns>
    /// <example>
    /// // Temperature latitude factor: cold poles, hot equator, cold poles
    /// float latitudeFactor = Interp(y_scaled,
    ///     xp: new[] { -0.5f, 0.0f, 0.5f },
    ///     fp: new[] { 0.0f, 1.0f, 0.0f });
    /// </example>
    public static float Interp(float x, float[] xp, float[] fp, float? left = null, float? right = null)
    {
        if (xp.Length != fp.Length)
            throw new ArgumentException("xp and fp must have the same length");

        if (xp.Length == 0)
            throw new ArgumentException("xp and fp must not be empty");

        // Handle out-of-bounds cases
        if (x <= xp[0])
            return left ?? fp[0];

        if (x >= xp[xp.Length - 1])
            return right ?? fp[xp.Length - 1];

        // Find interval [xp[i], xp[i+1]] containing x
        for (int i = 0; i < xp.Length - 1; i++)
        {
            if (x >= xp[i] && x <= xp[i + 1])
            {
                // Linear interpolation: y = y0 + (x - x0) * (y1 - y0) / (x1 - x0)
                float t = (x - xp[i]) / (xp[i + 1] - xp[i]);
                return fp[i] + t * (fp[i + 1] - fp[i]);
            }
        }

        // Fallback (should never reach here if xp is sorted)
        return fp[fp.Length - 1];
    }

    /// <summary>
    /// Samples from Gaussian (normal) distribution using Box-Muller transform.
    /// Converts two uniform random values to one Gaussian random value.
    /// </summary>
    /// <param name="rng">Random number generator</param>
    /// <param name="mean">Mean (center) of the distribution</param>
    /// <param name="hwhm">Half-width at half-maximum (relates to standard deviation)</param>
    /// <returns>Sample from Gaussian distribution</returns>
    /// <remarks>
    /// WorldEngine uses HWHM parameter instead of standard deviation:
    /// - sigma = hwhm / sqrt(2 * ln(2))
    /// - HWHM ≈ 1.177 × sigma
    ///
    /// Box-Muller transform (polar form):
    /// 1. Generate two uniform random values U1, U2 in [0, 1)
    /// 2. theta = 2π × U1
    /// 3. r = sqrt(-2 × ln(U2))
    /// 4. Z = r × cos(theta)  (normal distribution N(0,1))
    /// 5. Result = mean + sigma × Z
    /// </remarks>
    /// <example>
    /// var rng = new Random(seed);
    ///
    /// // Distance to sun: mean=1.0 (Earth-like), hwhm=0.12 (±22% variation)
    /// float distanceToSun = SampleGaussian(rng, mean: 1.0f, hwhm: 0.12f);
    /// distanceToSun = Math.Max(0.1f, distanceToSun);  // Clamp (no planets inside star!)
    ///
    /// // Result: Most values between 0.78 and 1.22 (hot vs cold planets)
    /// </example>
    public static float SampleGaussian(Random rng, float mean, float hwhm)
    {
        // Convert HWHM to standard deviation
        // sigma = HWHM / sqrt(2 * ln(2))
        // sqrt(2 * ln(2)) ≈ 1.177410023
        float sigma = hwhm / 1.177410023f;

        // Box-Muller transform (polar form)
        // Generate two uniform random values in [0, 1)
        float u1 = (float)rng.NextDouble();
        float u2 = (float)rng.NextDouble();

        // Avoid log(0) edge case
        if (u2 == 0.0f)
            u2 = float.Epsilon;

        // Box-Muller formula:
        // Z = sqrt(-2 * ln(U2)) * cos(2π * U1)
        float theta = 2.0f * MathF.PI * u1;
        float r = MathF.Sqrt(-2.0f * MathF.Log(u2));
        float z = r * MathF.Cos(theta);  // Standard normal N(0, 1)

        // Scale and shift to desired mean and sigma
        return mean + sigma * z;
    }
}
