using FluentAssertions;
using Xunit;
using Darklands.Core.Infrastructure.Validation;
using Darklands.Core.Domain.Common;
using Darklands.Core.Domain.Actor;
using Darklands.Core.Domain.Grid;
using Darklands.Core.Infrastructure.Identity;
using System.Collections.Immutable;

namespace Darklands.Core.Tests.Infrastructure.Validation;

public class SaveReadyValidatorTests
{
    [Fact]
    [Trait("Category", "Infrastructure")]
    [Trait("Category", "Phase3")]
    public void ValidateEntity_WithNullEntity_ReturnsFailure()
    {
        var result = SaveReadyValidator.ValidateEntity(null!);

        result.IsFail.Should().BeTrue();
        result.Match(
            Succ: _ => throw new InvalidOperationException("Should not succeed"),
            Fail: error => error.Message.Should().Contain("Entity cannot be null")
        );
    }

    [Fact]
    [Trait("Category", "Infrastructure")]
    [Trait("Category", "Phase3")]
    public void ValidateEntity_WithValidActor_ReturnsSuccess()
    {
        var actor = Actor.CreateAtFullHealth(
            ActorId.NewId(GuidIdGenerator.Instance),
            100,
            "Test Actor"
        ).Match(
            Succ: a => a,
            Fail: error => throw new InvalidOperationException($"Failed to create actor: {error.Message}")
        );

        var result = SaveReadyValidator.ValidateEntity(actor);

        result.IsSucc.Should().BeTrue("Valid Actor should pass save-ready validation");
    }

    [Fact]
    [Trait("Category", "Infrastructure")]
    [Trait("Category", "Phase3")]
    public void ValidateEntity_WithValidGrid_ReturnsSuccess()
    {
        var grid = Grid.Create(GuidIdGenerator.Instance, 3, 3).Match(
            Succ: g => g,
            Fail: error => throw new InvalidOperationException($"Failed to create grid: {error.Message}")
        );

        var result = SaveReadyValidator.ValidateEntity(grid);

        result.IsSucc.Should().BeTrue("Valid Grid should pass save-ready validation");
    }

    [Fact]
    [Trait("Category", "Infrastructure")]
    [Trait("Category", "Phase3")]
    public void ValidateEntities_WithMultipleValidEntities_ReturnsSuccess()
    {
        var actor = Actor.CreateAtFullHealth(
            ActorId.NewId(GuidIdGenerator.Instance),
            100,
            "Test Actor"
        ).Match(
            Succ: a => a,
            Fail: error => throw new InvalidOperationException($"Failed to create actor: {error.Message}")
        );

        var grid = Grid.Create(GuidIdGenerator.Instance, 3, 3).Match(
            Succ: g => g,
            Fail: error => throw new InvalidOperationException($"Failed to create grid: {error.Message}")
        );

        var entities = new object[] { actor, grid };
        var result = SaveReadyValidator.ValidateEntities(entities);

        result.IsSucc.Should().BeTrue("Valid entities should pass batch validation");
    }

    [Fact]
    [Trait("Category", "Infrastructure")]
    [Trait("Category", "Phase3")]
    public void ValidateEntities_WithEmptyCollection_ReturnsSuccess()
    {
        var entities = Array.Empty<object>();
        var result = SaveReadyValidator.ValidateEntities(entities);

        result.IsSucc.Should().BeTrue("Empty collection should pass validation");
    }

    [Fact]
    [Trait("Category", "Infrastructure")]
    [Trait("Category", "Phase3")]
    public void ValidateEntities_WithNullCollection_ReturnsFailure()
    {
        var result = SaveReadyValidator.ValidateEntities(null!);

        result.IsFail.Should().BeTrue();
        result.Match(
            Succ: _ => throw new InvalidOperationException("Should not succeed"),
            Fail: error => error.Message.Should().Contain("Entities collection cannot be null")
        );
    }

