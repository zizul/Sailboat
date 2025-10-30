using System.Threading;
using UnityEngine;

namespace SailboatGame.Interfaces
{
    /// <summary>
    /// Abstract base class for map loading systems.
    /// Allows switching between TextAsset, JSON, binary formats, network loading, etc.
    /// </summary>
    public abstract class IMapLoader : MonoBehaviour
    {
        /// <summary>
        /// Map data structure returned by the loader.
        /// </summary>
        public class MapData
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public int[,] Tiles { get; set; }

            public bool IsValid()
            {
                return Width > 0 && Height > 0 && Tiles != null;
            }

            public bool IsWater(int x, int y)
            {
                if (x < 0 || x >= Width || y < 0 || y >= Height) return false;
                return Tiles[x, y] == 0;
            }

            public bool IsTerrain(int x, int y)
            {
                if (x < 0 || x >= Width || y < 0 || y >= Height) return false;
                return Tiles[x, y] == 1;
            }
        }

        /// <summary>
        /// Loads a map asynchronously from the given source.
        /// </summary>
        public abstract Awaitable<MapData> LoadMapAsync(object source, CancellationToken cancellationToken = default);
    }
}

