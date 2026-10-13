using UnityEngine;
using VContainer;
using UnitSystem;

namespace PickupSystem
{
    /// <summary>
    /// 플레이어 근접 판정을 수행하는 Visualizer입니다. 매 프레임 플레이어 위치를 중심으로 pickupRadius 안의
    /// IPickupTarget(Physics.OverlapSphere + TryGetComponent, 12.AreaFreezeSystem과 동일한 방식) 중
    /// "아직 소유하지 않은 마법의 아이템"만 후보로 삼아 가장 가까운 것을 RuntimeDataPickupFocus에 기록합니다.
    /// 이미 소유한 마법의 아이템은 후보에서 제외되므로 프롬프트도 뜨지 않고 아이템은 그대로 남습니다.
    /// 감지 결과는 DetectedTarget으로만 기록하며, 입력 컨텍스트에 따른 줍기 가능 여부(CurrentTarget)는
    /// PickupLogicSystem이 결정합니다.
    /// </summary>
    public class PickupDetectorComponent : MonoBehaviour
    {
        private const int MaxOverlapCount = 32;

        [Tooltip("근접 판정 기준이 되는 플레이어 Transform(인스펙터 고정 참조).")]
        [SerializeField] private Transform playerTransform;

        [Tooltip("줍기 가능 반경(미터).")]
        [SerializeField, Min(0f)] private float pickupRadius = 1.5f;

        private readonly Collider[] overlapBuffer = new Collider[MaxOverlapCount];

        private RuntimeDataPickupFocus focus;
        private IUnitCatalogService catalogService;

        [Inject]
        public void Construct(RuntimeDataPickupFocus focus, IUnitCatalogService catalogService)
        {
            this.focus = focus;
            this.catalogService = catalogService;
        }

        private void Update()
        {
            if (focus == null || playerTransform == null) return;

            focus.SetDetectedTarget(FindNearestTarget());
        }

        private IPickupTarget FindNearestTarget()
        {
            Vector3 origin = playerTransform.position;
            int hitCount = Physics.OverlapSphereNonAlloc(origin, pickupRadius, overlapBuffer, ~0, QueryTriggerInteraction.Collide);

            IPickupTarget nearest = null;
            float nearestSqrDistance = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                if (!overlapBuffer[i].TryGetComponent<IPickupTarget>(out var candidate)) continue;
                if (catalogService != null && catalogService.IsPossessed(candidate.GrantedUnit)) continue;

                // OverlapSphere는 콜라이더 표면 기준이므로, 반경 판정은 아이템 중심 거리로 다시 확정한다.
                float sqrDistance = (candidate.Position - origin).sqrMagnitude;
                if (sqrDistance > pickupRadius * pickupRadius) continue;

                if (sqrDistance < nearestSqrDistance)
                {
                    nearestSqrDistance = sqrDistance;
                    nearest = candidate;
                }
            }

            return nearest;
        }

        private void OnDrawGizmosSelected()
        {
            if (playerTransform == null) return;

            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(playerTransform.position, pickupRadius);
        }
    }
}
