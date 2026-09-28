using System;
using UnityEngine;

namespace Common.InputSystem
{
    /// <summary>
    /// 이벤트 카메라(EventTargetTracking 등) 활성 중 전용 입력 컨텍스트입니다(10번 기획서 10.2/10.8절).
    /// 기존 UIInputContext와 동일한 구조를 따르되, 시네마틱 특성상 커서를 잠그고(Locked) 숨겨(Invisible)
    /// 유저가 커서로 다른 조작을 하지 못하게 합니다.
    /// </summary>
    public class EventCameraInputContext : IInputContext
    {
        public InputContextType ContextType => InputContextType.Event;
        public CursorLockMode TargetCursorLockMode => CursorLockMode.Locked;
        public bool TargetCursorVisible => false;

        public bool IsActive { get; private set; }

        public event Action OnEnter;
        public event Action OnExit;
        public event Action OnPause;
        public event Action OnResume;

        public void Enter()
        {
            IsActive = true;
            OnEnter?.Invoke();
        }

        public void Exit()
        {
            IsActive = false;
            OnExit?.Invoke();
        }

        public void Pause()
        {
            IsActive = false;
            OnPause?.Invoke();
        }

        public void Resume()
        {
            IsActive = true;
            OnResume?.Invoke();
        }
    }
}
