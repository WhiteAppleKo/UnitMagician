using UnityEngine;

namespace GameEvent
{
    /// <summary>
    /// 그룹(groupId)의 NPC들이 각자의 "비켜설 위치"(BlockerNPC.StepAsidePosition, 씬 고정 좌표 - 사용자 확정 25번 §7-2)로 MoveTo 하게 하는 동작입니다.
    /// 이동 방식은 NPC의 직선 이동이므로 경로가 비어 있어야 하며 이는 씬 배치 책임입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NpcStepAsideAction_", menuName = "GameEvent/Action/NpcStepAside")]
    public class NpcStepAsideAction : EventActionSO
    {
        [Tooltip("비켜설 NPC 그룹 id(BlockerNPC.GroupId).")]
        [SerializeField] private string groupId;
        [Tooltip("비켜서는 이동 속도(m/s). 0 이하이면 NPC 데이터(PureDataNpcActor)의 moveSpeed를 씁니다.")]
        [SerializeField] private float moveSpeed = 0f;

        public string GroupId => groupId;
        public float MoveSpeed => moveSpeed;
    }
}
