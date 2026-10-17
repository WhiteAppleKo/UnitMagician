using System.Collections.Generic;
using UnityEngine;
using UnitSystem;
using PipeLine.Combat;

namespace CharacterSystem
{
    [CreateAssetMenu(fileName = "PureStatData_", menuName = "CharacterSystem/PureStatData")]
    public class PureStatData : ScriptableObject
    {
        [Header("Character Base Stats")]
        [SerializeField] private FactionType defaultFaction = FactionType.Enemy;
        [SerializeField] private int maxHP = 100;
        [SerializeField] private int maxMP = 100;
        [SerializeField] private float baseMoveSpeed = 5.0f;
        [SerializeField] private List<PureDataUnit> defaultUnitMagics = new List<PureDataUnit>();

        [Header("Stamina (19-A)")]
        [Tooltip("최대 스태미나.")]
        [SerializeField] private float maxStamina = 100f;
        [Tooltip("회복이 시작된 뒤 초당 회복량(게임 시간 기준).")]
        [SerializeField] private float staminaRegenPerSecond = 30f;
        [Tooltip("마지막 소모 뒤 회복이 시작되기까지의 지연(초, 게임 시간 기준).")]
        [SerializeField] private float staminaRegenDelaySeconds = 1f;

        [Header("Combat Pipeline Flags (엔티티별 공격/피격 스텝 기본값)")]
        [Tooltip("이 캐릭터가 공격할 때 쓰는 공격 파이프라인 스텝 기본값. 크리티컬은 게임 전체에서 쓰지 않으므로 기본 None.")]
        [SerializeField] private AttackStepFlags defaultAttackFlags = AttackStepFlags.None;
        [Tooltip("이 캐릭터가 맞을 때 쓰는 피격 파이프라인 스텝 기본값. 기본 Evasion|Defense(기존 동작 유지).")]
        [SerializeField] private HitStepFlags defaultHitFlags = HitStepFlags.Evasion | HitStepFlags.Defense;

        [Header("Hostility Rule (21: 중립 NPC 적대화)")]
        [Tooltip("중립(Neutral) 상태에서 유효 시간 이내에 이 횟수만큼 연속으로 피격되면 Enemy가 된다. 0 이하이면 적대화하지 않는다.")]
        [SerializeField] private int hostileHitThreshold = 3;
        [Tooltip("연속 피격으로 인정되는 피격 간 최대 간격(초, 게임 시간).")]
        [SerializeField] private float hostileHitWindowSeconds = 10f;

        public int HostileHitThreshold => hostileHitThreshold;
        public float HostileHitWindowSeconds => hostileHitWindowSeconds;
        public AttackStepFlags DefaultAttackFlags => defaultAttackFlags;
        public HitStepFlags DefaultHitFlags => defaultHitFlags;
        public FactionType DefaultFaction => defaultFaction;
        public int MaxHP => maxHP;
        public int MaxMP => maxMP;
        public float BaseMoveSpeed => baseMoveSpeed;
        public float MaxStamina => maxStamina;
        public float StaminaRegenPerSecond => staminaRegenPerSecond;
        public float StaminaRegenDelaySeconds => staminaRegenDelaySeconds;
        public IReadOnlyList<PureDataUnit> DefaultUnitMagics => defaultUnitMagics;
    }
}
