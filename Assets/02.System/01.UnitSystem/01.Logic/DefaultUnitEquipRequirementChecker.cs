namespace UnitSystem
{
    /// <summary>
    /// 장착 조건이 되는 실제 게이팅 스탯이 아직 미정이므로, 항상 장착을 허용하는 스텁 구현체입니다.
    /// 스탯이 확정되면 이 클래스 내부 로직만 채우면 되고, UnitCatalogService/UI 쪽은
    /// 다시 손댈 필요가 없도록 IUnitEquipRequirementChecker 뒤로 분리되어 있습니다.
    /// </summary>
    public class DefaultUnitEquipRequirementChecker : IUnitEquipRequirementChecker
    {
        public bool CanEquip(PureDataUnit unit, out string failReason)
        {
            failReason = string.Empty;
            return true;
        }
    }
}
