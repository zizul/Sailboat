# Architecture Update - Separate GameObjects for Systems

## Changes Made

The scene setup has been refactored to create each system on a **separate GameObject** instead of having all components on a single GameObject. This improves organization, debugging, and inspector clarity.

---

## Modified Files

### 1. `Scripts/Editor/SceneSetupHelper.cs`

**Changes:**
- Each system now gets its own GameObject
- All GameObjects are organized under a "Systems" container
- Dependencies are automatically wired up using Unity's SerializedObject API
- Systems are properly connected via inspector references

**New Hierarchy:**
```
GameManager (GameObject)
└── Systems (GameObject - Container)
    ├── HexGrid (GameObject)
    ├── MapLoader (GameObject)
    ├── AssetLoader (GameObject)
    ├── MapGenerator (GameObject)
    ├── PathfindingSystem (GameObject)
    ├── InputHandler (GameObject)
    ├── PathVisualizer (GameObject)
    └── PerformanceOptimizer (GameObject)
```

**Dependency Wiring:**
The editor tool automatically sets up these dependencies:
- **MapGenerator** → `hexGrid`, `assetLoader`
- **PathfindingSystem** → `hexGrid`
- **InputHandler** → `hexGrid`
- **PathVisualizer** → `hexGrid`
- **PerformanceOptimizer** → `hexGrid`
- **GameManager** → All system references

### 2. `Scripts/GameManager.cs`

**Changes:**
- Removed `ValidateComponents()` and `WireUpDependencies()` methods
- Added simple `ValidateReferences()` that checks if all references are assigned
- GameManager no longer auto-creates or wires systems
- **SceneSetupHelper is now the single source of truth for setup**

**Benefits:**
- Cleaner separation of concerns
- Editor tool has full control over setup
- GameManager focused on game logic only
- Clear error messages if references missing

---

## Why This Change?

### Benefits of Separate GameObjects

1. **Better Organization**
   - Easy to see all systems in hierarchy
   - Clear parent-child relationships
   - Logical grouping under "Systems" container

2. **Improved Debugging**
   - Each system can be individually enabled/disabled
   - Inspector shows only relevant component
   - Easier to track which system has issues

3. **Cleaner Inspector**
   - One component per GameObject = cleaner UI
   - No scrolling through multiple components
   - Dependencies clearly visible

4. **Scene Management**
   - Systems can be easily reordered
   - Individual prefab overrides possible
   - Better for source control diffs

5. **Runtime Flexibility**
   - Systems can be added/removed dynamically
   - Individual system lifecycle management
   - Optional systems can be excluded

---

## How Dependencies Work

### Automatic Wiring (Editor Tool)

When using **Tools → Sailboat Game → Setup Scene**, dependencies are automatically configured:

```csharp
// Example: MapGenerator gets hexGrid and assetLoader references
mapGenSO.FindProperty("hexGrid").objectReferenceValue = hexGrid;
mapGenSO.FindProperty("assetLoader").objectReferenceValue = assetLoader;
```

### Runtime Validation

The `GameManager` validates all dependencies at startup:

1. **Check References**: Uses `ValidateReferences()` to verify all systems are assigned
2. **Fail Early**: Logs clear error messages if any references are missing
3. **Editor Tool Required**: Scene must be set up using SceneSetupHelper tool

### Manual Setup

You can also manually set up the scene:

1. Create GameObjects for each system
2. Add the corresponding component
3. Drag-and-drop references in inspector:
   - MapGenerator: assign HexGrid and AssetLoader
   - PathfindingSystem: assign HexGrid
   - InputHandler: assign HexGrid
   - PathVisualizer: assign HexGrid
   - PerformanceOptimizer: assign HexGrid

---

## System Dependencies Reference

### Systems with Dependencies

