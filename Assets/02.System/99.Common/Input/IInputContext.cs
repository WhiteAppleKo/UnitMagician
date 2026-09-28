using UnityEngine;

namespace Common.InputSystem
{
    public enum InputContextType
    {
        Player,
        Tactical,
        UI,

        /// <summary>이벤트 카메라(EventTargetTracking 등) 활성 중 전용 컨텍스트(10번 기획서 10.2/10.8절).
        /// 마우스 커서를 잠그고 시점 회전/줌 입력을 완전히 차단한다.</summary>
        Event
    }

    public interface IInputContext
    {
        InputContextType ContextType { get; }
        CursorLockMode TargetCursorLockMode { get; }
        bool TargetCursorVisible { get; }

        void Enter();
        void Exit();
        void Pause();
        void Resume();
    }
}
