using System.Collections.Generic;
using SailboatGame.Core;

namespace SailboatGame.Pathfinding
{
    /// <summary>
    /// Interface for pathfinding algorithms.
    /// Allows different pathfinding strategies to be swapped at runtime (Strategy Pattern).
    /// </summary>
    public interface IPathfindingStrategy
    {
        /// <summary>
        /// Finds a path from start to goal on the hex grid.
        /// Returns null if no path exists.
        /// </summary>
        List<HexCoordinates> FindPath(HexCoordinates start, HexCoordinates goal, HexGrid grid);

        /// <summary>
        /// Gets the name of this pathfinding strategy.
        /// </summary>
        string GetName();
    }
}


