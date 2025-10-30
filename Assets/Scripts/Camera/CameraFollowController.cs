using UnityEngine;

namespace SailboatGame.Camera
{
    /// <summary>
    /// Smoothly follows a target (the boat) with configurable offset and damping.
    /// Optimized for mobile with efficient update loop.
    /// </summary>
    public class CameraFollowController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Follow Settings")]
        [SerializeField] private Vector3 offset = new Vector3(0, 15, -10);
        [SerializeField] private float followSpeed = 5f;
        [SerializeField] private float rotationSpeed = 3f;
        [SerializeField] private bool lookAtTarget = true;

        [Header("Boundaries (Optional)")]
        [SerializeField] private bool useBoundaries = false;
        [SerializeField] private Vector2 minBounds = new Vector2(-100, -100);
        [SerializeField] private Vector2 maxBounds = new Vector2(100, 100);

        [Header("Zoom")]
        [SerializeField] private float minZoom = 5f;
        [SerializeField] private float maxZoom = 30f;
        [SerializeField] private float zoomSpeed = 2f;

        private Vector3 targetPosition;
        private Vector3 velocity = Vector3.zero;
        private float currentZoom;

        private void Start()
        {
            currentZoom = offset.magnitude;
            
            if (target != null)
            {
                // Initialize position immediately
                UpdateTargetPosition();
                transform.position = targetPosition;
            }
        }

        private void LateUpdate()
        {
            if (target == null)
                return;

            UpdateTargetPosition();
            UpdateCameraPosition();
            UpdateCameraRotation();
        }

        /// <summary>
        /// Calculates the desired camera position based on target and offset.
        /// </summary>
        private void UpdateTargetPosition()
        {
            targetPosition = target.position + offset;

            // Apply boundaries if enabled
            if (useBoundaries)
            {
                targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
                targetPosition.z = Mathf.Clamp(targetPosition.z, minBounds.y, maxBounds.y);
            }
        }

        /// <summary>
        /// Smoothly moves camera to target position.
        /// </summary>
        private void UpdateCameraPosition()
        {
            // Use SmoothDamp for smooth following
            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                followSpeed * Time.deltaTime
            );
        }

        /// <summary>
        /// Updates camera rotation to look at target.
        /// </summary>
        private void UpdateCameraRotation()
        {
            if (!lookAtTarget || target == null)
                return;

            Vector3 direction = target.position - transform.position;
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }

        /// <summary>
        /// Sets the target to follow.
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            
            if (target != null)
            {
                UpdateTargetPosition();
            }
        }

        /// <summary>
        /// Sets the camera offset from target.
        /// </summary>
        public void SetOffset(Vector3 newOffset)
        {
            offset = newOffset;
            currentZoom = offset.magnitude;
        }

        /// <summary>
        /// Adjusts the camera zoom (distance from target).
        /// </summary>
        public void AdjustZoom(float delta)
        {
            currentZoom = Mathf.Clamp(currentZoom + delta * zoomSpeed, minZoom, maxZoom);
            offset = offset.normalized * currentZoom;
        }

        /// <summary>
        /// Sets the follow speed.
        /// </summary>
        public void SetFollowSpeed(float speed)
        {
            followSpeed = Mathf.Max(0, speed);
        }

        /// <summary>
        /// Immediately snaps camera to target position (no smoothing).
        /// </summary>
        public void SnapToTarget()
        {
            if (target != null)
            {
                UpdateTargetPosition();
                transform.position = targetPosition;
                
                if (lookAtTarget)
                {
                    transform.LookAt(target);
                }
            }
        }

        /// <summary>
        /// Sets camera boundaries for constrained movement.
        /// </summary>
        public void SetBoundaries(Vector2 min, Vector2 max, bool enable = true)
        {
            minBounds = min;
            maxBounds = max;
            useBoundaries = enable;
        }

        private void OnDrawGizmos()
        {
            // Draw offset ray
            if (target != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(target.position, target.position + offset);
                Gizmos.DrawWireSphere(target.position + offset, 0.5f);
            }

            // Draw boundaries
            if (useBoundaries)
            {
                Gizmos.color = Color.red;
                Vector3 center = new Vector3((minBounds.x + maxBounds.x) / 2f, 0, (minBounds.y + maxBounds.y) / 2f);
                Vector3 size = new Vector3(maxBounds.x - minBounds.x, 1, maxBounds.y - minBounds.y);
                Gizmos.DrawWireCube(center, size);
            }
        }
    }
}


