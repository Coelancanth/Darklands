# Spatial Inventory Test Checklist (VS_018 Phase 1)

**Scene**: `res://test_scenes/SpatialInventoryTestScene.tscn`

**Purpose**: Manual testing for spatial inventory drag-drop UX with 3 containers.

---

## 🎯 Test Setup

1. **Open Scene** in Godot editor: `godot_project/test_scenes/SpatialInventoryTestScene.tscn`
2. **Run Scene** (F6) or from Project → Run Scene
3. **Expected**: 3 inventory containers appear:
   - **Backpack A**: 10×6 grid (60 slots) - General
   - **Backpack B**: 8×8 grid (64 slots) - General
   - **Weapon Slot**: 1×4 grid (4 slots) - WeaponOnly

---

## ✅ Test Cases

### **TC1: Container Initialization**
- [ ] All 3 containers render with grid cells visible
- [ ] Backpack A shows title "Backpack A (0/60)"
- [ ] Backpack B shows title "Backpack B (0/64)"
- [ ] Weapon Slot shows title "Weapon Slot (0/4)"
- [ ] Console logs: "Container nodes attached to scene"

**Expected**: Containers auto-create inventories via repository (capacity mapping: 60→10×6, 64→8×8, 4→1×4)

---

### **TC2: Add Test Items** (Manual via console/script)
Since there's no item palette in Phase 1, you'll need to add items programmatically:

```gdscript
# In Godot debugger console or via test script:
# Add weapon to Backpack A at position (2, 3)
var mediator = ServiceLocator.GetService<IMediator>()
var cmd = PlaceItemAtPositionCommand(backpackAActorId, weaponItemId, GridPosition(2, 3))
mediator.Send(cmd)
```

**Alternative**: Modify `InitializeInventories()` in controller to pre-populate with test items.

- [ ] Item appears at specified grid position
- [ ] Container title updates count: "Backpack A (1/60)"

---

### **TC3: Drag Item Within Same Container** (Repositioning)
- [ ] Click and hold on item in Backpack A
- [ ] Drag preview appears ("📦 Item")
- [ ] Drag to empty cell in same container
- [ ] Drop succeeds
- [ ] Item moves to new position
- [ ] Console logs: "✅ Item moved to Backpack A at (X, Y)"

**Expected**: MoveItemBetweenContainersCommand sent with source = target actor

---

### **TC4: Drag Item Between Containers** (Cross-Container Movement)
- [ ] Click and hold item in Backpack A
- [ ] Drag to empty cell in Backpack B
- [ ] Drop succeeds
- [ ] Item disappears from Backpack A
- [ ] Item appears in Backpack B
- [ ] Backpack A count decrements: "(0/60)"
- [ ] Backpack B count increments: "(1/64)"

**Expected**: Item removed from source, added to target

---

### **TC5: Drag to Occupied Cell** (Collision Detection)
- [ ] Drag item to cell already containing another item
- [ ] `_CanDropData` returns false (no visual feedback yet in Phase 1)
- [ ] Drop fails (item stays in original position)

**Expected**: Collision detection prevents overlapping items

---

### **TC6: Drag to Out-of-Bounds** (Boundary Validation)
- [ ] Drag item outside grid area
- [ ] `_CanDropData` returns false
- [ ] Drop fails

**Expected**: Grid boundary validation prevents invalid placements

---

### **TC7: Type Filtering - Weapon to Weapon Slot** (Accept)
- [ ] Add weapon item to Backpack A
- [ ] Drag weapon to Weapon Slot
- [ ] `_CanDropData` returns true
- [ ] Drop succeeds
- [ ] Weapon appears in Weapon Slot
- [ ] Console logs: "✅ Item moved to Weapon Slot at (0, 0)"

**Expected**: WeaponOnly container accepts weapon items

---

### **TC8: Type Filtering - Potion to Weapon Slot** (Reject)
- [ ] Add potion/consumable item to Backpack A
- [ ] Drag potion to Weapon Slot
- [ ] `_CanDropData` returns true (spatial check passes)
- [ ] Drop executes command
- [ ] **MoveItemBetweenContainersCommandHandler rejects** with error
- [ ] Console logs: "Failed to move item: Target container only accepts weapons"
- [ ] Item remains in source container

**Expected**: Handler enforces type filtering (Domain + Application layer validation)

---

## 🐛 Known Limitations (Phase 1 Scope)

- ❌ **No visual feedback** for valid/invalid drops (green/red highlight) - deferred
- ❌ **No item sprites** rendered in grid cells - Phase 1 shows empty cells only
- ❌ **No item palette** to spawn items - must add via code/console
- ❌ **No tooltip on hover** - basic Godot tooltips only (grid coordinates)
- ✅ **All items treated as 1×1** - multi-cell in Phase 2
- ✅ **Type filtering works** - validated in Application handler

---

## 📊 Success Criteria

**Phase 1 Complete** when all TCs pass and:
1. ✅ Drag-drop works between containers
2. ✅ Type filtering (weapon slot rejects potions)
3. ✅ Collision detection (can't drop on occupied cell)
4. ✅ Backward compatibility (VS_008 tests still pass: 260/260)
5. ✅ Zero Godot dependencies in Core (ADR-002 compliance)

---

## 🔧 Debugging Tips

**If containers don't render**:
- Check console for "Failed to resolve dependencies" error
- Verify `ItemTileSet` assigned in Inspector (res://assets/inventory_ref/item_sprites.tres)
- Check Main.cs initialized GameStrapper

**If drag-drop doesn't work**:
- Verify `_GetDragData`, `_CanDropData`, `_DropData` methods exist in SpatialInventoryContainerNode
- Check console for GUID parsing errors
- Ensure MediatR handlers registered (assembly scan in Main.cs line 111)

**If type filtering doesn't work**:
- Check Item.Type property ("weapon" vs "item")
- Verify ContainerType enum set correctly (weaponSlot should be WeaponOnly)
- Check handler logic in PlaceItemAtPositionCommandHandler.cs line 64

---

**Last Updated**: 2025-10-03 00:50 (Dev Engineer: VS_018 Phase 1 Presentation)
