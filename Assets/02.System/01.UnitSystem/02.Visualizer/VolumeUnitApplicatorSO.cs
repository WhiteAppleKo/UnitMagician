using UnityEngine;

namespace UnitSystem
{
    [CreateAssetMenu(fileName = "VolumeUnitApplicator_", menuName = "UnitSystem/Applicators/VolumeUnitApplicator")]
    public class VolumeUnitApplicatorSO : UnitVisualApplicatorSO
    {
        [Header("Volume & Liquid Specific Properties")]
        [SerializeField] private Material overlayMaterial;

        public override void Apply(GameObject target, RuntimeDataUnit runtimeData)
        {
            if (target == null || runtimeData == null) return;

            Material matToApply = runtimeData.CurrentUnitData != null && runtimeData.CurrentUnitData.OverlayMaterial != null 
                ? runtimeData.CurrentUnitData.OverlayMaterial 
                : overlayMaterial;

            if (matToApply != null)
            {
                var visualizer = target.GetComponent<IUnitTargetVisualizer>();
                if (visualizer != null)
                {
                    visualizer.ApplyMaterial(matToApply);
                }
                else
                {
                    // Fallback for targets without IUnitTargetVisualizer
                    var meshRenderer = target.GetComponent<MeshRenderer>();
                    if (meshRenderer != null)
                    {
                        meshRenderer.material = matToApply;
                    }
                }
            }

            Debug.Log($"[VolumeUnitApplicatorSO] Applied Material for {target.name}. Unit: {runtimeData.CurrentUnit}, Value: {runtimeData.CurrentValue}");
        }
    }
}
