using UnityEngine;
using CharacterSystem;
using PipeLine.Combat;

namespace NpcSystem
{
    /// <summary>
    /// 아군 투사체 1발의 발사 설정([D] 순수 값 묶음, 15-B). IPoolService.Get(TData)의 TData이며,
    /// 예전에 ProjectileAttackBehaviorSO가 SpawnAndInject 직후 하나씩 주입하던 값(발사자/발사 지점/방향/속도/데미지)을 담는다.
    /// 풀에서 꺼낼 때(NpcProjectile.Setting/SetActive) 한 번에 적용된다.
    /// 16-A: 투사체가 독자적인 공격 상태를 갖도록 발사자(Attacker)/발사 시점 진영(Faction)/공격 플래그(AttackFlags)를 함께 담는다.
    /// </summary>
    public readonly struct NpcProjectileLaunchData
    {
        /// <summary>발사한 NPC(= DamageContext.Attacker가 되는 "발사자". 발사 시 1회 확정되며 마법이 바꾸지 않는다).</summary>
        public readonly GameObject Attacker;

        /// <summary>발사 시점의 발사자 진영(발사자에게 IFactionProvider가 없으면 Neutral).</summary>
        public readonly FactionType Faction;

        /// <summary>발사 시점의 발사자 공격 플래그(발사자에게 IAttackStepFlagsProvider가 없으면 None).</summary>
        public readonly AttackStepFlags AttackFlags;

        /// <summary>발사 위치(= 최대 거리 판정의 기준 발사 지점).</summary>
        public readonly Vector3 Position;

        /// <summary>발사 방향을 바라보는 회전.</summary>
        public readonly Quaternion Rotation;

        /// <summary>발사 직후 Rigidbody 속도(방향 x 속력).</summary>
        public readonly Vector3 Velocity;

        /// <summary>CollisionDamageTrigger에 설정되는 기본 데미지.</summary>
        public readonly int BaseDamage;

        public NpcProjectileLaunchData(GameObject attacker, FactionType faction, AttackStepFlags attackFlags,
            Vector3 position, Quaternion rotation, Vector3 velocity, int baseDamage)
        {
            Attacker = attacker;
            Faction = faction;
            AttackFlags = attackFlags;
            Position = position;
            Rotation = rotation;
            Velocity = velocity;
            BaseDamage = baseDamage;
        }
    }
}
