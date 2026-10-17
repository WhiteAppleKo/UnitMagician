using UnityEngine;
using NpcSystem;
using GameEvent;

namespace AllySupportSystem
{
    /// <summary>
    /// EventProjectileLaunchAction(이벤트 동작 SO)을 수행하는 핸들러입니다([L] 호출형 순수 로직). 특수 아군 NPC(INpcCombatActor)에게 "이벤트 투사체를 보스에게 1발 쏘라"고만
    /// 지시하는 얇은 배선입니다 - 실제 발사 지점 계산은 비행 로직(IEventFlightLogic.Begin)이, 이동/발사/모션은 01번 프레임워크(INpcCombatActor 구현체)가 수행합니다
    /// (02번 기획서 2.1/2.8절, DLV Rule 2 - Blind Logic). MonoBehaviour/Transform/Collider 등 구체 타입은 참조하지 않습니다.
    ///
    /// 실행 순서(보스 50% 이벤트, 사용자 명세 "투사체 발사 위치로 NPC를 옮기고 투사체를 쏘아낸다"): flightLogic.Begin(action)이 P/B 스냅샷과 발사 지점 L을 정하고
    /// NPC 발사 지점/이벤트 투사체 속도·데미지를 예약 -> ally.TeleportTo(L)로 NPC를 L로 옮김 -> ally.Attack(보스)이 예약된 L에서 투사체를 쏨(발사 시점에 NPC 위치 == L)
    /// -> flightLogic.CloseLaunchWindow()가 발사 창을 닫음. L은 Begin 안에서만 정해지므로 "L 결정 -> 이동 -> 발사"를 한 동작이 순서대로 수행합니다.
    /// 이벤트 사격은 1회뿐입니다(22번 기획서 §11): 실패해도 아군이 즉시 재발사하지 않고, 이벤트가 HP 회복 후 다시 시작될 때만 이 동작이 다시 실행됩니다.
    ///
    /// 비행 로직은 이벤트 시스템 스코프에 있어 생성자 주입이 불가능하므로(09/10번 작업에서 반복 확인된 현상) AllySupportSystemLifetimeScope가
    /// Start()에서 BindFlightLogic으로 사후 배선합니다.
    /// </summary>
    public class EventProjectileLaunchActionHandler : EventActionHandler<EventProjectileLaunchAction>
    {
        private readonly INpcCombatActor ally;
        private readonly GameObject bossTarget;

        private IEventFlightLogic flightLogic;

        public EventProjectileLaunchActionHandler(INpcCombatActor ally, GameObject bossTarget)
        {
            this.ally = ally ?? throw new System.ArgumentNullException(nameof(ally));
            this.bossTarget = bossTarget;
        }

        /// <summary>이벤트 시스템 스코프의 비행 로직을 연결합니다. 재호출하면 새 로직으로 교체됩니다.</summary>
        public void BindFlightLogic(IEventFlightLogic flightLogic)
        {
            this.flightLogic = flightLogic;
        }

        protected override void Execute(EventProjectileLaunchAction action, IEventContext context)
        {
            if (flightLogic == null)
            {
                Debug.LogError($"[EventProjectileLaunchActionHandler] 이벤트 '{context.EventId}': 비행 로직이 연결되지 않아 이벤트 투사체를 발사할 수 없습니다.");
                return;
            }

            flightLogic.Begin(action);
            if (!flightLogic.IsActive) return;

            ally.TeleportTo(flightLogic.LaunchPosition);
            ally.Attack(bossTarget);
            flightLogic.CloseLaunchWindow();
        }
    }
}
