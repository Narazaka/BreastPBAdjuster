using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.SDKBase;
using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using nadena.dev.modular_avatar.core;
#endif

namespace Narazaka.VRChat.BreastPBAdjuster
{
    [AddComponentMenu(nameof(BreastPBAdjuster))]
    public class BreastPBAdjuster : MonoBehaviour, IEditorOnly
    {
        [SerializeField]
        public bool UseConstraint;
        [SerializeField]
        public Transform BreastL;
        [SerializeField]
        public Transform BreastR;
        [SerializeField]
        float Squish = 0.1f;
        [SerializeField]
        public Vector3 SquishScale = Vector3.one;
        [SerializeField]
        List<BreastKeyFrame> KeyFrames = new List<BreastKeyFrame>();
        BreastKeyFrame DefaultKeyFrame
        {
            get
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
                    TransformMemo.Set(self.FindPropertyRelative(nameof(Base)), boneSet.Base);
                    TransformMemo.Set(self.FindPropertyRelative(nameof(Start)), boneSet.Start);
                    TransformMemo.Set(self.FindPropertyRelative(nameof(End)), boneSet.End);
                }
#endif
                public TransformMemo Base = new TransformMemo();
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
                    Base.Apply(boneSet.Base);
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
            public readonly Transform Base;
            public readonly Transform Start;
            public readonly Transform End;
            public readonly VRCPhysBone PB;
            public BoneSet(Transform baseBone, Transform middleBone, Transform endBone)
            {
                Base = baseBone;
                Start = middleBone;
                End = endBone;
                PB = Start.GetComponent<VRCPhysBone>();
            }

            public bool Valid { get => Base != null && Start != null && End != null; }

            public void MoveEndPosition(Vector3 endPosition)
            {
                var startPosition = Base.position;
                var vec = endPosition - startPosition;
#if UNITY_EDITOR
                Undo.RecordObjects(new UnityEngine.Object[] { Base, Start, End }, "Move Breast PB");
#endif
                Base.position = startPosition;
                Start.position = startPosition;
                End.position = endPosition;
            }

#if UNITY_EDITOR
            public void DrawGizmos()
            {
                Handles.SphereHandleCap(0, Base.position, Quaternion.identity, 0.005f, EventType.Repaint);
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
                    Breast_L_parent,
                    Breast_L_base,
                    Breast_L,
                    Breast_L_end,
                    Breast_R_parent,
                    Breast_R_base,
                    Breast_R,
                    Breast_R_end,
                }

                Dictionary<Bone, Transform> Bones = new Dictionary<Bone, Transform>();
                ModularAvatarBoneProxy _Breast_L_BoneProxy;
                ModularAvatarBoneProxy _Breast_R_BoneProxy;
                Transform Parent;

                public BoneCache(Transform parent)
                {
                    Parent = parent;
                }

                public bool Valid { get => Parent != null; }

                public ModularAvatarBoneProxy Breast_L_BoneProxy { get => _Breast_L_BoneProxy == null ? (_Breast_L_BoneProxy = Breast_L_parent.GetComponent<ModularAvatarBoneProxy>()) : _Breast_L_BoneProxy; }
                public Transform Breast_L_parent { get => Bones.TryGetValue(Bone.Breast_L_parent, out var value) && value != null ? value : Parent.Find("Breast_L_parent"); }
                public Transform Breast_L_base { get => Bones.TryGetValue(Bone.Breast_L_base, out var value) && value != null ? value : Parent.Find("Breast_L_parent/Breast_L_base"); }
                public Transform Breast_L { get => Bones.TryGetValue(Bone.Breast_L, out var value) && value != null ? value : Parent.Find("Breast_L_parent/Breast_L_base/Breast_L"); }
                public Transform Breast_L_end { get => Bones.TryGetValue(Bone.Breast_L_end, out var value) && value != null ? value : Parent.Find("Breast_L_parent/Breast_L_base/Breast_L/Breast_L_end"); }

