namespace UnitSystem
{
    /// <summary>
    /// 질량(Mass) 마법 한 번의 런타임 인스턴스입니다(30번). ScaleMultiplier는 PureDataMassUnit.MassScaleMultiplier에
    /// 시전자 영향(계산 자리, MassUnitApplicator.CreateInstance)을 거친 최종 배율이며 생성 후 바뀌지 않습니다.
    /// </summary>
    public sealed class RuntimeDataMassUnit : RuntimeDataUnit
    {
        /// <summary>원본 마법의 Mass 전용 데이터(연출 에셋 포함). Source와 같은 에셋입니다.</summary>
        public PureDataMassUnit Data { get; }

        /// <summary>캐싱된 질량 곱 배율. 같은 속성 항목끼리 합산한 값이 대상의 질량 배율(CurrentMass)이 됩니다(MassUnitApplicator.Accumulate → UnitTargetStateCalculator.Recalculate, 32번).</summary>
        public float ScaleMultiplier { get; }

        public RuntimeDataMassUnit(PureDataMassUnit data, float scaleMultiplier) : base(data)
        {
            Data = data;
            ScaleMultiplier = scaleMultiplier;
        }
    }
}
