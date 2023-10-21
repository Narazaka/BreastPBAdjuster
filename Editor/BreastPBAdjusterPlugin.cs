﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using nadena.dev.ndmf;
using nadena.dev.ndmf.fluent;
using UnityEngine.Animations;
using VRC.SDK3.Dynamics.PhysBone.Components;
using System.Linq;

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

                ProcessBreast(breastPBAdjuster.BreastL, breastPBAdjuster.transform.Find("Breast_L_base/Breast_L/Parent"), breastPBAdjuster.transform.Find("Breast_L_base/Breast_L").GetComponent<VRCPhysBone>());
                ProcessBreast(breastPBAdjuster.BreastR, breastPBAdjuster.transform.Find("Breast_R_base/Breast_R/Parent"), breastPBAdjuster.transform.Find("Breast_R_base/Breast_R").GetComponent<VRCPhysBone>());
            });
        }

        void ProcessBreast(Transform avatarBreast, Transform targetBreast, VRCPhysBone pb)
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
        }
    }
}
