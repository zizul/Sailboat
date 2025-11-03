# Sailboat Game - Technical Solution Description

## Overview

This is a hexagonal grid-based sailing game implemented in Unity 6, featuring asynchronous pathfinding, addressable asset management, and mobile-optimized rendering. The solution emphasizes clean architecture, event-driven communication, and performance optimization for cross-platform deployment (Windows, Mac, Mobile).

---

## Architecture & Design Decisions

### 1. Event-Driven Architecture
**Files:** `Scripts/GameManager.cs`, `Scripts/UI/UIManager.cs`, `Scripts/Performance/PerformanceOptimizer.cs`, `Scripts/Systems/MapGenerator.cs`

The system uses a publish-subscribe pattern for loose coupling between components:
- **GameManager** publishes initialization events (`OnInitializationStarted`, `OnInitializationProgress`, `OnInitializationCompleted`, `OnInitializationFailed`, `OnMapGenerationCompleted`)
- **UIManager** subscribes to display loading progress without tight coupling to GameManager
- **PerformanceOptimizer** subscribes to `OnMapGenerationCompleted` to trigger distance culling exactly when map generation finishes
- **MapGenerator** publishes `OnGenerationProgress` events with granular progress updates (20+ updates during tile generation)

**Benefits:**
- Low coupling between systems
- Easy to add new subscribers without modifying publishers
- Clear separation of concerns

### 2. Abstract Class-Based Flexibility
**Files:** `Scripts/Interfaces/IMapLoader.cs`, `Scripts/Interfaces/IMapGenerator.cs`, `Scripts/Interfaces/IInputHandler.cs`, `Scripts/Interfaces/IPathVisualizer.cs`, `Scripts/Interfaces/IAssetLoader.cs`

Key systems extend abstract MonoBehaviour classes rather than interfaces:
- `IMapLoader` → `MapLoader` (text-based maps)
- `IMapGenerator` → `MapGenerator` (hex grid generation)
- `IInputHandler` → `InputHandler` (mouse/touch input)
- `IPathVisualizer` → `PathVisualizer` (LineRenderer-based)
- `IAssetLoader` → `AddressableAssetLoader` (Unity Addressables)

**Design Decision:** Abstract classes chosen over interfaces because:
- Unity can serialize abstract MonoBehaviour references in the Inspector
- Allows shared base functionality while maintaining polymorphism
- Enables easy swapping of implementations (e.g., different map loaders, visualizers)

### 3. Dependency Injection via SerializedFields
**Files:** `Scripts/GameManager.cs`, `Scripts/Editor/SceneSetupHelper.cs`

All dependencies are injected through `[SerializeField]` references:
- GameManager holds references to all system components
- No singleton pattern usage - promotes testability and clear dependency graphs

---

## Hexagonal Grid System

### Coordinate System
**File:** `Scripts/Core/HexCoordinates.cs`

Implements **axial coordinates** (q, r) for "pointy-top" hex orientation:
- Conversion between hex coordinates and world positions
- Distance calculation using cube coordinates
- Neighbor finding for all 6 adjacent hexes

### Grid Management
**File:** `Scripts/Core/HexGrid.cs`

Uses `Dictionary<HexCoordinates, HexTile>` for O(1) tile lookup:
- `GetTile()` - O(1) retrieval
- `GetWalkableNeighbors()` - O(6) for neighbor checks
- `GetTilesInRadius()` - Efficient radius-based queries for culling
- Spatial organization through cube coordinate arithmetic

---

## Pathfinding Implementation

### Algorithm: A* (A-Star)
**File:** `Scripts/Pathfinding/AStarPathfinding.cs`

**Time Complexity:** O(E log V) where E = edges, V = vertices
- Uses custom min-heap priority queue for open set
- Closed set implemented as `HashSet<HexCoordinates>` for O(1) lookup
- Heuristic: Manhattan distance on hex grid (admissible and consistent)

**Space Complexity:** O(V) for storing nodes

**Optimizations:**
- Object pooling for Node instances via `Dictionary<HexCoordinates, Node>`
- Priority queue with binary heap for O(log n) insertions/extractions
- Early termination when goal is reached
- Reuses collections across pathfinding calls to reduce allocations

### Asynchronous Execution
**File:** `Scripts/Pathfinding/PathfindingSystem.cs`

Pathfinding runs on background thread using Unity's `Awaitable`:
```csharp
await Awaitable.BackgroundThreadAsync();  // Move to background
path = currentStrategy.FindPath(start, goal, hexGrid);
await Awaitable.MainThreadAsync();  // Return to main thread
```

**Benefits:**
- No frame drops during pathfinding
- Supports cancellation via `CancellationToken`
- Strategy pattern allows algorithm swapping

---

## Addressables System

### Asset Loading
**File:** `Scripts/Systems/AddressableAssetLoader.cs`

Implements asynchronous asset loading with caching:

**Caching Strategy:**
- `Dictionary<string, AsyncOperationHandle>` stores loaded assets
- Assets loaded once, reused for all subsequent requests
- Automatic reference counting through Addressables API

