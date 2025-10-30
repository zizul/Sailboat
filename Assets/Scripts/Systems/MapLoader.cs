using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using SailboatGame.Interfaces;

namespace SailboatGame.Systems
{
    /// <summary>
    /// Loads and parses map data from TextAsset files.
    /// Converts text-based map layouts into usable grid data.
    /// Extends IMapLoader abstract class for flexibility.
    /// </summary>
    public class MapLoader : IMapLoader
    {
        /// <summary>
        /// Represents the parsed map data.
        /// Note: Using nested class for backward compatibility.
        /// Extends IMapLoader.MapData structure.
        /// </summary>
        public new class MapData : IMapLoader.MapData
        {
            // Inherits all properties and methods from IMapLoader.MapData
        }

        /// <summary>
        /// Loads a map from a TextAsset asynchronously.
        /// </summary>
        public async Awaitable<MapData> LoadMapAsync(TextAsset mapAsset, CancellationToken cancellationToken = default)
        {
            return await LoadMapFromTextAssetAsync(mapAsset, cancellationToken);
        }

        /// <summary>
        /// Loads a map from source (IMapLoader abstract class implementation).
        /// </summary>
        public override async Awaitable<IMapLoader.MapData> LoadMapAsync(object source, CancellationToken cancellationToken)
        {
            if (source is TextAsset mapAsset)
            {
                return await LoadMapFromTextAssetAsync(mapAsset, cancellationToken);
            }
            
            Debug.LogError("MapLoader: Unsupported source type. Expected TextAsset.");
            return null;
        }

        /// <summary>
        /// Internal method to load from TextAsset.
        /// </summary>
        private async Awaitable<MapData> LoadMapFromTextAssetAsync(TextAsset mapAsset, CancellationToken cancellationToken = default)
        {
            if (mapAsset == null)
            {
                Debug.LogError("MapLoader: TextAsset is null");
                return null;
            }

            // Parse on background to avoid blocking main thread for large maps
            MapData mapData = null;
            string mapText = mapAsset.text;

            // Yield to prevent frame spike
            await Awaitable.NextFrameAsync(cancellationToken);

            try
            {
                mapData = ParseMapText(mapText);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"MapLoader: Failed to parse map. Error: {e.Message}");
                return null;
            }

            if (mapData == null || !mapData.IsValid())
            {
                Debug.LogError("MapLoader: Invalid map data");
                return null;
            }

            Debug.Log($"MapLoader: Successfully loaded map - Size: {mapData.Width}x{mapData.Height}");
            return mapData;
        }

        /// <summary>
        /// Parses the map text into a 2D array.
        /// Expected format: Each line contains digits (0 = water, 1 = terrain).
        /// </summary>
        private MapData ParseMapText(string mapText)
        {
            if (string.IsNullOrEmpty(mapText))
            {
                Debug.LogError("MapLoader: Map text is empty");
                return null;
            }

            // Split into lines and remove empty lines
            string[] lines = mapText.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            
            if (lines.Length == 0)
            {
                Debug.LogError("MapLoader: No valid lines in map text");
                return null;
            }

            int height = lines.Length;
            int width = lines[0].Length;

            // Validate all lines have same width
            foreach (var line in lines)
            {
                if (line.Length != width)
                {
                    Debug.LogWarning($"MapLoader: Inconsistent line width. Expected {width}, got {line.Length}");
                }
            }

            // Create 2D array
            MapData mapData = new MapData
            {
                Width = width,
                Height = height,
                Tiles = new int[width, height]
            };

            // Parse each cell
            for (int y = 0; y < height; y++)
            {
                string line = lines[y];
                for (int x = 0; x < Mathf.Min(width, line.Length); x++)
                {
                    char c = line[x];
                    if (c == '0')
                        mapData.Tiles[x, y] = 0; // Water
                    else if (c == '1')
                        mapData.Tiles[x, y] = 1; // Terrain
                    else
                        mapData.Tiles[x, y] = 0; // Default to water for unknown chars
                }
            }

            return mapData;
        }

        /// <summary>
        /// Loads a map from Resources folder (fallback if addressables not set up).
        /// </summary>
        public async Awaitable<MapData> LoadMapFromResourcesAsync(string resourcePath, CancellationToken cancellationToken = default)
        {
            TextAsset mapAsset = Resources.Load<TextAsset>(resourcePath);
            return await LoadMapAsync(mapAsset, cancellationToken);
        }
    }
}


