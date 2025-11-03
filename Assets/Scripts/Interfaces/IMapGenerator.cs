using System;
using System.Threading;
using UnityEngine;

namespace SailboatGame.Interfaces
{
    /// <summary>
    /// Abstract base class for map generation systems.
    /// Allows switching between different world builders, procedural generation, etc.
    /// </summary>
    public abstract class IMapGenerator : MonoBehaviour
    {
        /// <summary>
        /// Generates a map asynchronously from map data.
        /// </summary>
        public abstract Awaitable<bool> GenerateMapAsync(IMapLoader.MapData mapData, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears the current map.
        /// </summary>
        public abstract void ClearMap();

        public abstract event Action<float, string> OnGenerationProgress;
    }
}
