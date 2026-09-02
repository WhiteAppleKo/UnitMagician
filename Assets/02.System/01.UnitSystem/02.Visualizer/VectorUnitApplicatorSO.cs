using UnityEngine;

namespace UnitSystem
{
    [CreateAssetMenu(fileName = "VectorUnitApplicator_", menuName = "UnitSystem/Applicators/VectorUnitApplicator")]
    public class VectorUnitApplicatorSO : UnitVisualApplicatorSO
    {
        public override void Apply(GameObject target, RuntimeDataUnit runtimeData)
        {
            if (target == null || runtimeData == null) return;

            Vector3 applyDirection = runtimeData.VectorDirection != Vector3.zero ? runtimeData.VectorDirection : target.transform.forward;
            float applySpeed = runtimeData.VectorSpeed;

            var visualizer = target.GetComponent<IUnitTargetVisualizer>();
            if (visualizer != null)
            {
                visualizer.ApplyVelocity(applyDirection, applySpeed);
            }
            else
            {
                // Fallback for targets without IUnitTargetVisualizer
                var rb = target.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = applyDirection * applySpeed;
                }

                if (applyDirection != Vector3.zero)
                {
                    target.transform.forward = applyDirection;
                }
            }

            Debug.Log($"[VectorUnitApplicatorSO] Applied Vector for {target.name}. Direction: {applyDirection}, Speed: {applySpeed}");
        }
    }
}
