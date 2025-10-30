using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using SailboatGame.Core;

namespace SailboatGame.Pathfinding
{
    /// <summary>
    /// Manages pathfinding operations using a pluggable strategy.
    /// Provides async pathfinding to avoid blocking the main thread.
    /// </summary>
    public class PathfindingSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HexGrid hexGrid;

        [Header("Settings")]
        [SerializeField] private bool useAsyncPathfinding = true;

        private IPathfindingStrategy currentStrategy;
        private CancellationTokenSource currentPathfindingCTS;

        private void Awake()
        {
            // Default to A* algorithm
            SetStrategy(new AStarPathfinding());
        }

        /// <summary>
        /// Sets the pathfinding strategy to use.
        /// </summary>
        public void SetStrategy(IPathfindingStrategy strategy)
        {
            if (strategy == null)
            {
                Debug.LogError("PathfindingSystem: Cannot set null strategy");
                return;
            }

            currentStrategy = strategy;
            Debug.Log($"PathfindingSystem: Strategy set to {strategy.GetName()}");
        }

        /// <summary>
        /// Finds a path asynchronously from start to goal.
        /// Cancels any ongoing pathfinding operation.
        /// </summary>
        public async Awaitable<List<HexCoordinates>> FindPathAsync(HexCoordinates start, HexCoordinates goal, CancellationToken cancellationToken = default)
        {
            // Cancel any ongoing pathfinding
            CancelCurrentPathfinding();

            if (currentStrategy == null)
            {
                Debug.LogError("PathfindingSystem: No pathfinding strategy set");
                return null;
            }

            if (hexGrid == null)
            {
                Debug.LogError("PathfindingSystem: HexGrid reference is null");
                return null;
            }

            // Create linked cancellation token
            currentPathfindingCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var linkedToken = currentPathfindingCTS.Token;

            List<HexCoordinates> path = null;

            try
            {
                if (useAsyncPathfinding)
                {
                    // Run pathfinding off main thread to avoid frame spikes for large paths
                    await Awaitable.BackgroundThreadAsync();

                    if (linkedToken.IsCancellationRequested)
                    {
                        return null;
                    }

                    path = currentStrategy.FindPath(start, goal, hexGrid);

                    // Return to main thread
                    await Awaitable.MainThreadAsync();
                }
                else
                {
                    // Run on main thread
                    path = currentStrategy.FindPath(start, goal, hexGrid);
                    await Awaitable.NextFrameAsync(linkedToken);
                }
            }
            catch (System.Exception e)
            {
                if (!linkedToken.IsCancellationRequested)
                {
                    Debug.LogError($"PathfindingSystem: Error during pathfinding: {e.Message}");
                }
                return null;
            }
            finally
            {
                currentPathfindingCTS?.Dispose();
                currentPathfindingCTS = null;
            }

            if (path != null && path.Count > 0)
            {
                Debug.Log($"PathfindingSystem: Path found with {path.Count} waypoints");
            }

            return path;
        }

        /// <summary>
        /// Finds a path synchronously (blocking).
        /// </summary>
        public List<HexCoordinates> FindPath(HexCoordinates start, HexCoordinates goal)
        {
            if (currentStrategy == null || hexGrid == null)
            {
                Debug.LogError("PathfindingSystem: Cannot find path - missing references");
                return null;
            }

            return currentStrategy.FindPath(start, goal, hexGrid);
        }

        /// <summary>
        /// Cancels the current pathfinding operation.
        /// </summary>
        public void CancelCurrentPathfinding()
        {
            if (currentPathfindingCTS != null)
            {
                currentPathfindingCTS.Cancel();
                currentPathfindingCTS.Dispose();
                currentPathfindingCTS = null;
            }
        }

        private void OnDestroy()
        {
            CancelCurrentPathfinding();
        }
    }
}


