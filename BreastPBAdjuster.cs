using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.SDKBase;
using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

namespace Narazaka.VRChat.BreastPBAdjuster
{
    [AddComponentMenu(nameof(BreastPBAdjuster))]
    public class BreastPBAdjuster : MonoBehaviour, IEditorOnly
    {
        [SerializeField]
        public Transform BreastL;
        [SerializeField]
        public Transform BreastR;
        [SerializeField]
        float Squish = 0.1f;
        [SerializeField]
        Vector3 SquishScale = Vector3.one;
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
        int DefaultKeyFrameIndex { get => KeyFrames.IndexOf(DefaultKeyFrame); }

        [Serializable]
        public class TransformMemo
        {
#if UNITY_EDITOR
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
                    memo.FindPropertyRelative(nameof(Position)).vector3Value = transform.localPosition;
                    memo.FindPropertyRelative(nameof(Rotation)).quaternionValue = transform.localRotation;
                    memo.FindPropertyRelative(nameof(Scale)).vector3Value = transform.localScale;
                }
            }
#endif

            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;

            /*
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
                    Position = transform.localPosition;
                    Rotation = transform.localRotation;
                    Scale = transform.localScale;
                }
            }
            */

            public void Apply(Transform transform)
            {
                if (transform == null) return;
                transform.localPosition = Position;
                transform.localRotation = Rotation;
                transform.localScale = Scale;
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

            [Serializable]
            public class BreastKeyFrameBoneSet
            {
#if UNITY_EDITOR
                public static void Set(SerializedProperty self, BoneSet boneSet)
                {
                    TransformMemo.Set(self.FindPropertyRelative(nameof(Start)), boneSet.Start);
                    TransformMemo.Set(self.FindPropertyRelative(nameof(End)), boneSet.End);
                }
#endif
                public TransformMemo Start = new TransformMemo();
                public TransformMemo End = new TransformMemo();

                /*
                public void Set(BoneSet boneSet)
                {
                    Start.Set(boneSet.Start);
                    Middle.Set(boneSet.Middle);
                    End.Set(boneSet.End);
                }
                */

                public void Apply(BoneSet boneSet)
                {
                    Start.Apply(boneSet.Start);
                    End.Apply(boneSet.End);
                }
            }

            public BreastKeyFrameBoneSet L = new BreastKeyFrameBoneSet();
            public BreastKeyFrameBoneSet R = new BreastKeyFrameBoneSet();
        }


        private void OnDrawGizmosSelected()
        {
        }

        public class BoneSet
        {
            public readonly Transform Start;
            public readonly Transform End;
            public readonly VRCPhysBone PB;
            public BoneSet(Transform baseBone, Transform endBone)
            {
                Start = baseBone;
                End = endBone;
                PB = Start.GetComponent<VRCPhysBone>();
            }

            public bool Valid { get => Start != null && End != null; }

