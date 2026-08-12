using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnitSystem
{
    public static class UnitMassBaker
    {
        [MenuItem("Tools/UnitMagician/Bake Mass Values")]
        public static void BakeAllMassValues()
        {
            var groups = Object.FindObjectsByType<RuntimeDataUnitGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int bakeCount = 0;

            foreach (var group in groups)
            {
                if (group.HasUnit(UnitType.Mass))
                {
                    float calculatedMass = 1.0f;
                    
                    var meshFilter = group.GetComponentInChildren<MeshFilter>();
                    if (meshFilter != null && meshFilter.sharedMesh != null)
                    {
                        // 원본 메시 크기에 씬에 배치된 실제 스케일(lossyScale) 곱셈 적용
                        Vector3 scaledSize = Vector3.Scale(meshFilter.sharedMesh.bounds.size, group.transform.lossyScale);
                        calculatedMass = scaledSize.x * scaledSize.y * scaledSize.z;
                    }
                    else
                    {
                        var renderer = group.GetComponentInChildren<Renderer>();
                        if (renderer != null)
                        {
                            Vector3 size = renderer.bounds.size;
                            calculatedMass = size.x * size.y * size.z;
                        }
                    }

                    if (calculatedMass <= 0f) calculatedMass = 1.0f;

                    group.bakedMassValue = calculatedMass;
                    EditorUtility.SetDirty(group);
                    bakeCount++;
                }
            }

            if (bakeCount > 0)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                Debug.Log($"[UnitMassBaker] Baked Mass values for {bakeCount} objects in the scene.");
            }
            else
            {
                Debug.Log("[UnitMassBaker] No objects required Mass baking.");
            }
        }
    }
}
