using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MergeDefense.EditorTools
{
    public static class PreviewSceneBuilder
    {
        private const string EnemyPrefabPath = "Assets/_Project/Prefabs/Enemies/base_enemy_1.prefab";
        private const string ScenePath = "Assets/_Project/Scenes/EnemyAnimationPreview.unity";

        [MenuItem("Tools/Merge Defense/Create Enemy Animation Preview Scene")]
        public static void CreateEnemyAnimationPreviewScene()
        {
            var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
            if (enemyPrefab == null)
            {
                Debug.LogError($"Enemy preview scene skipped: prefab not found at {EnemyPrefabPath}.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "EnemyAnimationPreview";

            var enemy = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab, scene);
            enemy.name = "base_enemy_1_preview";
            enemy.transform.position = Vector3.zero;
            enemy.transform.rotation = Quaternion.identity;

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Preview Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(0.35f, 1f, 0.35f);

            var lightObject = new GameObject("Preview Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

            var cameraObject = new GameObject("Preview Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 1.8f;
            cameraObject.transform.position = new Vector3(0f, 1.8f, -3.4f);
            cameraObject.transform.LookAt(new Vector3(0f, 0.65f, 0f));
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.09f, 0.1f, 1f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 50f;
            Camera.SetupCurrent(camera);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.OpenScene(ScenePath);
            Selection.activeGameObject = enemy;
            EditorGUIUtility.PingObject(enemy);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Enemy animation preview scene created at {ScenePath}.");
        }
    }
}
