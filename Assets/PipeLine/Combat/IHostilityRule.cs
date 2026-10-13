namespace PipeLine.Combat
{
    /// <summary>
    /// 중립 NPC 적대화 규칙(21번)을 노출하는 보조 인터페이스입니다. IFactionProvider와 같은 패턴으로 소비자는 TryGetComponent로만 조회합니다.
    /// 적대화 로직은 구체 캐릭터 클래스를 모른 채 이 인터페이스의 값만 읽습니다.
    /// </summary>
    public interface IHostilityRule
    {
        /// <summary>적대화에 필요한 연속 피격 횟수입니다. 0 이하이면 적대화하지 않습니다.</summary>
        int HostileHitThreshold { get; }

        /// <summary>연속 피격으로 인정되는 피격 간 최대 간격(초, 게임 시간)입니다.</summary>
        float HostileHitWindowSeconds { get; }
    }
}
