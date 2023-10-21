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
                var breastPBAdjuster = ctx.AvatarRootObject.GetComponentInChildren<BreastPBAdjuster>();
                if (breastPBAdjuster == null) return;

                ProcessBreast(breastPBAdjuster, breastPBAdjuster.BreastL, breastPBAdjuster.transform.Find("Breast_L_base/Breast_L/Parent"), breastPBAdjuster.transform.Find("Breast_L_base/Breast_L").GetComponent<VRCPhysBone>());
                ProcessBreast(breastPBAdjuster, breastPBAdjuster.BreastR, breastPBAdjuster.transform.Find("Breast_R_base/Breast_R/Parent"), breastPBAdjuster.transform.Find("Breast_R_base/Breast_R").GetComponent<VRCPhysBone>());
            });
        }

        void ProcessBreast(BreastPBAdjuster breastPBAdjuster, Transform avatarBreast, Transform targetBreast, VRCPhysBone pb)
        {
            var r = avatarBreast.GetComponent<RotationConstraint>();
            if (r == null) r = avatarBreast.gameObject.AddComponent<RotationConstraint>();
            r.AddSource(new ConstraintSource { sourceTransform = targetBreast, weight = 1 });
            var rotationDelta = Quaternion.Inverse(targetBreast.rotation) * avatarBreast.rotation;
            r.rotationOffset = rotationDelta.eulerAngles;
            r.constraintActive = true;

            var s = avatarBreast.GetComponent<ScaleConstraint>();
            if (s == null) s = avatarBreast.gameObject.AddComponent<ScaleConstraint>();
            s.AddSource(new ConstraintSource { sourceTransform = targetBreast, weight = 1 });
            s.scaleOffset = new Vector3(avatarBreast.lossyScale.x / targetBreast.lossyScale.x, avatarBreast.lossyScale.y / targetBreast.lossyScale.y, avatarBreast.lossyScale.z / targetBreast.lossyScale.z);
            s.constraintActive = true;

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
