using UnityEngine;

namespace UnitSystem
{
    [CreateAssetMenu(fileName = "MassUnitApplicator_", menuName = "UnitSystem/Applicators/MassUnitApplicator")]
    public class MassUnitApplicatorSO : UnitVisualApplicatorSO
    {
        public override void Apply(GameObject target, RuntimeDataUnit runtimeData)
        {
            if (target == null || runtimeData == null) return;

            float currentMass = runtimeData.CurrentValue;
            float originalMass = runtimeData.OriginalValue > 0f ? runtimeData.OriginalValue : 1.0f;
            float ratio = currentMass / Mathf.Max(0.0001f, originalMass);

            // 스케일 비례 적용: 질량(부피) 비율의 세제곱근(Cube Root) 산출
            float scaleRatio = Mathf.Pow(ratio, 1f / 3f);

            var visualizer = target.GetComponent<IUnitTargetVisualizer>();
            if (visualizer != null)
            {
                visualizer.ApplyMassScale(currentMass, scaleRatio);
            }
            else
            {
                // Fallback for targets without IUnitTargetVisualizer
                var rb = target.GetComponent<Rigidbody>();
                if (rb != null) rb.mass = Mathf.Max(0.1f, currentMass);
                target.transform.localScale = Vector3.one * scaleRatio;
            }

            Debug.Log($"[MassUnitApplicatorSO] Applied Mass & Scale for {target.name}. Mass: {currentMass}, Ratio: {ratio}");
        }
    }
}
