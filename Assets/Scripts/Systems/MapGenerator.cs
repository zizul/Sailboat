using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using SailboatGame.Core;
using SailboatGame.Interfaces;

namespace SailboatGame.Systems
{
    /// <summary>
    /// Generates the hex map from map data.
    /// Handles tile placement, decoration spawning, and visual setup.
    /// Optimized for mobile with LOD and culling considerations.
    /// Extends IMapGenerator abstract class for flexibility.
    /// </summary>
    public class MapGenerator : IMapGenerator
    {
        [Header("References")]
        [SerializeField] private HexGrid hexGrid;
        [SerializeField] private IAssetLoader assetLoader;

        [Header("Map Settings")]
        [SerializeField] private float hexSize = 1f;
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

        private GameObject backgroundWater;
        private MapLoader.MapData currentMapData;

        /// <summary>
        /// Generates the complete map asynchronously from map data.
        /// </summary>
        public override async Awaitable<bool> GenerateMapAsync(IMapLoader.MapData mapData, CancellationToken cancellationToken = default)
        {
            if (mapData == null || !mapData.IsValid())
            {
                Debug.LogError("MapGenerator: Invalid map data");
                return false;
            }

            currentMapData = (MapLoader.MapData) mapData;
            
            // Clear existing map
            ClearMap();

            // Initialize grid
            hexGrid.Initialize(mapData.Width, mapData.Height, hexSize);

            // Step 1: Create background water plane
            await CreateBackgroundAsync(mapData, cancellationToken);

            // Step 2: Generate tiles
            await GenerateTilesAsync(mapData, cancellationToken);

            // Step 3: Add decorations
            await AddDecorationsAsync(mapData, cancellationToken);

            Debug.Log($"MapGenerator: Map generation complete. {hexGrid.TileCount} tiles created.");
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
                
                // Calculate map center
                Vector3 center = new HexCoordinates(mapData.Width / 2, mapData.Height / 2).ToWorldPosition(hexSize);
                backgroundWater.transform.position = center + mapOffset;
                
                // Scale to cover entire map plus margin
                float mapWidthWorld = mapData.Width * hexSize * 1.73f; // Approximate hex width
                float mapHeightWorld = mapData.Height * hexSize * 1.5f;
                float scale = Mathf.Max(mapWidthWorld, mapHeightWorld) / 10f; // Adjust divisor based on prefab size
                backgroundWater.transform.localScale = Vector3.one * scale;

                // Activate shader effects
                ActivateWaterEffects(backgroundWater);
            }
            else
            {
                Debug.LogWarning("MapGenerator: Background water prefab not found");
            }
        }

        /// <summary>
        /// Generates all tiles based on map data.
        /// </summary>
        private async Awaitable GenerateTilesAsync(IMapLoader.MapData mapData, CancellationToken cancellationToken)
        {
            // Pre-load tile prefabs
            GameObject waterPrefab = await assetLoader.LoadAssetAsync<GameObject>(waterTileKey, cancellationToken);
            List<GameObject> terrainPrefabs = new List<GameObject>();

            foreach (var key in terrainTileKeys)
            {
                var prefab = await assetLoader.LoadAssetAsync<GameObject>(key, cancellationToken);
                if (prefab != null) terrainPrefabs.Add(prefab);
            }

            if (waterPrefab == null || terrainPrefabs.Count == 0)
            {
                Debug.LogError("MapGenerator: Failed to load tile prefabs");
                return;
            }

            int tilesCreated = 0;
            int batchSize = 50; // Process in batches to avoid frame spikes

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

                    // Create tile
                    GameObject tilePrefab = isWater ? waterPrefab : terrainPrefabs[Random.Range(0, terrainPrefabs.Count)];
                    GameObject tileVisual = Instantiate(tilePrefab);

                    // Create tile component
                    GameObject tileObj = new GameObject();
                    HexTile tile = tileObj.AddComponent<HexTile>();
                    tile.Initialize(coords, tileType);
                    tile.SetVisual(tileVisual);

                    // Add to grid
                    hexGrid.AddTile(coords, tile);

                    // Activate shader effects for terrain
                    if (!isWater)
                    {
                        ActivateTerrainEffects(tileVisual);
                    }

                    tilesCreated++;

                    // Yield every batch to maintain framerate
                    if (tilesCreated % batchSize == 0)
                    {
                        await Awaitable.NextFrameAsync(cancellationToken);
                    }
                }
            }
        }

        /// <summary>
        /// Adds decorations (vegetation, rocks, structures) to terrain tiles.
        /// </summary>
        private async Awaitable AddDecorationsAsync(IMapLoader.MapData mapData, CancellationToken cancellationToken)
        {
            // Pre-load decoration prefabs
            List<GameObject> vegetationPrefabs = await LoadPrefabList(vegetationKeys, cancellationToken);
            List<GameObject> rockPrefabs = await LoadPrefabList(rockKeys, cancellationToken);
            List<GameObject> structurePrefabs = await LoadPrefabList(structureKeys, cancellationToken);

            int decorationsAdded = 0;
            int batchSize = 30;

            // Iterate through terrain tiles
            foreach (var tile in hexGrid.GetAllTiles())
            {
                if (cancellationToken.IsCancellationRequested) return;
                if (tile.TileType != TileType.Terrain) continue;

                // Randomly add vegetation
                if (vegetationPrefabs.Count > 0 && Random.value < vegetationDensity)
                {
                    GameObject vegetation = Instantiate(vegetationPrefabs[Random.Range(0, vegetationPrefabs.Count)]);
                    tile.AddDecoration(vegetation);
                    decorationsAdded++;
                }

                // Randomly add rocks
                if (rockPrefabs.Count > 0 && Random.value < rockDensity)
                {
                    GameObject rock = Instantiate(rockPrefabs[Random.Range(0, rockPrefabs.Count)]);
                    tile.AddDecoration(rock);
                    decorationsAdded++;
                }

                // Randomly add structures (huts, palms)
                if (structurePrefabs.Count > 0 && Random.value < structureDensity)
                {
                    GameObject structure = Instantiate(structurePrefabs[Random.Range(0, structurePrefabs.Count)]);
                    tile.AddDecoration(structure);
                    decorationsAdded++;
                }

                // Yield periodically
                if (decorationsAdded % batchSize == 0)
                {
                    await Awaitable.NextFrameAsync(cancellationToken);
                }
            }

            Debug.Log($"MapGenerator: Added {decorationsAdded} decorations");
        }

        /// <summary>
        /// Helper to load multiple prefabs asynchronously.
        /// </summary>
        private async Awaitable<List<GameObject>> LoadPrefabList(string[] keys, CancellationToken cancellationToken)
        {
            List<GameObject> prefabs = new List<GameObject>();
            foreach (var key in keys)
            {
                var prefab = await assetLoader.LoadAssetAsync<GameObject>(key, cancellationToken);
                if (prefab != null) prefabs.Add(prefab);
            }
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


