using System.IO;
using MergeDefense.Enemies;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MergeDefense.EditorTools
{
    [InitializeOnLoad]
    public static class EnemyPrefabBuilder
    {
        private const string AssetName = "base_enemy_1";
        private const string SourceFolder = "Assets/_Project/Art/Models/Enemies/base_enemy_1";
        private const string ModelPath = SourceFolder + "/" + AssetName + ".fbx";
        private const string BaseTexturePath = SourceFolder + "/" + AssetName + "_texture.png";
        private const string NormalTexturePath = SourceFolder + "/" + AssetName + "_texture_normal.png";
        private const string MetallicTexturePath = SourceFolder + "/" + AssetName + "_texture_metallic.png";
        private const string RoughnessTexturePath = SourceFolder + "/" + AssetName + "_texture_roughness.png";
        private const string MaterialFolder = "Assets/_Project/Art/Materials/Enemies";
        private const string MaterialPath = MaterialFolder + "/" + AssetName + ".mat";
        private const string AnimationFolder = "Assets/_Project/Art/Animations/Enemies";
        private const string ControllerPath = AnimationFolder + "/" + AssetName + ".controller";
        private const string PrefabFolder = "Assets/_Project/Prefabs/Enemies";
        private const string PrefabPath = PrefabFolder + "/" + AssetName + ".prefab";

        static EnemyPrefabBuilder()
        {
            EditorApplication.delayCall += BuildIfNeeded;
        }

        [MenuItem("Tools/Merge Defense/Rebuild Base Enemy 1 Prefab")]
        public static void RebuildBaseEnemy1Prefab()
        {
            BuildBaseEnemy1Prefab(force: true);
        }

        private static void BuildIfNeeded()
        {
            BuildBaseEnemy1Prefab(force: false);
        }

        private static void BuildBaseEnemy1Prefab(bool force)
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
                Debug.LogWarning($"Enemy prefab build skipped: model not found at {ModelPath}.");
                return;
            }

            Directory.CreateDirectory(MaterialFolder);
            Directory.CreateDirectory(AnimationFolder);
            Directory.CreateDirectory(PrefabFolder);

            var material = CreateOrUpdateMaterial();
            var controller = CreateOrUpdateAnimatorController();
            var root = new GameObject(AssetName);
            var modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            modelInstance.name = "VisualModel";
            modelInstance.transform.SetParent(root.transform, false);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one;

            AssignMaterial(modelInstance, material);
            NormalizeModel(modelInstance, 0.8f, 1.4f);
            AddAnimator(modelInstance, controller);
            AddVisualAnchor(root, modelInstance.transform);
            AddPoint(root.transform, modelInstance, "HitPoint", 0.5f);
            AddPoint(root.transform, modelInstance, "HpBarPoint", 1f, 0.15f);
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
            if (!importer.importAnimation)
            {
                importer.importAnimation = true;
                changed = true;
            }

            if (importer.animationType != ModelImporterAnimationType.Generic)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
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

            var clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.defaultClipAnimations;
            }

            if (clips != null && clips.Length > 0)
            {
                var primaryClipIndex = GetLongestClipIndex(clips);
                for (var i = 0; i < clips.Length; i++)
                {
                    var clip = clips[i];
                    var clipName = i == primaryClipIndex ? "Walk" : clip.name;
                    if (string.IsNullOrWhiteSpace(clipName))
                    {
                        clipName = $"Clip_{i + 1}";
                    }

                    if (clip.name != clipName)
                    {
                        clip.name = clipName;
                        changed = true;
                    }

                    changed |= SetClipLoopAndRootLock(ref clip);
                    clips[i] = clip;
                }

                importer.clipAnimations = clips;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static bool SetClipLoopAndRootLock(ref ModelImporterClipAnimation clip)
        {
            var changed = false;

            if (!clip.loopTime)
            {
                clip.loopTime = true;
                changed = true;
            }

            if (!clip.loopPose)
            {
                clip.loopPose = true;
                changed = true;
            }

            if (!clip.lockRootRotation)
            {
                clip.lockRootRotation = true;
                changed = true;
            }

            if (!clip.lockRootHeightY)
            {
                clip.lockRootHeightY = true;
                changed = true;
            }

            if (!clip.lockRootPositionXZ)
            {
                clip.lockRootPositionXZ = true;
                changed = true;
            }

            if (clip.keepOriginalOrientation)
            {
                clip.keepOriginalOrientation = false;
                changed = true;
            }

            if (clip.keepOriginalPositionY)
            {
                clip.keepOriginalPositionY = false;
                changed = true;
            }

            if (clip.keepOriginalPositionXZ)
            {
                clip.keepOriginalPositionXZ = false;
                changed = true;
            }

            return changed;
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

        private static AnimatorController CreateOrUpdateAnimatorController()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            var clip = GetPrimaryAnimationClip();
            if (clip == null)
            {
                Debug.LogWarning($"Animator controller build skipped motion assignment: no animation clip found in {ModelPath}.");
                return controller;
            }

            var stateMachine = controller.layers[0].stateMachine;
            AnimatorState walkState = null;
            foreach (var childState in stateMachine.states)
            {
                if (childState.state.name == "Walk")
                {
                    walkState = childState.state;
                    break;
                }
            }

            if (walkState == null)
            {
                walkState = stateMachine.AddState("Walk");
            }

            walkState.motion = clip;
            stateMachine.defaultState = walkState;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static int GetLongestClipIndex(ModelImporterClipAnimation[] clips)
        {
            var longestIndex = 0;
            var longestDuration = float.MinValue;
            for (var i = 0; i < clips.Length; i++)
            {
                var duration = clips[i].lastFrame - clips[i].firstFrame;
                if (duration > longestDuration)
                {
                    longestDuration = duration;
                    longestIndex = i;
                }
            }

            return longestIndex;
        }

        private static AnimationClip GetPrimaryAnimationClip()
        {
            var assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(ModelPath);
            AnimationClip longestClip = null;
            foreach (var asset in assets)
            {
                if (!(asset is AnimationClip clip) || clip.name.StartsWith("__preview__"))
                {
                    continue;
                }

                if (clip.name == "Walk")
                {
                    return clip;
                }

                if (longestClip == null || clip.length > longestClip.length)
                {
                    longestClip = clip;
                }
            }

            return longestClip;
        }

        private static void AddAnimator(GameObject modelInstance, RuntimeAnimatorController controller)
        {
            var animator = modelInstance.GetComponent<Animator>();
            if (animator == null)
            {
                animator = modelInstance.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        }

        private static void AddVisualAnchor(GameObject root, Transform visualRoot)
        {
            var anchor = root.AddComponent<EnemyVisualAnchor>();
            anchor.VisualRoot = visualRoot;
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

        private static void AddPoint(Transform root, GameObject modelInstance, string pointName, float heightFactor, float extraHeight = 0f)
        {
            var bounds = CalculateBounds(modelInstance);
            var point = new GameObject(pointName);
            point.transform.SetParent(root, false);
            point.transform.localPosition = new Vector3(0f, bounds.min.y + bounds.size.y * heightFactor + extraHeight, 0f);
        }

        private static void AddBoundsCollider(GameObject root, GameObject modelInstance)
        {
            var bounds = CalculateBounds(modelInstance);
            var collider = root.AddComponent<CapsuleCollider>();
            collider.center = bounds.center;
            collider.height = Mathf.Max(bounds.size.y, 0.2f);
            collider.radius = Mathf.Max(bounds.extents.x, bounds.extents.z, 0.1f);
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



