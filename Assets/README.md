# Sailboat Hex Grid Game

A Unity 6 sailing boat game on a hexagonal map with efficient pathfinding, async programming, and addressable assets.

## Features

### Core Gameplay
- **Hexagonal Grid System**: Pointy-top hex grid with axial coordinates
- **Pathfinding**: A* algorithm with strategy pattern for extensibility
- **Boat Movement**: Smooth sailing with rotation and foam effects
- **Map Loading**: Two pre-defined maps (map1.txt, maze.txt) from TextAssets
- **Input Handling**: Unified PC (mouse) and mobile (touch) input
- **Path Visualization**: LineRenderer-based path display with tile highlighting
- **Camera Follow**: Smooth camera tracking of boat movement

### Technical Implementation
- **Asynchronous Programming**: Extensive use of Unity's Awaitable class
- **Addressables**: All assets loaded asynchronously via Addressables system
- **Object Pooling**: Efficient reuse of frequently spawned objects
- **Performance Optimization**: Mobile-focused with culling, caching, and batching
- **Event-Driven Architecture**: Low coupling, high cohesion design
- **Strategy Pattern**: Swappable pathfinding algorithms

## Project Structure

```
Scripts/
├── Core/
│   ├── HexCoordinates.cs       # Hex coordinate system
│   ├── HexTile.cs              # Individual tile representation
│   └── HexGrid.cs              # Grid management and spatial queries
├── Systems/
│   ├── AddressableAssetLoader.cs  # Asset loading with pooling
│   ├── MapLoader.cs               # Map file parsing
│   └── MapGenerator.cs            # Map generation and decoration
├── Pathfinding/
│   ├── IPathfindingStrategy.cs    # Strategy interface
│   ├── AStarPathfinding.cs        # A* implementation
│   └── PathfindingSystem.cs       # Pathfinding coordinator
├── Input/
│   └── InputHandler.cs            # Unified input handling
├── Boat/
│   └── BoatController.cs          # Boat movement and effects
├── Camera/
│   └── CameraFollowController.cs  # Camera following logic
├── Visualization/
│   └── PathVisualizer.cs          # Path rendering
├── Performance/
│   └── PerformanceOptimizer.cs    # Performance management
└── GameManager.cs                 # Main game orchestrator
```

## Setup Instructions

### Prerequisites
- Unity 6 (2023.2+)
- Addressables package installed
- TextMeshPro (optional, for UI)

### Asset Setup

1. **Configure Addressables**:
   - Mark all prefabs in `Prefabs/` folder as addressable
   - Use the following keys (or update in inspector):
     - `mauritania_boat` - Boat prefab
     - `mauritania_background_water` - Background water plane
     - `tile_water_02` - Water tile
     - `mauritania_tile_01` through `mauritania_tile_07` - Terrain tiles
     - Vegetation: `mauritania_grass_01`, `mauritania_grass_02`, `mauritania_plant_01`, etc.
     - Rocks: `mauritania_rock_01`, `mauritania_rock_02`, `mauritania_rock_03`, etc.
     - Structures: `mauritania_hut`, `mauritania_palm`
     - Effects: `boat_foam_effect` (for foam material/particle system)

2. **Map Files**:
   - Place `map1.txt` and `maze.txt` in `Assets/Maps/` folder
   - Assign them to GameManager in inspector

3. **Scene Setup**:
   - Create new scene or use `Scenes/Main.unity`
   - Add empty GameObject named "GameManager"
   - Attach `GameManager.cs` script
   - Assign map assets in inspector
   - Camera will be configured automatically

### Build Settings

#### Mobile (Android/iOS)
- Graphics API: OpenGL ES 3.0+ / Metal
- Scripting Backend: IL2CPP
- Target Architecture: ARM64
- Texture Compression: ASTC (Android) / PVRTC (iOS)

#### Desktop (Windows/Mac)
- Graphics API: DirectX 11 (Windows) / Metal (Mac)
- Scripting Backend: Mono or IL2CPP
- Target Architecture: x64

## How It Works

