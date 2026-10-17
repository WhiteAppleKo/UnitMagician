using UnityEngine;

namespace GameEvent
{
    /// <summary>이벤트 성공(onSuccess) 시 보스 위치를 중심으로 광역 빙결을 일으키는 동작입니다. 범위/지속 같은 수치는 광역 빙결 시스템의 PureData가 소유하므로 값이 없습니다.</summary>
    [CreateAssetMenu(fileName = "AreaFreezeAction_", menuName = "GameEvent/Action/AreaFreeze")]
    public class AreaFreezeAction : EventActionSO
    {
    }
}
