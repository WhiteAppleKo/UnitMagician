using UnityEngine;

namespace BossPhaseSystem
{
    /// <summary>
    /// BossPhaseLogicSystem의 불변 설정값([D] Pure Data)입니다. PureDataNpcActor(07.NpcSystem)와 동일한 결로
    /// 인스펙터에서 조정 가능하게 합니다(03번 기획서 3.2/3.8/3.9절). 평상시 값(일반 공격 간격)만 남고, 이벤트 전용 값(무적 중 초당 회복량, 무적 최대 지속시간,
    /// 실패 시 회복 비율)은 Goal 51에서 이벤트 SO(BossRecoverAction)로 이전되었습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "PureDataBossPattern_", menuName = "BossPhaseSystem/PureDataBossPattern")]
    public class PureDataBossPattern : ScriptableObject
    {
        [Header("BasicAttack")]
        [Tooltip("BasicAttack 상태에서 bossActor.Attack(playerTarget)을 반복 호출하는 간격(초).")]
        [SerializeField] private float attackInterval = 2.0f;

        public float AttackInterval => attackInterval;
    }
}
