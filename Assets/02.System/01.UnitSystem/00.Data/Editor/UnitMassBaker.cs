using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnitSystem
{
    /// <summary>
    /// 씬 내 질량 단위를 지원하는 오브젝트들의 부피와 스케일을 기반으로 기본 질량을 사전 연산(Bake)하는 에디터 툴입니다.
    /// </summary>
    public static class UnitMassBaker
    {
        [MenuItem("Tools/UnitMagician/Bake Mass Values")]
        public static void BakeAllMassValues()
        {
            var groups = Object.FindObjectsByType<RuntimeDataUnitGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int bakeCount = 0;

            foreach (var group in groups)
            {
                if (group == null) continue;

                if (group.HasUnit(UnitType.Mass))
                {
                    float calculatedMass = CalculateObjectVolume(group.gameObject);

                    if (calculatedMass <= 0f || float.IsNaN(calculatedMass) || float.IsInfinity(calculatedMass))
                    {
                        calculatedMass = 1.0f;
                    }

                    Undo.RecordObject(group, "Bake Unit Mass");
                    group.BakedMassValue = calculatedMass;
                    EditorUtility.SetDirty(group);

                    if (group.gameObject.scene.IsValid() && group.gameObject.scene.isLoaded)
                    {
                        EditorSceneManager.MarkSceneDirty(group.gameObject.scene);
                    }

                    bakeCount++;
                }
            }

            if (bakeCount > 0)
            {
                var activeScene = SceneManager.GetActiveScene();
                if (activeScene.IsValid() && activeScene.isLoaded)
                {
                    EditorSceneManager.MarkSceneDirty(activeScene);
                }
                Debug.Log($"[UnitMassBaker] Baked Mass values for {bakeCount} objects in the scene.");
            }
            else
            {
                Debug.Log("[UnitMassBaker] No objects required Mass baking.");
            }
        }

        /// <summary>
        /// 오브젝트 및 자식 계층의 메시/콜라이더/렌더러 바운드와 비균등 스케일(lossyScale)을 정밀 계산하여 총 부피를 산출합니다.
        /// </summary>
        private static float CalculateObjectVolume(GameObject target)
        {
            if (target == null) return 1.0f;

            // 1. MeshFilter 기반 볼륨 합산 (복합 메시 대응 및 비균등 lossyScale 절대값 반영)
            var meshFilters = target.GetComponentsInChildren<MeshFilter>(true);
            float totalMeshVolume = 0f;
            int validMeshCount = 0;

            foreach (var mf in meshFilters)
            {
                if (mf != null && mf.sharedMesh != null)
                {
                    Vector3 meshSize = mf.sharedMesh.bounds.size;
                    Vector3 lossy = mf.transform.lossyScale;
                    Vector3 scaledSize = Vector3.Scale(meshSize, new Vector3(Mathf.Abs(lossy.x), Mathf.Abs(lossy.y), Mathf.Abs(lossy.z)));
                    float meshVolume = scaledSize.x * scaledSize.y * scaledSize.z;
                    if (meshVolume > 0f)
                    {
                        totalMeshVolume += meshVolume;
                        validMeshCount++;
                    }
                }
            }

            if (validMeshCount > 0 && totalMeshVolume > 0f)
            {
                return totalMeshVolume;
            }

            // 2. Collider 기반 볼륨 합산 (메시가 없는 경우 콜라이더 바운드 대응)
            var colliders = target.GetComponentsInChildren<Collider>(true);
            float totalColliderVolume = 0f;
            int validColliderCount = 0;

            foreach (var col in colliders)
            {
                if (col != null)
                {
                    Vector3 size = col.bounds.size;
                    float colVolume = Mathf.Abs(size.x * size.y * size.z);
                    if (colVolume > 0f)
                    {
                        totalColliderVolume += colVolume;
                        validColliderCount++;
                    }
                }
            }

            if (validColliderCount > 0 && totalColliderVolume > 0f)
            {
                return totalColliderVolume;
            }

            // 3. Renderer 기반 볼륨 합산 (콜라이더도 없는 경우 렌더러 바운드 대응)
            var renderers = target.GetComponentsInChildren<Renderer>(true);
            float totalRendererVolume = 0f;
            int validRendererCount = 0;

            foreach (var rend in renderers)
            {
                if (rend != null)
                {
                    Vector3 size = rend.bounds.size;
                    float rendVolume = Mathf.Abs(size.x * size.y * size.z);
                    if (rendVolume > 0f)
                    {
                        totalRendererVolume += rendVolume;
                        validRendererCount++;
                    }
                }
            }

            if (validRendererCount > 0 && totalRendererVolume > 0f)
            {
                return totalRendererVolume;
            }

            return 1.0f;
        }
    }
}
