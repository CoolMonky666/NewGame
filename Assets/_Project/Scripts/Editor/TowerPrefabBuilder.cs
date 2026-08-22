using System.IO;
using UnityEditor;
using UnityEngine;

namespace MergeDefense.EditorTools
{
    [InitializeOnLoad]
    public static class TowerPrefabBuilder
    {
        private const string ModelPath = "Assets/_Project/Art/Models/Towers/base_tower_4.fbx";
        private const string BaseTexturePath = "Assets/_Project/Art/Models/Towers/base_tower_4_texture.png";
        private const string NormalTexturePath = "Assets/_Project/Art/Models/Towers/base_tower_4_texture_normal.png";
        private const string MetallicTexturePath = "Assets/_Project/Art/Models/Towers/base_tower_4_texture_metallic.png";
        private const string RoughnessTexturePath = "Assets/_Project/Art/Models/Towers/base_tower_4_texture_roughness.png";
        private const string MaterialFolder = "Assets/_Project/Art/Materials/Towers";
        private const string MaterialPath = MaterialFolder + "/base_tower_4.mat";
        private const string PrefabPath = "Assets/_Project/Prefabs/Towers/base_tower_4.prefab";

        static TowerPrefabBuilder()
        {
            EditorApplication.delayCall += BuildIfNeeded;
        }

        [MenuItem("Tools/Merge Defense/Rebuild Base Tower 4 Prefab")]
        public static void RebuildBaseTower4Prefab()
        {
            BuildBaseTower4Prefab(force: true);
        }

        private static void BuildIfNeeded()
        {
            BuildBaseTower4Prefab(force: false);
        }

        private static void BuildBaseTower4Prefab(bool force)
        {
            if (!force && AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            {
                return;
            }

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null)
            {
                Debug.LogWarning($"Tower prefab build skipped: model not found at {ModelPath}.");
                return;
            }

            Directory.CreateDirectory(MaterialFolder);
            ConfigureTextureImport(BaseTexturePath, TextureImporterType.Default, true);
            ConfigureTextureImport(NormalTexturePath, TextureImporterType.NormalMap, false);
            ConfigureTextureImport(MetallicTexturePath, TextureImporterType.Default, false);
            ConfigureTextureImport(RoughnessTexturePath, TextureImporterType.Default, false);

            var material = CreateOrUpdateMaterial();
            var root = new GameObject("base_tower_4");
            var modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            modelInstance.name = "Model";
            modelInstance.transform.SetParent(root.transform, false);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one;

            AssignMaterial(modelInstance, material);
            NormalizeModel(modelInstance, 1.4f, 1.8f);
            AddFirePoint(root.transform, modelInstance);
            AddBoundsCollider(root, modelInstance);

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"base_tower_4 prefab built at {PrefabPath}.");
        }

        private static Material CreateOrUpdateMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader)
                {
                    name = "base_tower_4"
                };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            SetTexture(material, "_BaseMap", BaseTexturePath);
            SetTexture(material, "_BumpMap", NormalTexturePath);
            SetTexture(material, "_MetallicGlossMap", MetallicTexturePath);
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
    }
}
