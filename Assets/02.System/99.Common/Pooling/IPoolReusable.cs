namespace Pooling
{
    /// <summary>
    /// 풀링되는 프리팹 안의 컴포넌트가 "반환 시점 정리"와 "재사용 직전 상태 초기화"를 맡기 위한 인터페이스입니다.
    /// 풀 항목(IPoolable)이 같은 프리팹 안의 이 인터페이스 구현체를 모아서 아래 순서로 호출합니다.
    ///  - OnReturnedToPool: 풀에 반환되는 순간, 비활성화되기 직전(오브젝트가 아직 활성이라 활성 상태에서만 가능한 정리가 가능)에 호출.
    ///  - ResetForReuse:    풀에서 다시 꺼낼 때, 활성화되기 직전(Setting 단계)에 호출. 이전 사용에서 바뀐 모든 상태를 초기값으로 되돌린다.
    /// </summary>
    public interface IPoolReusable
    {
        void OnReturnedToPool();
        void ResetForReuse();
    }
}