                public ModularAvatarBoneProxy Breast_R_BoneProxy { get => _Breast_R_BoneProxy == null ? (_Breast_R_BoneProxy = Breast_R_parent.GetComponent<ModularAvatarBoneProxy>()) : _Breast_R_BoneProxy; }
                public Transform Breast_R_parent { get => Bones.TryGetValue(Bone.Breast_R_parent, out var value) && value != null ? value : Parent.Find("Breast_R_parent"); }
                public Transform Breast_R_base { get => Bones.TryGetValue(Bone.Breast_R_base, out var value) && value != null ? value : Parent.Find("Breast_R_parent/Breast_R_base"); }
                public Transform Breast_R { get => Bones.TryGetValue(Bone.Breast_R, out var value) && value != null ? value : Parent.Find("Breast_R_parent/Breast_R_base/Breast_R"); }
                public Transform Breast_R_end { get => Bones.TryGetValue(Bone.Breast_R_end, out var value) && value != null ? value : Parent.Find("Breast_R_parent/Breast_R_base/Breast_R/Breast_R_end"); }
                public BoneSet L { get => _L == null || !_L.Valid ? new BoneSet(Breast_L_base, Breast_L, Breast_L_end) : _L; }
                BoneSet _L;
                public BoneSet R { get => _R == null || !_R.Valid ? new BoneSet(Breast_R_base, Breast_R, Breast_R_end) : _R; }
                BoneSet _R;

                public void SetBoneProxyL(Transform target)
                {
                    if (Breast_L_BoneProxy.target == target) return;
                    Undo.RecordObject(Breast_L_BoneProxy, "Change BoneProxy L");
                    Breast_L_BoneProxy.target = target;
                }

                public void SetBoneProxyR(Transform target)
                {
                    if (Breast_R_BoneProxy.target == target) return;
                    Undo.RecordObject(Breast_R_BoneProxy, "Change BoneProxy R");
                    Breast_R_BoneProxy.target = target;
                }
            }

            BoneCache Bones { get => _Bones == null || !_Bones.Valid ? (_Bones = new BoneCache(BreastPBAdjuster.transform)) : _Bones; }
            BoneCache _Bones;

