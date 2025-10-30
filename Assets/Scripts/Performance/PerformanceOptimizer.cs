using UnityEngine;
using System.Collections.Generic;
using SailboatGame.Core;

namespace SailboatGame.Performance
{
    /// <summary>
    /// Manages performance optimizations including LOD, culling, and memory management.
    /// Critical for mobile device performance.
    /// </summary>
    public class PerformanceOptimizer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HexGrid hexGrid;
        [SerializeField] private Transform cameraTransform;

        [Header("Culling Settings")]
        [SerializeField] private bool enableDistanceCulling = true;
        [SerializeField] private float cullingDistance = 50f;
        [SerializeField] private float cullingCheckInterval = 0.5f;

        [Header("LOD Settings")]
        [SerializeField] private bool enableLOD = true;
        [SerializeField] private float lodDistance1 = 20f;
        [SerializeField] private float lodDistance2 = 40f;

        [Header("Memory Management")]
        [SerializeField] private bool enableMemoryOptimization = true;
        [SerializeField] private float memoryCleanupInterval = 30f;

        private float nextCullingCheck;
        private float nextMemoryCleanup;
        private HashSet<GameObject> culledObjects = new HashSet<GameObject>();

        private void Start()
        {
            if (cameraTransform == null && UnityEngine.Camera.main != null)
            {
                cameraTransform = UnityEngine.Camera.main.transform;
            }

            // Set quality settings for mobile
            OptimizeQualitySettings();
        }

        private void Update()
        {
            float currentTime = Time.time;

            // Distance culling
            if (enableDistanceCulling && currentTime >= nextCullingCheck)
            {
                nextCullingCheck = currentTime + cullingCheckInterval;
                PerformDistanceCulling();
            }

            // Memory cleanup
            if (enableMemoryOptimization && currentTime >= nextMemoryCleanup)
            {
                nextMemoryCleanup = currentTime + memoryCleanupInterval;
                PerformMemoryCleanup();
            }
        }

        /// <summary>
        /// Optimizes Unity quality settings for mobile performance.
        /// </summary>
        private void OptimizeQualitySettings()
        {
#if UNITY_ANDROID || UNITY_IOS
            // Mobile-specific optimizations
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.shadowResolution = ShadowResolution.Low;
            QualitySettings.shadowDistance = 30f;
            QualitySettings.shadowCascades = 0;
            
            // Pixel light count
            QualitySettings.pixelLightCount = 1;
            
            // Texture quality
            QualitySettings.masterTextureLimit = 1; // Half resolution
            
            // Anisotropic filtering
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            
            // Anti-aliasing
            QualitySettings.antiAliasing = 0;
            
            // VSync
            QualitySettings.vSyncCount = 0;
            
            // Target frame rate
            Application.targetFrameRate = 60;
            
            Debug.Log("PerformanceOptimizer: Mobile quality settings applied");
#else
            // Desktop settings
            QualitySettings.shadows = ShadowQuality.HardOnly;
            QualitySettings.shadowResolution = ShadowResolution.Medium;
            QualitySettings.pixelLightCount = 2;
            Application.targetFrameRate = -1; // Unlimited
            
            Debug.Log("PerformanceOptimizer: Desktop quality settings applied");
#endif
        }

        /// <summary>
        /// Culls objects based on distance from camera.
        /// Reduces draw calls and improves performance.
        /// </summary>
        private void PerformDistanceCulling()
        {
            if (hexGrid == null || cameraTransform == null)
                return;

            Vector3 cameraPos = cameraTransform.position;
            float cullingDistanceSqr = cullingDistance * cullingDistance;

            foreach (var tile in hexGrid.GetAllTiles())
            {
                if (tile == null) continue;

                float distanceSqr = (tile.transform.position - cameraPos).sqrMagnitude;
                bool shouldBeActive = distanceSqr <= cullingDistanceSqr;

                if (tile.gameObject.activeSelf != shouldBeActive)
                {
                    tile.gameObject.SetActive(shouldBeActive);
                    
                    if (!shouldBeActive)
                        culledObjects.Add(tile.gameObject);
                    else
                        culledObjects.Remove(tile.gameObject);
                }
            }
        }

        /// <summary>
        /// Performs memory cleanup to reduce memory pressure.
        /// </summary>
        private void PerformMemoryCleanup()
        {
            // Unload unused assets
            Resources.UnloadUnusedAssets();

            // Force garbage collection on mobile (use sparingly)
#if UNITY_ANDROID || UNITY_IOS
            if (SystemInfo.systemMemorySize < 4096) // Less than 4GB RAM
            {
                System.GC.Collect();
            }
#endif

            Debug.Log("PerformanceOptimizer: Memory cleanup performed");
        }

        /// <summary>
        /// Enables or disables specific decorations based on performance profile.
        /// </summary>
        public void SetDecorationQuality(int quality)
        {
            if (hexGrid == null) return;

            foreach (var tile in hexGrid.GetAllTiles())
            {
                if (tile == null || tile.TileType != TileType.Terrain)
                    continue;

                var decorations = tile.GetComponentsInChildren<Transform>();
                
                switch (quality)
                {
                    case 0: // Low - disable most decorations
                        foreach (var decoration in decorations)
                        {
                            if (decoration != tile.transform && decoration.name.Contains("grass"))
                            {
                                decoration.gameObject.SetActive(false);
                            }
                        }
                        break;
                    
                    case 1: // Medium - some decorations
                        // Keep major decorations only
                        break;
                    
                    case 2: // High - all decorations
                        foreach (var decoration in decorations)
                        {
                            decoration.gameObject.SetActive(true);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Gets current performance statistics.
        /// </summary>
        public PerformanceStats GetStats()
        {
            return new PerformanceStats
            {
                FPS = 1f / Time.smoothDeltaTime,
                CulledObjectCount = culledObjects.Count,
                MemoryUsageMB = System.GC.GetTotalMemory(false) / (1024f * 1024f),
                DrawCalls = UnityEngine.Rendering.DebugManager.instance != null ? 0 : 0 // Placeholder
            };
        }

        public struct PerformanceStats
        {
            public float FPS;
            public int CulledObjectCount;
            public float MemoryUsageMB;
            public int DrawCalls;
        }

        private void OnDrawGizmos()
        {
            if (enableDistanceCulling && cameraTransform != null)
            {
                // Draw culling sphere
                Gizmos.color = new Color(1, 1, 0, 0.3f);
                Gizmos.DrawWireSphere(cameraTransform.position, cullingDistance);
            }
        }
    }
}


