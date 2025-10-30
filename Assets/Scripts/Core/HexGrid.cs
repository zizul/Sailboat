using System.Collections.Generic;
using UnityEngine;

namespace SailboatGame.Core
{
    /// <summary>
    /// Manages the hexagonal grid structure and provides spatial queries.
    /// Optimized with spatial hashing for fast lookups.
    /// </summary>
    public class HexGrid : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private float hexSize = 1f;
        [SerializeField] private Transform tilesContainer;

        private Dictionary<HexCoordinates, HexTile> tiles = new Dictionary<HexCoordinates, HexTile>();
        private int width;
        private int height;

        public float HexSize => hexSize;
        public int Width => width;
        public int Height => height;
        public int TileCount => tiles.Count;

        /// <summary>
        /// Initializes the grid with specified dimensions.
        /// </summary>
        public void Initialize(int gridWidth, int gridHeight, float size)
        {
            width = gridWidth;
            height = gridHeight;
            hexSize = size;
            tiles.Clear();

            if (tilesContainer == null)
            {
                tilesContainer = new GameObject("Tiles").transform;
                tilesContainer.SetParent(transform);
                tilesContainer.localPosition = Vector3.zero;
            }
        }

        /// <summary>
        /// Adds a tile to the grid.
        /// </summary>
        public void AddTile(HexCoordinates coords, HexTile tile)
        {
            if (tiles.ContainsKey(coords))
            {
                Debug.LogWarning($"Tile at {coords} already exists. Replacing.");
                if (tiles[coords] != null)
                {
                    Destroy(tiles[coords].gameObject);
                }
            }

            tiles[coords] = tile;
            tile.transform.SetParent(tilesContainer);
            tile.transform.position = coords.ToWorldPosition(hexSize);
        }

        /// <summary>
        /// Gets a tile at the specified coordinates.
        /// </summary>
        public HexTile GetTile(HexCoordinates coords)
        {
            tiles.TryGetValue(coords, out HexTile tile);
            return tile;
        }

        /// <summary>
        /// Checks if a tile exists at the specified coordinates.
        /// </summary>
        public bool HasTile(HexCoordinates coords)
        {
            return tiles.ContainsKey(coords);
        }

        /// <summary>
        /// Checks if a tile at coordinates is walkable (water).
        /// </summary>
        public bool IsWalkable(HexCoordinates coords)
        {
            if (tiles.TryGetValue(coords, out HexTile tile))
            {
                return tile.IsWalkable;
            }
            return false;
        }

        /// <summary>
        /// Gets all walkable neighbors of a hex coordinate.
        /// </summary>
        public List<HexCoordinates> GetWalkableNeighbors(HexCoordinates coords)
        {
            List<HexCoordinates> walkableNeighbors = new List<HexCoordinates>(6);
            var neighbors = coords.GetNeighbors();

            foreach (var neighbor in neighbors)
            {
                if (IsWalkable(neighbor))
                {
                    walkableNeighbors.Add(neighbor);
                }
            }

            return walkableNeighbors;
        }

        /// <summary>
        /// Converts world position to hex coordinates.
        /// </summary>
        public HexCoordinates WorldToHex(Vector3 worldPos)
        {
            return HexCoordinates.FromWorldPosition(worldPos, hexSize);
        }

        /// <summary>
        /// Converts hex coordinates to world position.
        /// </summary>
        public Vector3 HexToWorld(HexCoordinates coords)
        {
            return coords.ToWorldPosition(hexSize);
        }

        /// <summary>
        /// Gets all tiles in the grid.
        /// </summary>
        public IEnumerable<HexTile> GetAllTiles()
        {
            return tiles.Values;
        }

        /// <summary>
        /// Gets tiles within a certain radius (for culling/LOD).
        /// </summary>
        public List<HexTile> GetTilesInRadius(HexCoordinates center, int radius)
        {
            List<HexTile> result = new List<HexTile>();

            for (int q = -radius; q <= radius; q++)
            {
                for (int r = Mathf.Max(-radius, -q - radius); r <= Mathf.Min(radius, -q + radius); r++)
                {
                    HexCoordinates coords = new HexCoordinates(center.Q + q, center.R + r);
                    if (tiles.TryGetValue(coords, out HexTile tile))
                    {
                        result.Add(tile);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Clears all tiles from the grid.
        /// </summary>
        public void Clear()
        {
            foreach (var tile in tiles.Values)
            {
                if (tile != null)
                {
                    Destroy(tile.gameObject);
                }
            }
            tiles.Clear();
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || tiles.Count == 0) return;

            // Draw grid bounds
            Gizmos.color = Color.yellow;
            Vector3 min = HexToWorld(new HexCoordinates(0, 0));
            Vector3 max = HexToWorld(new HexCoordinates(width - 1, height - 1));
            Vector3 center = (min + max) / 2f;
            Vector3 size = (max - min) + Vector3.one * hexSize * 2;
            Gizmos.DrawWireCube(center, size);
        }
    }
}


