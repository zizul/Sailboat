using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;
using SailboatGame.Core;
using SailboatGame.Interfaces;
using Debug = UnityEngine.Debug;

namespace SailboatGame.Systems
{
    /// <summary>
    /// Generates the hex map from map data.
    /// Handles tile placement and stores prefab references for lazy loading.
    /// Optimized for mobile with LOD and culling considerations.
    /// Extends IMapGenerator abstract class for flexibility.
    /// All tile activation handled by PerformanceOptimizer.
    /// </summary>
    public class MapGenerator : IMapGenerator
    {
        [Header("References")]
        [SerializeField] private HexGrid hexGrid;
        [SerializeField] private IAssetLoader assetLoader;

        [Header("Map Settings")]
        [SerializeField] private float hexSize = 0.58f;
        [SerializeField] private Vector3 mapOffset = Vector3.zero;

        [Header("Prefab Keys - Background")]
        [SerializeField] private string backgroundWaterKey = "mauritania_background_water";

        [Header("Prefab Keys - Tiles")]
        [SerializeField] private string waterTileKey = "tile_water_02";
        [SerializeField] private string[] terrainTileKeys = new string[]
        {
            "mauritania_tile_01", "mauritania_tile_02", "mauritania_tile_03",
            "mauritania_tile_04", "mauritania_tile_05", "mauritania_tile_06",
            "mauritania_tile_07"
        };

        [Header("Prefab Keys - Decorations")]
        [SerializeField] private string[] vegetationKeys = new string[]
        {
            "mauritania_grass_01", "mauritania_grass_02", "mauritania_plant_01",
            "mauritania_vegetation_set_01", "mauritania_vegetation_set_02"
        };
        [SerializeField] private string[] rockKeys = new string[]
        {
            "mauritania_rock_01", "mauritania_rock_02", "mauritania_rock_03",
            "mauritania_rock_set_01", "mauritania_rock_set_02", "mauritania_rock_set_03"
        };
        [SerializeField] private string[] structureKeys = new string[]
        {
            "mauritania_hut", "mauritania_palm"
        };

        [Header("Decoration Settings")]
        [SerializeField, Range(0f, 1f)] private float vegetationDensity = 0.3f;
        [SerializeField, Range(0f, 1f)] private float rockDensity = 0.2f;
        [SerializeField, Range(0f, 1f)] private float structureDensity = 0.1f;
        [SerializeField, Range(0f, 1f)] private float decorationOffsetRange = 0.3f;

        private GameObject backgroundWater;
        private MapLoader.MapData currentMapData;

        /// <summary>
        /// Generates the complete map asynchronously from map data.
        /// </summary>
        public override async Awaitable<bool> GenerateMapAsync(IMapLoader.MapData mapData, CancellationToken cancellationToken = default)
        {
            Stopwatch totalStopwatch = Stopwatch.StartNew();

            if (mapData == null || !mapData.IsValid())
            {
                Debug.LogError("MapGenerator: Invalid map data");
                return false;
            }

            currentMapData = (MapLoader.MapData) mapData;
            
            // Clear existing map
            Stopwatch clearStopwatch = Stopwatch.StartNew();
            ClearMap();
            clearStopwatch.Stop();
            Debug.Log($"MapGenerator: ClearMap took {clearStopwatch.ElapsedMilliseconds}ms");

            // Initialize grid
            Stopwatch initStopwatch = Stopwatch.StartNew();
            hexGrid.Initialize(mapData.Width, mapData.Height, hexSize);
            initStopwatch.Stop();
            Debug.Log($"MapGenerator: Grid initialization took {initStopwatch.ElapsedMilliseconds}ms");

            // Step 1: Create background water plane
            Stopwatch backgroundStopwatch = Stopwatch.StartNew();
            await CreateBackgroundAsync(mapData, cancellationToken);
            backgroundStopwatch.Stop();
            Debug.Log($"MapGenerator: CreateBackgroundAsync took {backgroundStopwatch.ElapsedMilliseconds}ms");

            // Step 2: Generate tiles with decorations
            Stopwatch tilesStopwatch = Stopwatch.StartNew();
            await GenerateTilesAsync(mapData, cancellationToken);
            tilesStopwatch.Stop();
            Debug.Log($"MapGenerator: GenerateTilesAsync took {tilesStopwatch.ElapsedMilliseconds}ms");

            totalStopwatch.Stop();
            Debug.Log($"MapGenerator: Map generation complete. {hexGrid.TileCount} tiles created. Total time: {totalStopwatch.ElapsedMilliseconds}ms ({totalStopwatch.Elapsed.TotalSeconds:F2}s)");
            return true;
        }

