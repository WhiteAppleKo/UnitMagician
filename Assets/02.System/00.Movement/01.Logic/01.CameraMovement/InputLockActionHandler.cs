using System;
using Common.InputSystem;
using GameEvent;
using VContainer;

namespace CameraMovement
{
    /// <summary>
    /// InputLockAction(이벤트 동작 SO)을 수행하는 핸들러입니다([L] 호출형 순수 로직). 락을 걸면 Event 입력 컨텍스트(EventCameraInputContext)를 Push하고
    /// 풀면 Pop합니다. Event 컨텍스트가 걸려 있는 동안 카메라 서비스가 마우스 시점 회전/줌 입력을 차단합니다(22번 기획서 §3-1, 10번 기획서 10.8절).
    /// 예전에는 EventCameraTriggerLogicSystem이 06의 고정 신호를 받아 카메라 오버라이드와 함께 Push/Pop했으나, 지금은 이벤트 SO가 InputLockAction으로 명시적으로 지시합니다(Goal 51).
    /// 이미 락이 걸려 있으면 다시 걸지 않고, 걸려 있지 않으면 풀지 않습니다.
    /// </summary>
    public class InputLockActionHandler : EventActionHandler<InputLockAction>
    {
        private readonly IInputContextManager m_inputContextManager;

        private IInputContext m_pushedEventContext;

        [Inject]
        public InputLockActionHandler(IInputContextManager inputContextManager)
        {
            m_inputContextManager = inputContextManager ?? throw new ArgumentNullException(nameof(inputContextManager));
        }

        protected override void Execute(InputLockAction action, IEventContext context)
        {
            if (action.Release)
            {
                if (m_pushedEventContext == null) return;

                m_inputContextManager.PopContext(m_pushedEventContext);
                m_pushedEventContext = null;
                return;
            }

            if (m_pushedEventContext != null) return;

            m_pushedEventContext = new EventCameraInputContext();
            m_inputContextManager.PushContext(m_pushedEventContext);
        }
    }
}
