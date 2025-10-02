namespace Darklands.Core.Domain.Common;

/// <summary>
/// Uniquely identifies an item in the game world.
/// Value type with value semantics - two ItemIds with the same Guid are equal.
/// </summary>
/// <remarks>
/// ItemId is a shared primitive used across multiple features:
/// - Inventory (stores ItemId references)
/// - Combat (tracks wielded weapon ItemId)
/// - Loot (identifies dropped items)
/// - Crafting (recipe inputs/outputs)
/// </remarks>
public readonly record struct ItemId(Guid Value)
{
    /// <summary>
    /// Creates a new unique ItemId.
    /// </summary>
    public static ItemId NewId() => new(Guid.NewGuid());

    /// <summary>
    /// Creates an ItemId from a string representation.
    /// </summary>
    /// <param name="value">String representation of a Guid</param>
    /// <returns>ItemId parsed from the string</returns>
    /// <exception cref="FormatException">If the string is not a valid Guid format</exception>
    public static ItemId From(string value) => new(Guid.Parse(value));

    /// <summary>
    /// Returns the string representation of this ItemId.
    /// </summary>
    public override string ToString() => Value.ToString();
}
