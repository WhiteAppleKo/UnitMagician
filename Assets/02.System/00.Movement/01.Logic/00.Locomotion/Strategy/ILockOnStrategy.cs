using System;

namespace Movement.RefactoredLocomotion
{
    public interface ILockOnStrategy : IDisposable
    {
        void Enter();
        void Update();
        void Exit();
        void ToggleLockOn();
    }
}