        /// <summary>
        /// Creates the background water plane that covers the entire map.
        /// </summary>
        private async Awaitable CreateBackgroundAsync(IMapLoader.MapData mapData, CancellationToken cancellationToken)
        {
            GameObject backgroundPrefab = await assetLoader.LoadAssetAsync<GameObject>(backgroundWaterKey, cancellationToken);
            
            if (backgroundPrefab != null)
            {
                backgroundWater = Instantiate(backgroundPrefab, transform);
                
                // Calculate actual bounds of the hex grid in world space
                // Using offset coordinates for proper hex positioning
                float minX = float.MaxValue, maxX = float.MinValue;
                float minZ = float.MaxValue, maxZ = float.MinValue;
                
                for (int y = 0; y < mapData.Height; y++)
                {
                    for (int x = 0; x < mapData.Width; x++)
                    {
                        // Convert to offset coordinates (matching tile generation)
                        int q = x - y / 2;
                        int r = y;
                        HexCoordinates coords = new HexCoordinates(q, r);
                        Vector3 worldPos = coords.ToWorldPosition(hexSize);
                        
                        minX = Mathf.Min(minX, worldPos.x);
                        maxX = Mathf.Max(maxX, worldPos.x);
                        minZ = Mathf.Min(minZ, worldPos.z);
                        maxZ = Mathf.Max(maxZ, worldPos.z);
                    }
                }
                
                // Calculate center and size of the map with margins
                Vector3 center = new Vector3((minX + maxX) / 2f, 0, (minZ + maxZ) / 2f);
                
                // Add margins to ensure full coverage
                float hexWidth = hexSize * Mathf.Sqrt(3); // Width of a hex
                float mapWidthWorld = maxX - minX + hexWidth * 1.5f; // Add 1.5 hex widths for margin
                float mapHeightWorld = maxZ - minZ + hexSize * 3f; // Add 3 hex sizes for margin
                
                // Position at center with offset
                backgroundWater.transform.position = center + mapOffset;
                
                // The prefab is a Unity plane (10x10 units) with base scale (16, 6, 1)
                // We need to scale it to match the map dimensions
                // Plane's actual size at scale (1,1,1) = 10 units, with prefab scale (16,6,1) = effective size varies
                // Calculate required scale to cover the map area
                float scaleX = mapWidthWorld;  // Divide by plane's base size (10 units)
                float scaleZ = mapHeightWorld; // Divide by plane's base size (10 units)
                float scale = Mathf.Max(scaleX, scaleZ); // Use the larger scale to ensure full coverage
                
                backgroundWater.transform.localScale = Vector3.one * scale;
                
                // Lower the background slightly to ensure it's below tiles
                Vector3 pos = backgroundWater.transform.position;
                pos.y -= 0.1f;
                backgroundWater.transform.position = pos;

                // Activate shader effects
                ActivateWaterEffects(backgroundWater);
                
                Debug.Log($"MapGenerator: Background water bounds: width={mapWidthWorld:F2}, height={mapHeightWorld:F2}, scale={scale:F2}, center={center}");
            }
            else
            {
                Debug.LogWarning("MapGenerator: Background water prefab not found");
            }
        }

