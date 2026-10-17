using GameEvent;

namespace BossPhaseSystem
{
    /// <summary>BossRecoverAction(이벤트 동작 SO)을 수행하는 핸들러입니다. 보스 HP를 SO의 회복 비율까지 채우고 무적을 풀어 일반 공격 순환으로 복귀시킵니다.</summary>
    public class BossRecoverActionHandler : EventActionHandler<BossRecoverAction>
    {
        private readonly IBossPhaseController boss;

        public BossRecoverActionHandler(IBossPhaseController boss)
        {
            this.boss = boss ?? throw new System.ArgumentNullException(nameof(boss));
        }

        protected override void Execute(BossRecoverAction action, IEventContext context)
        {
            boss.RecoverFromFail(action.RecoverHpRatio);
        }
    }
}
