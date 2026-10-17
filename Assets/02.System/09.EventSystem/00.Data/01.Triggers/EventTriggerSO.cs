using UnityEngine;

namespace GameEvent
{
    /// <summary>
    /// 이벤트 "시작 조건"의 기반 클래스입니다([D] Pure Data 계열 전략 SO, NpcAttackBehaviorSO 선례). 파생 SO는 조건의 수치만 필드로 가지며,
    /// 이 조건을 실제로 감시하는 평가기(IEventTriggerEvaluator)는 파생 타입마다 한 개씩 등록됩니다. 실행기(EventLogicSystem)는 파생 타입을 모릅니다.
    /// </summary>
    public abstract class EventTriggerSO : ScriptableObject
    {
    }
}
