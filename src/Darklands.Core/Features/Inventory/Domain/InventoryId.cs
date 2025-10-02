namespace Darklands.Core.Features.Inventory.Domain;

/// <summary>
/// Uniquely identifies an inventory instance.
/// Value type with value semantics - two InventoryIds with the same Guid are equal.
/// </summary>
public readonly record struct InventoryId(Guid Value)
{
    /// <summary>
    /// Creates a new unique InventoryId.
    /// </summary>
    public static InventoryId NewId() => new(Guid.NewGuid());

    /// <summary>
    /// Creates an InventoryId from a string representation.
    /// </summary>
    /// <param name="value">String representation of a Guid</param>
    /// <returns>InventoryId parsed from the string</returns>
    /// <exception cref="FormatException">If the string is not a valid Guid format</exception>
    public static InventoryId From(string value) => new(Guid.Parse(value));

    /// <summary>
    /// Returns the string representation of this InventoryId.
    /// </summary>
    public override string ToString() => Value.ToString();
}
