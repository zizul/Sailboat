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

            if (GUILayout.Button("Create Complete Scene"))
            {
                CreateCompleteScene();
            }

            GUILayout.Space(20);
            GUILayout.Label("Instructions:", EditorStyles.boldLabel);
            GUILayout.Label("1. Click 'Create Complete Scene'", EditorStyles.wordWrappedLabel);
            GUILayout.Label("2. Assign map TextAssets in GameManager", EditorStyles.wordWrappedLabel);
            GUILayout.Label("3. Configure Addressables with prefab keys", EditorStyles.wordWrappedLabel);
            GUILayout.Label("4. Play!", EditorStyles.wordWrappedLabel);
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
            var pathVisualizer = pathVisualizerObj.AddComponent<Visualization.PathVisualizer>();
            
            GameObject performanceOptimizerObj = new GameObject("PerformanceOptimizer");
            performanceOptimizerObj.transform.SetParent(systemsContainer.transform);
            var performanceOptimizer = performanceOptimizerObj.AddComponent<Performance.PerformanceOptimizer>();
            
            // Wire up dependencies using reflection (inspector serialized fields)
            // MapGenerator dependencies
            UnityEditor.SerializedObject mapGenSO = new UnityEditor.SerializedObject(mapGenerator);
            mapGenSO.FindProperty("hexGrid").objectReferenceValue = hexGrid;
            mapGenSO.FindProperty("assetLoader").objectReferenceValue = assetLoader;
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
            pathVisSO.ApplyModifiedProperties();
            
            // PerformanceOptimizer dependencies
            UnityEditor.SerializedObject perfSO = new UnityEditor.SerializedObject(performanceOptimizer);
            perfSO.FindProperty("hexGrid").objectReferenceValue = hexGrid;
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

            Debug.Log("Complete scene setup finished! Remember to assign map assets in GameManager.");
        }
    }
}
#endif


