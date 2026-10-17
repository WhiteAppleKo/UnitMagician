using UnityEngine;
using CharacterSystem;
using PipeLine.Combat;

namespace NpcSystem
{
    /// <summary>
    /// 투사체를 발사하는 공격 방식입니다. 데미지 적용은 새로 만들지 않고 기존 CollisionDamageTrigger(공격/피격 파이프라인)를 그대로 재사용합니다.
    /// 투사체는 매번 생성하지 않고 풀링(15-B)된 인스턴스를 재사용합니다 - 이 SO는 발사 설정(NpcProjectileLaunchData)만 만들어
    /// visualizer.SpawnProjectile에 넘기고, 어떤 프리팹을 풀링하는지는 풀(NpcProjectilePool)이 안다.
    /// </summary>
    [CreateAssetMenu(fileName = "ProjectileAttackBehavior_", menuName = "NpcSystem/AttackBehavior/Projectile")]
    public class ProjectileAttackBehaviorSO : NpcAttackBehaviorSO
    {
        [SerializeField] private float projectileSpeed = 10.0f;
        [Tooltip("대상에 Collider가 없을 때만 쓰는 폴백 조준 높이(대상 transform.position 기준 위쪽 오프셋, 미터). 대상에 Collider가 있으면 무시된다.")]
        [SerializeField] private float fallbackAimHeight = 1.0f;

        public override void Execute(INpcCombatActorVisualizer visualizer, PureDataNpcActor pureData, GameObject target)
        {
            Vector3 spawnPosition = visualizer.AttackOrigin;
            Vector3 direction = (ResolveAimPoint(target) - spawnPosition).normalized;
            Quaternion spawnRotation = direction.sqrMagnitude > 0f ? Quaternion.LookRotation(direction) : visualizer.Owner.transform.rotation;

            // 발사자/진영/공격 플래그는 발사자 오브젝트의 provider에서 읽는다(없으면 공격 플래그 None, 진영 Neutral).
            GameObject attacker = visualizer.Owner;
            AttackStepFlags attackFlags = AttackStepFlags.None;
            FactionType faction = FactionType.Neutral;
            if (attacker != null)
            {
                if (attacker.TryGetComponent<IAttackStepFlagsProvider>(out var attackFlagsProvider))
                {
                    attackFlags = attackFlagsProvider.CurrentAttackFlags;
                }

                if (attacker.TryGetComponent<IFactionProvider>(out var factionProvider))
                {
                    faction = factionProvider.Faction;
                }
            }

            // 외부가 이번 발사에 이벤트 투사체 전용 속도/데미지를 예약했으면(0 초과인 항목만) 그 값을 쓴다. 예약이 없으면 이 SO와 NPC 데이터의 값 그대로다.
            float speed = projectileSpeed;
            int damage = pureData.BaseDamage;
            if (visualizer.TryGetArmedLaunchOverride(out float overrideSpeed, out int overrideDamage))
            {
                if (overrideSpeed > 0f) speed = overrideSpeed;
                if (overrideDamage > 0) damage = overrideDamage;
            }

            // 발사자/진영/플래그/데미지/속도 주입은 풀에서 꺼낼 때(NpcProjectile.Setting/SetActive) 발사 설정으로 한 번에 적용된다.
            visualizer.SpawnProjectile(new NpcProjectileLaunchData(
                attacker,
                faction,
                attackFlags,
                spawnPosition,
                spawnRotation,
                direction * speed,
                damage));
        }

        /// <summary>
        /// 조준점 규칙: 대상의 활성 Collider 중심(Collider.bounds.center - CharacterController 포함)을 쓰고, Collider가 없으면
        /// transform.position + up * fallbackAimHeight로 폴백한다. 피벗이 발 위치인 캐릭터(플레이어)도 몸통 높이를 겨냥하게 하는 단일 규칙이며,
        /// 피벗이 이미 몸통 중심인 대상(보스 캡슐, 돌문 박스)은 조준점이 변하지 않는다.
        /// </summary>
        private Vector3 ResolveAimPoint(GameObject target)
        {
            if (target.TryGetComponent<Collider>(out var targetCollider) && targetCollider.enabled)
            {
                return targetCollider.bounds.center;
            }

            return target.transform.position + Vector3.up * fallbackAimHeight;
        }
    }
}
