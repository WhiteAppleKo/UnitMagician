using System;
using UnityEngine;

namespace Common.InputSystem
{
    public class TacticalInputContext : IInputContext
    {
        public InputContextType ContextType => InputContextType.Tactical;
        public CursorLockMode TargetCursorLockMode => CursorLockMode.None;
        public bool TargetCursorVisible => true;

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
