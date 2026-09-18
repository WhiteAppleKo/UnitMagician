using UnityEngine;

namespace NpcSystem
{
    /// <summary>
    /// NpcCombatActorComponent의 불변 설정값(투사체/이동/공격 파라미터)을 보관하는 PureData입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "PureDataNpcActor_", menuName = "NpcSystem/PureDataNpcActor")]
    public class PureDataNpcActor : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3.0f;
        [SerializeField] private float arrivalThreshold = 0.1f;

        [Header("Attack")]
        [SerializeField] private float attackCooldown = 1.0f;
        [SerializeField] private int baseDamage = 10;
        [SerializeField] private NpcAttackBehaviorSO attackBehavior;

        public float MoveSpeed => moveSpeed;
        public float ArrivalThreshold => arrivalThreshold;

        public float AttackCooldown => attackCooldown;
        public int BaseDamage => baseDamage;

        /// <summary>이 NPC가 어떤 방식(투사체/근접/즉발 등)으로 공격을 실행할지 결정하는 교체 가능한 전략입니다.</summary>
        public NpcAttackBehaviorSO AttackBehavior => attackBehavior;
    }
}
