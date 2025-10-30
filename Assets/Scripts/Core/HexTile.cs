using UnityEngine;

namespace SailboatGame.Core
{
    /// <summary>
    /// Represents a single tile in the hex grid.
    /// Contains tile type and references to visual elements.
    /// </summary>
    public class HexTile : MonoBehaviour
    {
        [Header("Tile Data")]
        [SerializeField] private HexCoordinates coordinates;
        [SerializeField] private TileType tileType;

        [Header("Visual")]
        [SerializeField] private GameObject tileVisual;
        [SerializeField] private GameObject[] decorations;

        public HexCoordinates Coordinates => coordinates;
        public TileType TileType => tileType;
        public bool IsWalkable => tileType == TileType.Water;

        /// <summary>
        /// Initializes the tile with coordinates and type.
        /// </summary>
        public void Initialize(HexCoordinates coords, TileType type)
        {
            coordinates = coords;
            tileType = type;
            gameObject.name = $"Tile_{coords}_{type}";
        }

        /// <summary>
        /// Sets the visual representation of the tile.
        /// </summary>
        public void SetVisual(GameObject visual)
        {
            tileVisual = visual;
            if (visual != null)
            {
                visual.transform.SetParent(transform);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// Adds a decoration (vegetation, rocks, etc.) to the tile.
        /// </summary>
        public void AddDecoration(GameObject decoration)
        {
            if (decoration != null)
            {
                decoration.transform.SetParent(transform);
                
                // Add slight random offset and rotation for natural look
                float offsetRange = 0.3f;
                decoration.transform.localPosition = new Vector3(
                    Random.Range(-offsetRange, offsetRange),
                    0,
                    Random.Range(-offsetRange, offsetRange)
                );
                decoration.transform.localRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            }
        }

        /// <summary>
        /// Highlights the tile (for path visualization).
        /// </summary>
        public void SetHighlight(bool highlighted, Color color)
        {
            if (tileVisual == null) return;

            var renderers = tileVisual.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (highlighted)
                {
                    renderer.material.SetColor("_EmissionColor", color * 0.5f);
                    renderer.material.EnableKeyword("_EMISSION");
                }
                else
                {
                    renderer.material.DisableKeyword("_EMISSION");
                }
            }
        }

        private void OnDrawGizmos()
        {
            // Draw hex outline in editor for debugging
            if (Application.isPlaying)
            {
                Gizmos.color = IsWalkable ? new Color(0, 0, 1, 0.3f) : new Color(0, 1, 0, 0.3f);
                DrawHexGizmo(transform.position, 1f);
            }
        }

        private void DrawHexGizmo(Vector3 center, float size)
        {
            for (int i = 0; i < 6; i++)
            {
                float angle1 = 60 * i - 30;
                float angle2 = 60 * (i + 1) - 30;
                Vector3 corner1 = center + new Vector3(
                    size * Mathf.Cos(angle1 * Mathf.Deg2Rad),
                    0,
                    size * Mathf.Sin(angle1 * Mathf.Deg2Rad)
                );
                Vector3 corner2 = center + new Vector3(
                    size * Mathf.Cos(angle2 * Mathf.Deg2Rad),
                    0,
                    size * Mathf.Sin(angle2 * Mathf.Deg2Rad)
                );
                Gizmos.DrawLine(corner1, corner2);
            }
        }
    }

    /// <summary>
    /// Defines the type of a hex tile.
    /// </summary>
    public enum TileType
    {
        Water,
        Terrain
    }
}


