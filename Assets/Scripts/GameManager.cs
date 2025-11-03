using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;
using SailboatGame.Core;
using SailboatGame.Systems;
using SailboatGame.Pathfinding;
using SailboatGame.Input;
using SailboatGame.Boat;
using SailboatGame.Camera;
using SailboatGame.Visualization;
using SailboatGame.Interfaces;
using Debug = UnityEngine.Debug;

namespace SailboatGame
{
    /// <summary>
    /// Main game manager that orchestrates all systems.
    /// Handles initialization, game flow, and system coordination.
    /// Implements event-driven architecture for low coupling.
    /// Uses interfaces for dependency injection and flexibility.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Map Selection")]
        [SerializeField] private TextAsset[] mapAssets;
        [SerializeField] private int initialMapIndex = 0;

        [Header("System References")]
        [SerializeField] private HexGrid hexGrid;
        [SerializeField] private IMapLoader mapLoader;
        [SerializeField] private IMapGenerator mapGenerator;
        [SerializeField] private IAssetLoader assetLoader;
        [SerializeField] private PathfindingSystem pathfindingSystem;
        [SerializeField] private IInputHandler inputHandler;
        [SerializeField] private IPathVisualizer pathVisualizer;

        [Header("Boat Settings")]
        [SerializeField] private string boatPrefabKey = "mauritania_boat";
        [SerializeField] private HexCoordinates boatStartPosition = new HexCoordinates(5, 5);

        [Header("Camera")]
        [SerializeField] private CameraFollowController cameraController;

        [Header("Debug")]
        [SerializeField] private bool debugMode = false;

        private BoatController boatController;
        private MapLoader.MapData currentMapData;
        private CancellationTokenSource gameplayCTS;
        private bool isInitialized;

        private async void Start()
        {
            // Initialize cancellation token
            gameplayCTS = new CancellationTokenSource();

            // Validate required references are set
            if (!ValidateReferences())
            {
                Debug.LogError("GameManager: Missing required system references! Use Tools → Sailboat Game → Setup Scene to create properly configured scene.");
                return;
            }

            // Setup camera controller if not assigned
            if (cameraController == null)
            {
                cameraController = UnityEngine.Camera.main?.GetComponent<CameraFollowController>();
                if (cameraController == null && UnityEngine.Camera.main != null)
                {
                    cameraController = UnityEngine.Camera.main.gameObject.AddComponent<CameraFollowController>();
                }
            }

            // Start game initialization
            await InitializeGameAsync(gameplayCTS.Token);
        }

        /// <summary>
        /// Validates that all required system references are assigned.
        /// </summary>
        private bool ValidateReferences()
        {
            bool valid = true;

            if (hexGrid == null)
            {
                Debug.LogError("GameManager: HexGrid reference is missing!");
                valid = false;
            }
            if (mapLoader == null)
            {
                Debug.LogError("GameManager: MapLoader reference is missing!");
                valid = false;
            }
            if (mapGenerator == null)
            {
                Debug.LogError("GameManager: MapGenerator reference is missing!");
                valid = false;
            }
            if (assetLoader == null)
            {
                Debug.LogError("GameManager: AddressableAssetLoader reference is missing!");
                valid = false;
            }
            if (pathfindingSystem == null)
            {
                Debug.LogError("GameManager: PathfindingSystem reference is missing!");
                valid = false;
            }
            if (inputHandler == null)
            {
                Debug.LogError("GameManager: InputHandler reference is missing!");
                valid = false;
            }
            if (pathVisualizer == null)
            {
                Debug.LogError("GameManager: PathVisualizer reference is missing!");
                valid = false;
            }

            return valid;
        }

