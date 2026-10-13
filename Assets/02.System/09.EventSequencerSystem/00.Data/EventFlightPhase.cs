namespace EventSequencerSystem
{
    /// <summary>
    /// 3단계(이벤트 감속)에서 아군 투사체가 플레이어를 기준으로 어느 구간을 비행 중인지를 나타내는 구간 값입니다
    /// (13번 기획서 §3 - "접근/이탈 곡선과 전환점").
    /// </summary>
    public enum EventFlightPhase
    {
        /// <summary>이벤트 비행이 진행 중이 아님(이벤트 시작 전/종료 후).</summary>
        None = 0,

        /// <summary>투사체가 발사 지점에서 플레이어를 향해 접근 중(플레이어 앞 구간, dot(X - P, d) &lt; 0).</summary>
        Approach = 1,

        /// <summary>투사체가 플레이어를 지나쳐 보스를 향해 이탈 중(dot(X - P, d) &gt;= 0, 전환점 이후).</summary>
        Exit = 2
    }
}
