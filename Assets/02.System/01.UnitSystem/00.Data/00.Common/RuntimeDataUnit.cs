namespace UnitSystem
{
    /// <summary>
    /// 마법 한 번의 런타임 인스턴스(abstract, 30번)입니다. 원본 마법(PureData)을 바탕으로 시전자의 스탯·장착 아이템 등의 영향을 받아
    /// 최종 수치를 캐싱해 둔 값이며, 피격 대상의 적용 마법 목록(RuntimeDataUnitTarget.AppliedUnits)에 한 항목으로 쌓입니다.
    /// 캐싱 값은 마법 타입별 서브클래스(00.Data/03.Magic의 RuntimeDataMassUnit/RuntimeDataTemperatureUnit/RuntimeDataVectorUnit)가 가지며,
    /// 인스턴스는 타입별 Applicator의 CreateInstance로만 만들고 생성 후 값이 바뀌지 않습니다(불변 캐시).
    /// </summary>
    public abstract class RuntimeDataUnit
    {
        /// <summary>원본 마법(kg, Frost …). 연출 에셋도 여기서 읽습니다.</summary>
        public PureDataUnit Source { get; }

        /// <summary>이 마법이 바꾸는 대상 속성(Source.UnitType).</summary>
        public UnitType UnitType => Source != null ? Source.UnitType : UnitType.None;

        protected RuntimeDataUnit(PureDataUnit source)
        {
            Source = source;
        }
    }
}
