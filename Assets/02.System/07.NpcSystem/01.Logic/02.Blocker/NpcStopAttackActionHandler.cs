using UnityEngine;
using GameEvent;

namespace NpcSystem
{
    /// <summary>NpcStopAttackAction(이벤트 동작 SO)을 수행하는 핸들러입니다. 그룹 NPC들의 반복 공격을 멈추고, SO에 모션 키가 있으면 그 모션을 재생합니다.</summary>
    public class NpcStopAttackActionHandler : EventActionHandler<NpcStopAttackAction>
    {
        private readonly IBlockerNpcRegistry blockerNpcs;
        private readonly INpcRepeatAttackController repeatAttack;

        public NpcStopAttackActionHandler(IBlockerNpcRegistry blockerNpcs, INpcRepeatAttackController repeatAttack)
        {
            this.blockerNpcs = blockerNpcs ?? throw new System.ArgumentNullException(nameof(blockerNpcs));
            this.repeatAttack = repeatAttack ?? throw new System.ArgumentNullException(nameof(repeatAttack));
        }

        protected override void Execute(NpcStopAttackAction action, IEventContext context)
        {
            var group = blockerNpcs.GetGroup(action.GroupId);
            if (group.Count == 0)
            {
                Debug.LogWarning($"[NpcStopAttackActionHandler] 이벤트 '{context.EventId}': NPC 그룹 '{action.GroupId}'에 등록된 NPC가 없습니다.");
                return;
            }

            foreach (var npc in group)
            {
                repeatAttack.End(npc.CombatActor);

                if (!string.IsNullOrEmpty(action.StopMotionKey))
                {
                    npc.CombatActor.PlayMotion(action.StopMotionKey);
                }
            }
        }
    }
}
