using CSharpFunctionalExtensions;

namespace Darklands.Core.Domain.Common;

/// <summary>
/// Type of weapon, determines range and attack mechanics.
/// </summary>
public enum WeaponType
{
    /// <summary>
    /// Melee weapon - attacks adjacent tiles only (8-directional).
    /// Examples: Sword, Dagger, Axe, Spear
    /// </summary>
    Melee,

    /// <summary>
    /// Ranged weapon - attacks at distance with line-of-sight validation.
    /// Examples: Bow, Crossbow, Thrown weapons
    /// </summary>
    Ranged
}

/// <summary>
/// Represents a weapon's combat properties.
/// Immutable value object with smart constructor validation.
/// </summary>
/// <remarks>
/// <para><b>Weapon Properties</b>:</para>
/// <list type="bullet">
/// <item><description>Damage - How much health reduced on hit</description></item>
/// <item><description>TimeCost - Turn queue time units consumed per attack</description></item>
/// <item><description>Range - Maximum attack distance (tiles)</description></item>
/// <item><description>Type - Melee (adjacent) or Ranged (line-of-sight)</description></item>
/// </list>
///
/// <para><b>Range Semantics</b>:</para>
/// <list type="bullet">
/// <item><description>Melee: Range ignored, must be adjacent (1 tile, 8-directional)</description></item>
/// <item><description>Ranged: Range = max tiles, requires FOV line-of-sight</description></item>
/// </list>
///
/// <para><b>Examples</b>:</para>
/// <code>
/// // Iron Sword - melee, 15 damage, 100 time units
/// var sword = Weapon.Create("Iron Sword", 15, 100, 1, WeaponType.Melee).Value;
///
/// // Hunting Bow - ranged, 10 damage, 120 time units, 8 tile range
/// var bow = Weapon.Create("Hunting Bow", 10, 120, 8, WeaponType.Ranged).Value;
/// </code>
/// </remarks>
public sealed record Weapon
{
    /// <summary>
    /// Translation key for weapon name (e.g., "WEAPON_IRON_SWORD").
    /// Follows ADR-005 i18n discipline.
    /// </summary>
    public string NameKey { get; }

    /// <summary>
    /// Damage dealt per successful attack.
    /// </summary>
    public float Damage { get; }

    /// <summary>
    /// Time units consumed per attack (integrates with TurnQueue from VS_007).
    /// </summary>
    public int TimeCost { get; }

    /// <summary>
    /// Maximum attack range in tiles.
    /// For melee weapons, this is typically 1 (adjacent only).
    /// For ranged weapons, this is the maximum firing distance.
    /// </summary>
    public int Range { get; }

    /// <summary>
    /// Weapon type - determines attack mechanics (adjacent vs line-of-sight).
    /// </summary>
    public WeaponType Type { get; }

    private Weapon(string nameKey, float damage, int timeCost, int range, WeaponType type)
    {
        NameKey = nameKey;
        Damage = damage;
        TimeCost = timeCost;
        Range = range;
        Type = type;
    }

    /// <summary>
    /// Creates a new Weapon instance with validation.
    /// </summary>
    /// <param name="nameKey">Translation key for weapon name</param>
    /// <param name="damage">Damage per attack (must be > 0)</param>
    /// <param name="timeCost">Time units per attack (must be > 0)</param>
    /// <param name="range">Maximum range in tiles (must be > 0)</param>
    /// <param name="type">Weapon type (melee or ranged)</param>
    /// <returns>Result with Weapon on success, or failure message on validation error</returns>
    public static Result<Weapon> Create(
        string nameKey,
        float damage,
        int timeCost,
        int range,
        WeaponType type)
    {
        // Validate name key
        if (string.IsNullOrWhiteSpace(nameKey))
        {
            return Result.Failure<Weapon>("Weapon name key cannot be empty");
        }

        // Validate damage
        if (damage <= 0)
        {
            return Result.Failure<Weapon>("Weapon damage must be positive");
        }

        // Validate time cost
        if (timeCost <= 0)
        {
            return Result.Failure<Weapon>("Weapon time cost must be positive");
        }

        // Validate range
        if (range <= 0)
        {
            return Result.Failure<Weapon>("Weapon range must be positive");
        }

        return Result.Success(new Weapon(nameKey, damage, timeCost, range, type));
    }
}
