using UnityEngine;
using System.Collections.Generic;

namespace SailboatGame.Core
{
    /// <summary>
    /// Represents a single tile in the hex grid.
    /// Contains tile type and references to visual elements.
    /// Supports lazy loading for performance optimization.
    /// </summary>
    public class HexTile : MonoBehaviour
    {
        [Header("Tile Data")]
        [SerializeField] private HexCoordinates coordinates;
        [SerializeField] private TileType tileType;

        [Header("Visual")]
        [SerializeField] private GameObject tileVisual;
        [SerializeField] private List<GameObject> decorations = new List<GameObject>();

        [Header("Prefab References (for lazy loading)")]
        private GameObject visualPrefab;
        private List<DecorationData> decorationPrefabs = new List<DecorationData>();
        private bool visualsLoaded = false;
        private bool decorationsLoaded = false;
        private bool needsShaderActivation = false;

        public HexCoordinates Coordinates => coordinates;
        public TileType TileType => tileType;
        public bool IsWalkable => tileType == TileType.Water;
        public bool HasVisualsLoaded => visualsLoaded;
        public bool HasDecorationsLoaded => decorationsLoaded;

        /// <summary>
        /// Data structure to store decoration prefab and spawn parameters.
        /// </summary>
        [System.Serializable]
        public struct DecorationData
        {
            public GameObject prefab;
            public float offsetRange;
        }

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
        /// Stores the visual prefab for lazy loading.
        /// </summary>
        public void SetVisualPrefab(GameObject prefab, bool activateShaders = false)
        {
            visualPrefab = prefab;
            visualsLoaded = false;
            needsShaderActivation = activateShaders;
        }

        /// <summary>
        /// Sets the visual representation of the tile (immediate loading).
        /// </summary>
        public void SetVisual(GameObject visual)
        {
            tileVisual = visual;
            if (visual != null)
            {
                visual.transform.SetParent(transform);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visualsLoaded = true;
            }
        }

        /// <summary>
        /// Stores a decoration prefab for lazy loading.
        /// </summary>
        public void AddDecorationPrefab(GameObject prefab, float offsetRange = 0.3f)
        {
            decorationPrefabs.Add(new DecorationData
            {
                prefab = prefab,
                offsetRange = offsetRange
            });
            decorationsLoaded = false;
        }

        /// <summary>
        /// Adds a decoration (vegetation, rocks, etc.) to the tile (immediate loading).
        /// </summary>
        /// <param name="decoration">The decoration GameObject to add</param>
        /// <param name="offsetRange">Random position offset range (default 0.3f)</param>
        public void AddDecoration(GameObject decoration, float offsetRange = 0.3f)
        {
            if (decoration != null)
            {
                decoration.transform.SetParent(transform);

                // Add slight random offset and rotation for natural look
                decoration.transform.localPosition = new Vector3(
                    Random.Range(-offsetRange, offsetRange),
                    0.33f,
                    Random.Range(-offsetRange, offsetRange)
                );
                decoration.transform.localRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                
                decorations.Add(decoration);
            }
        }

        /// <summary>
        /// Loads the visual from prefab if not already loaded.
        /// </summary>
        private void LoadVisual()
        {
            if (visualsLoaded || visualPrefab == null) return;

            GameObject visual = Instantiate(visualPrefab);
            SetVisual(visual);
            
            // Activate shader effects if needed (for terrain tiles)
            if (needsShaderActivation && tileType == TileType.Terrain)
            {
                ActivateTerrainEffects(visual);
            }
            
            visualsLoaded = true;
        }

        /// <summary>
        /// Activates shader effects for terrain materials.
        /// </summary>
        private void ActivateTerrainEffects(GameObject terrainObject)
        {
            var renderers = terrainObject.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.materials)
                {
                    // Enable shader keywords for terrain effects
                    if (mat.HasProperty("_EnableTexture"))
                        mat.SetFloat("_EnableTexture", 1f);
                }
            }
        }

        /// <summary>
        /// Loads all decorations from prefabs if not already loaded.
        /// </summary>
        private void LoadDecorations()
        {
            if (decorationsLoaded || decorationPrefabs.Count == 0) return;

            foreach (var decorationData in decorationPrefabs)
            {
                if (decorationData.prefab != null)
                {
                    GameObject decoration = Instantiate(decorationData.prefab);
                    AddDecoration(decoration, decorationData.offsetRange);
                }
            }
            decorationsLoaded = true;
        }

        /// <summary>
        /// Unloads visual to free memory.
        /// </summary>
        private void UnloadVisual()
        {
            if (tileVisual != null)
            {
                Destroy(tileVisual);
                tileVisual = null;
                visualsLoaded = false;
            }
        }

        /// <summary>
        /// Unloads all decorations to free memory.
        /// </summary>
        private void UnloadDecorations()
        {
            foreach (var decoration in decorations)
            {
                if (decoration != null)
                {
                    Destroy(decoration);
                }
            }
            decorations.Clear();
            decorationsLoaded = false;
        }

        /// <summary>
        /// Sets the tile active/inactive with lazy loading support.
        /// Loads visuals and decorations when activating if not already loaded.
        /// </summary>
        public new void SetActive(bool active)
        {
            if (active)
            {
                // Load visuals if needed
                if (!visualsLoaded)
                {
                    LoadVisual();
                }

                // Load decorations if needed
                if (!decorationsLoaded)
                {
                    LoadDecorations();
                }

                gameObject.SetActive(true);
            }
            else
            {
                gameObject.SetActive(false);
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


