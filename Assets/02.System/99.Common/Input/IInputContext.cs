using UnityEngine;

namespace Common.InputSystem
{
    public enum InputContextType
    {
        Player,
        Tactical,
        UI
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
