using UnityEngine;

namespace UnitSystem
{
    [CreateAssetMenu(fileName = "MassUnitApplicator_", menuName = "UnitSystem/Applicators/MassUnitApplicator")]
    public class MassUnitApplicatorSO : UnitVisualApplicatorSO
    {
        [Header("Mass VFX Hook")]
        [SerializeField] private GameObject scaleChangeVfxPrefab;

        /// <summary>
        /// 질량 비율(CurrentValue/OriginalValue)을 계산하는 공개 정적 헬퍼입니다.
        /// 04번(파괴 가능 오브젝트) 시스템 등 외부에서 데미지 공식에 동일한 비율 값을 재사용할 때
        /// 이 헬퍼를 호출해 중복 계산을 피하십시오.
        /// </summary>
        public static float CalculateMassRatio(RuntimeDataUnit runtimeData)
        {
            if (runtimeData == null) return 1f;

            float currentMass = runtimeData.CurrentValue;
            float originalMass = runtimeData.OriginalValue > 0f ? runtimeData.OriginalValue : 1.0f;
            return currentMass / Mathf.Max(0.0001f, originalMass);
        }

        public override void Apply(GameObject target, RuntimeDataUnit runtimeData)
        {
            if (target == null || runtimeData == null) return;

            float currentMass = runtimeData.CurrentValue;
            float ratio = CalculateMassRatio(runtimeData);

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

            // VFX 훅: 스케일 변화가 실제로 일어나는 시점에 마법 링 회전 + 팽창 이펙트 스폰
            if (scaleChangeVfxPrefab != null)
            {
                if (visualizer != null)
                {
                    visualizer.SpawnEffect(scaleChangeVfxPrefab, target.transform.position, target.transform.rotation);
                }
                else
                {
                    // Fallback: 대상 자신의 컴포넌트가 아닌 독립된 이펙트 오브젝트 스폰이므로 직접 Instantiate 허용
                    Object.Instantiate(scaleChangeVfxPrefab, target.transform.position, target.transform.rotation);
                }
            }

            Debug.Log($"[MassUnitApplicatorSO] Applied Mass & Scale for {target.name}. Mass: {currentMass}, Ratio: {ratio}");
        }
    }
}
