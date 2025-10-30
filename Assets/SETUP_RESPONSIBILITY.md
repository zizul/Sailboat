# Setup Responsibility - Clear Separation of Concerns

## Overview

The architecture now has a **clear separation between setup and runtime logic**:

- **SceneSetupHelper** (Editor): Responsible for creating and wiring all systems
- **GameManager** (Runtime): Responsible for game logic only, validates references

---

## Design Philosophy

### Single Responsibility Principle

**SceneSetupHelper**
- ✅ Creates GameObjects
- ✅ Adds components
- ✅ Wires dependencies
- ✅ Configures initial state
- ❌ No runtime logic

**GameManager**
- ✅ Validates references
- ✅ Orchestrates game flow
- ✅ Manages game state
- ❌ No auto-creation
- ❌ No auto-wiring

### Why This Is Better

**Before (Mixed Responsibilities):**
```
GameManager:
- Create systems if missing ❌
- Wire dependencies ❌
- Validate setup ✅
- Run game logic ✅
```

**After (Clear Separation):**
```
SceneSetupHelper (Editor):
- Create systems ✅
- Wire dependencies ✅

GameManager (Runtime):
- Validate setup ✅
- Run game logic ✅
```

---

## Implementation Details

### SceneSetupHelper (Editor-Only)

**Location:** `Scripts/Editor/SceneSetupHelper.cs`

**Responsibilities:**

1. **Create System GameObjects**
```csharp
GameObject hexGridObj = new GameObject("HexGrid");
hexGridObj.transform.SetParent(systemsContainer.transform);
var hexGrid = hexGridObj.AddComponent<Core.HexGrid>();
```

2. **Wire Dependencies**
```csharp
var mapGenSO = new UnityEditor.SerializedObject(mapGenerator);
mapGenSO.FindProperty("hexGrid").objectReferenceValue = hexGrid;
mapGenSO.FindProperty("assetLoader").objectReferenceValue = assetLoader;
mapGenSO.ApplyModifiedProperties();
```

3. **Configure GameManager**
```csharp
var gmSO = new UnityEditor.SerializedObject(gameManager);
gmSO.FindProperty("hexGrid").objectReferenceValue = hexGrid;
gmSO.FindProperty("mapLoader").objectReferenceValue = mapLoader;
// ... all references
gmSO.ApplyModifiedProperties();
```

### GameManager (Runtime)

**Location:** `Scripts/GameManager.cs`

**Responsibilities:**

1. **Validate References**
```csharp
private bool ValidateReferences()
{
    bool valid = true;
    
    if (hexGrid == null)
    {
        Debug.LogError("GameManager: HexGrid reference is missing!");
        valid = false;
    }
    // ... check all references
    
    return valid;
}
```

2. **Fail Fast on Missing References**
```csharp
private async void Start()
{
    if (!ValidateReferences())
    {
        Debug.LogError("Missing references! Use Tools → Sailboat Game → Setup Scene");
        return; // Stop initialization
    }
    
    // Continue with game logic...
}
```

3. **Focus on Game Logic**
```csharp
// Load map, spawn boat, handle input, etc.
// No creation or wiring logic here
```

---

## Benefits

### 1. Cleaner Code
- GameManager has less code
- No editor-only `#if UNITY_EDITOR` blocks in GameManager
- Clear what happens at edit-time vs runtime

### 2. Better Error Messages
```
Before: Silently creates missing systems (may hide issues)
After:  Clear error: "HexGrid reference is missing! Use Tools → Setup Scene"
```

### 3. Easier Debugging
- If setup is wrong, you know it immediately
- No "magic" creation at runtime
- Explicit dependencies visible in inspector

### 4. Enforces Proper Workflow
```
Correct Workflow:
1. Tools → Sailboat Game → Setup Scene
2. Assign map assets
3. Play

Wrong Workflow:
1. Just play
2. Get clear error message
3. Follow error instructions
```

### 5. Editor Performance
- No runtime reflection for dependency wiring
- SerializedObject only used in editor tool
- Faster startup in play mode

---

## Usage

### Automatic Setup (Recommended)

**Step 1:** Use Editor Tool
```
Unity Menu → Tools → Sailboat Game → Setup Scene → "Create Complete Scene"
```

**Result:**
- All systems created ✅
- All dependencies wired ✅
- Ready to play ✅

### Manual Setup (Advanced)

**Step 1:** Create GameObjects
```
GameManager
└── Systems
    ├── HexGrid
    ├── MapLoader
    ├── AssetLoader
    ├── MapGenerator
    ├── PathfindingSystem
    ├── InputHandler
    ├── PathVisualizer
    └── PerformanceOptimizer
```

