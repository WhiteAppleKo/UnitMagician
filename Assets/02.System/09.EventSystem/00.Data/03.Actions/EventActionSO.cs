using UnityEngine;

namespace GameEvent
{
    /// <summary>
    /// 이벤트가 "강제할 동작"의 기반 클래스입니다([D] Pure Data 계열 전략 SO). 파생 SO는 동작의 수치(주기/속도/대상 id 등)만 필드로 가지며,
    /// 동작을 실제로 수행하는 핸들러(IEventActionHandler)는 그 동작을 소유한 시스템(NPC/카메라/감속 등)에 있습니다.
    /// 실행기는 동작 SO의 타입으로 핸들러를 찾아 호출만 하므로, 동작 종류가 늘어도 실행기 코드는 바뀌지 않습니다.
    /// </summary>
    public abstract class EventActionSO : ScriptableObject
    {
    }
}
