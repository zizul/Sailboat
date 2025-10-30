using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using SailboatGame.Core;
using SailboatGame.Interfaces;

namespace SailboatGame.Boat
{
    /// <summary>
    /// Controls boat movement along paths using smooth interpolation.
    /// Uses Awaitable for async movement operations.
    /// Handles rotation towards movement direction and spawns foam effects.
    /// Uses IAssetLoader interface for flexibility.
    /// </summary>
    public class BoatController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HexGrid hexGrid;
        [SerializeField] private GameObject boatVisual;
        [SerializeField] private Transform boatFoamSpawnPoint;

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float rotationSpeed = 180f;
        [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Foam Effect")]
        [SerializeField] private string boatFoamPrefabKey = "boat_foam_effect";
        [SerializeField] private float foamSpawnInterval = 0.2f;
        [SerializeField] private float foamLifetime = 2f;

        private HexCoordinates currentPosition;
        private List<HexCoordinates> currentPath;
        private bool isMoving;
        private CancellationTokenSource movementCTS;
        private IAssetLoader assetLoader;
        private Queue<GameObject> foamPool = new Queue<GameObject>();

        public HexCoordinates CurrentPosition => currentPosition;
        public bool IsMoving => isMoving;

        /// <summary>
        /// Initializes the boat at a specific hex position.
        /// </summary>
        public void Initialize(HexCoordinates startPosition, HexGrid grid, IAssetLoader loader)
        {
            hexGrid = grid;
            assetLoader = loader;
            currentPosition = startPosition;
            
            if (hexGrid != null)
            {
                transform.position = hexGrid.HexToWorld(startPosition);
            }

            // Setup foam spawn point if not assigned
            if (boatFoamSpawnPoint == null)
            {
                GameObject spawnPoint = new GameObject("FoamSpawnPoint");
                spawnPoint.transform.SetParent(transform);
                spawnPoint.transform.localPosition = new Vector3(0, 0, -0.5f); // Behind boat
                boatFoamSpawnPoint = spawnPoint.transform;
            }
        }

        /// <summary>
        /// Moves the boat along the given path asynchronously.
        /// Cancels any ongoing movement.
        /// </summary>
        public async Awaitable MoveAlongPathAsync(List<HexCoordinates> path, CancellationToken cancellationToken = default)
        {
            if (path == null || path.Count == 0)
            {
                Debug.LogWarning("BoatController: Invalid path provided");
                return;
            }

            // Cancel previous movement
            CancelMovement();

            // Create linked cancellation token
            movementCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var linkedToken = movementCTS.Token;

            currentPath = new List<HexCoordinates>(path);
            isMoving = true;

            try
            {
                // Start foam effect
                var foamTask = SpawnFoamEffectsAsync(linkedToken);

                // Move through each waypoint
                for (int i = 0; i < currentPath.Count; i++)
                {
                    if (linkedToken.IsCancellationRequested)
                        break;

                    HexCoordinates targetCoords = currentPath[i];
                    Vector3 targetPosition = hexGrid.HexToWorld(targetCoords);

                    await MoveToPositionAsync(targetPosition, linkedToken);
                    currentPosition = targetCoords;
                }

                await foamTask;
            }
            catch (System.Exception e)
            {
                if (!linkedToken.IsCancellationRequested)
                {
                    Debug.LogError($"BoatController: Error during movement: {e.Message}");
                }
            }
            finally
            {
                isMoving = false;
                currentPath = null;
                movementCTS?.Dispose();
                movementCTS = null;
            }
        }

        /// <summary>
        /// Moves the boat to a specific world position with smooth interpolation.
        /// </summary>
        private async Awaitable MoveToPositionAsync(Vector3 targetPosition, CancellationToken cancellationToken)
        {
            Vector3 startPosition = transform.position;
            float distance = Vector3.Distance(startPosition, targetPosition);
            
            if (distance < 0.01f)
                return;

            float duration = distance / moveSpeed;
            float elapsed = 0f;

            // Calculate target rotation
            Vector3 direction = (targetPosition - startPosition).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            while (elapsed < duration)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curvedT = movementCurve.Evaluate(t);

                // Move position
                transform.position = Vector3.Lerp(startPosition, targetPosition, curvedT);

                // Rotate towards movement direction
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );

                await Awaitable.NextFrameAsync(cancellationToken);
            }

            // Ensure we reach exact position
            if (!cancellationToken.IsCancellationRequested)
            {
                transform.position = targetPosition;
                transform.rotation = targetRotation;
            }
        }

        /// <summary>
        /// Spawns foam effects behind the boat during movement.
        /// </summary>
        private async Awaitable SpawnFoamEffectsAsync(CancellationToken cancellationToken)
        {
            if (assetLoader == null || string.IsNullOrEmpty(boatFoamPrefabKey))
                return;

            // Load foam prefab
            GameObject foamPrefab = await assetLoader.LoadAssetAsync<GameObject>(boatFoamPrefabKey, cancellationToken);

            float nextSpawnTime = 0f;

            while (isMoving && !cancellationToken.IsCancellationRequested)
            {
                if (Time.time >= nextSpawnTime)
                {
                    SpawnFoam(foamPrefab);
                    nextSpawnTime = Time.time + foamSpawnInterval;
                }

                await Awaitable.NextFrameAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Spawns a single foam effect.
        /// </summary>
        private void SpawnFoam(GameObject foamPrefab)
        {
            if (foamPrefab == null) return;

            GameObject foam;

            // Try to get from pool
            if (foamPool.Count > 0)
            {
                foam = foamPool.Dequeue();
                foam.SetActive(true);
            }
            else
            {
                foam = Instantiate(foamPrefab);
            }

            // Position at spawn point
            Vector3 spawnPos = boatFoamSpawnPoint != null ? boatFoamSpawnPoint.position : transform.position;
            foam.transform.position = spawnPos;
            foam.transform.rotation = transform.rotation;

            // Return to pool after lifetime
            ReturnFoamToPoolDelayed(foam, foamLifetime);
        }

        /// <summary>
        /// Returns foam to pool after delay.
        /// </summary>
        private async void ReturnFoamToPoolDelayed(GameObject foam, float delay)
        {
            try
            {
                await Awaitable.WaitForSecondsAsync(delay);
                
                if (foam != null)
                {
                    foam.SetActive(false);
                    foamPool.Enqueue(foam);
                }
            }
            catch
            {
                // Object might be destroyed
            }
        }

        /// <summary>
        /// Cancels the current movement operation.
        /// </summary>
        public void CancelMovement()
        {
            if (movementCTS != null)
            {
                movementCTS.Cancel();
                movementCTS.Dispose();
                movementCTS = null;
            }
            isMoving = false;
        }

        /// <summary>
        /// Teleports the boat to a specific hex position (no animation).
        /// </summary>
        public void TeleportTo(HexCoordinates position)
        {
            CancelMovement();
            currentPosition = position;
            if (hexGrid != null)
            {
                transform.position = hexGrid.HexToWorld(position);
            }
        }

        private void OnDestroy()
        {
            CancelMovement();

            // Clean up foam pool
            while (foamPool.Count > 0)
            {
                var foam = foamPool.Dequeue();
                if (foam != null) Destroy(foam);
            }
        }
    }
}


