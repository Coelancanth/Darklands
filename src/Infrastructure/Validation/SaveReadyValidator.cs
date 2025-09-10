using Darklands.Core.Domain.Common;
using LanguageExt;
using LanguageExt.Common;
using System.Collections.Immutable;
using System.Reflection;
using static LanguageExt.Prelude;

namespace Darklands.Core.Infrastructure.Validation;

/// <summary>
/// Validates that entities comply with ADR-005 save-ready entity requirements.
/// Ensures entities can be safely serialized, deserialized, and persisted to save files.
/// Used in testing and development to catch save-compatibility issues early.
/// </summary>
public static class SaveReadyValidator
{
    /// <summary>
    /// Validates that an entity implements save-ready patterns according to ADR-005.
    /// </summary>
    /// <param name="entity">Entity instance to validate</param>
    /// <returns>Success if entity is save-ready, or detailed failure information</returns>
    public static Fin<Unit> ValidateEntity(object entity)
    {
        if (entity == null)
            return FinFail<Unit>(Error.New("Entity cannot be null"));

        var entityType = entity.GetType();
        var violations = new List<string>();

        // Requirement 1: Must implement IPersistentEntity
        if (!typeof(IPersistentEntity).IsAssignableFrom(entityType))
        {
            violations.Add($"Entity {entityType.Name} must implement IPersistentEntity interface");
        }

        // Requirement 2: Should be a record or record struct for immutability
        if (!IsRecordType(entityType))
        {
            violations.Add($"Entity {entityType.Name} should be a record or record struct for immutability guarantees");
        }

        // Requirement 3: No mutable fields or properties
        var mutableMembers = FindMutableMembers(entityType);
        if (mutableMembers.Any())
        {
            violations.Add($"Entity {entityType.Name} has mutable members: {string.Join(", ", mutableMembers)}");
        }

        // Requirement 4: ID properties should use stable ID types
        var invalidIdProperties = FindInvalidIdProperties(entityType);
        if (invalidIdProperties.Any())
        {
            violations.Add($"Entity {entityType.Name} has non-stable ID properties: {string.Join(", ", invalidIdProperties)}");
        }

        // Requirement 5: Collections should be immutable
        var mutableCollections = FindMutableCollections(entityType);
        if (mutableCollections.Any())
        {
            violations.Add($"Entity {entityType.Name} has mutable collections: {string.Join(", ", mutableCollections)}");
        }

        // Return result
        if (violations.Any())
        {
            var violationText = string.Join("; ", violations);
            return FinFail<Unit>(Error.New($"ADR-005 violations found: {violationText}"));
        }

        return FinSucc(unit);
    }

    /// <summary>
    /// Validates multiple entities in a batch operation.
    /// </summary>
    /// <param name="entities">Collection of entities to validate</param>
    /// <returns>Success if all entities are save-ready, or aggregated failure information</returns>
    public static Fin<Unit> ValidateEntities(IEnumerable<object> entities)
    {
        if (entities == null)
            return FinFail<Unit>(Error.New("Entities collection cannot be null"));

        var allViolations = new List<string>();
        var entityList = entities.ToList();

        if (!entityList.Any())
            return FinSucc(unit);

        foreach (var (entity, index) in entityList.Select((e, i) => (e, i)))
        {
            var result = ValidateEntity(entity);
            if (result.IsFail)
            {
                result.Match(
                    Succ: _ => { }, // Won't happen since IsFail is true
                    Fail: error => allViolations.Add($"Entity[{index}]: {error.Message}")
                );
            }
        }

        if (allViolations.Any())
        {
            var violationText = string.Join("; ", allViolations);
            return FinFail<Unit>(Error.New($"ADR-005 batch validation failures: {violationText}"));
        }

        return FinSucc(unit);
    }