        /// <summary>
        /// Initializes the game asynchronously.
        /// </summary>
        private async Awaitable InitializeGameAsync(CancellationToken cancellationToken)
        {
            Stopwatch totalStopwatch = Stopwatch.StartNew();
            Debug.Log("GameManager: Initializing game...");

            // Load map
            Stopwatch loadMapStopwatch = Stopwatch.StartNew();
            if (!await LoadMapAsync(initialMapIndex, cancellationToken))
            {
                Debug.LogError("GameManager: Failed to load initial map");
                return;
            }
            loadMapStopwatch.Stop();
            Debug.Log($"GameManager: LoadMapAsync took {loadMapStopwatch.ElapsedMilliseconds}ms");

            // Generate map
            Stopwatch generateMapStopwatch = Stopwatch.StartNew();
            if (!await mapGenerator.GenerateMapAsync(currentMapData, cancellationToken))
            {
                Debug.LogError("GameManager: Failed to generate map");
                return;
            }
            generateMapStopwatch.Stop();
            Debug.Log($"GameManager: GenerateMapAsync took {generateMapStopwatch.ElapsedMilliseconds}ms");

            // Find valid boat start position
            Stopwatch findStartPosStopwatch = Stopwatch.StartNew();
            HexCoordinates validStartPos = FindValidBoatStartPosition();
            findStartPosStopwatch.Stop();
            Debug.Log($"GameManager: FindValidBoatStartPosition took {findStartPosStopwatch.ElapsedMilliseconds}ms");

            // Spawn boat
            Stopwatch spawnBoatStopwatch = Stopwatch.StartNew();
            await SpawnBoatAsync(validStartPos, cancellationToken);
            spawnBoatStopwatch.Stop();
            Debug.Log($"GameManager: SpawnBoatAsync took {spawnBoatStopwatch.ElapsedMilliseconds}ms");

            // Setup camera
            Stopwatch setupCameraStopwatch = Stopwatch.StartNew();
            if (cameraController != null && boatController != null)
            {
                cameraController.SetTarget(boatController.transform);
                cameraController.SnapToTarget();
            }
            setupCameraStopwatch.Stop();
            Debug.Log($"GameManager: Camera setup took {setupCameraStopwatch.ElapsedMilliseconds}ms");

            // Subscribe to input events
            Stopwatch subscribeEventsStopwatch = Stopwatch.StartNew();
            if (inputHandler != null)
            {
                inputHandler.OnTileClicked += HandleTileClicked;
            }
            subscribeEventsStopwatch.Stop();
            Debug.Log($"GameManager: Event subscription took {subscribeEventsStopwatch.ElapsedMilliseconds}ms");

            isInitialized = true;
            totalStopwatch.Stop();
            Debug.Log($"GameManager: Initialization complete. Total time: {totalStopwatch.ElapsedMilliseconds}ms ({totalStopwatch.Elapsed.TotalSeconds:F2}s)");
        }

        /// <summary>
        /// Loads a map by index.
        /// </summary>
        private async Awaitable<bool> LoadMapAsync(int mapIndex, CancellationToken cancellationToken)
        {
            if (mapAssets == null || mapAssets.Length == 0)
            {
                Debug.LogError("GameManager: No map assets assigned");
                return false;
            }

            mapIndex = Mathf.Clamp(mapIndex, 0, mapAssets.Length - 1);
            TextAsset mapAsset = mapAssets[mapIndex];

            if (mapAsset == null)
            {
                Debug.LogError($"GameManager: Map asset at index {mapIndex} is null");
                return false;
            }

            currentMapData = await mapLoader.LoadMapAsync(mapAsset, cancellationToken) as MapLoader.MapData;
            return currentMapData != null && currentMapData.IsValid();
        }

        /// <summary>
        /// Finds a valid starting position for the boat (water tile).
        /// Places boat in the middle of the map.
        /// </summary>
        private HexCoordinates FindValidBoatStartPosition()
        {
            if (currentMapData == null)
            {
                Debug.LogError("GameManager: No map data available for finding boat position!");
                return new HexCoordinates(0, 0);
            }

            // Calculate center of map in offset coordinates
            int centerX = currentMapData.Width / 2;
            int centerY = currentMapData.Height / 2;

            // Convert center position from offset coordinates to hex coordinates
            int centerQ = centerX - centerY / 2;
            int centerR = centerY;
            HexCoordinates centerCoords = new HexCoordinates(centerQ, centerR);

            // Try the center position first
            if (currentMapData.IsWater(centerX, centerY))
            {
                Debug.Log($"GameManager: Boat placed at map center {centerCoords}");
                return centerCoords;
            }

            // Search outward from center for nearest water tile
            int searchRadius = Mathf.Max(currentMapData.Width, currentMapData.Height);
            for (int radius = 1; radius <= searchRadius; radius++)
            {
                for (int offsetX = -radius; offsetX <= radius; offsetX++)
                {
                    for (int offsetY = -radius; offsetY <= radius; offsetY++)
                    {
                        // Skip if not on the edge of the search ring
                        if (Mathf.Abs(offsetX) != radius && Mathf.Abs(offsetY) != radius)
                            continue;

                        int x = centerX + offsetX;
                        int y = centerY + offsetY;

                        // Check bounds and if it's water
                        if (x >= 0 && x < currentMapData.Width && y >= 0 && y < currentMapData.Height)
                        {
                            if (currentMapData.IsWater(x, y))
                            {
                                // Convert from offset coordinates to hex coordinates
                                int q = x - y / 2;
                                int r = y;
                                HexCoordinates coords = new HexCoordinates(q, r);
                                Debug.Log($"GameManager: Found valid water tile near center at {coords} (offset from center by radius {radius})");
                                return coords;
                            }
                        }
                    }
                }
            }

            // Fallback: search all map data for any water tile
            for (int y = 0; y < currentMapData.Height; y++)
            {
                for (int x = 0; x < currentMapData.Width; x++)
                {
                    if (currentMapData.IsWater(x, y))
                    {
                        // Convert from offset coordinates to hex coordinates
                        int q = x - y / 2;
                        int r = y;
                        HexCoordinates coords = new HexCoordinates(q, r);
                        Debug.LogWarning($"GameManager: Using fallback start position at {coords}");
                        return coords;
                    }
                }
            }

            Debug.LogError("GameManager: No valid water tiles found for boat!");
            return new HexCoordinates(0, 0);
        }

