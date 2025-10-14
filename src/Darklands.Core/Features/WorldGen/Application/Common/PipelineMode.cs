namespace Darklands.Core.Features.WorldGen.Application.Common;

/// <summary>
/// Defines world generation pipeline execution modes (TD_027).
/// Controls stage ordering and iteration behavior for different use cases.
/// </summary>
/// <remarks>
/// Pipeline Architecture (TD_027):
///
/// **SinglePass Mode** (Fast Preview - 2s for 512×512):
/// - Stage Order: Climate → Erosion (one pass)
/// - Use Case: Real-time iteration, fast world preview, development testing
/// - Trade-off: Good approximation but no feedback convergence
/// - Physics: Assumes climate is independent of erosion (simplified)
///
/// **Iterative Mode** (High Quality - 6-10s for 512×512):
/// - Stage Order: (Erosion → Climate) × N iterations (feedback loop)
/// - Use Case: Final production worlds, maximum quality, research experiments
/// - Trade-off: Slower but converges to equilibrium (climate-erosion co-evolution)
/// - Physics: Models feedback loop (erosion changes terrain → climate responds → erosion adapts)
///
/// Why Two Modes?
/// - Different stage orders (not just iteration count!)
/// - Climate ↔ Erosion circular dependency requires different approaches
/// - Single-Pass: Climate BEFORE erosion (fast, one-shot approximation)
/// - Iterative: Erosion → Climate loop (slow, converges to equilibrium)
///
/// Real-World Analogy:
/// - Single-Pass: Taking a photo (captures current state instantly)
/// - Iterative: Time-lapse video (captures evolution over time)
///
/// Technical Implications:
/// - Single-Pass: Simpler pipeline, fewer stages, predictable performance
/// - Iterative: Requires convergence detection, iteration logging, more complex orchestration
///
/// Selection Guidance:
/// - Development/Testing: SinglePass (fast feedback loops)
/// - Content Creation: SinglePass then Iterative (fast preview → final quality)
/// - Research/Experimentation: Iterative (study feedback dynamics)
/// - Production Worlds: Iterative (maximum fidelity)
/// </remarks>
public enum PipelineMode
{
    /// <summary>
    /// Single-pass pipeline: Climate → Erosion (one iteration).
    /// Fast preview mode (~2s for 512×512 map).
    /// Good for real-time iteration, development, and testing.
    /// </summary>
    SinglePass = 0,

    /// <summary>
    /// Iterative pipeline: (Erosion → Climate) × N iterations (feedback loop).
    /// High-quality mode (~6-10s for 512×512 map with 3-5 iterations).
    /// Converges climate-erosion co-evolution to equilibrium.
    /// Best for final production worlds.
    /// </summary>
    Iterative = 1
}
