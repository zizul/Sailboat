using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using SailboatGame.Interfaces;

namespace SailboatGame.Systems
{
    /// <summary>
    /// Manages loading and caching of addressable assets asynchronously.
    /// Implements object pooling for frequently instantiated prefabs.
    /// Implements IAssetLoader interface for flexibility.
    /// </summary>
    public class AddressableAssetLoader : IAssetLoader
    {
        private Dictionary<string, AsyncOperationHandle> loadedAssets = new Dictionary<string, AsyncOperationHandle>();
        private Dictionary<string, Queue<GameObject>> objectPools = new Dictionary<string, Queue<GameObject>>();
        private Dictionary<GameObject, string> instanceToKey = new Dictionary<GameObject, string>();

        [Header("Pooling Settings")]
        [SerializeField] private int defaultPoolSize = 5;
        [SerializeField] private Transform poolContainer;

        private void Awake()
        {
            if (poolContainer == null)
            {
                poolContainer = new GameObject("Object Pool").transform;
                poolContainer.SetParent(transform);
            }
        }

        /// <summary>
        /// Loads a single asset asynchronously using Awaitable.
        /// Results are cached for subsequent requests.
        /// </summary>
        public override async Awaitable<T> LoadAssetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            // Return cached asset if already loaded
            if (loadedAssets.TryGetValue(key, out AsyncOperationHandle existingHandle))
            {
                if (existingHandle.IsDone && existingHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    return existingHandle.Result as T;
                }
            }

            try
            {
                // Load the asset
                var handle = Addressables.LoadAssetAsync<T>(key);
                loadedAssets[key] = handle;

                // Wait for completion
                while (!handle.IsDone)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        Addressables.Release(handle);
                        loadedAssets.Remove(key);
                        return null;
                    }
                    await Awaitable.NextFrameAsync(cancellationToken);
                }

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    return handle.Result as T;
                }
                else
                {
                    Debug.LogError($"Failed to load asset: {key}. Error: {handle.OperationException}");
                    loadedAssets.Remove(key);
                    return null;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception loading asset {key}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Loads multiple assets asynchronously in parallel.
        /// </summary>
        public override async Awaitable<List<T>> LoadAssetsAsync<T>(List<string> keys, CancellationToken cancellationToken = default) where T : class
        {
            List<T> results = new List<T>(keys.Count);
            List<Awaitable<T>> tasks = new List<Awaitable<T>>(keys.Count);

            // Start all loads in parallel
            foreach (var key in keys)
            {
                tasks.Add(LoadAssetAsync<T>(key, cancellationToken));
            }

            // Wait for all to complete
            foreach (var task in tasks)
            {
                var result = await task;
                if (result != null)
                {
                    results.Add(result);
                }
            }

            return results;
        }

        /// <summary>
        /// Instantiates a prefab with pooling support.
        /// Returns an instance from the pool if available, otherwise instantiates new.
        /// </summary>
        public override async Awaitable<GameObject> InstantiateAsync(string key, Transform parent = null, CancellationToken cancellationToken = default)
        {
            // Check pool first
            if (objectPools.TryGetValue(key, out Queue<GameObject> pool) && pool.Count > 0)
            {
                GameObject pooledObj = pool.Dequeue();
                if (pooledObj != null)
                {
                    pooledObj.SetActive(true);
                    if (parent != null)
                    {
                        pooledObj.transform.SetParent(parent);
                    }
                    return pooledObj;
                }
            }

            // Load prefab if not in cache
            GameObject prefab = await LoadAssetAsync<GameObject>(key, cancellationToken);
            if (prefab == null)
            {
                return null;
            }

            // Instantiate
            GameObject instance = Instantiate(prefab, parent);
            instanceToKey[instance] = key;
            return instance;
        }

        /// <summary>
        /// Returns an instantiated object to the pool for reuse.
        /// </summary>
        public override void ReturnToPool(GameObject instance)
        {
            if (instance == null) return;

            if (instanceToKey.TryGetValue(instance, out string key))
            {
                instance.SetActive(false);
                instance.transform.SetParent(poolContainer);

                if (!objectPools.ContainsKey(key))
                {
                    objectPools[key] = new Queue<GameObject>();
                }

                objectPools[key].Enqueue(instance);
            }
            else
            {
                // Not a pooled object, just destroy it
                Destroy(instance);
            }
        }

        /// <summary>
        /// Pre-warms the pool for a specific prefab key.
        /// </summary>
        public override async Awaitable PrewarmPoolAsync(string key, int count, CancellationToken cancellationToken = default)
        {
            GameObject prefab = await LoadAssetAsync<GameObject>(key, cancellationToken);
            if (prefab == null) return;

            if (!objectPools.ContainsKey(key))
            {
                objectPools[key] = new Queue<GameObject>();
            }

            for (int i = 0; i < count; i++)
            {
                if (cancellationToken.IsCancellationRequested) break;

                GameObject instance = Instantiate(prefab, poolContainer);
                instance.SetActive(false);
                instanceToKey[instance] = key;
                objectPools[key].Enqueue(instance);
            }
        }

        /// <summary>
        /// Releases all loaded assets and clears pools.
        /// </summary>
        public override void ReleaseAll()
        {
            // Clear pools
            foreach (var pool in objectPools.Values)
            {
                while (pool.Count > 0)
                {
                    var obj = pool.Dequeue();
                    if (obj != null) Destroy(obj);
                }
            }
            objectPools.Clear();
            instanceToKey.Clear();

            // Release addressable assets
            foreach (var handle in loadedAssets.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
            loadedAssets.Clear();
        }

        private void OnDestroy()
        {
            ReleaseAll();
        }
    }
}


