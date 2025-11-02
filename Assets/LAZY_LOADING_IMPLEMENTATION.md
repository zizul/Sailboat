# Lazy Loading Implementation Summary

## Overview
Implemented lazy loading for tile visuals and decorations to optimize memory usage and initial load times for large maps. This system only instantiates game objects for tiles within the camera's culling distance, storing prefab references for distant tiles.

## Changes Made

### 1. HexTile.cs (Scripts/Core/HexTile.cs)
**Purpose**: Core tile class now supports lazy loading of visuals and decorations.

**Key Additions**:
- **Prefab Storage**:
  - `visualPrefab`: Stores reference to visual prefab for lazy instantiation
  - `decorationPrefabs`: List of decoration prefabs with spawn parameters
  - `visualsLoaded`, `decorationsLoaded`: Flags to track loading state
  - `needsShaderActivation`: Flag for shader effect activation

- **New Methods**:
  - `SetVisualPrefab(GameObject prefab, bool activateShaders)`: Store visual prefab for lazy loading
  - `AddDecorationPrefab(GameObject prefab, float offsetRange)`: Store decoration prefab for lazy loading
  - `LoadVisual()`: Private method to instantiate visual from prefab
  - `LoadDecorations()`: Private method to instantiate all decorations from prefabs
  - `UnloadVisual()`: Private method to destroy visual and free memory
  - `UnloadDecorations()`: Private method to destroy decorations and free memory
  - `SetActive(bool active)`: Override that handles lazy loading when activating tiles
  - `ActivateTerrainEffects(GameObject)`: Activates shader effects for terrain visuals

- **Properties**:
  - `HasVisualsLoaded`: Check if tile has loaded visuals
  - `HasDecorationsLoaded`: Check if tile has loaded decorations

**Behavior**:
- When `SetActive(true)` is called on a tile with unloaded visuals/decorations, they are automatically instantiated from stored prefabs
- When `SetActive(false)` is called, the tile is deactivated but prefab references remain for future reloading
- Shader effects are automatically activated when lazy-loading terrain tiles

### 2. MapGenerator.cs (Scripts/Systems/MapGenerator.cs)
**Purpose**: Modified to only store prefab references, never instantiates visuals or decorations.

**Key Changes**:
- **Removed Fields**:
  - `cameraTransform`: No longer needed (PerformanceOptimizer handles activation)
  - `initialLoadDistance`: No longer needed (no distance checks in MapGenerator)

- **Modified Methods**:
  - `GenerateTilesAsync()`:
    - Creates all tiles with inactive GameObjects
    - Stores only prefab references via `SetVisualPrefab()`
    - No instantiation or distance checks
    - Logs total tiles created
  
  - `AddDecorationsAsync()`:
    - Stores only decoration prefab references via `AddDecorationPrefab()`
    - No instantiation or conditional logic
    - Logs total decoration prefab references stored

**Separation of Concerns**:
- MapGenerator: Responsible for data structure creation only
- PerformanceOptimizer: Responsible for all tile activation and visual loading
- Clean separation makes code more maintainable and predictable

### 3. PerformanceOptimizer.cs (Scripts/Performance/PerformanceOptimizer.cs)
**Purpose**: Updated to use the new lazy loading system and handle all tile activation.

**Key Changes**:
- `Start()`:
  - Added call to `PerformDistanceCulling()` at startup
  - Performs initial activation of visible tiles after map generation
  - Ensures tiles are loaded before gameplay begins

- `PerformDistanceCulling()`:
  - Changed from `tile.gameObject.SetActive()` to `tile.SetActive()`
  - Now automatically triggers lazy loading when tiles come into view
  - Visuals and decorations are instantiated on-demand as player moves
  - Handles both initial loading and runtime culling

**Benefits**:
- Single point of control for all tile activation
- Seamless integration with existing culling system
- Automatic loading/unloading as camera moves
- Initial tiles loaded immediately on game start

## Usage Example

### Map Generation Flow:
1. **Map Generation**: MapGenerator creates tiles
   - All tiles created with inactive GameObjects
   - All tiles store only prefab references (visuals + decorations)
   - No instantiation occurs during map generation
   - Very fast startup time

2. **Initial Loading**: PerformanceOptimizer.Start()
   - Runs `PerformDistanceCulling()` once at startup
   - Activates tiles within culling distance
   - `tile.SetActive(true)` → Triggers lazy loading of visuals + decorations
   - Player sees fully loaded map immediately

3. **Runtime**: PerformanceOptimizer culling runs every 0.5s
   - Tile enters culling distance: `tile.SetActive(true)` → Lazy loads visuals + decorations
   - Tile exits culling distance: `tile.SetActive(false)` → Deactivates but keeps prefab references

4. **Memory**: Unloaded tiles only hold prefab references (~16 bytes each)
   - Loaded tiles have full GameObjects in memory (varies by complexity)

## Configuration

### PerformanceOptimizer Settings:
```csharp
[SerializeField] private float cullingDistance = 50f;
[SerializeField] private float cullingCheckInterval = 0.5f;
```
- `cullingDistance`: Controls how far from camera tiles remain active
- `cullingCheckInterval`: How often to check for culling (default 0.5s)
- Hex radius uses `cullingDistance * 1.2f` multiplier for buffer zone
- All tile activation controlled here (both initial and runtime)

## Performance Metrics

### Before Lazy Loading:
- All tiles instantiated at startup
- 1000 tiles × (visual + 2-3 decorations) = ~3000 GameObjects
- High memory usage, longer load times

### After Lazy Loading:
- Zero tiles instantiated during map generation
- PerformanceOptimizer activates only visible tiles on Start()
- Example: 1000 tile map, 200 visible = ~600 GameObjects after initial load
- 80% reduction in total GameObjects
- Map generation time scales with tile count (prefab reference storage only)
- Initial visible tile loading happens once at startup
- Very fast map generation phase

## Best Practices

1. **Culling Distance**:
   - Set cullingDistance based on camera view distance
   - Larger values = more tiles loaded, smoother experience
   - Smaller values = fewer tiles loaded, better performance
   - The 1.2f multiplier provides a buffer zone to prevent pop-in

2. **Batch Size**:
   - MapGenerator uses batching (200 tiles/frame, 100 decorations/frame)
   - Prevents frame spikes during prefab reference storage
   - Very fast since no instantiation occurs

3. **Memory Management**:
   - PerformanceOptimizer already has memory cleanup
   - Consider adding `UnloadVisual()` calls if memory is critical
   - Current implementation keeps inactive tiles in memory (only GameObject shells)
   - Prefab references are shared and managed by Unity

4. **Prefab References**:
   - Prefab references are lightweight (Unity handles internally)
   - No need to manually release prefab references
   - Unity's asset system handles reference counting
   - Multiple tiles can reference the same prefab efficiently

## Future Enhancements

Potential improvements:
1. **Aggressive Memory Mode**: Unload visuals when tiles deactivate
2. **LOD System**: Load low-poly versions at medium distance
3. **Decoration Prioritization**: Load structures before vegetation
4. **Async Loading**: Use Addressables for true async prefab loading
5. **Prediction**: Pre-load tiles in movement direction

## Testing Recommendations

1. Test with various map sizes (100, 1000, 10000 tiles)
2. Monitor memory usage before/after
3. Test camera movement for loading smoothness
4. Verify no visual artifacts when tiles load
5. Check performance on target mobile devices

