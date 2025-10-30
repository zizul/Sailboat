using System.Collections.Generic;
using UnityEngine;
using SailboatGame.Core;

namespace SailboatGame.Pathfinding
{
    /// <summary>
    /// A* pathfinding algorithm implementation for hexagonal grids.
    /// Optimized with priority queue and efficient neighbor lookup.
    /// </summary>
    public class AStarPathfinding : IPathfindingStrategy
    {
        private class Node
        {
            public HexCoordinates Coordinates;
            public Node Parent;
            public float GCost; // Distance from start
            public float HCost; // Heuristic distance to goal
            public float FCost => GCost + HCost;

            public Node(HexCoordinates coords)
            {
                Coordinates = coords;
            }
        }

        /// <summary>
        /// Simple priority queue implementation for A*.
        /// </summary>
        private class PriorityQueue
        {
            private List<Node> nodes = new List<Node>();

            public int Count => nodes.Count;

            public void Enqueue(Node node)
            {
                nodes.Add(node);
                int currentIndex = nodes.Count - 1;

                // Bubble up
                while (currentIndex > 0)
                {
                    int parentIndex = (currentIndex - 1) / 2;
                    if (nodes[currentIndex].FCost >= nodes[parentIndex].FCost)
                        break;

                    (nodes[currentIndex], nodes[parentIndex]) = (nodes[parentIndex], nodes[currentIndex]);
                    currentIndex = parentIndex;
                }
            }

            public Node Dequeue()
            {
                if (nodes.Count == 0) return null;

                Node result = nodes[0];
                nodes[0] = nodes[nodes.Count - 1];
                nodes.RemoveAt(nodes.Count - 1);

                if (nodes.Count > 0)
                {
                    int currentIndex = 0;

                    // Bubble down
                    while (true)
                    {
                        int leftChild = currentIndex * 2 + 1;
                        int rightChild = currentIndex * 2 + 2;
                        int smallest = currentIndex;

                        if (leftChild < nodes.Count && nodes[leftChild].FCost < nodes[smallest].FCost)
                            smallest = leftChild;

                        if (rightChild < nodes.Count && nodes[rightChild].FCost < nodes[smallest].FCost)
                            smallest = rightChild;

                        if (smallest == currentIndex)
                            break;

                        (nodes[currentIndex], nodes[smallest]) = (nodes[smallest], nodes[currentIndex]);
                        currentIndex = smallest;
                    }
                }

                return result;
            }

            public void Clear()
            {
                nodes.Clear();
            }
        }

        // Pooled collections to avoid garbage allocation
        private PriorityQueue openSet = new PriorityQueue();
        private HashSet<HexCoordinates> closedSet = new HashSet<HexCoordinates>();
        private Dictionary<HexCoordinates, Node> allNodes = new Dictionary<HexCoordinates, Node>();

        public string GetName() => "A* Pathfinding";

        /// <summary>
        /// Finds the optimal path using A* algorithm.
        /// </summary>
        public List<HexCoordinates> FindPath(HexCoordinates start, HexCoordinates goal, HexGrid grid)
        {
            if (grid == null)
            {
                Debug.LogError("AStarPathfinding: Grid is null");
                return null;
            }

            // Validate start and goal
            if (!grid.IsWalkable(start))
            {
                Debug.LogWarning($"AStarPathfinding: Start position {start} is not walkable");
                return null;
            }

            if (!grid.IsWalkable(goal))
            {
                Debug.LogWarning($"AStarPathfinding: Goal position {goal} is not walkable");
                return null;
            }

            if (start == goal)
            {
                return new List<HexCoordinates> { start };
            }

            // Clear previous search
            openSet.Clear();
            closedSet.Clear();
            allNodes.Clear();

            // Initialize start node
            Node startNode = new Node(start)
            {
                GCost = 0,
                HCost = CalculateHeuristic(start, goal)
            };
            allNodes[start] = startNode;
            openSet.Enqueue(startNode);

            // A* main loop
            while (openSet.Count > 0)
            {
                Node currentNode = openSet.Dequeue();

                // Check if we reached the goal
                if (currentNode.Coordinates == goal)
                {
                    return ReconstructPath(currentNode);
                }

                closedSet.Add(currentNode.Coordinates);

                // Explore neighbors
                var neighbors = grid.GetWalkableNeighbors(currentNode.Coordinates);
                foreach (var neighborCoords in neighbors)
                {
                    if (closedSet.Contains(neighborCoords))
                        continue;

                    float tentativeGCost = currentNode.GCost + 1f; // Uniform cost for hex grid

                    // Get or create neighbor node
                    if (!allNodes.TryGetValue(neighborCoords, out Node neighborNode))
                    {
                        neighborNode = new Node(neighborCoords);
                        allNodes[neighborCoords] = neighborNode;
                    }

                    // Check if this path is better
                    if (neighborNode.GCost == 0 || tentativeGCost < neighborNode.GCost)
                    {
                        neighborNode.Parent = currentNode;
                        neighborNode.GCost = tentativeGCost;
                        neighborNode.HCost = CalculateHeuristic(neighborCoords, goal);

                        openSet.Enqueue(neighborNode);
                    }
                }
            }

            // No path found
            Debug.LogWarning($"AStarPathfinding: No path found from {start} to {goal}");
            return null;
        }

        /// <summary>
        /// Calculates the heuristic (estimated distance) between two hex coordinates.
        /// Uses hex distance which is admissible for A*.
        /// </summary>
        private float CalculateHeuristic(HexCoordinates from, HexCoordinates to)
        {
            return from.DistanceTo(to);
        }

        /// <summary>
        /// Reconstructs the path by following parent pointers from goal to start.
        /// </summary>
        private List<HexCoordinates> ReconstructPath(Node goalNode)
        {
            List<HexCoordinates> path = new List<HexCoordinates>();
            Node current = goalNode;

            while (current != null)
            {
                path.Add(current.Coordinates);
                current = current.Parent;
            }

            path.Reverse();
            return path;
        }
    }
}