            Transform Breast_L_second { get => _Breast_L_second == null ? (_Breast_L_second = BreastPBAdjuster.BreastL.GetChild(0)) : _Breast_L_second; }
            Transform _Breast_L_second;
            Transform Breast_R_second { get => _Breast_R_second == null ? (_Breast_R_second = BreastPBAdjuster.BreastR.GetChild(0)) : _Breast_R_second; }
            Transform _Breast_R_second;

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
                get => (KeyFramesList == null || KeyFramesList.index < 0 || KeyFramesList.index >= BreastPBAdjuster.KeyFrames.Count) ? BreastPBAdjuster.DefaultKeyFrameIndex : KeyFramesList.index;
            }
            int PrevIndex = -1;

            public override void OnInspectorGUI()
            {
                serializedObject.Update();

                var useConstraint = serializedObject.FindProperty(nameof(UseConstraint));
                var breastL = serializedObject.FindProperty(nameof(BreastL));
                var breastR = serializedObject.FindProperty(nameof(BreastR));
                var keyFrames = serializedObject.FindProperty(nameof(KeyFrames));
                var squish = serializedObject.FindProperty(nameof(Squish));
                var squishScale = serializedObject.FindProperty(nameof(SquishScale));

                var prevL = breastL.objectReferenceValue;
                var prevR = breastR.objectReferenceValue;

                EditorGUILayout.PropertyField(useConstraint);
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
                // KeyFramesList.DoLayoutList();

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

            private void OnDisable()
            {
                if (EditSquish)
                {
                    EditSquish = false;
                }
            }

            void NormalEdit()
            {
                if (BreastPBAdjuster.BreastL != null)
                {
                    var parent = BreastPBAdjuster.BreastL.parent;
                    Bones.SetBoneProxyL(parent);
                    Bones.Breast_L_parent.position = parent.position;
                    Bones.Breast_L_parent.rotation = parent.rotation;
                    Bones.Breast_L_parent.localScale = parent.localScale;
                    Bones.Breast_L_base.localPosition = BreastPBAdjuster.BreastL.localPosition;
                    Bones.Breast_L_base.localScale = BreastPBAdjuster.BreastL.localScale;
                    Bones.Breast_L_base.localRotation = BreastPBAdjuster.BreastL.localRotation;
                    Bones.Breast_L.localRotation = Quaternion.identity;
                    Bones.Breast_L_end.localRotation = Breast_L_second ? Breast_L_second.localRotation : Quaternion.identity;
                    ManipulatePosition(Bones.L, Bones.R);
                    ManipulatePB(Bones.L, Bones.R);
                    Bones.L.DrawGizmos();
                }
                if (BreastPBAdjuster.BreastR != null)
                {
                    var parent = BreastPBAdjuster.BreastR.parent;
                    Bones.SetBoneProxyR(parent);
                    Bones.Breast_R_parent.position = parent.position;
                    Bones.Breast_R_parent.rotation = parent.rotation;
                    Bones.Breast_R_parent.localScale = parent.localScale;
                    Bones.Breast_R_base.localPosition = BreastPBAdjuster.BreastR.localPosition;
                    Bones.Breast_R_base.localScale = BreastPBAdjuster.BreastR.localScale;
                    Bones.Breast_R_base.localRotation = BreastPBAdjuster.BreastR.localRotation;
                    Bones.Breast_R.localRotation = Quaternion.identity;
                    Bones.Breast_R_end.localRotation = Breast_R_second ? Breast_R_second.localRotation : Quaternion.identity;
                    ManipulatePosition(Bones.R, Bones.L);
                    ManipulatePB(Bones.R, Bones.L);
                    Bones.R.DrawGizmos();
                }
            }

            void SquishEdit()
            {
                ManipulateSquish(Bones.L);
                ManipulateSquish(Bones.R);
                ManipulateSquishScale(Bones.L, BreastPBAdjuster.BreastL);
                ManipulateSquishScale(Bones.R, BreastPBAdjuster.BreastR);
            }

            void OnChangeEditSquish()
            {
                if (EditSquish)
                {

                }
                else
                {
                    BreastPBAdjuster.BreastL.localScale = Bones.Breast_L_base.localScale;
                    BreastPBAdjuster.BreastR.localScale = Bones.Breast_R_base.localScale;
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
                    var scale = main.Base.localScale;
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
                Undo.RecordObjects(new UnityEngine.Object[] { Bones.L.PB, Bones.R.PB }, $"Change PB Radius to {radiusStart} .. {radiusEnd}");
                Bones.L.PB.radius = radiusEnd;
                Bones.L.PB.radiusCurve = curve;
                Bones.R.PB.radius = radiusEnd;
                Bones.R.PB.radiusCurve = curve;
            }

            void ManipulateSquish(BoneSet main)
            {
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    var diff = main.End.position - main.Base.position;
                    var pos = Handles.Slider(main.Base.position * BreastPBAdjuster.Squish + main.End.position * (1 - BreastPBAdjuster.Squish), -diff);
                    Handles.DrawLine(main.Base.position, pos);
                    Handles.color = Color.red;
                    Handles.DrawLine(main.End.position, pos);
                    Handles.color = Color.white;
                    if (check.changed)
                    {
                        serializedObject.FindProperty(nameof(Squish)).floatValue = Mathf.Clamp01(1 - (pos - main.Base.position).magnitude / diff.magnitude);
                    }
                }
            }

            void ManipulateSquishScale(BoneSet main, Transform breast)
            {
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    var scale = Handles.ScaleHandle(BreastPBAdjuster.SquishScale, main.Base.position, main.Base.rotation, 0.03f);
                    breast.localScale = Vector3.Scale(main.Base.localScale, scale);
                    if (check.changed)
                    {
                        serializedObject.FindProperty(nameof(SquishScale)).vector3Value = scale;
                    }
                }
            }
        }
#endif
    }
}
