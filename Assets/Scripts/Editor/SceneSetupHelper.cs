#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using SailboatGame;

namespace SailboatGame.Editor
{
    /// <summary>
    /// Editor utility to quickly set up a scene with all required components.
    /// </summary>
    public class SceneSetupHelper : EditorWindow
    {
        [MenuItem("Tools/Sailboat Game/Setup Scene")]
        public static void ShowWindow()
        {
            GetWindow<SceneSetupHelper>("Scene Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Sailboat Game Scene Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("Create Game Manager"))
            {
                CreateGameManager();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Setup Camera"))
            {
                SetupCamera();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Create UI Canvas"))
            {
                CreateUICanvas();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Create Complete Scene"))
            {
                CreateCompleteScene();
            }

            GUILayout.Space(20);
            GUILayout.Label("Instructions:", EditorStyles.boldLabel);
            GUILayout.Label("1. Click 'Create Complete Scene'", EditorStyles.wordWrappedLabel);
            GUILayout.Label("2. Configure Addressables with prefab keys", EditorStyles.wordWrappedLabel);
            GUILayout.Label("3. Play!", EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);
            GUILayout.Label("Auto-configured:", EditorStyles.boldLabel);
            GUILayout.Label("• Map assets (map1.txt, maze.txt)", EditorStyles.wordWrappedLabel);
            GUILayout.Label("• PathVisualizer (line width, height, material)", EditorStyles.wordWrappedLabel);
            GUILayout.Label("• MapGenerator (hex size = 0.58)", EditorStyles.wordWrappedLabel);
        }

        private static void CreateGameManager()
        {
            GameObject gameManagerObj = new GameObject("GameManager");
            var gameManager = gameManagerObj.AddComponent<GameManager>();
            
            // Create container for organization
            GameObject systemsContainer = new GameObject("Systems");
            systemsContainer.transform.SetParent(gameManagerObj.transform);
            
            // Create separate GameObjects for each system
            GameObject hexGridObj = new GameObject("HexGrid");
            hexGridObj.transform.SetParent(systemsContainer.transform);
            var hexGrid = hexGridObj.AddComponent<Core.HexGrid>();
            
            GameObject mapLoaderObj = new GameObject("MapLoader");
            mapLoaderObj.transform.SetParent(systemsContainer.transform);
            var mapLoader = mapLoaderObj.AddComponent<Systems.MapLoader>();
            
            GameObject assetLoaderObj = new GameObject("AssetLoader");
            assetLoaderObj.transform.SetParent(systemsContainer.transform);
            var assetLoader = assetLoaderObj.AddComponent<Systems.AddressableAssetLoader>();
            
            GameObject mapGeneratorObj = new GameObject("MapGenerator");
            mapGeneratorObj.transform.SetParent(systemsContainer.transform);
            var mapGenerator = mapGeneratorObj.AddComponent<Systems.MapGenerator>();
            
            GameObject pathfindingObj = new GameObject("PathfindingSystem");
            pathfindingObj.transform.SetParent(systemsContainer.transform);
            var pathfindingSystem = pathfindingObj.AddComponent<Pathfinding.PathfindingSystem>();
            
            GameObject inputHandlerObj = new GameObject("InputHandler");
            inputHandlerObj.transform.SetParent(systemsContainer.transform);
            var inputHandler = inputHandlerObj.AddComponent<Input.InputHandler>();
            
            GameObject pathVisualizerObj = new GameObject("PathVisualizer");
            pathVisualizerObj.transform.SetParent(systemsContainer.transform);
            pathVisualizerObj.transform.rotation = Quaternion.Euler(90, 0, 0);
            var pathVisualizer = pathVisualizerObj.AddComponent<Visualization.PathVisualizer>();
            
            GameObject performanceOptimizerObj = new GameObject("PerformanceOptimizer");
            performanceOptimizerObj.transform.SetParent(systemsContainer.transform);
            var performanceOptimizer = performanceOptimizerObj.AddComponent<Performance.PerformanceOptimizer>();
            
            // Wire up dependencies using reflection (inspector serialized fields)
            // MapGenerator dependencies
            UnityEditor.SerializedObject mapGenSO = new UnityEditor.SerializedObject(mapGenerator);
            mapGenSO.FindProperty("hexGrid").objectReferenceValue = hexGrid;
            mapGenSO.FindProperty("assetLoader").objectReferenceValue = assetLoader;
            mapGenSO.FindProperty("hexSize").floatValue = 0.58f;
            mapGenSO.ApplyModifiedProperties();
            
            // PathfindingSystem dependencies
            UnityEditor.SerializedObject pathfindingSO = new UnityEditor.SerializedObject(pathfindingSystem);
            pathfindingSO.FindProperty("hexGrid").objectReferenceValue = hexGrid;
            pathfindingSO.ApplyModifiedProperties();
            
            // InputHandler dependencies
            UnityEditor.SerializedObject inputSO = new UnityEditor.SerializedObject(inputHandler);
            inputSO.FindProperty("hexGrid").objectReferenceValue = hexGrid;
            inputSO.ApplyModifiedProperties();
            
            // PathVisualizer dependencies
            UnityEditor.SerializedObject pathVisSO = new UnityEditor.SerializedObject(pathVisualizer);
            pathVisSO.FindProperty("hexGrid").objectReferenceValue = hexGrid;
            pathVisSO.FindProperty("lineWidth").floatValue = 0.07f;
            pathVisSO.FindProperty("lineHeightOffset").floatValue = 0f;
            
            // Load Default-Line material
            Material defaultLineMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Line.mat");
            if (defaultLineMaterial != null)
            {
                pathVisSO.FindProperty("lineMaterial").objectReferenceValue = defaultLineMaterial;
            }
            else
            {
                Debug.LogWarning("Default-Line material not found. PathVisualizer lineMaterial not set.");
            }
            
            pathVisSO.ApplyModifiedProperties();
            
            // PerformanceOptimizer dependencies
            UnityEditor.SerializedObject perfSO = new UnityEditor.SerializedObject(performanceOptimizer);
            perfSO.FindProperty("hexGrid").objectReferenceValue = hexGrid;
            perfSO.FindProperty("gameManager").objectReferenceValue = gameManager;
            perfSO.ApplyModifiedProperties();
            
            // GameManager dependencies
            UnityEditor.SerializedObject gmSO = new UnityEditor.SerializedObject(gameManager);
            gmSO.FindProperty("hexGrid").objectReferenceValue = hexGrid;
            gmSO.FindProperty("mapLoader").objectReferenceValue = mapLoader;
            gmSO.FindProperty("mapGenerator").objectReferenceValue = mapGenerator;
            gmSO.FindProperty("assetLoader").objectReferenceValue = assetLoader;
            gmSO.FindProperty("pathfindingSystem").objectReferenceValue = pathfindingSystem;
            gmSO.FindProperty("inputHandler").objectReferenceValue = inputHandler;
            gmSO.FindProperty("pathVisualizer").objectReferenceValue = pathVisualizer;
            
            // Load and assign map assets
            TextAsset map1 = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Maps/map1.txt");
            TextAsset maze = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Maps/maze.txt");
            
            if (map1 != null && maze != null)
            {
                UnityEditor.SerializedProperty mapAssetsProperty = gmSO.FindProperty("mapAssets");
                mapAssetsProperty.arraySize = 2;
                mapAssetsProperty.GetArrayElementAtIndex(0).objectReferenceValue = map1;
                mapAssetsProperty.GetArrayElementAtIndex(1).objectReferenceValue = maze;
                Debug.Log("Map assets (map1.txt and maze.txt) assigned to GameManager.");
            }
            else
            {
                Debug.LogWarning("Could not find map1.txt or maze.txt in Assets/Maps/. Please assign manually.");
            }
            
            gmSO.ApplyModifiedProperties();

            Selection.activeGameObject = gameManagerObj;
            Debug.Log("GameManager created with all systems on separate GameObjects and dependencies wired!");
        }

        private static void SetupCamera()
        {
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<UnityEngine.Camera>();
                camObj.tag = "MainCamera";
                camObj.AddComponent<AudioListener>();
            }

            // Position camera for good view
            cam.transform.position = new Vector3(0, 20, -15);
            cam.transform.rotation = Quaternion.Euler(45, 0, 0);
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.farClipPlane = 500;

            // Add camera follow controller
            if (cam.GetComponent<Camera.CameraFollowController>() == null)
            {
                cam.gameObject.AddComponent<Camera.CameraFollowController>();
            }

            Debug.Log("Camera setup complete!");
        }

        private static void CreateCompleteScene()
        {
            // Clear existing
            GameObject[] existingObjects = GameObject.FindObjectsOfType<GameObject>();
            foreach (var obj in existingObjects)
            {
                if (obj.transform.parent == null && obj.name != "Main Camera")
                {
                    DestroyImmediate(obj);
                }
            }

            // Create game manager
            CreateGameManager();

            // Setup camera
            SetupCamera();

            // Create directional light
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            light.shadows = LightShadows.Soft;
            lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);

            // Create EventSystem for UI
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // Create UI Canvas with loading panel
            CreateUICanvas();

            Debug.Log("Complete scene setup finished! Maps, PathVisualizer settings, MapGenerator hex size, and UI canvas with loading panel auto-configured.");
        }

        private static void CreateUICanvas()
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("Canvas");
            UnityEngine.Canvas canvas = canvasObj.AddComponent<UnityEngine.Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // Ensure loading panel is on top
            
            UnityEngine.UI.CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Add UIManager component
            UI.UIManager uiManager = canvasObj.AddComponent<UI.UIManager>();

            // Create Loading Panel (full screen overlay)
            GameObject loadingPanel = new GameObject("LoadingPanel");
            loadingPanel.transform.SetParent(canvasObj.transform, false);
            
            UnityEngine.UI.Image panelBg = loadingPanel.AddComponent<UnityEngine.UI.Image>();
            panelBg.color = new Color(0, 0, 0, 0.95f); // Almost black background
            
            RectTransform panelRect = loadingPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Create content container centered in panel
            GameObject contentContainer = new GameObject("Content");
            contentContainer.transform.SetParent(loadingPanel.transform, false);
            RectTransform contentRect = contentContainer.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(600, 200);
            contentRect.anchoredPosition = Vector2.zero;

            // Create "Loading..." Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(contentContainer.transform, false);
            TMPro.TextMeshProUGUI titleText = titleObj.AddComponent<TMPro.TextMeshProUGUI>();
            titleText.text = "Loading...";
            titleText.fontSize = 48;
            titleText.alignment = TMPro.TextAlignmentOptions.Center;
            titleText.color = Color.white;
            
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.7f);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            // Create Progress Bar Background
            GameObject progressBgObj = new GameObject("ProgressBarBg");
            progressBgObj.transform.SetParent(contentContainer.transform, false);
            UnityEngine.UI.Image progressBg = progressBgObj.AddComponent<UnityEngine.UI.Image>();
            progressBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            RectTransform progressBgRect = progressBgObj.GetComponent<RectTransform>();
            progressBgRect.anchorMin = new Vector2(0, 0.4f);
            progressBgRect.anchorMax = new Vector2(1, 0.6f);
            progressBgRect.offsetMin = new Vector2(20, 0);
            progressBgRect.offsetMax = new Vector2(-20, 0);

            // Create Progress Bar (Slider)
            GameObject sliderObj = new GameObject("ProgressSlider");
            sliderObj.transform.SetParent(progressBgObj.transform, false);
            UnityEngine.UI.Slider slider = sliderObj.AddComponent<UnityEngine.UI.Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.transition = UnityEngine.UI.Selectable.Transition.None;
            
            RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchorMin = Vector2.zero;
            sliderRect.anchorMax = Vector2.one;
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;

            // Create Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            // Create Fill (the actual progress bar)
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            UnityEngine.UI.Image fillImage = fill.AddComponent<UnityEngine.UI.Image>();
            fillImage.color = new Color(0, 0.8f, 0, 1f); // Green progress bar
            
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            slider.fillRect = fillRect;

            // Create Progress Text (percentage)
            GameObject progressTextObj = new GameObject("ProgressText");
            progressTextObj.transform.SetParent(contentContainer.transform, false);
            TMPro.TextMeshProUGUI progressText = progressTextObj.AddComponent<TMPro.TextMeshProUGUI>();
            progressText.text = "0%";
            progressText.fontSize = 32;
            progressText.alignment = TMPro.TextAlignmentOptions.Center;
            progressText.color = Color.white;
            
            RectTransform progressTextRect = progressTextObj.GetComponent<RectTransform>();
            progressTextRect.anchorMin = new Vector2(0, 0.2f);
            progressTextRect.anchorMax = new Vector2(1, 0.4f);
            progressTextRect.offsetMin = Vector2.zero;
            progressTextRect.offsetMax = Vector2.zero;

            // Create Stage Text (loading stage description)
            GameObject stageTextObj = new GameObject("StageText");
            stageTextObj.transform.SetParent(contentContainer.transform, false);
            TMPro.TextMeshProUGUI stageText = stageTextObj.AddComponent<TMPro.TextMeshProUGUI>();
            stageText.text = "Initializing...";
            stageText.fontSize = 24;
            stageText.alignment = TMPro.TextAlignmentOptions.Center;
            stageText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            
            RectTransform stageTextRect = stageTextObj.GetComponent<RectTransform>();
            stageTextRect.anchorMin = new Vector2(0, 0);
            stageTextRect.anchorMax = new Vector2(1, 0.2f);
            stageTextRect.offsetMin = Vector2.zero;
            stageTextRect.offsetMax = Vector2.zero;

            // Wire up UIManager references
            UnityEditor.SerializedObject uiSO = new UnityEditor.SerializedObject(uiManager);
            uiSO.FindProperty("loadingPanel").objectReferenceValue = loadingPanel;
            uiSO.FindProperty("progressBar").objectReferenceValue = slider;
            uiSO.FindProperty("progressText").objectReferenceValue = progressText;
            uiSO.FindProperty("loadingStageText").objectReferenceValue = stageText;
            
            // Wire up GameManager reference to UIManager (for event subscription)
            GameObject gameManagerObj = GameObject.Find("GameManager");
            if (gameManagerObj != null)
            {
                var gameManager = gameManagerObj.GetComponent<GameManager>();
                if (gameManager != null)
                {
                    uiSO.FindProperty("gameManager").objectReferenceValue = gameManager;
                    Debug.Log("GameManager wired to UIManager successfully!");
                }
            }
            
            uiSO.ApplyModifiedProperties();

            // Initially hide the loading panel (GameManager will show it when needed)
            loadingPanel.SetActive(false);

            Debug.Log("UI Canvas with loading panel created and configured!");
        }
    }
}
#endif


