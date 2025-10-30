# Abstract Class Architecture

This document describes the abstract class-based architecture used in the Sailboat Game project.

## Overview

The project uses **abstract classes** instead of interfaces for key system abstractions. This provides several advantages:
- **MonoBehaviour inheritance**: Abstract classes can inherit from MonoBehaviour, allowing them to be used directly in Unity's serialization system
- **Cleaner inspector workflow**: No casting required - Unity can serialize abstract MonoBehaviour types directly
- **Code reuse**: Abstract classes can contain shared implementation code, not just signatures
- **Unity-native approach**: Works seamlessly with Unity's component system

## Architecture Pattern

### Abstract Base Classes

Four key systems use abstract base classes:

1. **IMapLoader** (`Scripts/Interfaces/IMapLoader.cs`)
   - Abstract base class for map loading systems
   - Allows switching between TextAsset, JSON, binary formats, network loading, etc.
   - Implementation: `MapLoader` (loads from TextAsset files)

2. **IMapGenerator** (`Scripts/Interfaces/IMapGenerator.cs`)
   - Abstract base class for map generation systems
   - Allows switching between different world builders, procedural generation, etc.
   - Implementation: `MapGenerator` (generates hex maps with decorations)

3. **IInputHandler** (`Scripts/Interfaces/IInputHandler.cs`)
   - Abstract base class for input handling systems
   - Allows switching between mouse, touch, gamepad, AI control, etc.
   - Implementation: `InputHandler` (handles both mouse and touch input)

4. **IPathVisualizer** (`Scripts/Interfaces/IPathVisualizer.cs`)
   - Abstract base class for path visualization systems
   - Allows switching between LineRenderer, particles, UI, AR overlays, etc.
   - Implementation: `PathVisualizer` (uses LineRenderer and tile highlighting)

### Key Features

#### 1. MonoBehaviour Inheritance
```csharp
public abstract class IMapLoader : MonoBehaviour
{
    public abstract Awaitable<MapData> LoadMapAsync(object source, CancellationToken cancellationToken = default);
}
```

#### 2. Direct Serialization
```csharp
public class GameManager : MonoBehaviour
{
    [SerializeField] private IMapLoader mapLoader;  // Serializes concrete implementation directly
    // No casting or interface initialization needed!
}
```

#### 3. Override Methods
```csharp
public class MapLoader : IMapLoader
{
    public override async Awaitable<IMapLoader.MapData> LoadMapAsync(object source, CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

#### 4. Abstract Events (InputHandler)
```csharp
public abstract class IInputHandler : MonoBehaviour
{
    public abstract event Action<HexCoordinates> OnTileClicked;
}

public class InputHandler : IInputHandler
{
    public override event Action<HexCoordinates> OnTileClicked;
}
```

## Benefits

### 1. Unity Integration
- **No casting required**: Unity serializes abstract MonoBehaviour types directly
- **Inspector-friendly**: See the actual implementation type in the inspector
- **Component workflow**: Works naturally with `GetComponent<>()` and `AddComponent<>()`

### 2. Flexibility
- **Easy to swap implementations**: Just change the component type
- **Testing**: Create mock implementations for unit testing
- **Runtime switching**: Can use `GetComponent<IMapLoader>()` to find any implementation

### 3. Code Quality
- **SOLID principles**: Dependency Inversion Principle (depend on abstractions)
- **Low coupling**: Systems only know about abstract interfaces
- **High cohesion**: Each implementation focuses on one responsibility
- **Extensibility**: Add new implementations without modifying existing code

### 4. Shared Implementation
- **Common code**: Abstract classes can contain shared implementation methods
- **Default behavior**: Can provide virtual methods with default implementations
- **Nested classes**: Share common data structures (e.g., `MapData`)

## Usage

### Creating New Implementations

To create a new implementation of any abstract system:

1. **Create a new class** that inherits from the abstract base class:
```csharp
public class NetworkMapLoader : IMapLoader
{
    public override async Awaitable<MapData> LoadMapAsync(object source, CancellationToken cancellationToken)
    {
        // Load map from network
    }
}
```

2. **Override all abstract methods/events**:
```csharp
public override async Awaitable<MapData> LoadMapAsync(object source, CancellationToken cancellationToken)
{
    // Your implementation
}
```

3. **Use it in the scene**:
   - Replace the component on the GameObject
   - Or use `SceneSetupHelper` and modify to use your implementation

### Wiring Dependencies

Dependencies are wired in `SceneSetupHelper.cs` using Unity's SerializedObject API:

```csharp
UnityEditor.SerializedObject gmSO = new UnityEditor.SerializedObject(gameManager);
gmSO.FindProperty("mapLoader").objectReferenceValue = mapLoader;
gmSO.ApplyModifiedProperties();
```

This sets the concrete implementation directly, and Unity handles the abstract class serialization automatically.

## Comparison to Previous Interface Approach

### Previous (Interfaces + Concrete Serialization)
```csharp
[SerializeField] private MapLoader mapLoader;  // Concrete type
private IMapLoader mapLoaderInterface;          // Interface reference

void Awake() 
{
    mapLoaderInterface = mapLoader;  // Manual initialization
}
```

### Current (Abstract Classes)
```csharp
[SerializeField] private IMapLoader mapLoader;  // Abstract type (works in Unity!)
// No initialization needed - use directly!
```

The abstract class approach is cleaner, more Unity-native, and removes boilerplate code.

## Implementation Details

### Abstract Methods
All abstract methods are marked with `public abstract` and must be overridden:
- `LoadMapAsync()` in IMapLoader
- `GenerateMapAsync()` and `ClearMap()` in IMapGenerator
- `SetEnabled()` in IInputHandler
- `ShowPath()`, `ClearPath()`, etc. in IPathVisualizer

### Abstract Events
Abstract events (IInputHandler) require special syntax:
```csharp
// Abstract class
public abstract event Action<HexCoordinates> OnTileClicked;

// Implementation
public override event Action<HexCoordinates> OnTileClicked;
```

### Nested Classes
Shared data structures are defined in the abstract class:
```csharp
public abstract class IMapLoader : MonoBehaviour
{
    public class MapData  // Shared by all implementations
    {
        public int Width { get; set; }
        public int Height { get; set; }
        // ...
    }
}
```

## Files

### Abstract Classes
- `Scripts/Interfaces/IMapLoader.cs`
- `Scripts/Interfaces/IMapGenerator.cs`
- `Scripts/Interfaces/IInputHandler.cs`
- `Scripts/Interfaces/IPathVisualizer.cs`

### Implementations
- `Scripts/Systems/MapLoader.cs`
- `Scripts/Systems/MapGenerator.cs`
- `Scripts/Input/InputHandler.cs`
- `Scripts/Visualization/PathVisualizer.cs`

### System Coordination
- `Scripts/GameManager.cs` - Orchestrates all systems
- `Scripts/Editor/SceneSetupHelper.cs` - Wires dependencies

## Related Documentation

- `ARCHITECTURE_UPDATE.md` - Architecture evolution
- `IMPLEMENTATION_SUMMARY.md` - Deep dive into implementation
- `SETUP_RESPONSIBILITY.md` - Setup and dependency injection
- `INTERFACE_ARCHITECTURE.md` - Previous interface-based approach (deprecated)



