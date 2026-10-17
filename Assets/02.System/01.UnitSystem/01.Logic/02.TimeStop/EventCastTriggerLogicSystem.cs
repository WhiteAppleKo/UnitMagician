using System;
using GameEvent;

namespace UnitSystem
{
    /// <summary>
    /// 이벤트 투사체의 접근 -> 이탈 전환점에서 쌓인 마법 큐를 발동하는 얇은 배선 로직입니다
    /// ([L] 호출형 순수 로직, 13번 기획서 §3 "전환점 발동"). MonoBehaviour/Transform을 참조하지 않으며
    /// RuntimeDataMultiLockOn의 전용 메서드만 호출해 지시합니다.
    ///
    /// 이벤트 투사체 비행 로직(IEventFlightLogic)의 신호를 구독합니다(예전에는 06의 OnBoss50Percent/OnEventFlightTurnaround/OnCoreHitSuccess·Fail 고정 신호였음 - Goal 51):
    ///  - 비행 시작(OnFlightStarted, 이벤트 재도전 포함): 마법 지정 창을 연다.
    ///  - 전환점(OnTurnaround) 수신 시:
    ///   (a) 마법 지정 창을 닫는다(CloseDesignation: 이후 락온 입력 무시) — 진행 중인 팝업/PendingTarget은 아래 발동 처리가
    ///       큐 발동 전에 정리한다(UnitCasterSystem.ExecuteBatchCast가 pending을 스냅샷해 하이라이트를 끄고 ClearEntries로 취소).
    ///   (b) RequestBatchCast()로 쌓인 큐를 한 번에 발동한다. 큐가 비어 있으면 아무것도 적용되지 않는다.
    ///       큐는 발동 시점에 비워지므로, 이벤트 종료 시 시간정지 해제 훅(MagicLockOnComponent.OnDisable)이
    ///       ExecuteBatchCast를 다시 호출해도 이중 발동/이중 차감은 일어나지 않는다.
    ///   (c) 위상을 Exit로 바꾸는 것은 비행 로직(EventFlightLogicSystem)이 같은 프레임에 구간 신호로 방송한다.
    ///  - 비행 종료(OnFlightEnded, 명중 판정/소실): 다음 이벤트/일반 시간정지를 위해 지정 창을 다시 연다.
    ///
    /// 이벤트 시스템과 UnitSystem은 서로 다른 VContainer 스코프 트리라 생성자 주입이 불가능하므로,
    /// 다른 시스템들과 같이 LifetimeScope가 Start()에서 BindEventFlight()로 사후 배선합니다.
    /// </summary>
    public class EventCastTriggerLogicSystem : IDisposable
    {
        private readonly RuntimeDataMultiLockOn multiLockOnData;
        private IEventFlightLogic flightLogic;

        public EventCastTriggerLogicSystem(RuntimeDataMultiLockOn multiLockOnData)
        {
            this.multiLockOnData = multiLockOnData ?? throw new ArgumentNullException(nameof(multiLockOnData));
        }

        public void BindEventFlight(IEventFlightLogic flightLogic)
        {
            UnbindEventFlight();

            this.flightLogic = flightLogic;
            if (this.flightLogic != null)
            {
                this.flightLogic.OnFlightStarted += HandleEventStart;
                this.flightLogic.OnTurnaround += HandleTurnaround;
                this.flightLogic.OnFlightEnded += HandleEventEnd;
            }
        }

        private void UnbindEventFlight()
        {
            if (flightLogic == null) return;

            flightLogic.OnFlightStarted -= HandleEventStart;
            flightLogic.OnTurnaround -= HandleTurnaround;
            flightLogic.OnFlightEnded -= HandleEventEnd;
            flightLogic = null;
        }

        public void Dispose()
        {
            UnbindEventFlight();
        }

        /// <summary>이벤트(재도전 포함) 시작: 지정 창을 연다.</summary>
        private void HandleEventStart()
        {
            multiLockOnData.OpenDesignation();
        }

        /// <summary>전환점: 지정 창을 닫고 쌓인 큐를 한 번에 발동한다.</summary>
        private void HandleTurnaround()
        {
            multiLockOnData.CloseDesignation();
            multiLockOnData.RequestBatchCast();

            // 발동 처리가 연결되어 있지 않은 비정상 상태에서도 지정 대기가 남지 않게 하는 안전망(이미 비었다면 아무 일 없음).
            multiLockOnData.CancelPending();
        }

        /// <summary>이벤트 종료(명중 판정): 다음 이벤트/일반 시간정지를 위해 지정 창을 다시 연다.</summary>
        private void HandleEventEnd()
        {
            multiLockOnData.OpenDesignation();
        }
    }
}
