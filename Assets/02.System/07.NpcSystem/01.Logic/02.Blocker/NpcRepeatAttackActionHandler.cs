using UnityEngine;
using GameEvent;

namespace NpcSystem
{
    /// <summary>
    /// NpcRepeatAttackAction(이벤트 동작 SO)을 수행하는 핸들러입니다. 그룹의 NPC들을 대상(IEventTarget)을 향한 반복 공격에 투입합니다.
    /// 발사 간격은 동작 SO 값이며, 실제 반복/발사는 INpcRepeatAttackController가 담당합니다.
    /// </summary>
    public class NpcRepeatAttackActionHandler : EventActionHandler<NpcRepeatAttackAction>
    {
        private readonly IBlockerNpcRegistry blockerNpcs;
        private readonly INpcRepeatAttackController repeatAttack;

        public NpcRepeatAttackActionHandler(IBlockerNpcRegistry blockerNpcs, INpcRepeatAttackController repeatAttack)
        {
            this.blockerNpcs = blockerNpcs ?? throw new System.ArgumentNullException(nameof(blockerNpcs));
            this.repeatAttack = repeatAttack ?? throw new System.ArgumentNullException(nameof(repeatAttack));
        }

        protected override void Execute(NpcRepeatAttackAction action, IEventContext context)
        {
            if (!context.TryGetTarget(action.TargetId, out var target))
            {
                Debug.LogError($"[NpcRepeatAttackActionHandler] 이벤트 '{context.EventId}': 공격 대상 '{action.TargetId}'를 찾을 수 없습니다.");
                return;
            }

            var group = blockerNpcs.GetGroup(action.GroupId);
            if (group.Count == 0)
            {
                Debug.LogWarning($"[NpcRepeatAttackActionHandler] 이벤트 '{context.EventId}': NPC 그룹 '{action.GroupId}'에 등록된 NPC가 없습니다.");
                return;
            }

            foreach (var npc in group)
            {
                repeatAttack.Begin(npc.CombatActor, target.TargetObject, action.IntervalSeconds);
            }
        }
    }
}
