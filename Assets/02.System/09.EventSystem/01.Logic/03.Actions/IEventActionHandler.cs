using System;

namespace GameEvent
{
    /// <summary>
    /// 동작 SO(EventActionSO 파생) 한 종류를 실제로 수행하는 핸들러의 규약입니다. 핸들러는 그 동작을 소유한 시스템(NPC/카메라/감속 등)의 01.Logic에 있고,
    /// 시스템 시작 시 IEventSystem.RegisterActionHandler로 실행기에 등록됩니다. 실행기는 동작 SO의 타입(ActionType)으로 핸들러를 찾아 호출만 합니다.
    /// </summary>
    public interface IEventActionHandler
    {
        /// <summary>이 핸들러가 처리하는 동작 SO의 타입.</summary>
        Type ActionType { get; }

        void Execute(EventActionSO action, IEventContext context);
    }
}
