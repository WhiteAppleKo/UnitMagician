using UnityEngine;
using GameEvent;

namespace BossPhaseSystem
{
    /// <summary>BossInvulnerableAction(이벤트 동작 SO)을 수행하는 핸들러입니다. 보스를 무적·회복 페이즈로 진입시키고, 회복 수치(초당 회복량, 회복 상한)는 SO가 가리키는 BossRecoverAction에서 읽습니다.</summary>
    public class BossInvulnerableActionHandler : EventActionHandler<BossInvulnerableAction>
    {
        private readonly IBossPhaseController boss;

        public BossInvulnerableActionHandler(IBossPhaseController boss)
        {
            this.boss = boss ?? throw new System.ArgumentNullException(nameof(boss));
        }

        protected override void Execute(BossInvulnerableAction action, IEventContext context)
        {
            if (action.RecoverSettings == null)
            {
                Debug.LogError($"[BossInvulnerableActionHandler] 이벤트 '{context.EventId}': 동작 '{action.name}'에 회복 수치(recoverSettings)가 연결되지 않아 무적 진입을 하지 않았습니다.");
                return;
            }

            boss.EnterInvulnerableHealing(action.RecoverSettings.HealPerSecond, action.RecoverSettings.RecoverHpRatio);
        }
    }
}
