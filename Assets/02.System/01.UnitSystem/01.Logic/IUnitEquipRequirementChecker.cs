namespace UnitSystem
{
    /// <summary>
    /// 유닛(마법) 장착 조건 판정을 담당하는 인터페이스입니다.
    /// 게이팅 스탯이 확정되기 전까지는 DefaultUnitEquipRequirementChecker(스텁)가
    /// 항상 통과시키며, 실제 조건이 정해지면 이 인터페이스의 새 구현체로 교체합니다.
    /// </summary>
    public interface IUnitEquipRequirementChecker
    {
        bool CanEquip(PureDataUnit unit, out string failReason);
    }
}