        /// <summary>
        /// Spawns the boat at the specified position.
        /// </summary>
        private async Awaitable SpawnBoatAsync(HexCoordinates position, CancellationToken cancellationToken)
        {
            GameObject boatPrefab = await assetLoader.LoadAssetAsync<GameObject>(boatPrefabKey, cancellationToken);
            
            if (boatPrefab == null)
            {
                Debug.LogError("GameManager: Failed to load boat prefab");
                return;
            }

            GameObject boatObject = Instantiate(boatPrefab);
            boatController = boatObject.GetComponent<BoatController>();
            
            if (boatController == null)
            {
                boatController = boatObject.AddComponent<BoatController>();
            }
            

            boatController.Initialize(position, hexGrid, assetLoader);

            // Register path visualizer to clear path when boat stops
            boatController.OnBoatStopped += pathVisualizer.ClearPath;

            Debug.Log($"GameManager: Boat spawned at {position}");
        }

        /// <summary>
        /// Handles tile click events from input system.
        /// </summary>
        private async void HandleTileClicked(HexCoordinates targetCoords)
        {
            if (!isInitialized || boatController == null || boatController.IsMoving)
            {
                // Allow interrupting current movement
                if (boatController != null && boatController.IsMoving)
                {
                    boatController.CancelMovement();
                    return;
                }
                else
                {
                    return;
                }
            }

            // Check if target is walkable
            if (!hexGrid.IsWalkable(targetCoords))
            {
                Debug.Log($"GameManager: Target {targetCoords} is not walkable (terrain)");
                return;
            }

            Debug.Log($"GameManager: Pathfinding from {boatController.CurrentPosition} to {targetCoords}");

            // Find path
            List<HexCoordinates> path = await pathfindingSystem.FindPathAsync(
                boatController.CurrentPosition,
                targetCoords,
                gameplayCTS.Token
            );

            if (path == null || path.Count == 0)
            {
                Debug.LogWarning("GameManager: No path found");
                pathVisualizer.ClearPath();
                return;
            }

            // Visualize path
            pathVisualizer.ShowPath(path);

            // Move boat along path (line renderer will be cleared automatically when movement finishes)
            await boatController.MoveAlongPathAsync(path, gameplayCTS.Token);
        }

        /// <summary>
        /// Switches to a different map.
        /// </summary>
        public async void SwitchMap(int mapIndex)
        {
            if (mapIndex < 0 || mapIndex >= mapAssets.Length)
            {
                Debug.LogError($"GameManager: Invalid map index {mapIndex}");
                return;
            }

            Debug.Log($"GameManager: Switching to map {mapIndex}");

            // Clear current state
            pathVisualizer.ClearPath();
            if (boatController != null)
            {
                boatController.CancelMovement();
                boatController.OnBoatStopped -= pathVisualizer.ClearPath;
                Destroy(boatController.gameObject);
                boatController = null;
            }

            // Reload map and regenerate
            await LoadMapAsync(mapIndex, gameplayCTS.Token);            
            
            // Respawn boat
            HexCoordinates validStartPos = FindValidBoatStartPosition();
            await SpawnBoatAsync(validStartPos, gameplayCTS.Token);

            // Reset camera
            if (cameraController != null && boatController != null)
            {
                cameraController.SetTarget(boatController.transform);
                cameraController.SnapToTarget();
            }

            await mapGenerator.GenerateMapAsync(currentMapData, gameplayCTS.Token);
        }

        /// <summary>
        /// Public method to load Map 1.
        /// </summary>
        [ContextMenu("Load Map 1")]
        public void LoadMap1()
        {
            SwitchMap(0);
        }

        /// <summary>
        /// Public method to load Maze map.
        /// </summary>
        [ContextMenu("Load Maze Map")]
        public void LoadMazeMap()
        {
            SwitchMap(1);
        }

        private void OnDestroy()
        {
            // Cleanup
            if (inputHandler != null)
            {
                inputHandler.OnTileClicked -= HandleTileClicked;
            }

            if (boatController != null)
            {
                boatController.OnBoatStopped -= pathVisualizer.ClearPath;
            }

            if (gameplayCTS != null)
            {
                gameplayCTS.Cancel();
                gameplayCTS.Dispose();
                gameplayCTS = null;
            }
        }

        private void OnApplicationQuit()
        {
            // Cancel all async operations
            gameplayCTS?.Cancel();
        }
    }
}