**Step 2:** Add Components
- Attach corresponding script to each GameObject

**Step 3:** Wire Dependencies (Inspector)
- MapGenerator: Drag HexGrid & AssetLoader
- PathfindingSystem: Drag HexGrid
- InputHandler: Drag HexGrid
- PathVisualizer: Drag HexGrid
- PerformanceOptimizer: Drag HexGrid
- GameManager: Drag all system references

**Step 4:** Assign Map Assets
- GameManager → Map Assets → Drag map1.txt and maze.txt

---

## Error Handling

### Missing Reference Error

**Error Message:**
```
GameManager: HexGrid reference is missing!
GameManager: Missing required system references! 
Use Tools → Sailboat Game → Setup Scene to create properly configured scene.
```

**Solution:**
Run the editor tool to create a properly configured scene.

### Invalid Dependency

**Error Message:**
```
MapGenerator: HexGrid is null!
(Runtime error during generation)
```

**Solution:**
- Check MapGenerator inspector
- Ensure HexGrid reference is assigned
- Or run editor tool again

---

## Comparison

### Old Approach (Auto-Creation)

**Pros:**
- ✅ Works without setup tool
- ✅ Forgiving of missing references

**Cons:**
- ❌ Hides configuration issues
- ❌ Runtime overhead
- ❌ Mixed responsibilities
- ❌ Editor code in runtime class
- ❌ Hard to debug

### New Approach (Explicit Setup)

**Pros:**
- ✅ Clear separation of concerns
- ✅ Fail fast with clear errors
- ✅ No runtime overhead
- ✅ Editor code only in editor
- ✅ Easy to debug
- ✅ Enforces proper workflow

**Cons:**
- ❌ Requires editor tool (or manual setup)
- ❌ Less "magic"

**Verdict:** New approach is more professional and maintainable.

---

## Testing

### Verify Proper Setup

**Test 1: Complete Setup**
```
1. Run editor tool
2. Press Play
3. ✅ Game initializes correctly
```

**Test 2: Missing Reference**
```
1. Run editor tool
2. Clear one reference in GameManager inspector
3. Press Play
4. ✅ Error logged with clear message
5. ✅ Game doesn't start (fail fast)
```

**Test 3: Missing Dependency**
```
1. Run editor tool
2. Clear MapGenerator's HexGrid reference
3. Press Play
4. ✅ GameManager validates successfully
5. ❌ MapGenerator fails at runtime (expected)
6. ✅ Clear error about missing dependency
```

---

## Best Practices

### For Developers

1. **Always Use Editor Tool**
   - Don't manually create scene structure
   - Let tool handle wiring

2. **Verify Inspector**
   - Check all references are assigned
   - Look for "None (Missing)" fields

3. **Read Error Messages**
   - GameManager provides clear instructions
   - Follow the suggested solution

4. **Don't Modify Setup Code**
   - SceneSetupHelper is the source of truth
   - Changes should go there, not GameManager

### For Team Leads

1. **Enforce Workflow**
   - Team must use editor tool
   - Add to onboarding documentation

2. **Scene Templates**
   - Save properly configured scene as template
   - Share with team

3. **Version Control**
   - Commit scene with all references set
   - Team can use as base

---

## Migration from Old Code

### If You Have Old Scenes

**Option 1: Recreate (Fastest)**
```
1. Delete old GameManager
2. Run Tools → Sailboat Game → Setup Scene
3. Assign map assets
```

**Option 2: Update Existing**
```
1. Keep existing GameManager
2. Create child GameObjects for each system
3. Move components to separate GameObjects
4. Manually wire dependencies in inspector
```

### What Changed

**Removed from GameManager:**
- ❌ `ValidateComponents()` method
- ❌ `WireUpDependencies()` method
- ❌ Auto-creation logic
- ❌ Editor-only wiring code

**Added to GameManager:**
- ✅ `ValidateReferences()` method (simple null checks)
- ✅ Clear error messages
- ✅ Fail-fast behavior

**No Changes to:**
- ✅ All game logic methods
- ✅ Public API
- ✅ Event handling
- ✅ Map loading
- ✅ Boat spawning

---

## Summary

### Key Takeaways

1. **SceneSetupHelper creates and wires everything** (editor-time)
2. **GameManager validates and runs the game** (runtime)
3. **Clear separation of concerns** (better architecture)
4. **Fail fast with helpful errors** (better debugging)
5. **Use the editor tool** (proper workflow)

### The Golden Rule

> "Setup is SceneSetupHelper's job. Runtime is GameManager's job."

---

**Date:** October 28, 2025
**Architecture:** Clean Separation of Concerns
**Status:** Production-Ready ✅


