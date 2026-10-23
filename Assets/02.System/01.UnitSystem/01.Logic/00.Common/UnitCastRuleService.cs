namespace UnitSystem
{
    /// <summary>
    /// 마법을 대상에 적용하는 기반 규칙 중 마나 비용 규칙을 제공하는 호출형 순수 로직 인터페이스입니다.
    /// 무엇이 대상이 되는가(시간 정지 락온 큐 / 퀵슬롯 무기 즉시 적용 등)와 무관하게 모든 시전 방식이 공용으로 쓰는 규칙이며,
    /// 대상 선별·마나 차감·ChangeUnit 호출 같은 시전 흐름은 여기에 없습니다.
    /// 대상 상태 계산 공식은 여기에 없고 IUnitTargetStateCalculator.Recalculate 한 곳에만 있습니다(31번 - 적용·미리보기·비용이 같은 함수를 씀).
    /// 속성 지원 여부는 대상 PureData(PureDataUnitTarget.SupportedUnits) 기준입니다.
    /// </summary>
    public interface IUnitCastRuleService
    {
        /// <summary>
        /// 비용 규칙의 최소 단위(단일 규칙): (대상, 마법) 한 쌍의 비용 = 대상이 그 마법 유형의 속성을 가졌을 때만 마법 BaseCost, 아니면 0.
        /// 모든 총비용 계산(단일 마법 대상 목록 / (대상, 마법) 쌍 큐)이 이 메서드로 합산한다.
        /// </summary>
        int CostOfPair(RuntimeDataUnitGroup target, PureDataUnit spell);
    }

    /// <summary>
    /// 마법 적용 기반 규칙(마나 비용)을 구현한 서비스 클래스입니다. 의존성이 없는 순수 계산입니다.
    /// </summary>
    public class UnitCastRuleService : IUnitCastRuleService
    {
        public int CostOfPair(RuntimeDataUnitGroup target, PureDataUnit spell)
        {
            if (target == null || spell == null) return 0;

            return target.HasUnit(spell.UnitType) ? spell.BaseCost : 0;
        }
    }
}