            public void MoveEndPosition(Vector3 endPosition)
            {
                var startPosition = Start.position;
                var vec = endPosition - startPosition;
                var rot = Quaternion.FromToRotation(Vector3.up, vec);
                Start.position = startPosition;
                Start.rotation = rot;
                End.position = endPosition;
                End.rotation = rot;
            }

#if UNITY_EDITOR
            public void DrawGizmos()
            {
                Handles.SphereHandleCap(0, Start.position, Quaternion.identity, 0.005f, EventType.Repaint);
                Handles.Label(Start.position, "Start");
                Handles.Label(End.position, "End");
                Handles.DrawLine(Start.position, End.position);
            }
#endif
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(BreastPBAdjuster))]
        public class BreastPBAdjusterEditor : Editor
        {
            BreastPBAdjuster BreastPBAdjuster
            {
                get => _BreastPBAdjuster == null ? (_BreastPBAdjuster = target as BreastPBAdjuster) : _BreastPBAdjuster;
            }
            BreastPBAdjuster _BreastPBAdjuster;

            class BoneCache
            {
                enum Bone
                {
                    Breast_L_base,
                    Breast_L_end,
                    Breast_R_base,
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
                public Transform Breast_L_end { get => Bones.TryGetValue(Bone.Breast_L_end, out var value) && value != null ? value : Parent.Find("Breast_L/Breast_L_end"); }

                public Transform Breast_R { get => Bones.TryGetValue(Bone.Breast_R_base, out var value) && value != null ? value : Parent.Find("Breast_R"); }
                public Transform Breast_R_end { get => Bones.TryGetValue(Bone.Breast_R_end, out var value) && value != null ? value : Parent.Find("Breast_R/Breast_R_end"); }
                public BoneSet L { get => _L == null || !_L.Valid ? new BoneSet(Breast_L, Breast_L_end) : _L; }
                BoneSet _L;
                public BoneSet R { get => _R == null || !_R.Valid ? new BoneSet(Breast_R, Breast_R_end) : _R; }
                BoneSet _R;
            }

            BoneCache Bones { get => _Bones == null || !_Bones.Valid ? (_Bones = new BoneCache(BreastPBAdjuster.transform)) : _Bones; }
            BoneCache _Bones;

            bool EditSquish
            {
                get => _EditSquish;
                set
                {
                    if (_EditSquish == value) return;
                    _EditSquish = value;
                    OnChangeEditSquish();
                }
            }
            bool _EditSquish;
            UnityEditorInternal.ReorderableList KeyFramesList;

            BreastKeyFrame CurrentKeyFrame
            {
                get => BreastPBAdjuster.KeyFrames[CurrentKeyFrameIndex];
            }
            SerializedProperty CurrentKeyFrameProperty
            {
                get => serializedObject.FindProperty(nameof(KeyFrames)).GetArrayElementAtIndex(CurrentKeyFrameIndex);
            }
            int CurrentKeyFrameIndex
            {
                get =>(KeyFramesList == null || KeyFramesList.index < 0 || KeyFramesList.index >= BreastPBAdjuster.KeyFrames.Count) ? BreastPBAdjuster.DefaultKeyFrameIndex : KeyFramesList.index;
            }
            int PrevIndex = -1;

            public override void OnInspectorGUI()
            {
                serializedObject.Update();

                var breastL = serializedObject.FindProperty(nameof(BreastL));
                var breastR = serializedObject.FindProperty(nameof(BreastR));
                var keyFrames = serializedObject.FindProperty(nameof(KeyFrames));
                var squish = serializedObject.FindProperty(nameof(Squish));
                var squishScale = serializedObject.FindProperty(nameof(SquishScale));
                var changeBreastSize = serializedObject.FindProperty(nameof(ChangeBreastSize));

                var prevL = breastL.objectReferenceValue;
                var prevR = breastR.objectReferenceValue;

                EditorGUILayout.PropertyField(breastL);
                EditorGUILayout.PropertyField(breastR);
                EditorGUILayout.PropertyField(squish);
                Bones.L.PB.maxSquish = squish.floatValue;
                Bones.R.PB.maxSquish = squish.floatValue;
                EditorGUILayout.PropertyField(squishScale);
                if (GUILayout.Button(EditSquish ? "Cancel" : "Edit Squish"))
                {
                    EditSquish = !EditSquish;
                }
                if (GUILayout.Button("make anim"))
                {
                    MakeAnim();
                }
                EditorGUILayout.PropertyField(changeBreastSize);
                if (KeyFramesList == null)
                {
                    KeyFramesList = new UnityEditorInternal.ReorderableList(serializedObject, keyFrames);
                    KeyFramesList.elementHeightCallback = (i) =>
                    {
                        var keyFrame = keyFrames.GetArrayElementAtIndex(i);
                        var keyFrameBreastL = keyFrame.FindPropertyRelative("BreastL");
                        var keyFrameBreastR = keyFrame.FindPropertyRelative("BreastR");
                        var keyFrameL = keyFrame.FindPropertyRelative("L");
                        var keyFrameR = keyFrame.FindPropertyRelative("R");
                        return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2 + EditorGUI.GetPropertyHeight(keyFrameBreastL) + EditorGUI.GetPropertyHeight(keyFrameBreastR) + EditorGUI.GetPropertyHeight(keyFrameL) + EditorGUI.GetPropertyHeight(keyFrameR);
                    };
                    KeyFramesList.drawElementCallback = (rect, index, isActive, isFocused) =>
                    {
                        var keyFrame = keyFrames.GetArrayElementAtIndex(index);
                        rect.height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                        EditorGUI.PropertyField(rect, keyFrame.FindPropertyRelative("IsDefault"));
                        rect.y += rect.height;

                        var keyFrameBreastL = keyFrame.FindPropertyRelative("BreastL");
                        var keyFrameBreastR = keyFrame.FindPropertyRelative("BreastR");
                        var keyFrameL = keyFrame.FindPropertyRelative("L");
                        var keyFrameR = keyFrame.FindPropertyRelative("R");
                        using (new EditorGUI.DisabledGroupScope(true))
                        {
                            rect.height = EditorGUI.GetPropertyHeight(keyFrameBreastL);
                            EditorGUI.PropertyField(rect, keyFrameBreastL, true);
                            rect.y += rect.height;
                            rect.height = EditorGUI.GetPropertyHeight(keyFrameBreastR);
                            EditorGUI.PropertyField(rect, keyFrameBreastR, true);
                            rect.y += rect.height;
                            rect.height = EditorGUI.GetPropertyHeight(keyFrameL);
                            EditorGUI.PropertyField(rect, keyFrameL, true);
                            rect.y += rect.height;
                            rect.height = EditorGUI.GetPropertyHeight(keyFrameR);
                            EditorGUI.PropertyField(rect, keyFrameR, true);
                            rect.y += rect.height;
                        }

                        rect.height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                        if (GUI.Button(rect, "Set current transforms"))
                        {
                            TransformMemo.Set(keyFrameBreastL, breastL.objectReferenceValue as Transform);
                            TransformMemo.Set(keyFrameBreastR, breastR.objectReferenceValue as Transform);
                        }
                        rect.y += rect.height;
                    };
                }
                KeyFramesList.DoLayoutList();

                var nowL = breastL.objectReferenceValue;
                var nowR = breastR.objectReferenceValue;

                if (prevL != nowL)
                {
                    TransformMemo.Set(GetDefaultKeyFrameProperty().FindPropertyRelative(nameof(BreastL)), breastL.objectReferenceValue as Transform);
                }
                if (prevR != nowR)
                {
                    TransformMemo.Set(GetDefaultKeyFrameProperty().FindPropertyRelative(nameof(BreastR)), breastR.objectReferenceValue as Transform);
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
                serializedObject.Update();
                if (EditSquish)
                {
                    SquishEdit();
                }
                else
                {
                    NormalEdit();
                }
                serializedObject.ApplyModifiedProperties();
            }

            void NormalEdit()
            {
                if (BreastPBAdjuster.BreastL != null)
                {
                    Bones.Breast_L.localPosition = CurrentKeyFrame.BreastL.Position;
                    Bones.Breast_L.localScale = CurrentKeyFrame.BreastL.Scale;
                    ManipulatePosition(Bones.L, Bones.R);
                    ManipulatePB(Bones.L, Bones.R);
                    Bones.L.DrawGizmos();
                }
                if (BreastPBAdjuster.BreastR != null)
                {
                    Bones.Breast_R.localPosition = CurrentKeyFrame.BreastR.Position;
                    Bones.Breast_R.localScale = CurrentKeyFrame.BreastR.Scale;
                    ManipulatePosition(Bones.R, Bones.L);
                    ManipulatePB(Bones.R, Bones.L);
                    Bones.R.DrawGizmos();
                }

                var currentKeyFrameProp = CurrentKeyFrameProperty;
                BreastKeyFrame.BreastKeyFrameBoneSet.Set(currentKeyFrameProp.FindPropertyRelative("L"), Bones.L);
                BreastKeyFrame.BreastKeyFrameBoneSet.Set(currentKeyFrameProp.FindPropertyRelative("R"), Bones.R);
            }

            void SquishEdit()
            {
                ManipulateSquish(Bones.L, CurrentKeyFrame.L);
                ManipulateSquish(Bones.R, CurrentKeyFrame.R);
                ManipulateSquishScale(Bones.L, CurrentKeyFrame.BreastL, BreastPBAdjuster.BreastL);
                ManipulateSquishScale(Bones.R, CurrentKeyFrame.BreastR, BreastPBAdjuster.BreastR);
            }

            void OnChangeEditSquish()
            {
                if (EditSquish)
                {

                }
                else
                {
                    CurrentKeyFrame.BreastL.Apply(BreastPBAdjuster.BreastL);
                    CurrentKeyFrame.BreastR.Apply(BreastPBAdjuster.BreastR);
                    CurrentKeyFrame.L.Apply(Bones.L);
                    CurrentKeyFrame.R.Apply(Bones.R);
                }
            }

            void ManipulatePosition(BoneSet main, BoneSet sub)
            {
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    var endPosition = Handles.PositionHandle(main.End.position, Quaternion.identity);
                    if (check.changed)
                    {
                        main.MoveEndPosition(endPosition);
                        var center = BreastPBAdjuster.transform.position;
                        endPosition = Vector3.Reflect(endPosition - center, Vector3.left) + center;
                        sub.MoveEndPosition(endPosition);
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
                        SetPB(radiusStart, radiusEnd);
                    }
                }
            }

            void SetPB(float radiusStart, float radiusEnd)
            {
                var rate = radiusStart / radiusEnd;
                var curve = new AnimationCurve(new Keyframe { time = 0f, value = rate }, new Keyframe { time = 1, value = 1 });
                Bones.L.PB.radius = radiusEnd;
                Bones.L.PB.radiusCurve = curve;
                Bones.R.PB.radius = radiusEnd;
                Bones.R.PB.radiusCurve = curve;
            }

            void ManipulateSquish(BoneSet main, BreastKeyFrame.BreastKeyFrameBoneSet mainKeyFrame)
            {
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    mainKeyFrame.Apply(main); // いったん計測のため元の長さに
                    var diff = main.End.position - main.Start.position;
                    var pos = Handles.Slider(main.Start.position * BreastPBAdjuster.Squish + main.End.position * (1 - BreastPBAdjuster.Squish), diff);
                    main.End.position -= diff * BreastPBAdjuster.Squish; // PB末端適用
                    if (check.changed)
                    {
                        serializedObject.FindProperty(nameof(Squish)).floatValue = Mathf.Clamp01(1 - (pos - main.Start.position).magnitude / diff.magnitude);
                    }
                }
            }

            void ManipulateSquishScale(BoneSet main, TransformMemo tr, Transform breast)
            {
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    var scale = Handles.ScaleHandle(BreastPBAdjuster.SquishScale, main.Start.position, main.Start.rotation, 0.03f);
                    breast.localScale = Vector3.Scale(tr.Scale, scale);
                    if (check.changed)
                    {
                        serializedObject.FindProperty(nameof(SquishScale)).vector3Value = scale;
                    }
                }
            }

            static string AnimPath = "Packages/net.narazaka.vrchat.breast-pb-adjuster/BreastPBScale.anim";

            void MakeAnim()
            {
                var anim = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimPath);
                anim.ClearCurves();
                anim.SetCurve("", typeof(Transform), "localScale.x", new AnimationCurve
                {
                    keys = new Keyframe[] {
                        new Keyframe { time = 0, value = 1f },
                        new Keyframe { time = 1, value = BreastPBAdjuster.SquishScale.x },
                    }
                });
                anim.SetCurve("", typeof(Transform), "localScale.y", new AnimationCurve
                {
                    keys = new Keyframe[] {
                        new Keyframe { time = 0, value = 1f },
                        new Keyframe { time = 1, value = BreastPBAdjuster.SquishScale.y },
                    }
                });
                anim.SetCurve("", typeof(Transform), "localScale.z", new AnimationCurve
                {
                    keys = new Keyframe[] {
                        new Keyframe { time = 0, value = 1f },
                        new Keyframe { time = 1, value = BreastPBAdjuster.SquishScale.z },
                    }
                });
            }
        }
#endif
    }
}
