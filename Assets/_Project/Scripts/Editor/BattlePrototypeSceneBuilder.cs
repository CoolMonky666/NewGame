using System.IO;
using MergeDefense.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MergeDefense.EditorTools
{
    public static class BattlePrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/BattlePrototype.unity";
        private const string Tower1PrefabPath = "Assets/_Project/Prefabs/Towers/base_tower_1.prefab";
        private const string Tower4PrefabPath = "Assets/_Project/Prefabs/Towers/base_tower_4.prefab";
        private const string EnemyPrefabPath = "Assets/_Project/Prefabs/Enemies/base_enemy_1.prefab";
        private const string MaterialFolder = "Assets/_Project/Art/Materials/Prototype";

        [MenuItem("Tools/Merge Defense/Create Battle Prototype Scene")]
        public static void CreateBattlePrototypeScene()
        {
            var tower1Prefab = LoadPrefab(Tower1PrefabPath);
            var tower4Prefab = LoadPrefab(Tower4PrefabPath);
            var enemyPrefab = LoadPrefab(EnemyPrefabPath);
            if (tower1Prefab == null || tower4Prefab == null || enemyPrefab == null)
            {
                return;
            }

            Directory.CreateDirectory(MaterialFolder);
            var boardMaterial = GetOrCreateMaterial("prototype_board_tile", new Color(0.25f, 0.34f, 0.29f, 1f));
            var boardAltMaterial = GetOrCreateMaterial("prototype_board_tile_alt", new Color(0.31f, 0.41f, 0.35f, 1f));
            var pathMaterial = GetOrCreateMaterial("prototype_enemy_path", new Color(0.48f, 0.38f, 0.26f, 1f));
            var castleMaterial = GetOrCreateMaterial("prototype_castle", new Color(0.55f, 0.58f, 0.62f, 1f));
            var markerMaterial = GetOrCreateMaterial("prototype_path_marker", new Color(0.70f, 0.24f, 0.20f, 1f));
            var projectileMaterial = GetOrCreateMaterial("prototype_projectile", new Color(0.95f, 0.84f, 0.28f, 1f));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "BattlePrototype";

            var boardRoot = new GameObject("Board 5x5");
            var board = boardRoot.AddComponent<PrototypeBattleBoard>();
            const float cellSize = 1.6f;
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
                new Vector3(-5.6f, 0.05f, 4.8f),
                new Vector3(-5.6f, 0.05f, -4.8f),
                new Vector3(0f, 0.05f, -4.8f),
                new Vector3(5.6f, 0.05f, -4.8f),
                new Vector3(5.6f, 0.05f, 3.2f),
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
            CreateCastle(waypointPositions[^1] + new Vector3(0f, 0.55f, 1.4f), castleMaterial);

            var towerRoot = new GameObject("Towers");
            InstantiateTower(tower1Prefab, towerRoot.transform, board, GridToWorld(1, 1, cellSize), 35f, "base_tower_1_A", projectileMaterial);
            InstantiateTower(tower1Prefab, towerRoot.transform, board, GridToWorld(3, 2, cellSize), -25f, "base_tower_1_B", projectileMaterial);
            InstantiateTower(tower4Prefab, towerRoot.transform, board, GridToWorld(2, 3, cellSize), 0f, "base_tower_4_A", projectileMaterial);

            var enemyRoot = new GameObject("Enemies");
            for (var i = 0; i < 4; i++)
            {
                var enemy = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab, scene);
                enemy.name = $"base_enemy_1_{i + 1:00}";
                enemy.transform.SetParent(enemyRoot.transform);

                var health = enemy.AddComponent<PrototypeEnemyHealth>();
                health.Configure(3, FindChild(enemy.transform, "HitPoint"));

                var follower = enemy.AddComponent<PrototypePathFollower>();
                follower.Configure(waypoints, 0.85f, i * 2.1f, true);
            }

            CreateLighting();
            CreateCamera();

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
                segment.transform.localScale = new Vector3(1.05f, 0.06f, delta.magnitude + 1.05f);
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

        private static void CreateCastle(Vector3 position, Material material)
        {
            var castle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            castle.name = "Castle Placeholder";
            castle.transform.position = position;
            castle.transform.localScale = new Vector3(1.8f, 1.1f, 1.8f);
            castle.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void InstantiateTower(GameObject prefab, Transform parent, PrototypeBattleBoard board, Vector3 position, float yRotation, string name, Material projectileMaterial)
        {
            var tower = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            tower.name = name;
            tower.transform.SetParent(parent);
            tower.transform.position = position + Vector3.up * board.TowerHeightOffset;
            tower.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);

            var attack = tower.AddComponent<PrototypeTowerAttack>();
            attack.Configure(FindChild(tower.transform, "FirePoint"), 6f, 1f, 1, 7f, projectileMaterial);

            var draggable = tower.AddComponent<PrototypeTowerDraggable>();
            draggable.Configure(board);
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

        private static void CreateCamera()
        {
            var cameraObject = new GameObject("Battle Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 7.2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.09f, 0.11f, 1f);
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 100f;
            cameraObject.transform.position = new Vector3(0f, 9f, -8f);
            cameraObject.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
            Camera.SetupCurrent(camera);
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

