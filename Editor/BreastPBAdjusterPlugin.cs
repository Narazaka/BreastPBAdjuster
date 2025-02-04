using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using nadena.dev.ndmf;
using nadena.dev.ndmf.fluent;
using UnityEngine.Animations;
using UnityEditor.Animations;
using VRC.SDK3.Dynamics.PhysBone.Components;
using System.Linq;
using nadena.dev.modular_avatar.core;
using VRC.SDK3.Dynamics.Constraint.Components;

[assembly: ExportsPlugin(typeof(Narazaka.VRChat.BreastPBAdjuster.Editor.BreastPBAdjusterPlugin))]

namespace Narazaka.VRChat.BreastPBAdjuster.Editor
{
    public class BreastPBAdjusterPlugin : Plugin<BreastPBAdjusterPlugin>
    {
        /// <summary>
        /// This name is used to identify the plugin internally, and can be used to declare BeforePlugin/AfterPlugin
        /// dependencies. If not set, the full type name will be used.
        /// </summary>
        public override string QualifiedName => "net.narazaka.vrchat.breast_pb_adjuster";

        /// <summary>
        /// The plugin name shown in debug UIs. If not set, the qualified name will be shown.
        /// </summary>
        public override string DisplayName => "BreastPBAdjuster";
        protected override void Configure()
        {
            InPhase(BuildPhase.Resolving).BeforePlugin("nadena.dev.modular-avatar").Run("BreastPBAdjuster", ctx =>
            {

                var breastPBAdjusters = ctx.AvatarRootObject.GetComponentsInChildren<BreastPBAdjuster>();
                if (breastPBAdjusters.Length == 0) return;

                if (breastPBAdjusters.Length > 1 && breastPBAdjusters.Any(a => !a.UseConstraint))
                {
                    throw new System.Exception("BreastPBAdjuster: 複数のBreastPBAdjusterがある場合、UseConstraintがtrueである必要があります");
                }

                /*
                var chest = ctx.AvatarRootObject.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Chest);
                breastPBAdjuster.transform.SetParent(chest, true);
                */

                foreach (var breastPBAdjuster in breastPBAdjusters)
                {
                    ProcessBreast(breastPBAdjuster, breastPBAdjuster.BreastL, breastPBAdjuster.transform.Find("Breast_L_parent/Breast_L_base/Breast_L/Parent"), breastPBAdjuster.transform.Find("Breast_L_parent/Breast_L_base/Breast_L").GetComponent<VRCPhysBone>());
                    ProcessBreast(breastPBAdjuster, breastPBAdjuster.BreastR, breastPBAdjuster.transform.Find("Breast_R_parent/Breast_R_base/Breast_R/Parent"), breastPBAdjuster.transform.Find("Breast_R_parent/Breast_R_base/Breast_R").GetComponent<VRCPhysBone>());
                    Object.DestroyImmediate(breastPBAdjuster);
                }
            });
        }

        void ProcessBreast(BreastPBAdjuster breastPBAdjuster, Transform avatarBreast, Transform targetBreast, VRCPhysBone pb)
        {
            var replaceTarget = MakeReplaceTarget(avatarBreast, targetBreast);

            if (breastPBAdjuster.UseConstraint)
            {
                AddRotationConstraint(avatarBreast, replaceTarget);
                AddScaleConstraint(avatarBreast, replaceTarget);
            }
            else
            {
                var replace = avatarBreast.gameObject.AddComponent<ModularAvatarReplaceObject>();
                replace.targetObject.Set(replaceTarget);
            }

            var avatarPb = avatarBreast.GetComponent<VRCPhysBone>();
            if (avatarPb != null)
            {
                pb.colliders = pb.colliders.Where(c => c != null).Union(avatarPb.colliders).ToList();
                Object.DestroyImmediate(avatarPb);
            }

            var clip = MakeAnimation(breastPBAdjuster.SquishScale);
            var controller = MakeAnimator(pb.parameter, clip);
            var mergeAnimator = targetBreast.gameObject.AddComponent<ModularAvatarMergeAnimator>();
            mergeAnimator.animator = controller;
            mergeAnimator.layerType = VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.FX;
            mergeAnimator.pathMode = MergeAnimatorPathMode.Relative;
            mergeAnimator.matchAvatarWriteDefaults = true;
        }

