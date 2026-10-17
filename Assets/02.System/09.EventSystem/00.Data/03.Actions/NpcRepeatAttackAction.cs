using UnityEngine;

namespace GameEvent
{
    /// <summary>
    /// 그룹(groupId)의 NPC들이 대상(targetId)을 일정 주기로 반복 공격하게 하는 동작입니다. 반복은 NpcStopAttackAction으로 멈출 때까지 계속됩니다.
    /// 투사체 속도/데미지는 NPC 자신의 데이터(PureDataNpcActor/공격 전략 SO)가 소유하므로 이 동작에는 없습니다(돌문 샷 값 변경 없음).
    /// 첫 발은 시작 후 intervalSeconds가 지난 시점에 나갑니다(기존 돌문 사격 타이머와 동일).
    /// </summary>
    [CreateAssetMenu(fileName = "NpcRepeatAttackAction_", menuName = "GameEvent/Action/NpcRepeatAttack")]
    public class NpcRepeatAttackAction : EventActionSO
    {
        [Tooltip("공격할 NPC 그룹 id(BlockerNPC.GroupId).")]
        [SerializeField] private string groupId;
        [Tooltip("공격 대상 id(IEventTarget.TargetId).")]
        [SerializeField] private string targetId;
        [Tooltip("발사 간격(초).")]
        [SerializeField] private float intervalSeconds = 3.5f;

        public string GroupId => groupId;
        public string TargetId => targetId;
        public float IntervalSeconds => intervalSeconds;
    }
}
