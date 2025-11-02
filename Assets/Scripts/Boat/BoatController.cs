using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using SailboatGame.Core;
using SailboatGame.Interfaces;

namespace SailboatGame.Boat
{
    /// <summary>
    /// Represents a straight segment of the path where the boat moves in the same direction.
    /// </summary>
    internal class PathSegment
    {
        public List<HexCoordinates> Waypoints { get; set; }
        public Vector3 Direction { get; set; }

        public PathSegment()
        {
            Waypoints = new List<HexCoordinates>();
        }
    }

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

                // Group path into segments based on direction changes
                List<PathSegment> segments = GroupPathIntoSegments(currentPath);

                // Move through each segment
                foreach (var segment in segments)
                {
                    if (linkedToken.IsCancellationRequested)
                        break;

                    await MoveAlongSegmentAsync(segment, linkedToken);
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
        /// Groups the path into segments based on direction changes.
        /// Each segment represents a straight line where the boat doesn't need to turn.
        /// </summary>
        private List<PathSegment> GroupPathIntoSegments(List<HexCoordinates> path)
        {
            List<PathSegment> segments = new List<PathSegment>();
            
            if (path == null || path.Count == 0)
                return segments;

            PathSegment currentSegment = new PathSegment();
            currentSegment.Waypoints.Add(path[0]);
            Vector3 lastDirection = Vector3.zero;

            for (int i = 1; i < path.Count; i++)
            {
                Vector3 currentWorldPos = hexGrid.HexToWorld(path[i]);
                Vector3 previousWorldPos = hexGrid.HexToWorld(path[i - 1]);
                Vector3 direction = (currentWorldPos - previousWorldPos).normalized;

                // Check if direction changed significantly (more than 1 degree)
                if (lastDirection != Vector3.zero && Vector3.Angle(lastDirection, direction) > 1f)
                {
                    // Direction changed, start new segment
                    currentSegment.Direction = lastDirection;
                    segments.Add(currentSegment);
                    
                    currentSegment = new PathSegment();
                    currentSegment.Waypoints.Add(path[i - 1]); // Include transition point
                }

                currentSegment.Waypoints.Add(path[i]);
                lastDirection = direction;
            }

            // Add the last segment
            if (currentSegment.Waypoints.Count > 0)
            {
                currentSegment.Direction = lastDirection;
                segments.Add(currentSegment);
            }

            return segments;
        }

        /// <summary>
        /// Moves the boat along a straight segment without stopping at intermediate tiles.
        /// </summary>
        private async Awaitable MoveAlongSegmentAsync(PathSegment segment, CancellationToken cancellationToken)
        {
            if (segment.Waypoints.Count == 0)
                return;

            // Get start and end positions for the segment
            Vector3 startPosition = transform.position;
            Vector3 endPosition = hexGrid.HexToWorld(segment.Waypoints[segment.Waypoints.Count - 1]);
            
            // Calculate target rotation based on segment direction
            if (segment.Direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(segment.Direction);

                // Rotate to face the segment direction
                while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation,
                        targetRotation,
                        rotationSpeed * Time.deltaTime
                    );

                    await Awaitable.NextFrameAsync(cancellationToken);
                }

                transform.rotation = targetRotation;
            }

            // Move smoothly along the entire segment
            float distance = Vector3.Distance(startPosition, endPosition);
            float duration = distance / moveSpeed;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curvedT = movementCurve.Evaluate(t);

                transform.position = Vector3.Lerp(startPosition, endPosition, curvedT);

                await Awaitable.NextFrameAsync(cancellationToken);
            }

            // Ensure we reach exact position
            if (!cancellationToken.IsCancellationRequested)
            {
                transform.position = endPosition;
                currentPosition = segment.Waypoints[segment.Waypoints.Count - 1];
            }
        }

        /// <summary>
        /// Moves the boat to a specific world position with smooth interpolation.
        /// Rotates to face the target direction before moving.
        /// </summary>
        private async Awaitable MoveToPositionAsync(Vector3 targetPosition, CancellationToken cancellationToken)
        {
            Vector3 startPosition = transform.position;
            float distance = Vector3.Distance(startPosition, targetPosition);
            
            if (distance < 0.01f)
                return;

            // Calculate target rotation
            Vector3 direction = (targetPosition - startPosition).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // First, rotate to face the target direction
            while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );

                await Awaitable.NextFrameAsync(cancellationToken);
            }

            // Ensure exact rotation
            transform.rotation = targetRotation;

            // Then, move to the target position
            float duration = distance / moveSpeed;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curvedT = movementCurve.Evaluate(t);

                // Move position
                transform.position = Vector3.Lerp(startPosition, targetPosition, curvedT);

                await Awaitable.NextFrameAsync(cancellationToken);
            }

            // Ensure we reach exact position
            if (!cancellationToken.IsCancellationRequested)
            {
                transform.position = targetPosition;
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


