using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.SDKBase;
using UnityEditor.Hardware;
using System;
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
        float BaseBoneLength = 0.1f;
        [SerializeField]
        bool ChangeBreastSize = true;
        [SerializeField]
        List<BreastKeyFrame> KeyFrames = new List<BreastKeyFrame>();
        BreastKeyFrame DefaultKeyFrame { get
            {
                var kf = KeyFrames.FirstOrDefault(f => f.IsDefault);
                if (kf != null)
                {
                    return kf;
                }
                if (KeyFrames.Count == 0)
                {
                    KeyFrames.Add(new BreastKeyFrame());
                }
                kf = KeyFrames.First();
                kf.IsDefault = true;
                return kf;
            }
        }

        [Serializable]
        public class TransformMemo
        {
            public static void Set(SerializedProperty memo, Transform transform)
            {
                if (transform == null)
                {
                    memo.FindPropertyRelative(nameof(Position)).vector3Value = Vector3.zero;
                    memo.FindPropertyRelative(nameof(Rotation)).quaternionValue = Quaternion.identity;
                    memo.FindPropertyRelative(nameof(Scale)).vector3Value = Vector3.one;
                }
                else
                {
                    memo.FindPropertyRelative(nameof(Position)).vector3Value = transform.position;
                    memo.FindPropertyRelative(nameof(Rotation)).quaternionValue = transform.rotation;
                    memo.FindPropertyRelative(nameof(Scale)).vector3Value = transform.localScale;
                }
            }

            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;

            public void Set(Transform transform)
            {
                if (transform == null)
                {
                    Position = Vector3.zero;
                    Rotation = Quaternion.identity;
                    Scale = Vector3.one;
                }
                else
                {
                    Position = transform.position;
                    Rotation = transform.rotation;
                    Scale = transform.localScale;
                }
            }
        }

        [Serializable]
        public class PhysBoneValue
        {
            public FloatValue Pull;
            public FloatValue Momentum;
            public FloatValue Stiffness;
            public FloatValue Gravity;
            public FloatValue GravityFalloff;
            public FloatValue Immobile;
            public FloatValue MaxAngle;
            public FloatValue Radius;

            [Serializable]
            public class FloatValue
            {
                public bool IsOverride;
                public float Value;
            }
        }

        [Serializable]
        public class BreastKeyFrame
        {
            public bool IsDefault;
            public TransformMemo BreastL = new TransformMemo();
            public TransformMemo BreastR = new TransformMemo();
        }


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
                    Middle.position = startPosition + vec * baseBoneLength;
                    Middle.rotation = rot;
                    End.position = endPosition;
                    End.rotation = rot;
                }

                public void ChangeBaseBoneLength(float baseBoneLength)
                {
                    MoveEndPosition(End.position, baseBoneLength);
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

            enum EditMode
            {
                Normal,
                Squish,
            }

            EditMode Mode;
            UnityEditorInternal.ReorderableList KeyFramesList;

            public override void OnInspectorGUI()
            {
                serializedObject.Update();

                var breastL = serializedObject.FindProperty(nameof(BreastL));
                var breastR = serializedObject.FindProperty(nameof(BreastR));
                var keyFrames = serializedObject.FindProperty(nameof(KeyFrames));
                var baseBoneLength = serializedObject.FindProperty(nameof(BaseBoneLength));
                var changeBreastSize = serializedObject.FindProperty(nameof(ChangeBreastSize));

                var prevL = breastL.objectReferenceValue;
                var prevR = breastR.objectReferenceValue;
                var prevBaseBoneLength = baseBoneLength.floatValue;

                EditorGUILayout.PropertyField(breastL);
                EditorGUILayout.PropertyField(breastR);
                EditorGUILayout.PropertyField(baseBoneLength);
                EditorGUILayout.PropertyField(changeBreastSize);
                if (KeyFramesList == null)
                {
                    KeyFramesList = new UnityEditorInternal.ReorderableList(serializedObject, keyFrames);
                    KeyFramesList.elementHeightCallback = (i) =>
                    {
                        var keyFrame = keyFrames.GetArrayElementAtIndex(i);
                        var keyFrameBreastL = keyFrame.FindPropertyRelative("BreastL");
                        var keyFrameBreastR = keyFrame.FindPropertyRelative("BreastR");
                        return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2 + EditorGUI.GetPropertyHeight(keyFrameBreastL) + EditorGUI.GetPropertyHeight(keyFrameBreastR);
                    };
                    KeyFramesList.drawElementCallback = (rect, index, isActive, isFocused) =>
                    {
                        var keyFrame = keyFrames.GetArrayElementAtIndex(index);
                        rect.height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                        EditorGUI.PropertyField(rect, keyFrame.FindPropertyRelative("IsDefault"));
                        rect.y += rect.height;

                        var keyFrameBreastL = keyFrame.FindPropertyRelative("BreastL");
                        var keyFrameBreastR = keyFrame.FindPropertyRelative("BreastR");
                        using (new EditorGUI.DisabledGroupScope(true))
                        {
                            rect.height = EditorGUI.GetPropertyHeight(keyFrameBreastL);
                            EditorGUI.PropertyField(rect, keyFrameBreastL, true);
                            rect.y += rect.height;
                            rect.height = EditorGUI.GetPropertyHeight(keyFrameBreastR);
                            EditorGUI.PropertyField(rect, keyFrameBreastR, true);
                            rect.y += rect.height;
                        }

                        rect.height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                        if (GUI.Button(rect, "Set current transforms"))
                        {
                            TransformMemo.Set(keyFrameBreastL, breastL.objectReferenceValue as Transform);
                            TransformMemo.Set(keyFrameBreastR, breastR.objectReferenceValue as Transform);
                        }
                    };
                }
                KeyFramesList.DoLayoutList();

                var nowL = breastL.objectReferenceValue;
                var nowR = breastR.objectReferenceValue;
                var nowBaseBoneLength = baseBoneLength.floatValue;

                if (prevL != nowL)
                {
                    TransformMemo.Set(GetDefaultKeyFrameProperty().FindPropertyRelative(nameof(BreastL)), breastL.objectReferenceValue as Transform);
                }
                if (prevR != nowR)
                {
                    TransformMemo.Set(GetDefaultKeyFrameProperty().FindPropertyRelative(nameof(BreastR)), breastR.objectReferenceValue as Transform);
                }
                if (prevBaseBoneLength != nowBaseBoneLength)
                {
                    Bones.L.ChangeBaseBoneLength(nowBaseBoneLength);
                    Bones.R.ChangeBaseBoneLength(nowBaseBoneLength);
                    SetPB(Bones.L.PB.CalcRadius(0f), Bones.L.PB.radius, nowBaseBoneLength);
                }
                serializedObject.ApplyModifiedProperties();
            }

            SerializedProperty GetDefaultKeyFrameProperty()
            {
                var keyFrames = serializedObject.FindProperty(nameof(KeyFrames));

                SerializedProperty keyFrame = null;
                for (var i = 0; i < keyFrames.arraySize; i++)
                {
                    var kf = keyFrames.GetArrayElementAtIndex(i);
                    if (kf.FindPropertyRelative("IsDefault").boolValue)
                    {
                        keyFrame = kf;
                        break;
                    }
                }

                if (keyFrame != null)
                {
                    return keyFrame;
                }

                if (keyFrames.arraySize == 0)
                {
                    keyFrames.InsertArrayElementAtIndex(0);
                }
                keyFrame = keyFrames.GetArrayElementAtIndex(0);
                keyFrame.FindPropertyRelative("IsDefault").boolValue = true;
                return keyFrame;
            }

            private void OnSceneGUI()
            {
                if (BreastPBAdjuster.BreastL != null)
                {
                    Bones.Breast_L.position = BreastPBAdjuster.DefaultKeyFrame.BreastL.Position;
                    Bones.Breast_L.localScale = BreastPBAdjuster.DefaultKeyFrame.BreastL.Scale;
                    ManipulatePosition(Bones.L, Bones.R);
                    ManipulateScale(Bones.L, Bones.R);
                    ManipulatePB(Bones.L, Bones.R);
                    Bones.L.DrawGizmos();
                }
                if (BreastPBAdjuster.BreastR != null)
                {
                    Bones.Breast_R.position = BreastPBAdjuster.DefaultKeyFrame.BreastR.Position;
                    Bones.Breast_R.localScale = BreastPBAdjuster.DefaultKeyFrame.BreastR.Scale;
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
                        var center = (BreastPBAdjuster.DefaultKeyFrame.BreastL.Position + BreastPBAdjuster.DefaultKeyFrame.BreastR.Position) / 2;
                        endPosition = Vector3.Reflect(endPosition - center, Vector3.left) + center;
                        sub.MoveEndPosition(endPosition, BreastPBAdjuster.BaseBoneLength);
                    }
                }
            }

            void ManipulateScale(BoneSet main, BoneSet sub)
            {
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    var scale = Handles.ScaleHandle(main.Start.localScale, main.Start.position, main.Start.rotation, 0.02f);
                    if (check.changed)
                    {
                        main.Start.localScale = scale;
                        sub.Start.localScale = scale;
                    }
                }
            }

            void ManipulatePB(BoneSet main, BoneSet sub)
            {
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    var scale = main.Start.localScale;
                    var avgScale = (scale.x + scale.y + scale.z) / 3f;
                    var radiusEnd = Handles.RadiusHandle(Quaternion.identity, main.End.position, main.PB.radius * avgScale) / avgScale;
                    var radiusStart = Handles.RadiusHandle(Quaternion.identity, main.Start.position, main.PB.CalcRadius(0f) * avgScale) / avgScale;
                    if (check.changed)
                    {
                        SetPB(radiusStart, radiusEnd, BreastPBAdjuster.BaseBoneLength);
                    }
                }
            }

            void SetPB(float radiusStart, float radiusEnd, float baseBoneLength)
            {
                var rate = radiusStart / radiusEnd;
                var curve = new AnimationCurve(new Keyframe { time = 0f, value = rate }, new Keyframe { time = 0.5f, value = baseBoneLength + rate * (1 - baseBoneLength) }, new Keyframe { time = 1, value = 1 });
                Bones.L.PB.radius = radiusEnd;
                Bones.L.PB.radiusCurve = curve;
                Bones.R.PB.radius = radiusEnd;
                Bones.R.PB.radiusCurve = curve;
            }
        }
#endif
    }
}
