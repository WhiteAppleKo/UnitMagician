using CharacterSystem;

namespace PipeLine.Combat
{
    /// <summary>
    /// 마법 등으로 진영을 바꿀 수 있는 대상(투사체 등)이 구현하는 보조 인터페이스입니다.
    /// 진영 변경 헬퍼(FactionChange)는 구체 클래스를 모른 채 이 인터페이스로만 진영을 설정합니다.
    /// </summary>
    public interface IFactionSettable
    {
        /// <summary>대상의 진영을 바꿉니다(진영에 종속된 레이어 등도 구현체가 함께 맞춥니다).</summary>
        void SetFaction(FactionType faction);
    }
}