        GameObject MakeReplaceTarget(Transform avatarBreast, Transform targetBreast)
        {
            var inverseScale = new GameObject("InverseScale");
            inverseScale.transform.SetParent(avatarBreast, false);
            inverseScale.transform.localPosition = Vector3.zero;
            inverseScale.transform.SetParent(targetBreast, true);
            inverseScale.transform.localRotation = Quaternion.identity;
            inverseScale.transform.localScale = new Vector3(1 / avatarBreast.localScale.x, 1 / avatarBreast.localScale.y, 1 / avatarBreast.localScale.z);
            var inverseRotation = new GameObject("InverseRotation");
            inverseRotation.transform.SetParent(inverseScale.transform, false);
            inverseRotation.transform.localPosition = Vector3.zero;
            inverseRotation.transform.localRotation = Quaternion.Inverse(avatarBreast.localRotation);
            inverseRotation.transform.localScale = Vector3.one;
            var inversePosition = new GameObject("InversePosition");
            inversePosition.transform.SetParent(inverseRotation.transform, false);
            inversePosition.transform.localPosition = -avatarBreast.localPosition;
            inversePosition.transform.localRotation = Quaternion.identity;
            inversePosition.transform.localScale = Vector3.one;
            var replaceTarget = new GameObject(avatarBreast.name);
            replaceTarget.transform.SetParent(inversePosition.transform, false);
            replaceTarget.transform.localPosition = avatarBreast.localPosition;
            replaceTarget.transform.localRotation = avatarBreast.localRotation;
            replaceTarget.transform.localScale = avatarBreast.localScale;
            return replaceTarget;
        }

        void AddRotationConstraint(Transform avatarBreast, GameObject replaceTarget)
        {
            var r = avatarBreast.gameObject.AddComponent<VRCRotationConstraint>();
            r.AffectsRotationX = true;
            r.AffectsRotationY = true;
            r.AffectsRotationZ = true;
            r.Locked = true;
            r.IsActive = true;
            r.GlobalWeight = 1;
            var weight = r.Sources.Count == 0 ? 1 : 0;
            r.Sources.Add(new VRC.Dynamics.VRCConstraintSource
            {
                SourceTransform = replaceTarget.transform,
                ParentPositionOffset = Vector3.zero,
                ParentRotationOffset = Vector3.zero,
                Weight = weight,
            });
        }

        void AddScaleConstraint(Transform avatarBreast, GameObject replaceTarget)
        {
            var s = avatarBreast.gameObject.AddComponent<VRCScaleConstraint>();
            s.AffectsScaleX = true;
            s.AffectsScaleX = true;
            s.AffectsScaleX = true;
            s.Locked = true;
            s.IsActive = true;
            s.GlobalWeight = 1;
            var weight = s.Sources.Count == 0 ? 1 : 0;
            s.Sources.Add(new VRC.Dynamics.VRCConstraintSource
            {
                SourceTransform = replaceTarget.transform,
                ParentPositionOffset = Vector3.zero,
                ParentRotationOffset = Vector3.zero,
                Weight = weight,
            });
        }

        AnimatorController MakeAnimator(string parameterPrefix, AnimationClip clip)
        {
            var parameterName = $"{parameterPrefix}_Squish";
            var animator = new AnimatorController();
            animator.AddParameter(new AnimatorControllerParameter { name = parameterName, type = AnimatorControllerParameterType.Float, defaultFloat = 0f });
            animator.AddLayer(parameterName);
            var layer = animator.layers[0];
            var state = layer.stateMachine.AddState(parameterName);
            state.timeParameter = parameterName;
            state.timeParameterActive = true;
            state.writeDefaultValues = false;
            state.motion = clip;
            return animator;
        }

        AnimationClip MakeAnimation(Vector3 squishScale)
        {
            var anim = new AnimationClip();
            anim.SetCurve("", typeof(Transform), "localScale.x", new AnimationCurve
            {
                keys = new Keyframe[] {
                        new Keyframe { time = 0, value = 1f },
                        new Keyframe { time = 1, value = squishScale.x },
                    }
            });
            anim.SetCurve("", typeof(Transform), "localScale.y", new AnimationCurve
            {
                keys = new Keyframe[] {
                        new Keyframe { time = 0, value = 1f },
                        new Keyframe { time = 1, value = squishScale.y },
                    }
            });
            anim.SetCurve("", typeof(Transform), "localScale.z", new AnimationCurve
            {
                keys = new Keyframe[] {
                        new Keyframe { time = 0, value = 1f },
                        new Keyframe { time = 1, value = squishScale.z },
                    }
            });
            return anim;
        }
    }
}
