namespace UnitSystem
{
    /// <summary>
    /// 온도(Temperature) 마법 한 번의 런타임 인스턴스입니다(30번). TargetTemperature는 PureDataTemperatureUnit.TemperatureTarget에
    /// 시전자 영향(계산 자리, TemperatureUnitApplicator.CreateInstance)을 거친 최종 목표 온도이며 생성 후 바뀌지 않습니다.
    /// </summary>
    public sealed class RuntimeDataTemperatureUnit : RuntimeDataUnit
    {
        /// <summary>원본 마법의 Temperature 전용 데이터(얼음 머티리얼·서리 파티클 포함). Source와 같은 에셋입니다.</summary>
        public PureDataTemperatureUnit Data { get; }

        /// <summary>캐싱된 목표 온도(음수 = 얼음 방향).</summary>
        public float TargetTemperature { get; }

        public RuntimeDataTemperatureUnit(PureDataTemperatureUnit data, float targetTemperature) : base(data)
        {
            Data = data;
            TargetTemperature = targetTemperature;
        }
    }
}
