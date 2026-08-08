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

            var meshRenderer = target.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                Material matToApply = runtimeData.CurrentUnitData != null && runtimeData.CurrentUnitData.OverlayMaterial != null ? runtimeData.CurrentUnitData.OverlayMaterial : overlayMaterial;

                if (matToApply != null)
                {
                    meshRenderer.material = matToApply;
                }
            }

            Debug.Log($"[VolumeUnitApplicatorSO] Applied Material for {target.name}. Unit: {runtimeData.CurrentUnit}, Value: {runtimeData.CurrentValue}");
        }
    }
}
