using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SailboatGame.UI
{
    /// <summary>
    /// Manages UI elements for map selection and game information.
    /// Includes loading progress tracking for game initialization.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameManager gameManager;

        [Header("Status")]
        [SerializeField] private Button map1Button;
        [SerializeField] private Button mazeMapButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI fpsText;
        [SerializeField] private GameObject menuPanel;

        [Header("Loading UI")]
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI loadingStageText;

        [Header("Settings")]
        [SerializeField] private bool showFPS = true;
        [SerializeField] private float fpsUpdateInterval = 0.5f;

        private float nextFPSUpdate;
        private int frameCount;
        private float currentProgress;
        private string currentStage;

        private void Awake()
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

            // Initialize loading UI
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }

            // Subscribe to GameManager initialization events
            if (gameManager != null)
            {
                gameManager.OnInitializationStarted += HandleInitializationStarted;
                gameManager.OnInitializationProgress += HandleInitializationProgress;
                gameManager.OnInitializationCompleted += HandleInitializationCompleted;
                gameManager.OnInitializationFailed += HandleInitializationFailed;
            }
            else
            {
                Debug.LogWarning("UIManager: GameManager reference is not set. Loading progress will not be displayed.");
            }
        }

        private void Update()
        {
            if (showFPS && Time.time >= nextFPSUpdate)
            {
                UpdateFPS();
                nextFPSUpdate = Time.time + fpsUpdateInterval;
            }

            // Smoothly animate progress bar
            if (progressBar != null && loadingPanel != null && loadingPanel.activeSelf)
            {
                float targetValue = currentProgress / 100f;
                progressBar.value = Mathf.Lerp(progressBar.value, targetValue, Time.deltaTime * 5f);
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

        /// <summary>
        /// Event handler for initialization started.
        /// </summary>
        private void HandleInitializationStarted()
        {
            Debug.Log("UIManager: Initialization started");
            ShowLoadingPanel();
            menuPanel.SetActive(false);
        }

        /// <summary>
        /// Event handler for initialization progress updates.
        /// </summary>
        /// <param name="progress">Progress value (0-1)</param>
        /// <param name="stage">Current loading stage description</param>
        private void HandleInitializationProgress(float progress, string stage)
        {
            float progressPercentage = progress * 100f;
            UpdateLoadingProgress(progressPercentage, stage);
        }

        /// <summary>
        /// Event handler for initialization completed.
        /// </summary>
        private void HandleInitializationCompleted()
        {
            Debug.Log("UIManager: Initialization completed");
            HideLoadingPanel();
            UpdateStatus("Game Ready - Click on water to move boat");
            menuPanel.SetActive(true);
        }

        /// <summary>
        /// Event handler for initialization failed.
        /// </summary>
        /// <param name="errorMessage">Error message describing the failure</param>
        private void HandleInitializationFailed(string errorMessage)
        {
            Debug.LogError($"UIManager: Initialization failed - {errorMessage}");

            menuPanel.SetActive(true);

            // Show error message in loading stage text
            if (loadingStageText != null)
            {
                loadingStageText.text = $"ERROR: {errorMessage}";
                loadingStageText.color = Color.red;
            }
            
            // Keep panel visible to show error
            // User can check console for details
        }

        /// <summary>
        /// Shows the loading panel and initializes progress.
        /// </summary>
        private void ShowLoadingPanel()
        {
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(true);
                currentProgress = 0f;
                
                if (progressBar != null)
                {
                    progressBar.value = 0f;
                }
                
                UpdateLoadingProgress(0f, "Initializing...");
            }
        }

        /// <summary>
        /// Hides the loading panel.
        /// </summary>
        private void HideLoadingPanel()
        {
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Updates the loading progress bar and text.
        /// </summary>
        /// <param name="progress">Progress percentage (0-100)</param>
        /// <param name="stage">Current loading stage description</param>
        private void UpdateLoadingProgress(float progress, string stage)
        {
            currentProgress = Mathf.Clamp(progress, 0f, 100f);
            currentStage = stage;

            if (progressText != null)
            {
                progressText.text = $"{currentProgress:F0}%";
            }

            if (loadingStageText != null)
            {
                loadingStageText.text = stage;
                // Reset color in case it was red from previous error
                loadingStageText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            }

            Debug.Log($"UIManager: Loading progress: {currentProgress:F1}% - {stage}");
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

            // Unsubscribe from GameManager events
            if (gameManager != null)
            {
                gameManager.OnInitializationStarted -= HandleInitializationStarted;
                gameManager.OnInitializationProgress -= HandleInitializationProgress;
                gameManager.OnInitializationCompleted -= HandleInitializationCompleted;
                gameManager.OnInitializationFailed -= HandleInitializationFailed;
            }
        }
    }
}