    /// <summary>
    /// Validates that a type definition complies with save-ready patterns.
    /// Useful for architecture tests that validate design-time compliance.
    /// </summary>
    /// <param name="entityType">Type to validate</param>
    /// <returns>Success if type is save-ready, or detailed failure information</returns>
    public static Fin<Unit> ValidateType(Type entityType)
    {
        if (entityType == null)
            return FinFail<Unit>(Error.New("Entity type cannot be null"));

        // For type validation, we check structural requirements without needing an instance
        var violations = new List<string>();

        // Check IPersistentEntity implementation
        if (!typeof(IPersistentEntity).IsAssignableFrom(entityType))
        {
            violations.Add($"Type {entityType.Name} must implement IPersistentEntity interface");
        }

        // Check record type
        if (!IsRecordType(entityType))
        {
            violations.Add($"Type {entityType.Name} should be a record or record struct for immutability");
        }

        // Check for mutable members
        var mutableMembers = FindMutableMembers(entityType);
        if (mutableMembers.Any())
        {
            violations.Add($"Type {entityType.Name} has mutable members: {string.Join(", ", mutableMembers)}");
        }

        if (violations.Any())
        {
            var violationText = string.Join("; ", violations);
            return FinFail<Unit>(Error.New($"Type ADR-005 violations: {violationText}"));
        }

        return FinSucc(unit);
    }

    private static bool IsRecordType(Type type)
    {
        // Check for record type indicators
        // In .NET, records have specific characteristics we can detect
        return type.IsSealed &&
               (type.IsValueType || // record struct
                HasRecordAttributes(type)); // record class
    }

    private static bool HasRecordAttributes(Type type)
    {
        // Records typically have compiler-generated members and attributes
        // This is a simplified check - in production might need more sophisticated detection
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        // Records have generated equality methods
        return methods.Any(m => m.Name == "Equals" && m.GetParameters().Length == 1 &&
                               m.GetParameters()[0].ParameterType == type) ||
               methods.Any(m => m.Name == "GetHashCode" && m.GetParameters().Length == 0);
    }

    private static IEnumerable<string> FindMutableMembers(Type type)
    {
        var mutableMembers = new List<string>();

        // Check properties
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            if (prop.CanWrite && prop.SetMethod?.IsPublic == true)
            {
                // Allow init-only setters (records use these)
                if (!IsInitOnlySetter(prop.SetMethod))
                {
                    mutableMembers.Add($"Property: {prop.Name}");
                }
            }
        }

        // Check fields
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (var field in fields)
        {
            if (!field.IsInitOnly)
            {
                mutableMembers.Add($"Field: {field.Name}");
            }
        }

        return mutableMembers;
    }

    private static bool IsInitOnlySetter(MethodInfo setMethod)
    {
        // Check if the setter has the init-only modifier
        // This is a simplified check - more sophisticated detection may be needed
        return setMethod.ReturnParameter.GetRequiredCustomModifiers()
            .Any(t => t.Name.Contains("IsExternalInit"));
    }

    private static IEnumerable<string> FindInvalidIdProperties(Type type)
    {
        var invalidIds = new List<string>();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            // Check if property name suggests it's an ID
            if (prop.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
            {
                // Validate it's a proper ID type
                if (!IsValidIdType(prop.PropertyType))
                {
                    invalidIds.Add(prop.Name);
                }
            }
        }

        return invalidIds;
    }

    private static bool IsValidIdType(Type type)
    {
        // Valid ID types for save-ready entities
        return type == typeof(Guid) ||
               type == typeof(string) ||
               type.Name.EndsWith("Id") || // Custom ID types like ActorId, GridId
               type.IsValueType; // Assume value types are properly designed ID types
    }

    private static IEnumerable<string> FindMutableCollections(Type type)
    {
        var mutableCollections = new List<string>();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            if (IsCollectionType(prop.PropertyType))
            {
                if (!IsImmutableCollectionType(prop.PropertyType))
                {
                    mutableCollections.Add(prop.Name);
                }
            }
        }

        return mutableCollections;
    }

    private static bool IsCollectionType(Type type)
    {
        return typeof(System.Collections.IEnumerable).IsAssignableFrom(type) &&
               type != typeof(string); // String is enumerable but not a collection
    }

    private static bool IsImmutableCollectionType(Type type)
    {
        // Check for known immutable collection types
        if (type.IsGenericType)
        {
            var genericTypeDef = type.GetGenericTypeDefinition();
            return genericTypeDef == typeof(ImmutableArray<>) ||
                   genericTypeDef == typeof(ImmutableList<>) ||
                   genericTypeDef == typeof(ImmutableHashSet<>) ||
                   genericTypeDef == typeof(ImmutableDictionary<,>) ||
                   genericTypeDef == typeof(IReadOnlyList<>) ||
                   genericTypeDef == typeof(IReadOnlyCollection<>) ||
                   genericTypeDef == typeof(IReadOnlyDictionary<,>);
        }

        return false;
    }
}
