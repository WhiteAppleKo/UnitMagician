using UnityEngine;

namespace GameEvent
{
    /// <summary>
    /// NPC 한 명(npcId)을 목적지로 순간이동시키는 범용 동작입니다(이동 중이면 이동을 취소). destination = EventLaunchPoint는 이미 시작된 이벤트 투사체 비행의
    /// 발사 지점 L(EventProjectileLaunchAction 뒤에 둘 때만 유효)입니다. 보스 50% 이벤트의 발사 NPC 이동은 EventProjectileLaunchAction 핸들러가
    /// "L 결정 -> NPC를 L로 이동 -> 발사" 순서로 직접 수행하므로 그 이벤트는 이 동작을 쓰지 않습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NpcTeleportAction_", menuName = "GameEvent/Action/NpcTeleport")]
    public class NpcTeleportAction : EventActionSO
    {
        public enum DestinationKind
        {
            /// <summary>이 이벤트의 투사체 발사 지점 L(EventProjectileLaunchAction이 정한 값).</summary>
            EventLaunchPoint,

            /// <summary>아래 fixedPosition(월드 좌표).</summary>
            FixedPosition
        }

        [Tooltip("순간이동시킬 NPC id(NpcCombatActorVisualizer.NpcId).")]
        [SerializeField] private string npcId;
        [SerializeField] private DestinationKind destination = DestinationKind.EventLaunchPoint;
        [Tooltip("destination이 FixedPosition일 때의 월드 좌표.")]
        [SerializeField] private Vector3 fixedPosition;

        public string NpcId => npcId;
        public DestinationKind Destination => destination;
        public Vector3 FixedPosition => fixedPosition;
    }
}
