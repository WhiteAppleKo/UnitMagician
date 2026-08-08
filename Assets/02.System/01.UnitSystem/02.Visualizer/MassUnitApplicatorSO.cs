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

            // 초기 스케일 기준 상대 비율 스케일 적용
            Vector3 baseScale = Vector3.one;
            var visualizer = target.GetComponent<UnitTargetObjectVisualizer>();
            if (visualizer != null)
            {
                baseScale = visualizer.InitialScale;
            }
            target.transform.localScale = baseScale * ratio;

            Debug.Log($"[MassUnitApplicatorSO] Applied Mass & Scale for {target.name}. Mass: {currentMass}, Ratio: {ratio}");
        }
    }
}
