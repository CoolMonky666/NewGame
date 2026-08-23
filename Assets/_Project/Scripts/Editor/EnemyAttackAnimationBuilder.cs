using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MergeDefense.EditorTools
{
    [InitializeOnLoad]
    public static class EnemyAttackAnimationBuilder
    {
        private const string AttackClipName = "Attack";
        private const string AttackParameterName = "IsAttacking";
        private const string AttackModelPath = "Assets/_Project/Art/Models/Enemies/base_enemy_1/Animations/Attack/Meshy_AI_First_zombie_biped_Animation_01a02ff5-4d1b-7c08-8f3c-0164dcd8633c_withSkin.fbx";
        private const string ControllerPath = "Assets/_Project/Art/Animations/Enemies/base_enemy_1.controller";

        static EnemyAttackAnimationBuilder()
        {
            EditorApplication.delayCall += BuildIfNeeded;
        }

        [MenuItem("Tools/Merge Defense/Configure Base Enemy 1 Attack Animation")]
        public static void ConfigureBaseEnemy1AttackAnimation()
        {
            Build(force: true);
        }

        private static void BuildIfNeeded()
        {
            Build(force: false);
        }

        private static void Build(bool force)
        {
            if (!AttackAssetExists())
            {
                return;
            }

            ConfigureAttackImport();

            var attackClip = GetAttackClip();
            if (attackClip == null)
            {
                if (force)
                {
                    Debug.LogWarning($"Attack animation setup skipped: no usable animation clip found in {AttackModelPath}.");
                }

                return;
            }

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                Debug.LogWarning($"Attack animation setup skipped: Animator Controller not found at {ControllerPath}.");
                return;
            }

            EnsureAttackState(controller, attackClip);
            AssetDatabase.SaveAssets();
        }

        private static bool AttackAssetExists()
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(AttackModelPath) != null)
            {
                return true;
            }

            var projectRelativePath = AttackModelPath.StartsWith("Assets/") ? AttackModelPath.Substring("Assets/".Length) : AttackModelPath;
            var absolutePath = Path.Combine(Application.dataPath, projectRelativePath);
            return File.Exists(absolutePath);
        }

        private static void ConfigureAttackImport()
        {
            var importer = AssetImporter.GetAtPath(AttackModelPath) as ModelImporter;
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
                var attackClip = clips[GetLongestClipIndex(clips)];
                if (attackClip.name != AttackClipName)
                {
                    attackClip.name = AttackClipName;
                    changed = true;
                }

                changed |= SetClipLoopAndRootLock(ref attackClip);

                if (importer.clipAnimations == null || importer.clipAnimations.Length != 1 || importer.clipAnimations[0].name != AttackClipName)
                {
                    changed = true;
                }

                importer.clipAnimations = new[] { attackClip };
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

        private static AnimationClip GetAttackClip()
        {
            AnimationClip longestClip = null;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(AttackModelPath))
            {
                if (!(asset is AnimationClip clip) || clip.name.StartsWith("__preview__"))
                {
                    continue;
                }

                if (clip.name == AttackClipName)
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

        private static void EnsureAttackState(AnimatorController controller, Motion attackClip)
        {
            EnsureBoolParameter(controller, AttackParameterName);

            var stateMachine = controller.layers[0].stateMachine;
            var walkState = FindState(stateMachine, "Walk") ?? stateMachine.AddState("Walk");
            var attackState = FindState(stateMachine, AttackClipName) ?? stateMachine.AddState(AttackClipName, new Vector3(430f, 0f, 0f));

            if (attackState.motion != attackClip)
            {
                attackState.motion = attackClip;
            }

            attackState.speed = 1f;
            attackState.writeDefaultValues = true;

            EnsureBoolTransition(walkState, attackState, AttackParameterName, true);
            EnsureBoolTransition(attackState, walkState, AttackParameterName, false);
            EditorUtility.SetDirty(controller);
        }

        private static AnimatorState FindState(AnimatorStateMachine stateMachine, string stateName)
        {
            foreach (var childState in stateMachine.states)
            {
                if (childState.state.name == stateName)
                {
                    return childState.state;
                }
            }

            return null;
        }

        private static void EnsureBoolParameter(AnimatorController controller, string parameterName)
        {
            foreach (var parameter in controller.parameters)
            {
                if (parameter.name == parameterName && parameter.type == AnimatorControllerParameterType.Bool)
                {
                    return;
                }
            }

            controller.AddParameter(parameterName, AnimatorControllerParameterType.Bool);
        }

        private static void EnsureBoolTransition(AnimatorState source, AnimatorState destination, string parameterName, bool expectedValue)
        {
            var mode = expectedValue ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot;
            foreach (var transition in source.transitions)
            {
                if (transition.destinationState == destination && HasCondition(transition, parameterName, mode))
                {
                    ConfigureTransition(transition);
                    return;
                }
            }

            var newTransition = source.AddTransition(destination);
            newTransition.AddCondition(mode, 0f, parameterName);
            ConfigureTransition(newTransition);
        }

        private static bool HasCondition(AnimatorStateTransition transition, string parameterName, AnimatorConditionMode mode)
        {
            foreach (var condition in transition.conditions)
            {
                if (condition.parameter == parameterName && condition.mode == mode)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ConfigureTransition(AnimatorStateTransition transition)
        {
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.1f;
            transition.offset = 0f;
            transition.exitTime = 0f;
        }
    }
}
