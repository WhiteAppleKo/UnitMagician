using UnityEngine;

namespace UnitSystem
{
    [CreateAssetMenu(fileName = "MassUnitApplicator_", menuName = "UnitSystem/Applicators/MassUnitApplicator")]
    public class MassUnitApplicatorSO : UnitVisualApplicatorSO
    {
        public override void Apply(GameObject target, RuntimeDataUnit runtimeData)
        {
            if (target == null || runtimeData == null) return;

            // Rigidbody 질량 변경
            var rb = target.GetComponent<Rigidbody>();
            float currentMass = runtimeData.CurrentValue;
            if (rb != null) rb.mass = Mathf.Max(0.1f, currentMass);

            // 상대 비율 스케일 연산 적용 (Ratio = CurrentMass / OriginalMass)
            float originalMass = runtimeData.OriginalValue > 0f ? runtimeData.OriginalValue : 1.0f;
            float ratio = currentMass / Mathf.Max(0.0001f, originalMass);

            // 스케일 비례 적용: 질량(부피) 비율의 세제곱근(Cube Root) 산출
            float scaleRatio = Mathf.Pow(ratio, 1f / 3f);
            
            // 초기 스케일 기준 세제곱근 비율 스케일 적용
            Vector3 baseScale = Vector3.one;
            var visualizer = target.GetComponent<UnitTargetObjectVisualizer>();
            if (visualizer != null)
            {
                baseScale = visualizer.InitialScale;
            }
            target.transform.localScale = baseScale * scaleRatio;

            Debug.Log($"[MassUnitApplicatorSO] Applied Mass & Scale for {target.name}. Mass: {currentMass}, Ratio: {ratio}");
        }
    }
}
