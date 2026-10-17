using UnityEngine;

namespace GameEvent
{
    /// <summary>이벤트 대상(IEventTarget, 예: 돌문)이 파괴되면 성공으로 완료되는 완료 조건입니다.</summary>
    [CreateAssetMenu(fileName = "TargetDestroyedCompletion_", menuName = "GameEvent/Completion/TargetDestroyed")]
    public class TargetDestroyedCompletion : EventCompletionSO
    {
        [Tooltip("파괴를 지켜볼 이벤트 대상의 id(IEventTarget.TargetId). 이벤트 시스템 스코프에 등록된 대상이어야 합니다.")]
        [SerializeField] private string targetId;

        public string TargetId => targetId;
    }
}
