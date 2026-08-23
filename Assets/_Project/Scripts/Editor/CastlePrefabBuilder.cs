using System.IO;
using UnityEditor;
using UnityEngine;

namespace MergeDefense.EditorTools
{
    [InitializeOnLoad]
    public static class CastlePrefabBuilder
    {
        private const string AssetName = "base_castle_1";
        private const string SourceFolder = "Assets/_Project/Art/Models/Environment/Castle/base_castle_1";
        private const string ModelPath = SourceFolder + "/" + AssetName + ".fbx";
        private const string BaseTexturePath = SourceFolder + "/" + AssetName + "_texture.png";
        private const string NormalTexturePath = SourceFolder + "/" + AssetName + "_texture_normal.png";
        private const string MetallicTexturePath = SourceFolder + "/" + AssetName + "_texture_metallic.png";
        private const string RoughnessTexturePath = SourceFolder + "/" + AssetName + "_texture_roughness.png";
        private const string MaterialFolder = "Assets/_Project/Art/Materials/Environment";
        private const string MaterialPath = MaterialFolder + "/" + AssetName + ".mat";
        private const string PrefabFolder = "Assets/_Project/Prefabs/Environment";
        private const string PrefabPath = PrefabFolder + "/" + AssetName + ".prefab";

        static CastlePrefabBuilder()
        {
            EditorApplication.delayCall += BuildIfNeeded;
        }

        [MenuItem("Tools/Merge Defense/Rebuild Base Castle 1 Prefab")]
        public static void RebuildBaseCastle1Prefab()
        {
            BuildBaseCastle1Prefab(force: true);
        }

        private static void BuildIfNeeded()
        {
            BuildBaseCastle1Prefab(force: false);
        }

        private static void BuildBaseCastle1Prefab(bool force)
        {
            if (!force && AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            {
                return;
            }

            ConfigureModelImport();
            ConfigureTextureImport(BaseTexturePath, TextureImporterType.Default, true);
            ConfigureTextureImport(NormalTexturePath, TextureImporterType.NormalMap, false);
            ConfigureTextureImport(MetallicTexturePath, TextureImporterType.Default, false);
            ConfigureTextureImport(RoughnessTexturePath, TextureImporterType.Default, false);

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null)
            {
                Debug.LogWarning($"Castle prefab build skipped: model not found at {ModelPath}.");
                return;
            }

            Directory.CreateDirectory(MaterialFolder);
            Directory.CreateDirectory(PrefabFolder);

            var material = CreateOrUpdateMaterial();
            var root = new GameObject(AssetName);
            var modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            modelInstance.name = "Model";
            modelInstance.transform.SetParent(root.transform, false);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            modelInstance.transform.localScale = Vector3.one;

            AssignMaterial(modelInstance, material);
            NormalizeModel(modelInstance, 2.8f, 2.2f);
            AddTargetPoint(root.transform, modelInstance);
            AddBoundsCollider(root, modelInstance);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"{AssetName} prefab built at {PrefabPath}.");
        }

        private static void ConfigureModelImport()
        {
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null)
            {
                return;
            }

            var changed = false;
            if (importer.importAnimation)
            {
                importer.importAnimation = false;
                changed = true;
            }

            if (importer.importCameras)
            {
                importer.importCameras = false;
                changed = true;
            }

            if (importer.importLights)
            {
                importer.importLights = false;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static Material CreateOrUpdateMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader)
                {
                    name = AssetName
                };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            SetTexture(material, "_BaseMap", BaseTexturePath);
            SetTexture(material, "_BumpMap", NormalTexturePath);
            SetTexture(material, "_MetallicGlossMap", MetallicTexturePath);
            SetFloat(material, "_Metallic", 0.15f);
            SetFloat(material, "_Smoothness", 0.34f);
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

        private static void AddTargetPoint(Transform root, GameObject modelInstance)
        {
            var bounds = CalculateBounds(modelInstance);
            var targetPoint = new GameObject("TargetPoint");
            targetPoint.transform.SetParent(root, false);
            targetPoint.transform.localPosition = new Vector3(0f, bounds.min.y + bounds.size.y * 0.48f, -bounds.extents.z * 0.75f);
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

