namespace UnitSystem
{
    /// <summary>
    /// 마법을 대상에 적용하는 기반 규칙(값 변환 · 마나 비용)을 제공하는 호출형 순수 로직 인터페이스입니다.
    /// 무엇이 대상이 되는가(시간 정지 락온 큐 / 퀵슬롯 무기 즉시 적용 등)와 무관하게 모든 시전 방식이 공용으로 쓰는 규칙이며,
    /// 대상 선별·마나 차감·ChangeUnit 호출 같은 시전 흐름은 여기에 없습니다.
    /// </summary>
    public interface IUnitCastRuleService
    {
        float CalculateNewValue(RuntimeDataUnit targetUnit, PureDataUnit spellUnit);

        /// <summary>
        /// 변환 규칙의 값 기반 오버로드입니다. RuntimeDataUnit 없이 (원본값, 현재값)만으로 같은 규칙을 계산하므로,
        /// 적용 전 미리보기(13-C)가 같은 대상에 마법이 여러 개일 때 "앞 마법이 적용된 뒤의 현재값"을 가정해
        /// 실제 시전(ChangeUnit 순차 적용)과 동일한 누적 결과를 예측할 수 있다. 시전 로직도 이 규칙을 그대로 사용한다.
        /// </summary>
        float CalculateNewValue(float originalValue, float currentValue, PureDataUnit spellUnit);

        int CalculateUnitCost(RuntimeDataUnit targetUnit, PureDataUnit spellUnit);

        /// <summary>
        /// 비용 규칙의 최소 단위(단일 규칙): (대상, 마법) 한 쌍의 비용 = 대상이 그 마법 유형의 유닛을 가졌을 때만 마법 BaseCost, 아니면 0.
        /// 모든 총비용 계산(단일 마법 대상 목록 / (대상, 마법) 쌍 큐)이 이 메서드로 합산한다.
        /// </summary>
        int CostOfPair(RuntimeDataUnitGroup target, PureDataUnit spell);
    }

    /// <summary>
    /// 마법 적용 기반 규칙(값 변환 · 마나 비용)을 구현한 서비스 클래스입니다. 의존성이 없는 순수 계산입니다.
    /// </summary>
    public class UnitCastRuleService : IUnitCastRuleService
    {
        public float CalculateNewValue(RuntimeDataUnit targetUnit, PureDataUnit spellUnit)
        {
            if (targetUnit == null || spellUnit == null) return 0f;

            return CalculateNewValue(targetUnit.OriginalValue, targetUnit.CurrentValue, spellUnit);
        }

        public float CalculateNewValue(float originalValue, float currentValue, PureDataUnit spellUnit)
        {
            if (spellUnit == null) return 0f;

            float originalVal = originalValue > 0f ? originalValue : 1.0f;

            switch (spellUnit.UnitType)
            {
                case UnitType.Mass:
                    return originalVal * spellUnit.MassScaleMultiplier;
                case UnitType.Volume:
                    return originalVal;
                case UnitType.Vector:
                    return -currentValue;
                case UnitType.Temperature:
                    return spellUnit.TemperatureTarget;
                default:
                    return currentValue;
            }
        }

        public int CalculateUnitCost(RuntimeDataUnit targetUnit, PureDataUnit spellUnit)
        {
            if (spellUnit == null) return 0;
            return spellUnit.BaseCost;
        }

        public int CostOfPair(RuntimeDataUnitGroup target, PureDataUnit spell)
        {
            if (target == null || spell == null) return 0;

            var matchingUnitData = target.GetMatchingUnitData(spell.UnitType);
            return matchingUnitData != null ? CalculateUnitCost(matchingUnitData, spell) : 0;
        }
    }
}
