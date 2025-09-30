namespace Darklands.Core.Domain.Common;

/// <summary>
/// Uniquely identifies an actor in the game world.
/// Value type with value semantics - two ActorIds with the same Guid are equal.
/// </summary>
public readonly record struct ActorId(Guid Value)
{
    /// <summary>
    /// Creates a new unique ActorId.
    /// </summary>
    public static ActorId NewId() => new(Guid.NewGuid());

    /// <summary>
    /// Creates an ActorId from a string representation.
    /// </summary>
    /// <param name="value">String representation of a Guid</param>
    /// <returns>ActorId parsed from the string</returns>
    /// <exception cref="FormatException">If the string is not a valid Guid format</exception>
    public static ActorId From(string value) => new(Guid.Parse(value));

    /// <summary>
    /// Returns the string representation of this ActorId.
    /// </summary>
    public override string ToString() => Value.ToString();
}
