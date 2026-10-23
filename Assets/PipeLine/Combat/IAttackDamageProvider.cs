namespace PipeLine.Combat
{
    /// <summary>
    /// 공격 데미지 데이터(RuntimeDataAttackDamage)를 가진 대상이 이를 노출하는 보조 인터페이스입니다(28번 §6-9).
    /// IFactionProvider와 같은 패턴으로, 같은 오브젝트의 RuntimeDataUnitGroup이 Awake에서 조회해 마법 적용 대상 묶음(UnitApplyTarget)에 싣습니다.
    /// </summary>
    public interface IAttackDamageProvider
    {
        RuntimeDataAttackDamage AttackDamage { get; }
    }
}
