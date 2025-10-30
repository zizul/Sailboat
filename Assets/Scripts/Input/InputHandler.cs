using System;
using UnityEngine;
using UnityEngine.EventSystems;
using SailboatGame.Core;

namespace SailboatGame.Input
{
    /// <summary>
    /// Handles input for both PC (mouse) and mobile (touch) platforms.
    /// Provides unified interface for map interaction.
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UnityEngine.Camera mainCamera;
        [SerializeField] private HexGrid hexGrid;
        [SerializeField] private LayerMask interactionLayerMask = -1;

        [Header("Settings")]
        [SerializeField] private float raycastDistance = 1000f;
        [SerializeField] private bool blockInputOverUI = true;

        public event Action<HexCoordinates> OnTileClicked;
        public event Action<Vector3> OnWorldPositionClicked;

        private void Awake()
        {
            if (mainCamera == null)
            {
                mainCamera = UnityEngine.Camera.main;
            }
        }

        private void Update()
        {
            HandleInput();
        }

        /// <summary>
        /// Processes input from mouse or touch.
        /// </summary>
        private void HandleInput()
        {
            // Check if we should block input (e.g., over UI)
            if (blockInputOverUI && IsPointerOverUI())
            {
                return;
            }

            // Handle touch input (mobile)
            if (UnityEngine.Input.touchCount > 0)
            {
                Touch touch = UnityEngine.Input.GetTouch(0);
                
                if (touch.phase == TouchPhase.Began)
                {
                    ProcessClick(touch.position);
                }
            }
            // Handle mouse input (PC)
            else if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                ProcessClick(UnityEngine.Input.mousePosition);
            }
        }

        /// <summary>
        /// Processes a click/tap at screen position.
        /// </summary>
        private void ProcessClick(Vector2 screenPosition)
        {
            if (mainCamera == null) return;

            Ray ray = mainCamera.ScreenPointToRay(screenPosition);
            
            // Raycast to find clicked position
            if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, interactionLayerMask))
            {
                Vector3 worldPos = hit.point;
                OnWorldPositionClicked?.Invoke(worldPos);

                // Convert to hex coordinates if grid available
                if (hexGrid != null)
                {
                    HexCoordinates hexCoords = hexGrid.WorldToHex(worldPos);
                    
                    // Validate it's a valid tile
                    if (hexGrid.HasTile(hexCoords))
                    {
                        OnTileClicked?.Invoke(hexCoords);
                        Debug.Log($"InputHandler: Clicked tile {hexCoords}");
                    }
                }
            }
            else
            {
                // Raycast against XZ plane if no object hit
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                if (groundPlane.Raycast(ray, out float distance))
                {
                    Vector3 worldPos = ray.GetPoint(distance);
                    OnWorldPositionClicked?.Invoke(worldPos);

                    if (hexGrid != null)
                    {
                        HexCoordinates hexCoords = hexGrid.WorldToHex(worldPos);
                        if (hexGrid.HasTile(hexCoords))
                        {
                            OnTileClicked?.Invoke(hexCoords);
                            Debug.Log($"InputHandler: Clicked tile {hexCoords} (plane intersection)");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks if pointer is over UI element.
        /// </summary>
        private bool IsPointerOverUI()
        {
            if (EventSystem.current == null)
                return false;

            // Check for touch
            if (UnityEngine.Input.touchCount > 0)
            {
                return EventSystem.current.IsPointerOverGameObject(UnityEngine.Input.GetTouch(0).fingerId);
            }
            // Check for mouse
            else
            {
                return EventSystem.current.IsPointerOverGameObject();
            }
        }

        /// <summary>
        /// Enables or disables input handling.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            this.enabled = enabled;
        }
    }
}


