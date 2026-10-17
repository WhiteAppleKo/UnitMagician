using System;
using GameEvent;

namespace CharacterSystem
{
    /// <summary>
    /// TimeSlowOverrideAction(이벤트 동작 SO)을 수행하는 핸들러입니다([L] 호출형 순수 로직). 시작 동작이면 이벤트 SO의 감속 수치(최저 timeScale, 접근/이탈 곡선)를
    /// TimeSlowLogicSystem에 넘겨 이벤트 감속(투사체 비행 구간/진행도로 구동되는 시간 개입)과 마나 무제한을 시작하고, 해제 동작이면 끝냅니다.
    /// 예전에는 TimeSlowLogicSystem이 06(GameEvent)의 OnBoss50Percent/OnCoreHitSuccess/OnCoreHitFail 고정 신호를 직접 구독했으나,
    /// 지금은 이벤트 SO가 이 동작으로 명시적으로 지시합니다(Goal 51). 이벤트 시스템은 CharacterLifetimeScope와 서로 다른 VContainer 스코프 트리라
    /// CharacterLifetimeScope가 Start()에서 이벤트 시스템에 사후 등록합니다.
    /// </summary>
    public class TimeSlowOverrideActionHandler : EventActionHandler<TimeSlowOverrideAction>
    {
        private readonly TimeSlowLogicSystem timeSlow;

        public TimeSlowOverrideActionHandler(TimeSlowLogicSystem timeSlow)
        {
            this.timeSlow = timeSlow ?? throw new ArgumentNullException(nameof(timeSlow));
        }

        protected override void Execute(TimeSlowOverrideAction action, IEventContext context)
        {
            if (action.Release)
            {
                timeSlow.EndEventSlow();
                return;
            }

            timeSlow.BeginEventSlow(action);
        }
    }
}
