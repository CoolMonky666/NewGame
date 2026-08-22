using System.IO;
using UnityEditor;
using UnityEngine;

namespace MergeDefense.EditorTools
{
    [InitializeOnLoad]
    public static class TowerPrefabBuilder
    {
        private const string MaterialFolder = "Assets/_Project/Art/Materials/Towers";
        private const string PrefabFolder = "Assets/_Project/Prefabs/Towers";

        private static readonly TowerAssetDefinition[] Towers =
        {
            new("base_tower_1", "Assets/_Project/Art/Models/Towers/base_tower_1", 1.4f, 1.8f, new Vector3(-90f, 0f, 0f)),
            new("base_tower_4", "Assets/_Project/Art/Models/Towers/base_tower_4", 1.4f, 1.8f, new Vector3(-90f, 0f, 0f)),
        };

        static TowerPrefabBuilder()
        {
            EditorApplication.delayCall += BuildIfNeeded;
        }

        [MenuItem("Tools/Merge Defense/Rebuild All Tower Prefabs")]
        public static void RebuildAllTowerPrefabs()
        {
            foreach (var tower in Towers)
            {
                BuildTowerPrefab(tower, force: true);
            }
        }

        [MenuItem("Tools/Merge Defense/Rebuild Base Tower 1 Prefab")]
        public static void RebuildBaseTower1Prefab()
        {
            BuildTowerPrefab(Towers[0], force: true);
        }

        [MenuItem("Tools/Merge Defense/Rebuild Base Tower 4 Prefab")]
        public static void RebuildBaseTower4Prefab()
        {
            BuildTowerPrefab(Towers[1], force: true);
        }

        private static void BuildIfNeeded()
        {
            foreach (var tower in Towers)
            {
                BuildTowerPrefab(tower, force: false);
            }
        }

        private static void BuildTowerPrefab(TowerAssetDefinition tower, bool force)
        {
            if (!force && AssetDatabase.LoadAssetAtPath<GameObject>(tower.PrefabPath) != null)
            {
                return;
            }

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(tower.ModelPath);
            if (model == null)
            {
                Debug.LogWarning($"Tower prefab build skipped: model not found at {tower.ModelPath}.");
                return;
            }

            Directory.CreateDirectory(MaterialFolder);
            Directory.CreateDirectory(PrefabFolder);
            ConfigureTextureImport(tower.BaseTexturePath, TextureImporterType.Default, true);
            ConfigureTextureImport(tower.NormalTexturePath, TextureImporterType.NormalMap, false);
            ConfigureTextureImport(tower.MetallicTexturePath, TextureImporterType.Default, false);
            ConfigureTextureImport(tower.RoughnessTexturePath, TextureImporterType.Default, false);

            var material = CreateOrUpdateMaterial(tower);
            var root = new GameObject(tower.AssetName);
            var modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            modelInstance.name = "Model";
            modelInstance.transform.SetParent(root.transform, false);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.Euler(tower.VisualRotationEuler);
            modelInstance.transform.localScale = Vector3.one;

            AssignMaterial(modelInstance, material);
            NormalizeModel(modelInstance, tower.TargetFootprint, tower.TargetHeight);
            AddFirePoint(root.transform, modelInstance);
            AddBoundsCollider(root, modelInstance);

            PrefabUtility.SaveAsPrefabAsset(root, tower.PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"{tower.AssetName} prefab built at {tower.PrefabPath}.");
        }

        private static Material CreateOrUpdateMaterial(TowerAssetDefinition tower)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(tower.MaterialPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader)
                {
                    name = tower.AssetName
                };
                AssetDatabase.CreateAsset(material, tower.MaterialPath);
            }

            SetTexture(material, "_BaseMap", tower.BaseTexturePath);
            SetTexture(material, "_BumpMap", tower.NormalTexturePath);
            SetTexture(material, "_MetallicGlossMap", tower.MetallicTexturePath);
            SetFloat(material, "_Metallic", 0.2f);
            SetFloat(material, "_Smoothness", 0.42f);
            if (material.HasProperty("_WorkflowMode"))
            {
                material.SetFloat("_WorkflowMode", 1f);
            }

            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureTextureImport(string assetPath, TextureImporterType type, bool srgb)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            var changed = false;
            if (importer.textureType != type)
            {
                importer.textureType = type;
                changed = true;
            }

            if (importer.sRGBTexture != srgb)
            {
                importer.sRGBTexture = srgb;
                changed = true;
            }

            if (importer.maxTextureSize > 1024)
            {
                importer.maxTextureSize = 1024;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static void SetTexture(Material material, string propertyName, string texturePath)
        {
            if (!material.HasProperty(propertyName))
            {
                return;
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture != null)
            {
                material.SetTexture(propertyName, texture);
            }
        }

        private static void SetFloat(Material material, string propertyName, float value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private static void AssignMaterial(GameObject modelInstance, Material material)
        {
            foreach (var renderer in modelInstance.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (var i = 0; i < materials.Length; i++)
                {
                    materials[i] = material;
                }

                renderer.sharedMaterials = materials;
            }
        }

        private static void NormalizeModel(GameObject modelInstance, float targetFootprint, float targetHeight)
        {
            var bounds = CalculateBounds(modelInstance);
            if (bounds.size == Vector3.zero)
            {
                return;
            }

            var width = Mathf.Max(bounds.size.x, bounds.size.z);
            var height = bounds.size.y;
            var scaleByFootprint = width > 0f ? targetFootprint / width : 1f;
            var scaleByHeight = height > 0f ? targetHeight / height : 1f;
            var scale = Mathf.Min(scaleByFootprint, scaleByHeight);
            modelInstance.transform.localScale = Vector3.one * scale;

            bounds = CalculateBounds(modelInstance);
            modelInstance.transform.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }

        private static void AddFirePoint(Transform root, GameObject modelInstance)
        {
            var bounds = CalculateBounds(modelInstance);
            var firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(root, false);
            firePoint.transform.localPosition = new Vector3(0f, bounds.max.y + 0.08f, 0f);
        }

        private static void AddBoundsCollider(GameObject root, GameObject modelInstance)
        {
            var bounds = CalculateBounds(modelInstance);
            var collider = root.AddComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = bounds.size;
        }

        private static Bounds CalculateBounds(GameObject gameObject)
        {
            var renderers = gameObject.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private readonly struct TowerAssetDefinition
        {
            public TowerAssetDefinition(string assetName, string sourceFolder, float targetFootprint, float targetHeight, Vector3 visualRotationEuler)
            {
                AssetName = assetName;
                SourceFolder = sourceFolder;
                TargetFootprint = targetFootprint;
                TargetHeight = targetHeight;
                VisualRotationEuler = visualRotationEuler;
            }

            public string AssetName { get; }
            public string SourceFolder { get; }
            public float TargetFootprint { get; }
            public float TargetHeight { get; }
            public Vector3 VisualRotationEuler { get; }
            public string ModelPath => $"{SourceFolder}/{AssetName}.fbx";
            public string BaseTexturePath => $"{SourceFolder}/{AssetName}_texture.png";
            public string NormalTexturePath => $"{SourceFolder}/{AssetName}_texture_normal.png";
            public string MetallicTexturePath => $"{SourceFolder}/{AssetName}_texture_metallic.png";
            public string RoughnessTexturePath => $"{SourceFolder}/{AssetName}_texture_roughness.png";
            public string MaterialPath => $"{MaterialFolder}/{AssetName}.mat";
            public string PrefabPath => $"{PrefabFolder}/{AssetName}.prefab";
        }
    }
}

