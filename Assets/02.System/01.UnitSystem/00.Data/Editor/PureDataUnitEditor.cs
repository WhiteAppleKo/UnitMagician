using UnityEditor;
using UnityEngine;

namespace UnitSystem
{
    [CustomEditor(typeof(PureDataUnit))]
    public class PureDataUnitEditor : Editor
    {
        private SerializedProperty unitTypeProp;
        private SerializedProperty unitNameProp;
        private SerializedProperty baseCostProp;
        private SerializedProperty iconProp;
        private SerializedProperty applicatorProp;

        private SerializedProperty overlayMaterialProp;
        private SerializedProperty massScaleMultiplierProp;

        private void OnEnable()
        {
            unitTypeProp = serializedObject.FindProperty("unitType");
            unitNameProp = serializedObject.FindProperty("unitName");
            baseCostProp = serializedObject.FindProperty("baseCost");
            iconProp = serializedObject.FindProperty("icon");
            applicatorProp = serializedObject.FindProperty("applicator");

            overlayMaterialProp = serializedObject.FindProperty("overlayMaterial");
            massScaleMultiplierProp = serializedObject.FindProperty("massScaleMultiplier");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Common Metadata", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(unitTypeProp);
            bool isTypeChanged = EditorGUI.EndChangeCheck();

            EditorGUILayout.PropertyField(unitNameProp);
            EditorGUILayout.PropertyField(baseCostProp);
            EditorGUILayout.PropertyField(iconProp);
            EditorGUILayout.PropertyField(applicatorProp);

            UnitType currentType = (UnitType)unitTypeProp.enumValueIndex;

            // UnitType 변경되거나 Applicator 미할당 시 100% 자동 매핑
            if (isTypeChanged || applicatorProp.objectReferenceValue == null)
            {
                AutoAssignApplicator(currentType);
            }

            EditorGUILayout.Space(10);

            // 부피 / 액체 단위 타입 선택 시
            if (currentType == UnitType.Volume)
            {
                EditorGUILayout.LabelField("Volume / Liquid Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(overlayMaterialProp);
            }
            // 무게 단위 타입 선택 시
            else if (currentType == UnitType.Mass)
            {
                EditorGUILayout.LabelField("Mass Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(massScaleMultiplierProp);
            }
            // 방향 단위 타입 선택 시
            else if (currentType == UnitType.Vector)
            {
                EditorGUILayout.LabelField("Vector / Direction Settings", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Vector uses VectorUnitApplicatorSO to reverse object velocity & direction.", MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void AutoAssignApplicator(UnitType type)
        {
            string searchType = "";
            string defaultAssetName = "";
            System.Type targetClassType = null;

            switch (type)
            {
                case UnitType.Mass:
                    searchType = "t:MassUnitApplicatorSO";
                    defaultAssetName = "MassUnitApplicator";
                    targetClassType = typeof(MassUnitApplicatorSO);
                    break;
                case UnitType.Volume:
                    searchType = "t:VolumeUnitApplicatorSO";
                    defaultAssetName = "VolumeUnitApplicator";
                    targetClassType = typeof(VolumeUnitApplicatorSO);
                    break;
                case UnitType.Vector:
                    searchType = "t:VectorUnitApplicatorSO";
                    defaultAssetName = "VectorUnitApplicator";
                    targetClassType = typeof(VectorUnitApplicatorSO);
                    break;
            }

            if (targetClassType == null) return;

            string[] guids = AssetDatabase.FindAssets(searchType);
            UnitVisualApplicatorSO targetApplicator = null;

            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                targetApplicator = AssetDatabase.LoadAssetAtPath<UnitVisualApplicatorSO>(path);
            }

            // 에셋 미존재 시 자동 에셋 생성 및 정밀 바인딩
            if (targetApplicator == null)
            {
                string folderPath = "Assets/02.System/01.UnitSystem/02.Visualizer";
                if (!AssetDatabase.IsValidFolder(folderPath))
                {
                    System.IO.Directory.CreateDirectory(folderPath);
                }

                string assetPath = $"{folderPath}/{defaultAssetName}.asset";
                targetApplicator = ScriptableObject.CreateInstance(targetClassType) as UnitVisualApplicatorSO;
                AssetDatabase.CreateAsset(targetApplicator, assetPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[PureDataUnitEditor] Auto Created Applicator Asset: {assetPath}");
            }

            if (targetApplicator != null)
            {
                applicatorProp.objectReferenceValue = targetApplicator;
                Debug.Log($"[PureDataUnitEditor] Bound {type} -> {targetApplicator.name}");
            }
        }
    }
}
