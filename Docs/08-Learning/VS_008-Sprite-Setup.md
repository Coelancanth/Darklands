# VS_008 Prototype Sprite Setup Guide

**Purpose**: Quick setup for grid visualization using temporary assets

## 📁 Temporary Asset Setup

### 1. Create Local Folders (NOT in Git)
```
darklands/
├── temp_assets/          # ADD TO .gitignore IMMEDIATELY
│   ├── sprites/         
│   │   ├── hero.png     # Copy from SPD warrior.png
│   │   ├── enemy.png    # Copy from SPD rat.png
│   │   └── tiles.png    # Copy from SPD tiles_sewers.png
│   └── README.txt       # "TEMPORARY - DO NOT COMMIT"
```

### 2. Update .gitignore
```gitignore
# Temporary prototype assets - NEVER COMMIT
temp_assets/
*.tmp.png
*_prototype.*
```

### 3. Copy Needed SPD Assets

From SPD folder (`shattered-pixel-dungeon/core/src/main/assets/`):

**For Hero**: 
- `sprites/warrior.png` (32x32 sprite sheet, 7 columns x 4 rows)
- Contains: idle, run, attack, die animations

**For Basic Enemy**:
- `sprites/rat.png` (16x16 sprite sheet)
- Simple enemy for testing combat

**For Tiles**:
- `environment/tiles_sewers.png` (16x16 tileset)
- Basic floor/wall tiles

## 🎮 Godot Setup

### 1. Create Sprite Scenes

**res://godot_project/temp_proto/hero_proto.tscn**:
```
- CharacterBody2D
  - Sprite2D (using temp_assets/sprites/hero.png)
    - Set texture region: 32x32
    - Animation frames: 7 columns
  - CollisionShape2D

⚠️ Save as .tscn.tmp to mark as temporary
```

**res://godot_project/temp_proto/enemy_proto.tscn**:
```
- CharacterBody2D
  - Sprite2D (using temp_assets/sprites/enemy.png)
    - Set texture region: 16x16
  - CollisionShape2D
```

### 2. Grid Scene Setup

**res://godot_project/temp_proto/grid_proto.tscn**:
```
- Node2D (GridRoot)
  - TileMap
    - Use temp_assets/tiles.png
    - Cell size: 16x16
    - Create simple 10x10 grid
  - Node2D (Entities)
    - Instance hero_proto
    - Instance enemy_proto
```

## 🔄 Replacement Strategy

### Phase 1: Immediate Prototype (Current)
```gdscript
# Quick and dirty for testing
extends CharacterBody2D

var grid_position: Vector2i
var sprite: Sprite2D

func _ready():
    sprite = $Sprite2D
    # Use SPD sprite temporarily
    sprite.texture = load("res://temp_assets/sprites/hero.png")
```

### Phase 2: Before Commit (Required)
```gdscript
# Replace with simple shapes
extends CharacterBody2D

func _ready():
    # Create simple colored square
    var img = Image.create(32, 32, false, Image.FORMAT_RGBA8)
    img.fill(Color.BLUE)  # Hero = blue square
    
    var texture = ImageTexture.create_from_image(img)
    $Sprite2D.texture = texture
```

### Phase 3: Production (Later)
```gdscript
# Use real Darklands assets
extends CharacterBody2D

func _ready():
    $Sprite2D.texture = load("res://assets/sprites/darklands_hero.png")
```

## 📊 SPD Sprite Structure Reference

### Warrior Sprite Layout (32x32 per frame):
```
Row 0: Idle (frames 0-6)
Row 1: Run (frames 7-13)  
Row 2: Attack (frames 14-20)
Row 3: Die (frames 21-27)
```

### Animation Frame Timing:
- Idle: 1.0s cycle through 6 frames
- Run: 0.5s cycle through 6 frames
- Attack: 0.3s play once
- Die: 0.5s play once

## ✅ Validation Checklist

Before proceeding with VS_008:

- [ ] Created temp_assets/ folder
- [ ] Added temp_assets/ to .gitignore
- [ ] Copied minimal SPD sprites needed
- [ ] Created WARNING.txt in temp_assets/
- [ ] Set up Godot scenes with .tmp extension
- [ ] Have replacement plan ready

## 🔴 Critical Reminders

1. **NEVER**: Push temp_assets to Git
2. **NEVER**: Share screenshots with SPD sprites
3. **ALWAYS**: Mark temporary files with .tmp
4. **MUST**: Replace before ANY commit

## 🎯 Goal for VS_008

Use temporary sprites to quickly validate:
1. Grid rendering works
2. Click-to-move functions
3. Turn order visualization
4. Basic animation system

Once validated, immediately replace with simple shapes and commit clean code.

---

**Remember**: These SPD assets are learning tools only. The real value is understanding their structure and animation approach, not using their actual art.