using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Item.Application;
using FluentAssertions;
using Xunit;
using ItemEntity = Darklands.Core.Features.Item.Domain.Item;

namespace Darklands.Core.Tests.Features.Item.Infrastructure;

/// <summary>
/// Contract tests for IItemRepository implementations.
/// These tests define the expected behavior of ANY item repository.
/// </summary>
/// <remarks>
/// WHY CONTRACT TESTS:
/// - TileSetItemRepository lives in Godot project (can't easily test from Core.Tests)
/// - These tests validate the IItemRepository contract using an in-memory test implementation
/// - Ensures TileSetItemRepository will work if it adheres to this contract
///
/// TESTING STRATEGY:
/// - Phase 3: Contract tests (this file) verify repository behavior patterns
/// - Phase 4: Manual testing with real TileSet validates Godot integration
/// </remarks>
[Trait("Category", "Item")]
[Trait("Category", "Unit")]
public class ItemRepositoryContractTests
{
    #region GetById Tests

    [Fact]
    public void GetById_ItemExists_ShouldReturnItem()
    {
        // Arrange
        var repository = CreateTestRepository();
        var item = CreateTestItem("ray_gun", "weapon");
        repository.AddItem(item);

        // Act
        var result = repository.GetById(item.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(item);
    }

    [Fact]
    public void GetById_ItemNotExists_ShouldReturnFailure()
    {
        // BUSINESS RULE: Invalid IDs return failure, not exceptions

        // Arrange
        var repository = CreateTestRepository();
        var nonExistentId = ItemId.NewId();

        // Act
        var result = repository.GetById(nonExistentId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    #endregion

    #region GetAll Tests

    [Fact]
    public void GetAll_WithItemsInCatalog_ShouldReturnAllItems()
    {
        // Arrange
        var repository = CreateTestRepository();
        var item1 = CreateTestItem("ray_gun", "weapon");
        var item2 = CreateTestItem("baton", "weapon");
        var item3 = CreateTestItem("green_vial", "item");

        repository.AddItem(item1);
        repository.AddItem(item2);
        repository.AddItem(item3);

        // Act
        var result = repository.GetAll();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Should().Contain(item1);
        result.Value.Should().Contain(item2);
        result.Value.Should().Contain(item3);
    }

    [Fact]
    public void GetAll_EmptyCatalog_ShouldReturnEmptyList()
    {
        // EDGE CASE: Catalog not loaded or no items defined

        // Arrange
        var repository = CreateTestRepository();

        // Act
        var result = repository.GetAll();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    #endregion

    #region GetByType Tests

    [Fact]
    public void GetByType_WeaponType_ShouldReturnOnlyWeapons()
    {
        // Arrange
        var repository = CreateTestRepository();
        var weapon1 = CreateTestItem("ray_gun", "weapon");
        var weapon2 = CreateTestItem("baton", "weapon");
        var consumable = CreateTestItem("green_vial", "item");

        repository.AddItem(weapon1);
        repository.AddItem(weapon2);
        repository.AddItem(consumable);

        // Act
        var result = repository.GetByType("weapon");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(weapon1);
        result.Value.Should().Contain(weapon2);
        result.Value.Should().NotContain(consumable);
    }

    [Fact]
    public void GetByType_CaseInsensitive_ShouldMatch()
    {
        // WHY: Type filtering should be case-insensitive (UX improvement)

        // Arrange
        var repository = CreateTestRepository();
        var weapon = CreateTestItem("ray_gun", "weapon");
        repository.AddItem(weapon);

        // Act
        var result = repository.GetByType("WEAPON"); // Uppercase query

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.Should().Contain(weapon);
    }

    [Fact]
    public void GetByType_NoMatchingType_ShouldReturnEmptyList()
    {
        // EDGE CASE: Filter for type that doesn't exist

        // Arrange
        var repository = CreateTestRepository();
        var weapon = CreateTestItem("ray_gun", "weapon");
        repository.AddItem(weapon);

        // Act
        var result = repository.GetByType("nonexistent_type");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public void GetByType_EmptyType_ShouldReturnFailure()
    {
        // BUSINESS RULE: Type cannot be empty

        // Arrange
        var repository = CreateTestRepository();

        // Act
        var result = repository.GetByType("");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Type cannot be empty");
    }

    [Fact]
    public void GetByType_WhitespaceType_ShouldReturnFailure()
    {
        // BUSINESS RULE: Whitespace-only type is invalid

        // Arrange
        var repository = CreateTestRepository();

        // Act
        var result = repository.GetByType("   ");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Type cannot be empty");
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void GetById_WithLargeCatalog_ShouldBeEfficient()
    {
        // PERFORMANCE: GetById should be O(1) via dictionary lookup

        // Arrange
        var repository = CreateTestRepository();

        // Add 100 items
        for (int i = 0; i < 100; i++)
        {
            var item = CreateTestItem($"item_{i}", "test_type");
            repository.AddItem(item);
        }

        var targetItem = CreateTestItem("target", "weapon");
        repository.AddItem(targetItem);

        // Act - Should be instant, not O(n) scan
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = repository.GetById(targetItem.Id);
        stopwatch.Stop();

        // Assert
        result.IsSuccess.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10); // O(1) lookup
    }

    #endregion

    #region Test Helpers

    private static ItemEntity CreateTestItem(string name, string type)
    {
        return ItemEntity.Create(
            ItemId.NewId(),
            atlasX: 0,
            atlasY: 0,
            name: name,
            type: type,
            spriteWidth: 2,
            spriteHeight: 2,
            inventoryWidth: 1,
            inventoryHeight: 1,
            maxStackSize: 1).Value;
    }

    private static InMemoryItemRepository CreateTestRepository()
    {
        return new InMemoryItemRepository();
    }

    #endregion

    #region In-Memory Test Implementation

    /// <summary>
    /// Simple in-memory implementation of IItemRepository for contract testing.
    /// Mirrors the behavior expected from TileSetItemRepository.
    /// </summary>
    private sealed class InMemoryItemRepository : IItemRepository
    {
        private readonly Dictionary<ItemId, ItemEntity> _itemsById = new();
        private readonly List<ItemEntity> _allItems = new();

        public void AddItem(ItemEntity item)
        {
            _itemsById[item.Id] = item;
            _allItems.Add(item);
        }

        public Result<ItemEntity> GetById(ItemId itemId)
        {
            if (_itemsById.TryGetValue(itemId, out var item))
            {
                return Result.Success(item);
            }

            return Result.Failure<ItemEntity>($"Item {itemId} not found in catalog");
        }

        public Task<Result<ItemEntity>> GetByIdAsync(ItemId itemId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GetById(itemId));
        }

        public Result<List<ItemEntity>> GetAll()
        {
            return Result.Success(_allItems);
        }

        public Result<List<ItemEntity>> GetByType(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return Result.Failure<List<ItemEntity>>("Type cannot be empty");
            }

            var matchingItems = _allItems
                .Where(item => item.Type.Equals(type, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return Result.Success(matchingItems);
        }
    }

    #endregion
}