    [Fact]
    [Trait("Category", "Infrastructure")]
    [Trait("Category", "Phase3")]
    public void ValidateType_WithActorType_ReturnsSuccess()
    {
        var result = SaveReadyValidator.ValidateType(typeof(Actor));

        result.IsSucc.Should().BeTrue("Actor type should pass save-ready validation");
    }

    [Fact]
    [Trait("Category", "Infrastructure")]
    [Trait("Category", "Phase3")]
    public void ValidateType_WithGridType_ReturnsSuccess()
    {
        var result = SaveReadyValidator.ValidateType(typeof(Grid));

        result.IsSucc.Should().BeTrue("Grid type should pass save-ready validation");
    }

    [Fact]
    [Trait("Category", "Infrastructure")]
    [Trait("Category", "Phase3")]
    public void ValidateType_WithNullType_ReturnsFailure()
    {
        var result = SaveReadyValidator.ValidateType(null!);

        result.IsFail.Should().BeTrue();
        result.Match(
            Succ: _ => throw new InvalidOperationException("Should not succeed"),
            Fail: error => error.Message.Should().Contain("Entity type cannot be null")
        );
    }

    [Fact]
    [Trait("Category", "Infrastructure")]
    [Trait("Category", "Phase3")]
    public void ValidateType_WithNonPersistentEntity_ReturnsFailure()
    {
        var result = SaveReadyValidator.ValidateType(typeof(NonPersistentTestEntity));

        result.IsFail.Should().BeTrue();
        result.Match(
            Succ: _ => throw new InvalidOperationException("Should not succeed"),
            Fail: error => error.Message.Should().Contain("must implement IPersistentEntity")
        );
    }

    // Test entity that doesn't implement IPersistentEntity (should fail validation)
    private sealed record NonPersistentTestEntity(string Name, int Value);

    // Test entity with mutable properties (should fail validation)
    private sealed class MutableTestEntity : IPersistentEntity
    {
        public IEntityId Id { get; } = new TestEntityId(Guid.NewGuid());
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }

    // Test entity ID implementation
    private sealed record TestEntityId(Guid Value) : IEntityId;

    [Fact]
    [Trait("Category", "Infrastructure")]
    [Trait("Category", "Phase3")]
    public void ValidateType_WithMutableEntity_ReturnsFailure()
    {
        var result = SaveReadyValidator.ValidateType(typeof(MutableTestEntity));

        result.IsFail.Should().BeTrue();
        result.Match(
            Succ: _ => throw new InvalidOperationException("Should not succeed"),
            Fail: error => error.Message.Should().Contain("mutable members")
        );
    }

    [Fact]
    [Trait("Category", "Infrastructure")]
    [Trait("Category", "Phase3")]
    public void ValidateEntity_WithMutableEntityInstance_ReturnsFailure()
    {
        var entity = new MutableTestEntity { Name = "Test", Value = 42 };
        var result = SaveReadyValidator.ValidateEntity(entity);

        result.IsFail.Should().BeTrue();
        result.Match(
            Succ: _ => throw new InvalidOperationException("Should not succeed"),
            Fail: error => error.Message.Should().Contain("mutable members")
        );
    }

    // Test deterministic validation behavior
    [Fact]
    [Trait("Category", "Infrastructure")]
    [Trait("Category", "Phase3")]
    public void ValidateEntity_SameEntityMultipleTimes_ReturnsSameResult()
    {
        var actor = Actor.CreateAtFullHealth(
            ActorId.NewId(GuidIdGenerator.Instance),
            100,
            "Test Actor"
        ).Match(
            Succ: a => a,
            Fail: error => throw new InvalidOperationException($"Failed to create actor: {error.Message}")
        );

        var result1 = SaveReadyValidator.ValidateEntity(actor);
        var result2 = SaveReadyValidator.ValidateEntity(actor);
        var result3 = SaveReadyValidator.ValidateEntity(actor);

        result1.IsSucc.Should().BeTrue();
        result2.IsSucc.Should().BeTrue();
        result3.IsSucc.Should().BeTrue();

        // All results should be consistent
        result1.IsSucc.Should().Be(result2.IsSucc);
        result2.IsSucc.Should().Be(result3.IsSucc);
    }
}
