using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace SailboatGame.Interfaces
{
    /// <summary>
    /// Interface for asset loading systems.
    /// Allows switching between Addressables, Resources, AssetBundles, etc.
    /// </summary>
    public abstract class IAssetLoader : MonoBehaviour
    {
        /// <summary>
        /// Loads a single asset asynchronously.
        /// </summary>
        public abstract Awaitable<T> LoadAssetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Loads multiple assets asynchronously.
        /// </summary>
        public abstract Awaitable<List<T>> LoadAssetsAsync<T>(List<string> keys, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Instantiates a prefab with pooling support.
        /// </summary>
        public abstract Awaitable<GameObject> InstantiateAsync(string key, Transform parent = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns an object to the pool for reuse.
        /// </summary>
        public abstract void ReturnToPool(GameObject instance);

        /// <summary>
        /// Pre-warms the pool for a specific prefab.
        /// </summary>
        public abstract Awaitable PrewarmPoolAsync(string key, int count, CancellationToken cancellationToken = default);

        /// <summary>
        /// Releases all loaded assets and clears pools.
        /// </summary>
        public abstract void ReleaseAll();
    }
}


