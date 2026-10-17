using UnityEngine;

namespace GameEvent
{
    /// <summary>
    /// 그룹(groupId)의 NPC들의 반복 공격(NpcRepeatAttackAction)을 멈추게 하는 동작입니다.
    /// 공격 명령이 이동을 끊는 NPC 규칙 때문에, 비켜서기(NpcStepAsideAction) 전에 반드시 먼저 실행해야 합니다(25번 기획서 §9).
    /// </summary>
    [CreateAssetMenu(fileName = "NpcStopAttackAction_", menuName = "GameEvent/Action/NpcStopAttack")]
    public class NpcStopAttackAction : EventActionSO
    {
        [Tooltip("공격을 멈출 NPC 그룹 id(BlockerNPC.GroupId).")]
        [SerializeField] private string groupId;
        [Tooltip("멈춘 직후 재생할 모션 키. 비워 두면 모션을 재생하지 않습니다(Animator가 없는 NPC는 무시됩니다).")]
        [SerializeField] private string stopMotionKey = "Idle";

        public string GroupId => groupId;
        public string StopMotionKey => stopMotionKey;
    }
}
