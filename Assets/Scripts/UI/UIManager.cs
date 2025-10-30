using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SailboatGame.UI
{
    /// <summary>
    /// Manages UI elements for map selection and game information.
    /// Optional component for enhanced user experience.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameManager gameManager;

        [Header("UI Elements")]
        [SerializeField] private Button map1Button;
        [SerializeField] private Button mazeMapButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI fpsText;

        [Header("Settings")]
        [SerializeField] private bool showFPS = true;
        [SerializeField] private float fpsUpdateInterval = 0.5f;

        private float nextFPSUpdate;
        private int frameCount;

        private void Start()
        {
            // Setup button listeners
            if (map1Button != null)
            {
                map1Button.onClick.AddListener(OnMap1Clicked);
            }

            if (mazeMapButton != null)
            {
                mazeMapButton.onClick.AddListener(OnMazeMapClicked);
            }

            UpdateStatus("Game Ready - Click on water to move boat");
        }

        private void Update()
        {
            if (showFPS && Time.time >= nextFPSUpdate)
            {
                UpdateFPS();
                nextFPSUpdate = Time.time + fpsUpdateInterval;
            }
        }

        /// <summary>
        /// Updates the FPS display.
        /// </summary>
        private void UpdateFPS()
        {
            if (fpsText != null)
            {
                float fps = 1f / Time.smoothDeltaTime;
                fpsText.text = $"FPS: {fps:F0}";

                // Color code based on performance
                if (fps >= 55)
                    fpsText.color = Color.green;
                else if (fps >= 30)
                    fpsText.color = Color.yellow;
                else
                    fpsText.color = Color.red;
            }
        }

        /// <summary>
        /// Updates the status text display.
        /// </summary>
        public void UpdateStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            Debug.Log($"UI: {message}");
        }

        /// <summary>
        /// Handles Map 1 button click.
        /// </summary>
        private void OnMap1Clicked()
        {
            if (gameManager != null)
            {
                UpdateStatus("Loading Map 1...");
                gameManager.LoadMap1();
            }
        }

        /// <summary>
        /// Handles Maze Map button click.
        /// </summary>
        private void OnMazeMapClicked()
        {
            if (gameManager != null)
            {
                UpdateStatus("Loading Maze Map...");
                gameManager.LoadMazeMap();
            }
        }

        /// <summary>
        /// Shows a temporary message.
        /// </summary>
        public async void ShowTemporaryMessage(string message, float duration = 2f)
        {
            UpdateStatus(message);
            await Awaitable.WaitForSecondsAsync(duration);
            UpdateStatus("Ready");
        }

        private void OnDestroy()
        {
            // Cleanup button listeners
            if (map1Button != null)
            {
                map1Button.onClick.RemoveListener(OnMap1Clicked);
            }

            if (mazeMapButton != null)
            {
                mazeMapButton.onClick.RemoveListener(OnMazeMapClicked);
            }
        }
    }
}


