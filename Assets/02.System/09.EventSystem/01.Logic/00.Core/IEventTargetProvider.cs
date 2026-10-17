namespace GameEvent
{
    /// <summary>이벤트 대상(IEventTarget)을 id로 조회하는 호출형 규약입니다. 평가기/핸들러가 구체 컴포넌트 없이 대상을 얻는 경로입니다.</summary>
    public interface IEventTargetProvider
    {
        /// <summary>id에 해당하는 대상을 돌려줍니다. 없으면 false.</summary>
        bool TryGetTarget(string targetId, out IEventTarget target);
    }
}