**Pooling System:**
- `Dictionary<string, Queue<GameObject>>` for prefab instances
- `Dictionary<GameObject, string>` for instance-to-key mapping
- `InstantiateAsync()` checks pool before creating new instances
- `ReturnToPool()` deactivates and queues instances for reuse

### Map Generation
**File:** `Scripts/Systems/MapGenerator.cs`

All assets loaded asynchronously via Addressables.

**Asset Organization:**
- Prefab keys defined as serialized string arrays
- Parallel loading of decoration sets
- Single prefab reference stored per tile (lazy instantiation)

---

## Asynchronous Programming

### Unity Awaitable Pattern
Used throughout for non-blocking operations:

**Map Loading** (`Scripts/Systems/MapLoader.cs`):
```csharp
await Awaitable.NextFrameAsync(cancellationToken);
```

**Map Generation** (`Scripts/Systems/MapGenerator.cs`):
- Batch processing: yield every 200 tiles to maintain framerate
- Progress reporting: 20+ updates during tile generation
- Sub-step timing: individual stopwatches for performance profiling

**Boat Movement** (`Scripts/Boat/BoatController.cs`):
- Path segmentation by direction changes
- Smooth interpolation with `AnimationCurve`
- Rotation and movement in separate async phases
- Particle system activation synchronized with movement state

---

## Performance Optimizations

### 1. Static Batching
**Files:** `Scripts/Core/HexTile.cs`, `Scripts/Systems/MapGenerator.cs`

All static geometry marked with `isStatic = true`:
- Tile visuals
- Decorations (vegetation, rocks, structures)
- Background water plane
- Applied recursively to all children


### 2. Distance Culling
**File:** `Scripts/Performance/PerformanceOptimizer.cs`

Radius-based tile activation/deactivation:
- Culling distance: 50 units (configurable)
- Check interval: 0.5 seconds
- Uses `HexGrid.GetTilesInRadius()` for spatial queries
- Squared distance comparisons to avoid `sqrt()` calls
- Lazy loading: tiles instantiate visuals only when activated

**Algorithm:**
1. Convert camera position to hex coordinates
2. Calculate hex radius from culling distance
3. Query tiles in radius
4. Activate/deactivate based on squared distance check

### 3. Lazy Loading
**File:** `Scripts/Core/HexTile.cs`

Tiles store prefab references, not instances:
- Visuals instantiated on first activation
- Decorations created on demand
- Reduces initial memory footprint
- Integrated with distance culling system

### 4. Object Pooling
**File:** `Scripts/Systems/AddressableAssetLoader.cs`

Generic pooling system for frequently used prefabs:
- Pre-warming support via `PrewarmPoolAsync()`
- Automatic pool management per asset key
- Reduces instantiation overhead
- Used for boat foam effects (planned feature)

### 5. Mobile Optimizations
**File:** `Scripts/Performance/PerformanceOptimizer.cs`

Platform-specific quality settings:
```csharp
#if UNITY_ANDROID || UNITY_IOS
    QualitySettings.shadows = ShadowQuality.Disable;
    QualitySettings.shadowCascades = 0;
    QualitySettings.pixelLightCount = 1;
    QualitySettings.masterTextureLimit = 1;  // Half resolution textures
    QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
    QualitySettings.antiAliasing = 0;
    QualitySettings.vSyncCount = 0;
    Application.targetFrameRate = 60;
#endif
```

### 6. Memory Management
**File:** `Scripts/Performance/PerformanceOptimizer.cs`

Periodic cleanup:
- `Resources.UnloadUnusedAssets()` every 30 seconds
- `System.GC.Collect()` on low-memory mobile devices (<4GB RAM)
- Addressables reference tracking and release

### 7. Batch Processing
**File:** `Scripts/Systems/MapGenerator.cs`

Tile generation in batches:
- Batch size: 200 tiles per frame
- `await Awaitable.NextFrameAsync()` after each batch
- Prevents frame spikes on large maps
- Progress reporting at 5% intervals (~20 updates)

---

## Movement System

### Segment-Based Path Following
**File:** `Scripts/Boat/BoatController.cs`

Path divided into straight segments:
1. Group waypoints by direction (angle threshold: 1°)
2. Rotate to face segment direction
3. Move smoothly along entire segment
4. Repeat for next segment

**Benefits:**
- Smooth continuous movement (no stopping at waypoints)
- Natural rotation transitions
- Particle system activation synchronized with movement phases

### Camera Following
**File:** `Scripts/Camera/CameraFollowController.cs`

Smooth camera tracking:
- Lerp-based position following (configurable speed)
- Fixed offset from target
- Optional look-at rotation
- Boundary constraints (optional)
- Snap-to-target for immediate positioning

---

## Summary

This solution implements a production-ready hexagonal grid sailing game with:
- **Clean Architecture:** Event-driven, dependency-injected, abstract class-based
- **Efficient Pathfinding:** A* with O(E log V) complexity, background thread execution
- **Smart Asset Management:** Addressables with caching and pooling
- **Mobile-First Performance:** Static batching, distance culling, lazy loading, platform-specific settings
- **Modern Unity Patterns:** Async/await throughout, proper cancellation support

