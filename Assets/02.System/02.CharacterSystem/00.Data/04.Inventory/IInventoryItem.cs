namespace CharacterSystem
{
    /// <summary>
    /// 인벤토리에 넣을 수 있는 PureData의 공통 계약(33번 기획서 §3-1).
    /// 항목 자체가 PureData 에셋이므로, 같은 에셋(참조 동일)이면 같은 항목이다.
    /// 마법 PureData(<c>UnitSystem.PureDataUnit</c>)가 구현하며, 나중에 장비 PureData 등도 구현하면 인벤토리에 들어간다.
    /// </summary>
    public interface IInventoryItem
    {
        /// <summary>탭 분류 키.</summary>
        ItemType ItemType { get; }
    }
}
