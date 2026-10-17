using UnityEngine;
using GameEvent;

namespace NpcSystem
{
    /// <summary>
    /// NpcTeleportAction(이벤트 동작 SO)을 수행하는 핸들러입니다. npcId의 NPC를 목적지로 순간이동시킵니다(INpcCombatActor.TeleportTo).
    /// 목적지가 이벤트 투사체 발사 지점(EventLaunchPoint)이면 비행 로직(IEventFlightLogic)이 정한 L을 읽습니다 - 비행 로직은 이벤트 시스템 스코프에 있어
    /// 생성자 주입이 불가능하므로 NpcSystemLifetimeScope가 Start()에서 BindFlightLogic으로 사후 배선합니다.
    /// </summary>
    public class NpcTeleportActionHandler : EventActionHandler<NpcTeleportAction>
    {
        private readonly INpcActorRegistry npcs;
        private IEventFlightLogic flightLogic;

        public NpcTeleportActionHandler(INpcActorRegistry npcs)
        {
            this.npcs = npcs ?? throw new System.ArgumentNullException(nameof(npcs));
        }

        public void BindFlightLogic(IEventFlightLogic flightLogic)
        {
            this.flightLogic = flightLogic;
        }

        protected override void Execute(NpcTeleportAction action, IEventContext context)
        {
            if (!npcs.TryGetActor(action.NpcId, out var actor))
            {
                Debug.LogError($"[NpcTeleportActionHandler] 이벤트 '{context.EventId}': NPC '{action.NpcId}'를 찾을 수 없어 순간이동하지 않았습니다.");
                return;
            }

            Vector3 destination;
            if (action.Destination == NpcTeleportAction.DestinationKind.EventLaunchPoint)
            {
                if (flightLogic == null || !flightLogic.IsActive)
                {
                    Debug.LogError($"[NpcTeleportActionHandler] 이벤트 '{context.EventId}': 진행 중인 이벤트 투사체 비행이 없어 발사 지점으로 순간이동할 수 없습니다(EventProjectileLaunchAction 뒤에 두세요).");
                    return;
                }

                destination = flightLogic.LaunchPosition;
            }
            else
            {
                destination = action.FixedPosition;
            }

            actor.TeleportTo(destination);
        }
    }
}
