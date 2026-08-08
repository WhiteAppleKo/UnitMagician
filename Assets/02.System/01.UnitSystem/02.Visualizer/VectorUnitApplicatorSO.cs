using UnityEngine;

namespace UnitSystem
{
    [CreateAssetMenu(fileName = "VectorUnitApplicator_", menuName = "UnitSystem/Applicators/VectorUnitApplicator")]
    public class VectorUnitApplicatorSO : UnitVisualApplicatorSO
    {
        public override void Apply(GameObject target, RuntimeDataUnit runtimeData)
        {
            if (target == null || runtimeData == null) return;

            var rb = target.GetComponent<Rigidbody>();
            if (rb != null && rb.linearVelocity != Vector3.zero)
            {
                rb.linearVelocity = -rb.linearVelocity;
            }

            target.transform.forward = -target.transform.forward;

            Debug.Log($"[VectorUnitApplicatorSO] Applied Vector Reversal for {target.name}. Forward: {target.transform.forward}");
        }
    }
}
