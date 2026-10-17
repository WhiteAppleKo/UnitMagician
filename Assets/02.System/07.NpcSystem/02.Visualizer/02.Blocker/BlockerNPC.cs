using UnityEngine;

namespace NpcSystem
{
    /// <summary>
    /// 이벤트(돌문 이벤트 등)가 길을 막고 서 있게 하는 일반 NPC에 붙는 [V] 컴포넌트입니다(사용자 확정 25번 기획서 §7-2).
    /// 막고 있는 위치와 비켜설 위치(씬 고정 월드 좌표)와 그룹 id를 직렬화 필드로 가질 뿐 로직은 없습니다 - 같은 GameObject의
    /// NpcCombatActorVisualizer.CombatActor(INpcCombatActor)를 IBlockerNpc로 노출하고, 언제 무엇을 시킬지는 이벤트 시스템의 동작 핸들러가 정합니다.
    /// NpcSystemLifetimeScope의 BlockerNPC 인스펙터 목록에 등록되어 그룹 id로 조회됩니다(Find* 금지).
    /// </summary>
    public class BlockerNPC : MonoBehaviour, IBlockerNpc
    {
        [Tooltip("이벤트 SO가 이 NPC를 묶어 가리키는 그룹 id.")]
        [SerializeField] private string groupId;
        [Tooltip("같은 GameObject의 NpcCombatActorVisualizer(이동/공격 수행자).")]
        [SerializeField] private NpcCombatActorVisualizer actorVisualizer;
        [Tooltip("길을 막고 있는 위치(월드 좌표).")]
        [SerializeField] private Vector3 blockPosition;
        [Tooltip("비켜설 위치(월드 좌표). 비켜서기 동작은 NPC의 직선 이동이므로 여기까지 경로가 비어 있어야 합니다.")]
        [SerializeField] private Vector3 stepAsidePosition;

        public string GroupId => groupId;
        public Vector3 BlockPosition => blockPosition;
        public Vector3 StepAsidePosition => stepAsidePosition;
        public INpcCombatActor CombatActor => actorVisualizer != null ? actorVisualizer.CombatActor : null;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(blockPosition, 0.3f);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(stepAsidePosition, 0.3f);
            Gizmos.DrawLine(blockPosition, stepAsidePosition);
        }
    }
}
