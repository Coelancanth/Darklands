namespace Darklands.SharedKernel.Domain;

/// <summary>
/// Marker interface for business rules that can be evaluated across bounded contexts.
/// </summary>
public interface IBusinessRule
{
    /// <summary>
    /// Evaluates whether the business rule is satisfied.
    /// </summary>
    /// <returns>True if the rule is satisfied, false otherwise.</returns>
    bool IsSatisfied();
    
    /// <summary>
    /// Gets a human-readable message describing why the rule is not satisfied.
    /// </summary>
    string ErrorMessage { get; }
}