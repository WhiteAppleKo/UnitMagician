using UnityEditor;
using UnityEngine;

namespace UnitSystem
{
    public static class PureDataUnitValidator
    {
        [MenuItem("Tools/UnitMagician/Validate PureDataUnits", priority = 1)]
        public static void ValidateAllUnitsMenu()
        {
            ValidateAllUnits();
            Debug.Log("[PureDataUnitValidator] Validation Complete from Menu.");
        }

        [InitializeOnLoadMethod]
        private static void OnProjectLoadedInEditor()
        {
            ValidateAllUnits();
        }

        private static void ValidateAllUnits()
        {
            string[] guids = AssetDatabase.FindAssets("t:PureDataUnit");
            bool changed = false;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                PureDataUnit unitData = AssetDatabase.LoadAssetAtPath<PureDataUnit>(path);

                // 마이그레이션 로직 (이름 기반으로 잘못된 Enum 자동 복구)
                string uName = unitData.UnitName.ToLower();
                UnitType correctType = unitData.UnitType;
                if (uName.Contains("kg") || uName.Contains("g")) correctType = UnitType.Mass;
                else if (uName.Contains("l") || uName.Contains("ml")) correctType = UnitType.Volume;
                else if (uName.Contains("vector") || uName.Contains("reverse")) correctType = UnitType.Vector;

                bool needsSave = false;
                if (unitData.UnitType != correctType)
                {
                    var so = new SerializedObject(unitData);
                    so.FindProperty("unitType").intValue = (int)correctType;
                    so.ApplyModifiedProperties();
                    needsSave = true;
                    Debug.Log($"[PureDataUnitValidator] Migrated {unitData.name} to {correctType}");
                }

                if (unitData != null && unitData.Applicator == null)
                {
                    AutoAssignApplicatorDirect(unitData, path);
                    needsSave = true;
                }
                if (needsSave) changed = true;
            }

            if (changed)
            {
                AssetDatabase.SaveAssets();
                Debug.Log("[PureDataUnitValidator] Fixed missing applicators and saved assets.");
            }
        }

        private static void AutoAssignApplicatorDirect(PureDataUnit unitData, string assetPath)
        {
            string searchType = "";
            string defaultAssetName = "";
            System.Type targetClassType = null;

            switch (unitData.UnitType)
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

            string[] applicatorGuids = AssetDatabase.FindAssets(searchType);
            UnitVisualApplicatorSO targetApplicator = null;

            if (applicatorGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(applicatorGuids[0]);
                targetApplicator = AssetDatabase.LoadAssetAtPath<UnitVisualApplicatorSO>(path);
            }

            if (targetApplicator == null)
            {
                string folderPath = "Assets/02.System/01.UnitSystem/02.Visualizer";
                if (!AssetDatabase.IsValidFolder(folderPath))
                {
                    System.IO.Directory.CreateDirectory(folderPath);
                }

                string newAssetPath = $"{folderPath}/{defaultAssetName}.asset";
                targetApplicator = ScriptableObject.CreateInstance(targetClassType) as UnitVisualApplicatorSO;
                AssetDatabase.CreateAsset(targetApplicator, newAssetPath);
                Debug.Log($"[PureDataUnitValidator] Auto Created Applicator Asset: {newAssetPath}");
            }

            if (targetApplicator != null)
            {
                var serializedObject = new SerializedObject(unitData);
                var prop = serializedObject.FindProperty("applicator");
                if (prop != null)
                {
                    prop.objectReferenceValue = targetApplicator;
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(unitData);
                    Debug.Log($"[PureDataUnitValidator] Bound {unitData.UnitType} -> {targetApplicator.name} in {unitData.name}");
                }
            }
        }
    }
}
