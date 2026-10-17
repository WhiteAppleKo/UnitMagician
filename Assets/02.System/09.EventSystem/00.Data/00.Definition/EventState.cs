namespace GameEvent
{
    /// <summary>
    /// 이벤트 한 개의 진행 상태입니다. Locked = 선행 이벤트 미완료(트리거 감시 안 함), Armed = 트리거 감시 중,
    /// Running = 시작되어 완료 판정 대기 중, Completed = 완료됨(결과는 EventResult).
    /// </summary>
    public enum EventState
    {
        Locked,
        Armed,
        Running,
        Completed
    }
}
