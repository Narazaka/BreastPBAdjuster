using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.SDKBase;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Narazaka.VRChat.BreastPBAdjuster
{
    [AddComponentMenu(nameof(BreastPBAdjuster))]
    public class BreastPBAdjuster : MonoBehaviour, IEditorOnly
    {
#if UNITY_EDITOR
        [SerializeField]
        public Transform BreastL;
        [SerializeField]
        public Transform BreastR;
        [SerializeField]
        float BaseBoneLength = 0.01f;
        

        private void OnDrawGizmosSelected()
        {
        }

        [CustomEditor(typeof(BreastPBAdjuster))]
        public class BreastPBAdjusterEditor : Editor
        {
            BreastPBAdjuster BreastPBAdjuster
            {
                get => _BreastPBAdjuster == null ? (_BreastPBAdjuster = target as BreastPBAdjuster) : _BreastPBAdjuster;
            }
            BreastPBAdjuster _BreastPBAdjuster;

            class BoneSet
            {
                public readonly Transform Start;
                public readonly Transform Middle;
                public readonly Transform End;
                public readonly VRCPhysBone PB;
                public BoneSet(Transform baseBone, Transform middleBone, Transform endBone)
                {
                    Start = baseBone;
                    Middle = middleBone;
                    End = endBone;
                    PB = Start.GetComponent<VRCPhysBone>();
                }

                public bool Valid { get => Start != null && Middle != null && End != null; }

                public void MoveEndPosition(Vector3 endPosition, float baseBoneLength)
                {
                    var startPosition = Start.position;
                    var vec = endPosition - startPosition;
                    var rot = Quaternion.FromToRotation(Vector3.up, vec);
                    Start.position = startPosition;
                    Start.rotation = rot;
                    Middle.position = startPosition + vec.normalized * baseBoneLength;
                    Middle.rotation = rot;
                    End.position = endPosition;
                    End.rotation = rot;
                }

                public void DrawGizmos()
                {
                    Handles.SphereHandleCap(0, Start.position, Quaternion.identity, 0.005f, EventType.Repaint);
                    Handles.SphereHandleCap(0, Middle.position, Quaternion.identity, 0.005f, EventType.Repaint);
                    Handles.Label(Start.position, "Start");
                    Handles.Label(Middle.position, "Middle");
                    Handles.Label(End.position, "End");
                    Handles.DrawLine(Middle.position, End.position);
                }
            }

            class BoneCache
            {
                enum Bone
                {
                    Breast_L_base,
                    Breast_L,
                    Breast_L_end,
                    Breast_R_base,
                    Breast_R,
                    Breast_R_end,
                }

                Dictionary<Bone, Transform> Bones = new Dictionary<Bone, Transform>();
                Transform Parent;

                public BoneCache(Transform parent)
                {
                    Parent = parent;
                }

                public bool Valid { get => Parent != null; }

                public Transform Breast_L { get => Bones.TryGetValue(Bone.Breast_L_base, out var value) && value != null ? value : Parent.Find("Breast_L"); }
                public Transform Breast_L_middle { get => Bones.TryGetValue(Bone.Breast_L, out var value) && value != null ? value : Parent.Find("Breast_L/Breast_L_middle"); }
                public Transform Breast_L_end { get => Bones.TryGetValue(Bone.Breast_L_end, out var value) && value != null ? value : Parent.Find("Breast_L/Breast_L_middle/Breast_L_end"); }

                public Transform Breast_R { get => Bones.TryGetValue(Bone.Breast_R_base, out var value) && value != null ? value : Parent.Find("Breast_R"); }
                public Transform Breast_R_middle { get => Bones.TryGetValue(Bone.Breast_R, out var value) && value != null ? value : Parent.Find("Breast_R/Breast_R_middle"); }
                public Transform Breast_R_end { get => Bones.TryGetValue(Bone.Breast_R_end, out var value) && value != null ? value : Parent.Find("Breast_R/Breast_R_middle/Breast_R_end"); }
                public BoneSet L { get => _L == null || !_L.Valid ? new BoneSet(Breast_L, Breast_L_middle, Breast_L_end) : _L; }
                BoneSet _L;
                public BoneSet R { get => _R == null || !_R.Valid ? new BoneSet(Breast_R, Breast_R_middle, Breast_R_end) : _R; }
                BoneSet _R;
            }

            BoneCache Bones { get => _Bones == null || !_Bones.Valid ? (_Bones = new BoneCache(BreastPBAdjuster.transform)) : _Bones; }
            BoneCache _Bones;

            private void OnSceneGUI()
            {
                if (BreastPBAdjuster.BreastL != null)
                {
                    Bones.Breast_L.position = BreastPBAdjuster.BreastL.position;
                    ManipulatePosition(Bones.L, Bones.R);
                    ManipulateScale(Bones.L, Bones.R);
                    ManipulatePB(Bones.L, Bones.R);
                    Bones.L.DrawGizmos();
                }
                if (BreastPBAdjuster.BreastR != null)
                {
                    Bones.Breast_R.position = BreastPBAdjuster.BreastR.position;
                    ManipulatePosition(Bones.R, Bones.L);
                    ManipulateScale(Bones.R, Bones.L);
                    ManipulatePB(Bones.R, Bones.L);
                    Bones.R.DrawGizmos();
                }
            }

            void ManipulatePosition(BoneSet main, BoneSet sub)
            {
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    var endPosition = Handles.PositionHandle(main.End.position, Quaternion.identity);
                    if (check.changed)
                    {
                        main.MoveEndPosition(endPosition, BreastPBAdjuster.BaseBoneLength);
                        var center = (BreastPBAdjuster.BreastL.position + BreastPBAdjuster.BreastR.position) / 2;
                        endPosition = Vector3.Reflect(endPosition - center, Vector3.left) + center;
                        sub.MoveEndPosition(endPosition, BreastPBAdjuster.BaseBoneLength);
                    }
                }
            }

            void ManipulateScale(BoneSet main, BoneSet sub)
            {
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    var scale = Handles.ScaleHandle(main.Middle.localScale, main.Middle.position, main.Middle.rotation, 0.02f);
                    if (check.changed)
                    {
                        main.Middle.localScale = scale;
                        sub.Middle.localScale = scale;
                    }
                }
            }

            void ManipulatePB(BoneSet main, BoneSet sub)
            {
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    var scale = main.Middle.localScale;
                    var avgScale = (scale.x + scale.y + scale.z) / 3f;
                    var radiusEnd = Handles.RadiusHandle(Quaternion.identity, main.End.position, main.PB.radius * avgScale);
                    var radiusStart = Handles.RadiusHandle(Quaternion.identity, main.Middle.position, main.PB.CalcRadius(0.5f) * avgScale);
                    if (check.changed)
                    {
                        var curve = new AnimationCurve(new Keyframe { time = 0, value = 0 }, new Keyframe { time = 0.5f, value = radiusStart / radiusEnd}, new Keyframe { time = 1, value = 1});
                        main.PB.radius = radiusEnd / avgScale;
                        main.PB.radiusCurve = curve;
                        sub.PB.radius = radiusEnd / avgScale;
                        sub.PB.radiusCurve = curve;
                    }
                }
            }
        }
#endif
    }
}
