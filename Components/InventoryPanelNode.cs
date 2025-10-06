using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Inventory.Application.Commands;
using Darklands.Core.Features.Inventory.Application.Queries;
using Darklands.Core.Infrastructure.DependencyInjection;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Components;

/// <summary>
/// Godot node that displays inventory UI with test buttons.
/// Demonstrates slot-based inventory MVP for VS_008.
///
/// ARCHITECTURE (ADR-002):
/// - Uses ServiceLocator.Get<T>() in _Ready() (Godot constraint)
/// - No events in MVP: UI queries on-demand after commands
/// - Commands: AddItemCommand, RemoveItemCommand
/// - Query: GetInventoryQuery (returns DTO)
/// </summary>
public partial class InventoryPanelNode : VBoxContainer
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // GODOT EDITOR PROPERTIES (will resolve in _Ready)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private Label? _capacityLabel;
    private GridContainer? _slotsGrid;
    private Button? _addItemButton;
    private Button? _removeItemButton;

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // DEPENDENCIES (resolved via ServiceLocator in _Ready)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private IMediator? _mediator;
    private ILogger<InventoryPanelNode>? _logger;

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // STATE
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private ActorId _actorId;
    private readonly List<ItemId> _addedItems = new();

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // GODOT LIFECYCLE
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public override void _Ready()
    {
        base._Ready();

        _logger?.LogInformation("InventoryPanelNode initializing...");

        // GODOT 4 FIX: Must manually resolve nodes using GetNode()
        _capacityLabel = GetNode<Label>("CapacityLabel");
        _slotsGrid = GetNode<GridContainer>("SlotsGrid");
        _addItemButton = GetNode<Button>("ButtonContainer/AddItemButton");
        _removeItemButton = GetNode<Button>("ButtonContainer/RemoveItemButton");

        // Resolve dependencies via ServiceLocator (Godot constraint)
        var mediatorResult = ServiceLocator.GetService<IMediator>();
        var loggerResult = ServiceLocator.GetService<ILogger<InventoryPanelNode>>();

        if (mediatorResult.IsFailure || loggerResult.IsFailure)
        {
            GD.PrintErr("[InventoryPanelNode] Failed to resolve dependencies");
            return;
        }

        _mediator = mediatorResult.Value;
        _logger = loggerResult.Value;

        // Create test actor (in real game, this would be the player's ActorId)
        _actorId = ActorId.NewId();

        _logger.LogInformation("InventoryPanelNode initialized for actor {ActorId}", _actorId);

        // NOTE: Button signals connected in scene file (inventory_panel.tscn)
        // Do NOT wire up here - causes double press!

        // Initialize UI
        _ = RefreshInventoryDisplay();
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // BUTTON HANDLERS (Send commands, then query for updated state)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private async void OnAddItemButtonPressed()
    {
        if (_mediator == null)
        {
            GD.PrintErr("[InventoryPanelNode] Cannot add item - mediator is null");
            return;
        }

        var itemId = ItemId.NewId();
        _logger?.LogDebug("Adding test item {ItemId}", itemId);

        var command = new AddItemCommand(_actorId, itemId);
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            _logger?.LogWarning("Add item failed: {Error}", result.Error);
            GD.PrintErr($"❌ Add failed: {result.Error}");
        }
        else
        {
            _addedItems.Add(itemId);
            _logger?.LogInformation("Item {ItemId} added successfully", itemId);

            // Refresh UI (no events in MVP, query on-demand)
            await RefreshInventoryDisplay();
        }
    }

    private async void OnRemoveItemButtonPressed()
    {
        if (_mediator == null)
        {
            GD.PrintErr("[InventoryPanelNode] Cannot remove item - mediator is null");
            return;
        }

        // Remove last added item
        if (_addedItems.Count == 0)
        {
            GD.Print("⚠️ No items to remove");
            return;
        }

        var itemToRemove = _addedItems[^1];
        _logger?.LogDebug("Removing item {ItemId}", itemToRemove);

        var command = new RemoveItemCommand(_actorId, itemToRemove);
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            _logger?.LogWarning("Remove item failed: {Error}", result.Error);
            GD.PrintErr($"❌ Remove failed: {result.Error}");
        }
        else
        {
            _addedItems.RemoveAt(_addedItems.Count - 1);
            _logger?.LogInformation("Item {ItemId} removed successfully", itemToRemove);

            // Refresh UI
            await RefreshInventoryDisplay();
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // UI UPDATE (Query-based, no events)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private async Task RefreshInventoryDisplay()
    {
        if (_mediator == null || _capacityLabel == null || _slotsGrid == null || _addItemButton == null)
            return;

        // Query current inventory state
        var query = new GetInventoryQuery(_actorId);
        var result = await _mediator.Send(query);

        if (result.IsFailure)
        {
            _logger?.LogError("Failed to query inventory: {Error}", result.Error);
            return;
        }

        var inventory = result.Value;

        // Update capacity label
        _capacityLabel.Text = $"Inventory: {inventory.Count}/{inventory.Capacity}";

        // Update button states
        _addItemButton.Disabled = inventory.IsFull;
        _addItemButton.Text = inventory.IsFull ? "🚫 Inventory Full" : "➕ Add Test Item";

        // Update slot visuals (simplified: just show count of filled slots)
        // FUTURE: Show actual item sprites when Item definitions exist

        // GODOT FIX: Must properly free old children before creating new ones
        // Clear() only detaches, doesn't dispose memory
        foreach (var child in _slotsGrid.GetChildren())
        {
            child.QueueFree();  // Schedule for disposal on next frame
        }

        for (int i = 0; i < inventory.Capacity; i++)
        {
            var slot = new Panel();
            slot.CustomMinimumSize = new Vector2(40, 40);

            if (i < inventory.Count)
            {
                // Filled slot (show item ID last 4 chars)
                var label = new Label
                {
                    Text = inventory.Items[i].ToString()[^4..],
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                label.AddThemeColorOverride("font_color", Colors.White);
                slot.AddChild(label);

                // Style filled slot
                var styleBox = new StyleBoxFlat
                {
                    BgColor = new Color(0.2f, 0.6f, 0.2f) // Green
                };
                styleBox.BorderWidthLeft = 2;
                styleBox.BorderWidthRight = 2;
                styleBox.BorderWidthTop = 2;
                styleBox.BorderWidthBottom = 2;
                styleBox.BorderColor = Colors.White;
                slot.AddThemeStyleboxOverride("panel", styleBox);
            }
            else
            {
                // Empty slot
                var styleBox = new StyleBoxFlat
                {
                    BgColor = new Color(0.2f, 0.2f, 0.2f) // Dark gray
                };
                styleBox.BorderWidthLeft = 1;
                styleBox.BorderWidthRight = 1;
                styleBox.BorderWidthTop = 1;
                styleBox.BorderWidthBottom = 1;
                styleBox.BorderColor = new Color(0.4f, 0.4f, 0.4f); // Gray border
                slot.AddThemeStyleboxOverride("panel", styleBox);
            }

            _slotsGrid.AddChild(slot);
        }

        _logger?.LogDebug("Inventory display refreshed: {Count}/{Capacity}", inventory.Count, inventory.Capacity);
    }
}
