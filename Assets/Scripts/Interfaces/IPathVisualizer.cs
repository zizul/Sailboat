using System.Collections.Generic;
using UnityEngine;
using SailboatGame.Core;

namespace SailboatGame.Interfaces
{
    /// <summary>
    /// Abstract base class for path visualization systems.
    /// Allows switching between LineRenderer, particles, UI, AR overlays, etc.
    /// </summary>
    public abstract class IPathVisualizer : MonoBehaviour
    {
        /// <summary>
        /// Visualizes a path on the map.
        /// </summary>
        public abstract void ShowPath(List<HexCoordinates> path);

        /// <summary>
        /// Clears the currently visualized path.
        /// </summary>
        public abstract void ClearPath();

        /// <summary>
        /// Updates visualization as progress is made along the path.
        /// </summary>
        public abstract void UpdateProgress(HexCoordinates currentPosition);

        /// <summary>
        /// Sets the visualization color.
        /// </summary>
        public abstract void SetLineColor(Color color);

        /// <summary>
        /// Sets the tile highlight color.
        /// </summary>
        public abstract void SetHighlightColor(Color color);

        /// <summary>
        /// Enables or disables path visualization.
        /// </summary>
        public abstract void SetVisible(bool visible);
    }
}

