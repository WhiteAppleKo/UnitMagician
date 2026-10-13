using UnityEngine;
using VContainer;
using Pooling;
using InteractionSystem.Logic;

namespace NpcSystem
{
    /// <summary>
    /// 풀링되는 아군 투사체(NpcProjectile_Dummy)의 [V] World Visualizer입니다(15-B). IPoolable&lt;NpcProjectileLaunchData&gt;를 구현해
    /// IPoolService.Get(발사 설정)으로 꺼내고 Release로 반환합니다. "언제 반환할지"의 빗나감 판단은 NpcProjectileMissLogicSystem(순수 로직)이 하고,
    /// 이 클래스는 Rigidbody/Transform 값을 읽고 쓰는 수행자입니다.
    ///
    /// 반환 경로(두 가지):
    ///  (a) 충돌 후: CollisionDamageTrigger.OnImpactResolved(판정 ProcessDamageAsync가 모두 끝난 뒤) → Release. 13번 이벤트 투사체의 판정/종료(End)가 먼저 처리된다.
    ///  (b) 빗나감: 인정된 충돌 없이 발사 지점으로부터 최대 거리 또는 최대 수명을 넘으면 → Release. (판정이 진행 중이면 반환하지 않는다.)
    ///
    /// 재사용 시 상태 초기화는 같은 프리팹 안의 IPoolReusable 구현체들(RuntimeDataUnitGroup/UnitTargetObjectVisualizer/CollisionDamageTrigger)이
    /// Setting 단계(활성화 이전)에서 각자 되돌리고, 이 클래스가 Rigidbody 속도를 되돌립니다.
    /// 투사체의 공격 상태(발사자/진영/공격 플래그)는 CollisionDamageTrigger가 보관하며, ResetForReuse에서 초기화된 뒤 SetActive에서 발사 설정 값으로 확정됩니다(16-A).
    /// Generation은 사용 횟수(세대) 토큰으로, 반환 후 재사용된 인스턴스를 "같은 투사체"로 오인하지 않게 하는 데 쓰입니다(13번 이벤트 추적기).
    /// </summary>
    public class NpcProjectile : MonoBehaviour, IPoolable<NpcProjectileLaunchData>
    {
        private IPoolService<NpcProjectile, NpcProjectileLaunchData> pool;
        private NpcProjectileMissLogicSystem missLogic;

        private Rigidbody body;
        private CollisionDamageTrigger damageTrigger;
        private IPoolReusable[] reusables;

        private NpcProjectileLaunchData launchData;
        private Vector3 launchOrigin;

        /// <summary>풀에서 꺼내져 사용 중인지 여부(Release 중복 호출 방지).</summary>
        public bool IsInUse { get; private set; }

        /// <summary>풀에서 꺼내질 때마다 1씩 증가하는 세대 값. 같은 인스턴스라도 세대가 다르면 다른 발사다.</summary>
        public int Generation { get; private set; }

        [Inject]
        public void Construct(IPoolService<NpcProjectile, NpcProjectileLaunchData> pool, PureDataNpcProjectile pureData)
        {
            this.pool = pool;
            missLogic = new NpcProjectileMissLogicSystem(pureData);
        }

        private void Awake()
        {
            // 같은 프리팹 내부 구성요소 참조(VContainer_가이드라인 §4: 프리팹 자체 내부 참조는 직렬화/로컬 탐색 유지 대상)
            body = GetComponent<Rigidbody>();
            damageTrigger = GetComponent<CollisionDamageTrigger>();
            reusables = GetComponentsInChildren<IPoolReusable>(true);
        }

        private void OnEnable()
        {
            if (damageTrigger != null) damageTrigger.OnImpactResolved += HandleImpactResolved;
        }

        private void OnDisable()
        {
            if (damageTrigger != null) damageTrigger.OnImpactResolved -= HandleImpactResolved;
        }

        /// <summary>IPoolable: 활성화 이전. 이전 사용에서 바뀐 모든 상태를 초기값으로 되돌리고 발사 설정을 받아 둔다.</summary>
        public void Setting(NpcProjectileLaunchData data)
        {
            for (int i = 0; i < reusables.Length; i++)
            {
                reusables[i].ResetForReuse();
            }

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(data.Position, data.Rotation);

            launchData = data;
            launchOrigin = data.Position;
            Generation++;
            missLogic.Begin();
        }

        /// <summary>IPoolable: 활성화. 활성 상태에서만 가능한 설정(발사 진영 레이어, 속도)을 적용한다.</summary>
        public void SetActive()
        {
            gameObject.SetActive(true);
            IsInUse = true;

            if (damageTrigger != null)
            {
                // 16-A/16-B: 투사체의 독자 공격 상태(발사자/진영/공격 플래그)를 발사 설정 값으로 확정하고, 발사 진영 레이어를
                // 루트와 모든 자식 콜라이더에 적용한다. 발사자는 attacker로만 존재한다(owner/충돌 쌍 없음).
                damageTrigger.ApplyLaunchAttackState(launchData.Attacker, launchData.Faction, launchData.AttackFlags);
                damageTrigger.SetBaseDamage(launchData.BaseDamage);
            }

            body.linearVelocity = launchData.Velocity;
        }

        /// <summary>IPoolable: 반환. 소실을 통지하고(락온 큐 정리 등) 속도를 멈춘 뒤 비활성화한다.</summary>
        public void ReturnToPool()
        {
            IsInUse = false;

            for (int i = 0; i < reusables.Length; i++)
            {
                reusables[i].OnReturnedToPool();
            }

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!IsInUse) return;

            // 인정된 충돌이 있으면 판정이 끝날 때까지(OnImpactResolved) 기다린다 - 판정 중에 반환하면 안 된다.
            if (damageTrigger != null && damageTrigger.HasImpacted) return;

            float distance = Vector3.Distance(transform.position, launchOrigin);
            if (missLogic.Tick(Time.deltaTime, distance))
            {
                ReleaseToPool();
            }
        }

        private void HandleImpactResolved()
        {
            ReleaseToPool();
        }

        private void ReleaseToPool()
        {
            if (!IsInUse || pool == null) return;

            pool.Release(this);
        }
    }
}