### Map Format
Maps are text files where each character represents a tile:
- `0` = Water (walkable)
- `1` = Terrain (non-walkable)

Example:
```
000000
011110
011110
000000
```

### Hex Coordinate System
Uses axial coordinates (Q, R) with cube coordinate math for efficient pathfinding:
- Q: Column offset
- R: Row
- S: -Q-R (derived)

Distance formula: `(|q1-q2| + |r1-r2| + |s1-s2|) / 2`

### Pathfinding Algorithm
A* implementation optimized for hex grids:
1. Priority queue for open set
2. Manhattan distance heuristic (admissible for hex grids)
3. Neighbor lookup via hex math (6 directions)
4. Path reconstruction via parent pointers

### Async Operations
All heavy operations use `Awaitable`:
- Map generation: Batched tile creation
- Asset loading: Parallel addressable loads
- Pathfinding: Background thread execution
- Boat movement: Smooth interpolation with cancellation support

## Controls

### PC
- **Left Click**: Select destination for boat

### Mobile
- **Tap**: Select destination for boat

### Features
- New destination interrupts current path
- Cannot sail on terrain tiles
- Visual feedback with path highlighting

## Performance

### Optimizations Applied
1. **CPU**: Object pooling, spatial hashing, async pathfinding
2. **GPU**: Material instancing, static batching, distance culling
3. **Memory**: Addressables, periodic cleanup, struct usage
4. **Mobile**: Quality settings, texture compression, minimal effects

See `PERFORMANCE_NOTES.md` for detailed analysis.

### Performance Targets
- **Desktop**: 60+ FPS at 1080p
- **Mobile High-end**: 60 FPS at 720p  
- **Mobile Low-end**: 30 FPS at 540p
- **Memory**: <500MB mobile, <1GB desktop

## Architecture

### Design Patterns
- **Strategy Pattern**: Pathfinding algorithms
- **Observer Pattern**: Event-based system communication
- **Object Pool Pattern**: Asset reuse
- **Singleton Pattern**: GameManager coordination

### Principles
- **Low Coupling**: Systems independent, communicate via events
- **High Cohesion**: Each class has single responsibility
- **Dependency Injection**: References passed via constructors/Initialize methods
- **Open/Closed**: Extensible without modifying existing code

### Event Flow
```
Input → InputHandler → OnTileClicked Event
  ↓
GameManager receives event
  ↓
PathfindingSystem.FindPathAsync()
  ↓
PathVisualizer.ShowPath()
  ↓
BoatController.MoveAlongPathAsync()
  ↓
CameraFollowController follows boat
```

## Extending the System

### Adding New Pathfinding Algorithm
1. Implement `IPathfindingStrategy` interface
2. Create new class (e.g., `DijkstraPathfinding`)
3. Set via `PathfindingSystem.SetStrategy(new DijkstraPathfinding())`

### Adding New Map
1. Create text file with 0s (water) and 1s (terrain)
2. Add to project and assign to GameManager
3. Call `GameManager.SwitchMap(index)`

### Adding New Boat Behavior
1. Extend `BoatController` or create new controller
2. Subscribe to movement events
3. Implement custom movement logic in async method

## Troubleshooting

### Assets Not Loading
- Verify addressable keys match script references
- Build addressables: `Window → Asset Management → Addressables → Build → New Build → Default Build Script`

### Performance Issues
- Reduce decoration density in MapGenerator inspector
- Enable distance culling in PerformanceOptimizer
- Lower quality settings for target platform

### Pathfinding Fails
- Ensure start and destination are water tiles
- Check HexGrid is properly initialized
- Verify map data parsed correctly

## Credits

### Assets Used
- Mauritania asset pack (meshes, textures, materials)
- Custom shaders (FTWater, BPSeaCreatures, etc.)

### Technologies
- Unity 6
- Addressables
- Awaitable (Unity async/await)

## License

This project is created for evaluation purposes.

---

For detailed performance analysis, see `PERFORMANCE_NOTES.md`.
For architecture decisions, see inline code documentation.


