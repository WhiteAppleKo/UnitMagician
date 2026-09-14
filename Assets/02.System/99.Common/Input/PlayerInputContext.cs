using System;
using UnityEngine;

namespace Common.InputSystem
{
    public class PlayerInputContext : IInputContext
    {
        public InputContextType ContextType => InputContextType.Player;
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
