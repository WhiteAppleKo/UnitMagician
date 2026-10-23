namespace UnitSystem
{
    public interface IUnitChangeService
    {
        /// <summary>
        /// 대상에 마법 한 번을 시전한다(30번). spell의 Applicator로 마법 인스턴스를 만들어(시전자 정보 source) 대상의 적용 마법 목록 끝에 추가한다.
        /// pipeLine이 있으면 UnitMagicContext(TargetUnitGroup·UnitInstance)에 담아 파이프라인(ApplyUnitChangeStep이 추가)으로, 없으면 직접 추가한다(폴백).
        /// manaPayer는 마나를 차감할 스탯이다(null이면 차감하지 않음 - 일괄 시전처럼 이미 총비용을 차감한 경로). 대상이 그 속성을 갖지 않으면 false.
        /// </summary>
        bool ChangeUnit(RuntimeDataUnitGroup target, PureDataUnit spell, UnitCastSource source, CharacterSystem.RuntimeStatData manaPayer = null, PipeLine.UnitMagic.UnitMagicPipeLine pipeLine = null);
    }
}
