namespace Darklands.SharedKernel.Domain;

/// <summary>
/// A strongly-typed identifier for entities that can be referenced across bounded contexts.
/// Uses Guid for uniqueness while providing type safety.
/// </summary>
/// <param name="Value">The unique identifier value.</param>
public readonly record struct EntityId(Guid Value)
{
    /// <summary>
    /// Creates a new unique EntityId.
    /// </summary>
    public static EntityId New() => new(Guid.NewGuid());
    
    /// <summary>
    /// Creates an EntityId from a string representation.
    /// </summary>
    /// <param name="value">The string representation of the GUID.</param>
    /// <returns>An EntityId if parsing succeeds.</returns>
    /// <exception cref="FormatException">Thrown when the string is not a valid GUID format.</exception>
    public static EntityId Parse(string value) => new(Guid.Parse(value));
    
    /// <summary>
    /// Tries to create an EntityId from a string representation.
    /// </summary>
    /// <param name="value">The string representation of the GUID.</param>
    /// <param name="entityId">The resulting EntityId if parsing succeeds.</param>
    /// <returns>True if parsing succeeded, false otherwise.</returns>
    public static bool TryParse(string value, out EntityId entityId)
    {
        if (Guid.TryParse(value, out var guid))
        {
            entityId = new EntityId(guid);
            return true;
        }
        
        entityId = default;
        return false;
    }
    
    /// <summary>
    /// Returns the string representation of this EntityId.
    /// </summary>
    public override string ToString() => Value.ToString();
}