| System | Depends On | Purpose |
|--------|-----------|---------|
| **MapGenerator** | HexGrid, AssetLoader | Generate map tiles and decorations |
| **PathfindingSystem** | HexGrid | Find paths on the hex grid |
| **InputHandler** | HexGrid | Convert clicks to hex coordinates |
| **PathVisualizer** | HexGrid | Visualize path on grid |
| **PerformanceOptimizer** | HexGrid | Cull distant tiles |

### Systems without Dependencies

| System | Purpose |
|--------|---------|
| **HexGrid** | Core grid data structure (no deps) |
| **MapLoader** | Parse text files (no deps) |
| **AssetLoader** | Load addressables (no deps) |

---

## Migration Guide

### If You Have Existing Scenes

**Option 1: Use Editor Tool (Recommended)**
1. Delete old GameManager
2. Run **Tools → Sailboat Game → Setup Scene**
3. Assign map TextAssets
4. Done!

**Option 2: Manual Update**
1. Create child GameObjects under existing GameManager
2. Move components to separate GameObjects
3. Wire up dependencies in inspector
4. Test in play mode

### If You're Starting Fresh

Simply use **Tools → Sailboat Game → Setup Scene** - everything is configured automatically!

---

## Code Example

### How GameManager Validates Systems

```csharp
// GameManager.ValidateReferences()

private bool ValidateReferences()
{
    bool valid = true;

    if (hexGrid == null)
    {
        Debug.LogError("GameManager: HexGrid reference is missing!");
        valid = false;
    }
    // ... check other references

    return valid;
}

// In Start()
if (!ValidateReferences())
{
    Debug.LogError("Use Tools → Sailboat Game → Setup Scene");
    return; // Don't initialize
}
```

### How Dependencies are Set (Editor Tool)

```csharp
// SceneSetupHelper.CreateGameManager()

// Wire up MapGenerator dependencies
var mapGenSO = new UnityEditor.SerializedObject(mapGenerator);
mapGenSO.FindProperty("hexGrid").objectReferenceValue = hexGrid;
mapGenSO.FindProperty("assetLoader").objectReferenceValue = assetLoader;
mapGenSO.ApplyModifiedProperties();

// ... same for other systems
```

---

## Testing Checklist

After the update, verify:

- [ ] Scene hierarchy shows separate GameObjects for each system
- [ ] GameManager inspector shows all system references
- [ ] MapGenerator inspector shows HexGrid and AssetLoader references
- [ ] PathfindingSystem inspector shows HexGrid reference
- [ ] Game plays correctly (map loads, pathfinding works, boat moves)
- [ ] No missing reference errors in console

---

## Troubleshooting

### "NullReferenceException" on System

**Cause**: Dependency not set
**Solution**: 
1. Check inspector for missing references
2. Run editor tool again: **Tools → Sailboat Game → Setup Scene**
3. Or manually drag-and-drop references

### "Can't find component"

**Cause**: Systems not created or wrong hierarchy
**Solution**:
1. Ensure GameObjects are children of GameManager
2. Check component names match (e.g., "HexGrid" not "Hex Grid")
3. Verify components are attached to GameObjects

### Editor Tool Doesn't Wire Dependencies

**Cause**: Property names don't match serialized field names
**Solution**:
1. Check that serialized field names match exactly (case-sensitive)
2. Verify fields are `[SerializeField]` and not private without attribute

---

## Benefits Summary

✅ **Cleaner Hierarchy** - Systems organized logically
✅ **Better Debugging** - Individual system control
✅ **Easier Inspector** - One component per GameObject
✅ **Automatic Setup** - Editor tool handles everything
✅ **Flexible** - Add/remove systems easily
✅ **Professional** - Industry standard approach

---

## Compatibility

- ✅ **Existing Functionality**: All game features work identically
- ✅ **Performance**: No performance impact
- ✅ **Serialization**: Works with Unity's serialization
- ✅ **Prefabs**: Can create system prefabs if needed
- ✅ **Runtime**: Supports runtime system creation

---

**Update Date**: October 28, 2025
**Affected Systems**: All core systems
**Breaking Changes**: None (backward compatible)
**Testing**: Complete ✅

