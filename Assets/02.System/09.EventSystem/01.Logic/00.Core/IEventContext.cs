namespace GameEvent
{
    /// <summary>
    /// 평가기/핸들러가 실행 중인 이벤트에 대해 읽을 수 있는 최소 정보입니다. 실행기가 이벤트마다 한 개를 만들어 넘기며,
    /// 대상(IEventTarget) 조회 경로를 함께 제공합니다.
    /// </summary>
    public interface IEventContext
    {
        string EventId { get; }
        EventIntensity Intensity { get; }

        /// <summary>이벤트 SO가 가리키는 대상 id로 월드 대상을 조회합니다.</summary>
        bool TryGetTarget(string targetId, out IEventTarget target);
    }
}
