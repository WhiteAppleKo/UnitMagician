using UnityEngine;
using GameEvent;

namespace NpcSystem
{
    /// <summary>
    /// NpcStepAsideAction(이벤트 동작 SO)을 수행하는 핸들러입니다. 그룹 NPC들을 각자의 비켜설 위치(IBlockerNpc.StepAsidePosition)로 MoveTo 시킵니다.
    /// 공격 명령이 이동을 끊는 NPC 규칙 때문에 이 동작은 반드시 반복 공격 중단(NpcStopAttackAction) 뒤에 실행되도록 이벤트 SO에서 순서를 둡니다.
    /// </summary>
    public class NpcStepAsideActionHandler : EventActionHandler<NpcStepAsideAction>
    {
        private readonly IBlockerNpcRegistry blockerNpcs;

        public NpcStepAsideActionHandler(IBlockerNpcRegistry blockerNpcs)
        {
            this.blockerNpcs = blockerNpcs ?? throw new System.ArgumentNullException(nameof(blockerNpcs));
        }

        protected override void Execute(NpcStepAsideAction action, IEventContext context)
        {
            var group = blockerNpcs.GetGroup(action.GroupId);
            if (group.Count == 0)
            {
                Debug.LogWarning($"[NpcStepAsideActionHandler] 이벤트 '{context.EventId}': NPC 그룹 '{action.GroupId}'에 등록된 NPC가 없습니다.");
                return;
            }

            foreach (var npc in group)
            {
                npc.CombatActor.MoveTo(npc.StepAsidePosition, action.MoveSpeed);
            }
        }
    }
}