        /// <summary>
        /// Generates all tiles based on map data and adds decorations.
        /// Only stores prefab references, does not instantiate visuals.
        /// Tile activation handled by PerformanceOptimizer.
        /// </summary>
        private async Awaitable GenerateTilesAsync(IMapLoader.MapData mapData, CancellationToken cancellationToken)
        {
            // Pre-load tile prefabs
            Stopwatch loadTilesStopwatch = Stopwatch.StartNew();
            GameObject waterPrefab = await assetLoader.LoadAssetAsync<GameObject>(waterTileKey, cancellationToken);
            List<GameObject> terrainPrefabs = new List<GameObject>();

            foreach (var key in terrainTileKeys)
            {
                var prefab = await assetLoader.LoadAssetAsync<GameObject>(key, cancellationToken);
                if (prefab != null) terrainPrefabs.Add(prefab);
            }
            loadTilesStopwatch.Stop();
            Debug.Log($"MapGenerator: Loading tile prefabs took {loadTilesStopwatch.ElapsedMilliseconds}ms");

            // Pre-load decoration prefabs
            List<GameObject> vegetationPrefabs = await LoadPrefabList(vegetationKeys, cancellationToken, "vegetation");
            List<GameObject> rockPrefabs = await LoadPrefabList(rockKeys, cancellationToken, "rock");
            List<GameObject> structurePrefabs = await LoadPrefabList(structureKeys, cancellationToken, "structure");

            if (waterPrefab == null || terrainPrefabs.Count == 0)
            {
                Debug.LogError("MapGenerator: Failed to load tile prefabs");
                return;
            }

            int tilesCreated = 0;
            int waterTilesCreated = 0;
            int terrainTilesCreated = 0;
            int batchSize = 200; // Process in batches to avoid frame spikes
            int logInterval = 100; // Log every N tiles

            Debug.Log($"MapGenerator: Starting tile generation for {mapData.Width}x{mapData.Height} map ({mapData.Width * mapData.Height} total tiles)");
            Stopwatch tileCreationStopwatch = Stopwatch.StartNew();

            // Convert 2D array to hex coordinates and create tiles
            for (int y = 0; y < mapData.Height; y++)
            {
                for (int x = 0; x < mapData.Width; x++)
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    // Convert to offset coordinates (hex grid)
                    int q = x - y / 2;
                    int r = y;
                    HexCoordinates coords = new HexCoordinates(q, r);

                    // Determine tile type
                    bool isWater = mapData.IsWater(x, y);
                    TileType tileType = isWater ? TileType.Water : TileType.Terrain;

                    // Create tile component
                    GameObject tileObj = new GameObject();
                    HexTile tile = tileObj.AddComponent<HexTile>();
                    tile.Initialize(coords, tileType);

                    // Choose and store prefab reference (no instantiation)
                    GameObject tilePrefab = isWater ? waterPrefab : terrainPrefabs[Random.Range(0, terrainPrefabs.Count)];
                    bool needsShaderActivation = !isWater; // Terrain tiles need shader activation
                    tile.SetVisualPrefab(tilePrefab, needsShaderActivation);

                    // Add decorations to terrain tiles
                    if (!isWater)
                    {
                        // Randomly add vegetation prefab
                        if (vegetationPrefabs.Count > 0 && Random.value < vegetationDensity)
                        {
                            GameObject vegetationPrefab = vegetationPrefabs[Random.Range(0, vegetationPrefabs.Count)];
                            tile.AddDecorationPrefab(vegetationPrefab, decorationOffsetRange);
                        }

                        // Randomly add rocks prefab
                        if (rockPrefabs.Count > 0 && Random.value < rockDensity)
                        {
                            GameObject rockPrefab = rockPrefabs[Random.Range(0, rockPrefabs.Count)];
                            tile.AddDecorationPrefab(rockPrefab, decorationOffsetRange);
                        }

                        // Randomly add structures prefab (huts, palms)
                        if (structurePrefabs.Count > 0 && Random.value < structureDensity)
                        {
                            GameObject structurePrefab = structurePrefabs[Random.Range(0, structurePrefabs.Count)];
                            tile.AddDecorationPrefab(structurePrefab, decorationOffsetRange);
                        }
                    }

                    // Start inactive - PerformanceOptimizer will activate visible tiles
                    tileObj.SetActive(false);

                    // Add to grid
                    hexGrid.AddTile(coords, tile);

                    tilesCreated++;
                    if (isWater)
                        waterTilesCreated++;
                    else
                        terrainTilesCreated++;

                    // Log progress periodically
                    if (tilesCreated % logInterval == 0)
                    {
                        string tileTypeStr = isWater ? "Water" : "Terrain";
                        Debug.Log($"MapGenerator: Processing tile [{x},{y}] Hex({q},{r}) - Type: {tileTypeStr} | Progress: {tilesCreated}/{mapData.Width * mapData.Height} ({(tilesCreated * 100f / (mapData.Width * mapData.Height)):F1}%)");
                    }

                    // Yield every batch to maintain framerate
                    if (tilesCreated % batchSize == 0)
                    {
                        await Awaitable.NextFrameAsync(cancellationToken);
                    }
                }
            }

            tileCreationStopwatch.Stop();
            Debug.Log($"MapGenerator: Tile creation loop took {tileCreationStopwatch.ElapsedMilliseconds}ms");
            Debug.Log($"MapGenerator: Created {tilesCreated} tiles total (Water: {waterTilesCreated}, Terrain: {terrainTilesCreated}) with prefab references. PerformanceOptimizer will handle activation.");
        }

        /// <summary>
        /// Helper to load multiple prefabs asynchronously.
        /// </summary>
        private async Awaitable<List<GameObject>> LoadPrefabList(string[] keys, CancellationToken cancellationToken, string category = "prefabs")
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            List<GameObject> prefabs = new List<GameObject>();
            foreach (var key in keys)
            {
                var prefab = await assetLoader.LoadAssetAsync<GameObject>(key, cancellationToken);
                if (prefab != null) prefabs.Add(prefab);
            }
            stopwatch.Stop();
            Debug.Log($"MapGenerator: LoadPrefabList ({category}) - loaded {prefabs.Count}/{keys.Length} prefabs, took {stopwatch.ElapsedMilliseconds}ms");
            return prefabs;
        }

        /// <summary>
        /// Activates shader effects for water materials.
        /// </summary>
        private void ActivateWaterEffects(GameObject waterObject)
        {
            var renderers = waterObject.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.materials)
                {
                    // Enable shader keywords for water effects
                    if (mat.HasProperty("_EnableWaves"))
                        mat.SetFloat("_EnableWaves", 1f);
                    if (mat.HasProperty("_EnableSeaCreatures"))
                        mat.SetFloat("_EnableSeaCreatures", 1f);
                }
            }
        }

        /// <summary>
        /// Activates shader effects for terrain materials.
        /// </summary>
        private void ActivateTerrainEffects(GameObject terrainObject)
        {
            var renderers = terrainObject.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.materials)
                {
                    // Enable shader keywords for terrain effects
                    if (mat.HasProperty("_EnableTexture"))
                        mat.SetFloat("_EnableTexture", 1f);
                }
            }
        }

        /// <summary>
        /// Clears the current map.
        /// </summary>
        public override void ClearMap()
        {
            if (hexGrid != null)
            {
                hexGrid.Clear();
            }

            if (backgroundWater != null)
            {
                Destroy(backgroundWater);
                backgroundWater = null;
            }
        }

        private void OnDestroy()
        {
            ClearMap();
        }
    }
}


