namespace Movement.Visualizer
{
    /// <summary>
    /// 락온 전환 및 슬로우 모드 상태 변경을 위한 인터페이스
    /// </summary>
    public interface ILockOnController
    {
        void ToggleLockOn();
        void SetSlowMode(bool isSlowActive);
    }
}
