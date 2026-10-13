namespace UnitSystem
{
    /// <summary>
    /// 시간정지 마법 지정 큐의 한 항목 — "(락온 대상, 그 락온에 지정된 마법)" 쌍입니다(13번 기획서 §3).
    /// 한 번 생성되면 바뀌지 않는 불변 값이며, 같은 대상이 여러 쌍에 등장할 수 있습니다.
    /// </summary>
    public sealed class LockOnCastEntry
    {
        public RuntimeDataUnitGroup Target { get; }
        public PureDataUnit Spell { get; }

        public LockOnCastEntry(RuntimeDataUnitGroup target, PureDataUnit spell)
        {
            Target = target;
            Spell = spell;
        }
    }
}
