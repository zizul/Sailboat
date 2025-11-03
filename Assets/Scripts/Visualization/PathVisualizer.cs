using System.Collections.Generic;
using UnityEngine;
using SailboatGame.Core;
using SailboatGame.Interfaces;

namespace SailboatGame.Visualization
{
    /// <summary>
    /// Visualizes the computed path using LineRenderer and tile highlighting.
    /// Efficient for mobile with object pooling and minimal draw calls.
    /// Extends IPathVisualizer abstract class for flexibility.
    /// </summary>
    public class PathVisualizer : IPathVisualizer
    {
        [Header("References")]
        [SerializeField] private HexGrid hexGrid;
        [SerializeField] private LineRenderer lineRenderer;

        [Header("Line Settings")]
        [SerializeField] private float lineWidth = 0.1f;
        [SerializeField] private float lineHeightOffset = 0.05f;
        [SerializeField] private Color lineColor = new Color(0, 1, 0, 1f);

        [Header("Tile Highlight Settings")]
        [SerializeField] private bool highlightTiles = true;
        [SerializeField] private Color tileHighlightColor = new Color(0, 1, 1, 1);

        [Header("Animation")]
        [SerializeField] private bool animateLine = true;
        [SerializeField] private float animationSpeed = 2f;
        [SerializeField] private Material lineMaterial;

        private List<HexCoordinates> currentPath;
        private List<HexTile> highlightedTiles = new List<HexTile>();
        private float animationOffset;

        private void Awake()
        {
            // Setup line renderer
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            ConfigureLineRenderer();
        }

        private void Update()
        {
            // Animate line material offset
            if (animateLine && lineRenderer != null && lineRenderer.enabled)
            {
                animationOffset += animationSpeed * Time.deltaTime;
                if (lineMaterial != null)
                {
                    lineMaterial.SetFloat("_Offset", animationOffset);
                }
            }
        }

        /// <summary>
        /// Configures the line renderer properties.
        /// </summary>
        private void ConfigureLineRenderer()
        {
            if (lineRenderer == null) return;
            
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;
            lineRenderer.numCapVertices = 5;
            lineRenderer.numCornerVertices = 5;
            lineRenderer.alignment = LineAlignment.TransformZ;
            lineRenderer.textureMode = LineTextureMode.Tile;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;

            if (lineMaterial != null)
            {
                lineRenderer.material = lineMaterial;
            }

            lineRenderer.enabled = false;
        }

        /// <summary>
        /// Visualizes a path on the hex grid.
        /// </summary>
        public override void ShowPath(List<HexCoordinates> path)
        {
            ClearPath();

            ConfigureLineRenderer();

            if (path == null || path.Count == 0)
            {
                return;
            }

            currentPath = new List<HexCoordinates>(path);

            // Draw line
            DrawPathLine(path);

            // Highlight tiles
            if (highlightTiles)
            {
                HighlightPathTiles(path);
            }
        }

        /// <summary>
        /// Draws the path using LineRenderer.
        /// </summary>
        private void DrawPathLine(List<HexCoordinates> path)
        {
            if (lineRenderer == null || hexGrid == null)
                return;

            lineRenderer.positionCount = path.Count;
            Vector3[] positions = new Vector3[path.Count];

            for (int i = 0; i < path.Count; i++)
            {
                Vector3 worldPos = hexGrid.HexToWorld(path[i]);
                positions[i] = worldPos + Vector3.up * lineHeightOffset;
            }

            lineRenderer.SetPositions(positions);
            lineRenderer.enabled = true;
        }

        /// <summary>
        /// Highlights tiles along the path.
        /// </summary>
        private void HighlightPathTiles(List<HexCoordinates> path)
        {
            if (hexGrid == null)
                return;

            foreach (var coords in path)
            {
                HexTile tile = hexGrid.GetTile(coords);
                if (tile != null)
                {
                    tile.SetHighlight(true, tileHighlightColor);
                    highlightedTiles.Add(tile);
                }
            }
        }

        /// <summary>
        /// Clears the currently visualized path.
        /// </summary>
        public override void ClearPath()
        {
            // Clear line
            if (lineRenderer != null)
            {
                lineRenderer.positionCount = 0;
                lineRenderer.enabled = false;
            }

            // Clear tile highlights
            foreach (var tile in highlightedTiles)
            {
                if (tile != null)
                {
                    tile.SetHighlight(false, Color.white);
                }
            }
            highlightedTiles.Clear();

            currentPath = null;
            animationOffset = 0f;
        }

        /// <summary>
        /// Updates the path visualization as the boat progresses.
        /// </summary>
        public override void UpdateProgress(HexCoordinates currentPosition)
        {
            if (currentPath == null || currentPath.Count == 0)
                return;

            // Find current position in path
            int currentIndex = currentPath.IndexOf(currentPosition);
            if (currentIndex >= 0 && currentIndex < currentPath.Count - 1)
            {
                // Update line to show remaining path
                List<HexCoordinates> remainingPath = currentPath.GetRange(currentIndex, currentPath.Count - currentIndex);
                DrawPathLine(remainingPath);
            }
        }

        /// <summary>
        /// Sets the line color.
        /// </summary>
        public override void SetLineColor(Color color)
        {
            lineColor = color;
            if (lineRenderer != null)
            {
                lineRenderer.startColor = color;
                lineRenderer.endColor = color;
            }
        }

        /// <summary>
        /// Sets the tile highlight color.
        /// </summary>
        public override void SetHighlightColor(Color color)
        {
            tileHighlightColor = color;
        }

        /// <summary>
        /// Enables or disables path visualization.
        /// </summary>
        public override void SetVisible(bool visible)
        {
            if (lineRenderer != null)
            {
                lineRenderer.enabled = visible && currentPath != null && currentPath.Count > 0;
            }
        }

        private void OnDestroy()
        {
            ClearPath();
        }
    }
}


