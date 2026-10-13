using CharacterSystem;

namespace PipeLine.Combat
{
    /// <summary>
    /// 캐릭터/NPC/돌문/투사체가 자신의 현재 진영을 스스로 노출할 수 있게 하는 보조 인터페이스입니다.
    /// IHitStepFlagsProvider와 같은 패턴으로, 소비자는 TryGetComponent로만 조회합니다.
    /// 구현하지 않은 대상은 진영 Neutral로 취급됩니다. 같은 편/적대 판정은 FactionRelation(규칙표) 한 곳에서만 합니다.
    /// </summary>
    public interface IFactionProvider
    {
        /// <summary>대상의 현재(런타임) 진영입니다.</summary>
        FactionType Faction { get; }
    }
}
