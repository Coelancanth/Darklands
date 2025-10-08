using Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;
using FluentAssertions;
using Xunit;

namespace Darklands.Core.Tests.Features.WorldGen.Infrastructure.Algorithms;

/// <summary>
/// Tests for PrevailingWinds utility (VS_027 Phase 0).
/// Validates Earth's atmospheric circulation patterns (Hadley/Ferrel/Polar cells).
/// </summary>
public class PrevailingWindsTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // Trade Winds Band (0°-30°) - Westward Winds
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void GetWindDirection_Equator_ShouldReturnTradeWindsWestward()
    {
        // WHY: Equator (0°) is in Trade Winds band (Hadley cell) → Westward winds

        // Arrange
        float equatorLat = 0.5f;  // 0° latitude

        // Act
        var (x, y) = PrevailingWinds.GetWindDirection(equatorLat);

        // Assert
        x.Should().Be(-1f, because: "Trade Winds blow westward (X=-1)");
        y.Should().Be(0f, because: "Horizontal-only simplification (no Y-component)");
    }

    [Fact]
    public void GetWindDirection_Tropical_ShouldReturnTradeWindsWestward()
    {
        // WHY: 15°N and 15°S are both in Trade Winds band → Westward

        // Arrange
        float lat15N = 0.5f + (15f / 180f);   // 15°N
        float lat15S = 0.5f - (15f / 180f);   // 15°S

        // Act
        var (xNorth, yNorth) = PrevailingWinds.GetWindDirection(lat15N);
        var (xSouth, ySouth) = PrevailingWinds.GetWindDirection(lat15S);

        // Assert: Both hemispheres have westward trade winds
        xNorth.Should().Be(-1f, because: "Northern tropics have westward trade winds");
        xSouth.Should().Be(-1f, because: "Southern tropics have westward trade winds");
        yNorth.Should().Be(0f);
        ySouth.Should().Be(0f);
    }

    [Fact]
    public void GetWindDirection_TradeWindsBoundary_ShouldReturnWesterlies()
    {
        // WHY: 30° is the boundary between Trade Winds and Westerlies
        // At exactly 30°, Westerlies band claims the boundary (absLat >= 30)

        // Arrange
        float lat30N = 0.5f + (30f / 180f);   // 30°N (boundary)

        // Act
        var (x, y) = PrevailingWinds.GetWindDirection(lat30N);

        // Assert: At 30°, absLat = 30, condition is "absLat >= 30" (true) → Westerlies
        x.Should().Be(1f, because: "At 30° boundary, Westerlies band claims boundary (absLat >= 30)");
        y.Should().Be(0f);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Westerlies Band (30°-60°) - Eastward Winds
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void GetWindDirection_MidLatitudes_ShouldReturnWesterliesEastward()
    {
        // WHY: 45°N and 45°S are in Westerlies band (Ferrel cell) → Eastward winds

        // Arrange
        float lat45N = 0.5f + (45f / 180f);   // 45°N (classic westerlies)
        float lat45S = 0.5f - (45f / 180f);   // 45°S (roaring forties)

        // Act
        var (xNorth, yNorth) = PrevailingWinds.GetWindDirection(lat45N);
        var (xSouth, ySouth) = PrevailingWinds.GetWindDirection(lat45S);

        // Assert: Both hemispheres have eastward westerlies
        xNorth.Should().Be(1f, because: "Mid-latitude Northern Hemisphere has eastward westerlies");
        xSouth.Should().Be(1f, because: "Mid-latitude Southern Hemisphere has eastward westerlies (roaring forties)");
        yNorth.Should().Be(0f);
        ySouth.Should().Be(0f);
    }

    [Fact]
    public void GetWindDirection_WesterliesLowerBoundary_ShouldReturnEastward()
    {
        // WHY: Just above 30° enters Westerlies band

        // Arrange
        float lat31N = 0.5f + (31f / 180f);   // 31°N (just entered Westerlies)

        // Act
        var (x, y) = PrevailingWinds.GetWindDirection(lat31N);

        // Assert: absLat = 31 >= 30 → Westerlies
        x.Should().Be(1f, because: "31° is in Westerlies band (condition absLat >= 30)");
        y.Should().Be(0f);
    }

    [Fact]
    public void GetWindDirection_WesterliesUpperBoundary_ShouldReturnPolarEasterlies()
    {
        // WHY: 60° is the boundary between Westerlies and Polar Easterlies
        // At exactly 60°, Polar Easterlies band claims the boundary (absLat >= 60)

        // Arrange
        float lat60N = 0.5f + (60f / 180f);   // 60°N (boundary)

        // Act
        var (x, y) = PrevailingWinds.GetWindDirection(lat60N);

        // Assert: At 60°, absLat = 60, condition is "absLat >= 60" (true) → Polar Easterlies
        x.Should().Be(-1f, because: "At 60° boundary, Polar Easterlies band claims boundary (absLat >= 60)");
        y.Should().Be(0f);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Polar Easterlies Band (60°-90°) - Westward Winds
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void GetWindDirection_Polar_ShouldReturnPolarEasterliesWestward()
    {
        // WHY: 75°N and 75°S are in Polar Easterlies band → Westward winds

        // Arrange
        float lat75N = 0.5f + (75f / 180f);   // 75°N (Arctic)
        float lat75S = 0.5f - (75f / 180f);   // 75°S (Antarctic)

        // Act
        var (xNorth, yNorth) = PrevailingWinds.GetWindDirection(lat75N);
        var (xSouth, ySouth) = PrevailingWinds.GetWindDirection(lat75S);

        // Assert: Both poles have westward polar easterlies
        xNorth.Should().Be(-1f, because: "Arctic has westward polar easterlies");
        xSouth.Should().Be(-1f, because: "Antarctic has westward polar easterlies");
        yNorth.Should().Be(0f);
        ySouth.Should().Be(0f);
    }

    [Fact]
    public void GetWindDirection_Poles_ShouldReturnPolarEasterliesWestward()
    {
        // WHY: North/South Poles (90°) are in Polar Easterlies band

        // Arrange
        float northPole = 1.0f;   // 90°N
        float southPole = 0.0f;   // 90°S

        // Act
        var (xNorth, yNorth) = PrevailingWinds.GetWindDirection(northPole);
        var (xSouth, ySouth) = PrevailingWinds.GetWindDirection(southPole);

        // Assert
        xNorth.Should().Be(-1f, because: "North Pole has westward polar easterlies");
        xSouth.Should().Be(-1f, because: "South Pole has westward polar easterlies");
        yNorth.Should().Be(0f);
        ySouth.Should().Be(0f);
    }

    [Fact]
    public void GetWindDirection_PolarBoundary_ShouldReturnWestward()
    {
        // WHY: Just above 60° enters Polar Easterlies band

        // Arrange
        float lat61N = 0.5f + (61f / 180f);   // 61°N (just entered Polar Easterlies)

        // Act
        var (x, y) = PrevailingWinds.GetWindDirection(lat61N);

        // Assert: absLat = 61 >= 60 → Polar Easterlies
        x.Should().Be(-1f, because: "61° is in Polar Easterlies band (condition absLat >= 60)");
        y.Should().Be(0f);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Helper Methods (GetWindBandName, GetWindDirectionString)
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void GetWindBandName_ForAllLatitudes_ShouldReturnCorrectNames()
    {
        // WHY: Validate wind band name lookup for probe/UI display

        // Arrange & Act & Assert
        PrevailingWinds.GetWindBandName(0.0f).Should().Be("Polar Easterlies", because: "90°S");
        PrevailingWinds.GetWindBandName(0.25f).Should().Be("Westerlies", because: "45°S");
        PrevailingWinds.GetWindBandName(0.4f).Should().Be("Trade Winds", because: "18°S");
        PrevailingWinds.GetWindBandName(0.5f).Should().Be("Trade Winds", because: "0° Equator");
        PrevailingWinds.GetWindBandName(0.6f).Should().Be("Trade Winds", because: "18°N");
        PrevailingWinds.GetWindBandName(0.75f).Should().Be("Westerlies", because: "45°N");
        PrevailingWinds.GetWindBandName(1.0f).Should().Be("Polar Easterlies", because: "90°N");
    }

    [Fact]
    public void GetWindDirectionString_ForAllLatitudes_ShouldReturnCorrectDirections()
    {
        // WHY: Validate wind direction string for probe/UI display

        // Arrange & Act & Assert
        // Trade Winds (westward)
        PrevailingWinds.GetWindDirectionString(0.5f).Should().Be("← Westward",
            because: "Equator has westward trade winds");

        // Westerlies (eastward)
        PrevailingWinds.GetWindDirectionString(0.75f).Should().Be("→ Eastward",
            because: "45°N has eastward westerlies");

        // Polar Easterlies (westward)
        PrevailingWinds.GetWindDirectionString(1.0f).Should().Be("← Westward",
            because: "North Pole has westward polar easterlies");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Hemispheric Symmetry Validation
    // ═══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(15f)]   // Trade Winds
    [InlineData(45f)]   // Westerlies
    [InlineData(75f)]   // Polar Easterlies
    public void GetWindDirection_HemisphericSymmetry_ShouldBehaveIdentically(float latDegrees)
    {
        // WHY: Northern and Southern hemispheres should have identical wind patterns

        // Arrange
        float northLat = 0.5f + (latDegrees / 180f);
        float southLat = 0.5f - (latDegrees / 180f);

        // Act
        var (xNorth, yNorth) = PrevailingWinds.GetWindDirection(northLat);
        var (xSouth, ySouth) = PrevailingWinds.GetWindDirection(southLat);

        // Assert: Both hemispheres should have identical wind direction
        xNorth.Should().Be(xSouth, because: $"{latDegrees}°N and {latDegrees}°S have symmetric atmospheric circulation");
        yNorth.Should().Be(ySouth);
        yNorth.Should().Be(0f, because: "Horizontal-only wind vectors");
    }
}
