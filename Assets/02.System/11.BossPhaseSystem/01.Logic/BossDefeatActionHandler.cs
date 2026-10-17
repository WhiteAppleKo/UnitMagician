using GameEvent;

namespace BossPhaseSystem
{
    /// <summary>BossDefeatAction(이벤트 동작 SO)을 수행하는 핸들러입니다. 보스를 처치합니다(HP 0 반영, 사망 페이즈, 사망 모션).</summary>
    public class BossDefeatActionHandler : EventActionHandler<BossDefeatAction>
    {
        private readonly IBossPhaseController boss;

        public BossDefeatActionHandler(IBossPhaseController boss)
        {
            this.boss = boss ?? throw new System.ArgumentNullException(nameof(boss));
        }

        protected override void Execute(BossDefeatAction action, IEventContext context)
        {
            boss.Defeat();
        }
    }
}
