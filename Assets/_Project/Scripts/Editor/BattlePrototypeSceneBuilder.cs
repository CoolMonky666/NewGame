using System.IO;
using MergeDefense.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MergeDefense.EditorTools
{
    public static class BattlePrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/BattlePrototype.unity";
        private const string Tower1PrefabPath = "Assets/_Project/Prefabs/Towers/base_tower_1.prefab";
        private const string Tower4PrefabPath = "Assets/_Project/Prefabs/Towers/base_tower_4.prefab";
        private const string EnemyPrefabPath = "Assets/_Project/Prefabs/Enemies/base_enemy_1.prefab";
        private const string CastlePrefabPath = "Assets/_Project/Prefabs/Environment/base_castle_1.prefab";
        private const string MaterialFolder = "Assets/_Project/Art/Materials/Prototype";

        [MenuItem("Tools/Merge Defense/Create Battle Prototype Scene")]
        public static void CreateBattlePrototypeScene()
        {
            var tower1Prefab = LoadPrefab(Tower1PrefabPath);
            var tower4Prefab = LoadPrefab(Tower4PrefabPath);
            var enemyPrefab = LoadPrefab(EnemyPrefabPath);
            var castlePrefab = LoadPrefab(CastlePrefabPath);
            if (tower1Prefab == null || tower4Prefab == null || enemyPrefab == null || castlePrefab == null)
            {
                return;
            }

            Directory.CreateDirectory(MaterialFolder);
            var boardMaterial = GetOrCreateMaterial("prototype_board_tile", new Color(0.25f, 0.34f, 0.29f, 1f));
            var boardAltMaterial = GetOrCreateMaterial("prototype_board_tile_alt", new Color(0.31f, 0.41f, 0.35f, 1f));
            var pathMaterial = GetOrCreateMaterial("prototype_enemy_path", new Color(0.48f, 0.38f, 0.26f, 1f));
            var markerMaterial = GetOrCreateMaterial("prototype_path_marker", new Color(0.70f, 0.24f, 0.20f, 1f));
            var projectileMaterial = GetOrCreateMaterial("prototype_projectile", new Color(0.95f, 0.84f, 0.28f, 1f));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "BattlePrototype";

            var boardRoot = new GameObject("Board 5x5");
            const float cellSize = 1.2f;
            var board = boardRoot.AddComponent<PrototypeBattleBoard>();
            board.Configure(5, cellSize, 0.08f);
            for (var x = 0; x < 5; x++)
            {
                for (var z = 0; z < 5; z++)
                {
                    var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = $"Cell_{x}_{z}";
                    tile.transform.SetParent(boardRoot.transform);
                    tile.transform.position = GridToWorld(x, z, cellSize);
                    tile.transform.localScale = new Vector3(cellSize * 0.94f, 0.08f, cellSize * 0.94f);
                    tile.GetComponent<Renderer>().sharedMaterial = (x + z) % 2 == 0 ? boardMaterial : boardAltMaterial;
                }
            }

            var pathRoot = new GameObject("Enemy Path");
            var waypointPositions = new[]
            {
                new Vector3(-3.45f, 0.05f, 4.4f),
                new Vector3(-3.45f, 0.05f, -4.4f),
                new Vector3(0f, 0.05f, -4.4f),
                new Vector3(3.45f, 0.05f, -4.4f),
                new Vector3(3.45f, 0.05f, 4.4f),
            };

            var waypoints = new Transform[waypointPositions.Length];
            for (var i = 0; i < waypointPositions.Length; i++)
            {
                var waypoint = new GameObject($"Waypoint_{i + 1:00}");
                waypoint.transform.SetParent(pathRoot.transform);
                waypoint.transform.position = waypointPositions[i];
                waypoints[i] = waypoint.transform;
            }

            CreatePathTiles(pathRoot.transform, waypointPositions, pathMaterial);
            CreatePathMarker("Spawn Marker", waypointPositions[0], markerMaterial);
            CreateCastle(castlePrefab, waypointPositions[^1] + new Vector3(0f, 0f, 0.85f));

            var towerRoot = new GameObject("Towers");

            var enemyRoot = new GameObject("Enemies");
            var enemySpawner = new GameObject("Enemy Spawner").AddComponent<PrototypeEnemySpawner>();
            enemySpawner.Configure(enemyPrefab, enemyRoot.transform, waypoints, 10, 1.35f, 0.85f, 3, false);

            CreateLighting();
            CreateCamera();
            CreateBattleUi(board, towerRoot.transform, new[] { tower1Prefab, tower4Prefab }, projectileMaterial);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.OpenScene(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Battle prototype scene created at {ScenePath}.");
        }

        private static GameObject LoadPrefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"Battle prototype scene skipped: prefab not found at {path}.");
            }

            return prefab;
        }

        private static Material GetOrCreateMaterial(string materialName, Color color)
        {
            var materialPath = $"{MaterialFolder}/{materialName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader)
                {
                    name = materialName
                };
                AssetDatabase.CreateAsset(material, materialPath);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Vector3 GridToWorld(int x, int z, float cellSize)
        {
            return new Vector3((x - 2) * cellSize, 0f, (z - 2) * cellSize);
        }

        private static void CreatePathTiles(Transform root, Vector3[] waypoints, Material material)
        {
            for (var i = 0; i < waypoints.Length - 1; i++)
            {
                var start = waypoints[i];
                var end = waypoints[i + 1];
                var center = (start + end) * 0.5f;
                var delta = end - start;
                var segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                segment.name = $"PathSegment_{i + 1:00}";
                segment.transform.SetParent(root);
                segment.transform.position = center;
                segment.transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
                segment.transform.localScale = new Vector3(0.9f, 0.06f, delta.magnitude + 0.9f);
                segment.GetComponent<Renderer>().sharedMaterial = material;
            }
        }

        private static void CreatePathMarker(string name, Vector3 position, Material material)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = name;
            marker.transform.position = position + Vector3.up * 0.12f;
            marker.transform.localScale = new Vector3(0.45f, 0.06f, 0.45f);
            marker.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void CreateCastle(GameObject castlePrefab, Vector3 position)
        {
            var castle = (GameObject)PrefabUtility.InstantiatePrefab(castlePrefab);
            castle.name = "Castle";
            castle.transform.position = position;
            castle.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }

        private static void CreateLighting()
        {
            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.62f, 1f);

            var lightObject = new GameObject("Battle Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
        }

        private static Camera CreateCamera()
        {
            var cameraObject = new GameObject("Battle Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 7.4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.09f, 0.11f, 1f);
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 100f;
            cameraObject.transform.position = new Vector3(0f, 9.2f, -5.75f);
            cameraObject.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
            Camera.SetupCurrent(camera);

            var dragController = new GameObject("Tower Drag Controller").AddComponent<PrototypeTowerDragController>();
            dragController.Configure(camera);
            return camera;
        }

        private static void CreateBattleUi(PrototypeBattleBoard board, Transform towerRoot, GameObject[] towerPrefabs, Material projectileMaterial)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            var canvasObject = new GameObject("Battle UI");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<GraphicRaycaster>();

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 1f;

            var panel = new GameObject("Top Resource Panel");
            panel.transform.SetParent(canvasObject.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = new Vector2(0f, -56f);
            panelRect.sizeDelta = new Vector2(880f, 172f);
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.08f, 0.10f, 0.12f, 0.82f);

            var coinText = CreateText("Coin Counter", panel.transform, font, "Coins: 0", 44, TextAnchor.MiddleLeft, new Color(1f, 0.86f, 0.32f, 1f));
            var coinRect = coinText.GetComponent<RectTransform>();
            coinRect.anchorMin = new Vector2(0f, 0.5f);
            coinRect.anchorMax = new Vector2(0f, 0.5f);
            coinRect.pivot = new Vector2(0f, 0.5f);
            coinRect.anchoredPosition = new Vector2(38f, 0f);
            coinRect.sizeDelta = new Vector2(330f, 92f);

            var buttonObject = new GameObject("Summon Tower Button");
            buttonObject.transform.SetParent(panel.transform, false);
            var buttonRect = buttonObject.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(1f, 0.5f);
            buttonRect.anchorMax = new Vector2(1f, 0.5f);
            buttonRect.pivot = new Vector2(1f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(-34f, 0f);
            buttonRect.sizeDelta = new Vector2(420f, 108f);
            var buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.24f, 0.48f, 0.78f, 1f);
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;

            var buttonText = CreateText("Button Label", buttonObject.transform, font, "Summon - 2", 38, TextAnchor.MiddleCenter, Color.white);
            var buttonTextRect = buttonText.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = Vector2.zero;
            buttonTextRect.offsetMax = Vector2.zero;

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            var uiInputModule = eventSystem.AddComponent<InputSystemUIInputModule>();
            uiInputModule.AssignDefaultActions();

            var summonController = new GameObject("Tower Summon Controller").AddComponent<PrototypeTowerSummonController>();
            summonController.Configure(board, towerRoot, towerPrefabs, projectileMaterial, coinText, button);
        }

        private static Text CreateText(string name, Transform parent, Font font, string text, int fontSize, TextAnchor alignment, Color color)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            var textComponent = textObject.AddComponent<Text>();
            textComponent.font = font;
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.alignment = alignment;
            textComponent.color = color;
            textComponent.raycastTarget = false;
            return textComponent;
        }

        private static Transform FindChild(Transform root, string childName)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName)
                {
                    return child;
                }
            }

            return root;
        }
    }
}

