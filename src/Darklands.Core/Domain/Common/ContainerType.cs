namespace Darklands.Core.Domain.Common;

/// <summary>
/// Defines the type restrictions for inventory containers.
/// Used to enforce item type compatibility (e.g., weapon slots only accept weapons).
/// </summary>
/// <remarks>
/// PHASE 1: General (backpack) and WeaponOnly (weapon slot).
/// PHASE 2+: ConsumableOnly, ArmorOnly, etc. as design evolves.
///
/// WHY ENUM: Simple, compile-time safe, easy to extend without breaking existing code.
/// Type filtering logic lives in Application handlers (cross-aggregate orchestration).
/// </remarks>
public enum ContainerType
{
    /// <summary>
    /// Accepts all item types (backpack, storage chest).
    /// </summary>
    General = 0,

    /// <summary>
    /// Only accepts items with Type="weapon" (weapon slot).
    /// </summary>
    WeaponOnly = 1
}
