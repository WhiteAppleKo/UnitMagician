using UnityEngine;

namespace BossPhaseSystem
{
    /// <summary>
    /// BossPhaseLogicSystem의 불변 설정값([D] Pure Data)입니다. PureDataNpcActor(07.NpcSystem)와 동일한 결로
    /// 인스펙터에서 조정 가능하게 합니다(03번 기획서 3.2/3.8/3.9절).
    /// </summary>
    [CreateAssetMenu(fileName = "PureDataBossPattern_", menuName = "BossPhaseSystem/PureDataBossPattern")]
    public class PureDataBossPattern : ScriptableObject
    {
        [Header("BasicAttack")]
        [Tooltip("BasicAttack 상태에서 bossActor.Attack(playerTarget)을 반복 호출하는 간격(초).")]
        [SerializeField] private float attackInterval = 2.0f;

        [Header("InvulnerableHealing")]
        [Tooltip("InvulnerableHealing 상태 동안 초당 회복량(HP).")]
        [SerializeField] private float healPerSecond = 5.0f;

        [Tooltip("InvulnerableHealing 상태의 최대 지속시간(초) - 06번 시퀀서의 타임아웃 판정이 이 값을 참조합니다" +
                 "(이 시스템 자신은 타임아웃을 판정하지 않고 값만 노출합니다).")]
        [SerializeField] private float invulnerablePhaseTimeoutSeconds = 10.0f;

        [Header("OnCoreHitFail")]
        [Tooltip("4단계 실패(OnCoreHitFail) 시 재설정할 HP 비율(현재/최대). 기본 0.6 (60%).")]
        [SerializeField] private float reviveHpRatioOnFail = 0.6f;

        public float AttackInterval => attackInterval;
        public float HealPerSecond => healPerSecond;
        public float InvulnerablePhaseTimeoutSeconds => invulnerablePhaseTimeoutSeconds;
        public float ReviveHpRatioOnFail => reviveHpRatioOnFail;
    }
}
