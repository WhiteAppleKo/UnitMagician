using UnityEngine;

namespace UnitSystem
{
    /// <summary>
    /// 온도(Temperature) 단위 적용자입니다. RuntimeDataUnit.CurrentValue(온도값)의 부호에 따라
    /// 얼음 방향(음수)일 때 얼음 머티리얼 교체 + 서리 파티클 연출을 수행합니다.
    /// 얼음/불 상성 반응(파지직 판정 등)은 04/05번 시스템 소관이며 이 클래스의 책임이 아닙니다.
    /// </summary>
    [CreateAssetMenu(fileName = "TemperatureUnitApplicator_", menuName = "UnitSystem/Applicators/TemperatureUnitApplicator")]
    public class TemperatureUnitApplicatorSO : UnitVisualApplicatorSO
    {
        [Header("Temperature(Ice) Specific Properties")]
        [SerializeField] private Material iceMaterial;
        [SerializeField] private GameObject frostParticlePrefab;

        public override void Apply(GameObject target, RuntimeDataUnit runtimeData)
        {
            if (target == null || runtimeData == null) return;

            float temperature = runtimeData.CurrentValue;
            bool isIceDirection = temperature < 0f;

            var visualizer = target.GetComponent<IUnitTargetVisualizer>();

            if (isIceDirection)
            {
                ApplyIceMaterial(target, visualizer);
                SpawnFrostParticle(target, visualizer, temperature);
            }

            Debug.Log($"[TemperatureUnitApplicatorSO] Applied Temperature for {target.name}. Value: {temperature}, IsIceDirection: {isIceDirection}");
        }

        private void ApplyIceMaterial(GameObject target, IUnitTargetVisualizer visualizer)
        {
            if (iceMaterial == null) return;

            if (visualizer != null)
            {
                visualizer.ApplyMaterial(iceMaterial);
            }
            else
            {
                // Fallback for targets without IUnitTargetVisualizer
                var meshRenderer = target.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.material = iceMaterial;
                }
            }
        }

        private void SpawnFrostParticle(GameObject target, IUnitTargetVisualizer visualizer, float temperature)
        {
            if (frostParticlePrefab == null) return;

            // 온도 크기에 비례한 서리 연출 강도 (부호는 이미 얼음 방향으로 확정된 상태)
            float intensity = Mathf.Clamp(Mathf.Abs(temperature) / 10f, 0.5f, 3f);

            GameObject effect;
            if (visualizer != null)
            {
                effect = visualizer.SpawnEffect(frostParticlePrefab, target.transform.position, target.transform.rotation);
            }
            else
            {
                // Fallback: 대상 자신의 컴포넌트가 아닌 독립된 이펙트 오브젝트 스폰이므로 직접 Instantiate 허용
                effect = Object.Instantiate(frostParticlePrefab, target.transform.position, target.transform.rotation);
            }

            if (effect != null)
            {
                effect.transform.localScale *= intensity;
            }
        }
    }
}
