using UnityEngine;
using InteractionSystem.Logic;

namespace NpcSystem
{
    /// <summary>
    /// 투사체를 스폰해서 발사하는 공격 방식입니다. 데미지 적용은 새로 만들지 않고
    /// 기존 CollisionDamageTrigger(공격/피격 파이프라인)를 그대로 재사용합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "ProjectileAttackBehavior_", menuName = "NpcSystem/AttackBehavior/Projectile")]
    public class ProjectileAttackBehaviorSO : NpcAttackBehaviorSO
    {
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileSpeed = 10.0f;

        public override void Execute(NpcCombatActorComponent actor, GameObject target)
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning($"[{name}] projectilePrefab이 설정되지 않았습니다.");
                return;
            }

            Vector3 spawnPosition = actor.transform.position;
            Vector3 direction = (target.transform.position - spawnPosition).normalized;
            Quaternion spawnRotation = direction.sqrMagnitude > 0f ? Quaternion.LookRotation(direction) : actor.transform.rotation;

            // 런타임 스폰 오브젝트는 자동 DI 대상이 아니므로 actor의 안전 스폰 헬퍼를 통해 주입까지 처리한다.
            var projectile = actor.SpawnAndInject(projectilePrefab, spawnPosition, spawnRotation);
            if (projectile == null) return;

            if (projectile.TryGetComponent<CollisionDamageTrigger>(out var trigger))
            {
                trigger.SetOwner(actor.gameObject);
                trigger.SetBaseDamage(actor.PureData.BaseDamage);
            }

            if (projectile.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.linearVelocity = direction * projectileSpeed;
            }
        }
    }
}
