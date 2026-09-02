using UnityEditor;
using UnityEngine;

namespace UnitSystem
{
    /// <summary>
    /// PureDataUnit ScriptableObject 에셋들의 데이터 무결성 검증 및 누락된 어플리케이터 자동 복구를 수행하는 에디터 툴입니다.
    /// </summary>
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

        public static void ValidateAllUnits()
        {
            string[] guids = AssetDatabase.FindAssets("t:PureDataUnit");
            bool changed = false;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                PureDataUnit unitData = AssetDatabase.LoadAssetAtPath<PureDataUnit>(path);
                if (unitData == null) continue;

                // 마이그레이션 로직 (이름 기반으로 잘못된 Enum 자동 복구)
                string uName = (unitData.UnitName ?? "").ToLower();
                string aName = unitData.name.ToLower();
                UnitType correctType = unitData.UnitType;

                if (uName.Contains("kg") || uName.Contains("g") || uName.Contains("mass") || uName.Contains("무게") ||
                    aName.Contains("kg") || aName.Contains("mass") || aName.Contains("무게"))
                {
                    correctType = UnitType.Mass;
                }
                else if (uName.Contains("l") || uName.Contains("ml") || uName.Contains("volume") || uName.Contains("부피") ||
                         aName.Contains("volume") || aName.Contains("부피"))
                {
                    correctType = UnitType.Volume;
                }
                else if (uName.Contains("vector") || uName.Contains("reverse") || uName.Contains("방향") || uName.Contains("벡터") ||
                         aName.Contains("vector") || aName.Contains("방향") || aName.Contains("벡터"))
                {
                    correctType = UnitType.Vector;
                }

                bool needsSave = false;
                if (unitData.UnitType != correctType && correctType != UnitType.None)
                {
                    var so = new SerializedObject(unitData);
                    so.FindProperty("unitType").intValue = (int)correctType;
                    so.ApplyModifiedProperties();
                    needsSave = true;
                    Debug.Log($"[PureDataUnitValidator] Migrated {unitData.name} to {correctType}");
                }

                if (unitData.Applicator == null)
                {
                    AutoAssignApplicatorDirect(unitData, path);
                    needsSave = true;
                }

                if (needsSave)
                {
                    EditorUtility.SetDirty(unitData);
                    changed = true;
                }
            }

            if (changed)
            {
                AssetDatabase.SaveAssets();
                Debug.Log("[PureDataUnitValidator] Fixed missing applicators and saved assets.");
            }
        }

        private static void AutoAssignApplicatorDirect(PureDataUnit unitData, string assetPath)
        {
            if (unitData == null) return;

            string searchType = "";
            string defaultAssetName = "";
            System.Type targetClassType = null;

            if ((unitData.UnitType & UnitType.Mass) != 0)
            {
                searchType = "t:MassUnitApplicatorSO";
                defaultAssetName = "MassUnitApplicator";
                targetClassType = typeof(MassUnitApplicatorSO);
            }
            else if ((unitData.UnitType & UnitType.Volume) != 0)
            {
                searchType = "t:VolumeUnitApplicatorSO";
                defaultAssetName = "VolumeUnitApplicator";
                targetClassType = typeof(VolumeUnitApplicatorSO);
            }
            else if ((unitData.UnitType & UnitType.Vector) != 0)
            {
                searchType = "t:VectorUnitApplicatorSO";
                defaultAssetName = "VectorUnitApplicator";
                targetClassType = typeof(VectorUnitApplicatorSO);
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
