using UnityEngine;

namespace GameEvent
{
    /// <summary>씬 시작 시(이벤트 시스템이 시작될 때) 곧바로 충족되는 시작 조건입니다. 필드가 없습니다.</summary>
    [CreateAssetMenu(fileName = "SceneStartTrigger_", menuName = "GameEvent/Trigger/SceneStart")]
    public class SceneStartTrigger : EventTriggerSO
    {
    }
}
