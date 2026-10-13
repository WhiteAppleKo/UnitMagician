namespace UnitSystem
{
    /// <summary>
    /// 한 락온 대상에 대해 "지금 큐의 마법을 순서대로 모두 적용하면 대상이 어떻게 바뀌는가"의 예측 결과입니다(13-C).
    /// LockOnPreviewCalculator가 실제 시전 규칙(UnitBatchCastingService.CalculateNewValue)으로 큐 순서대로 누적 계산해 만들고,
    /// 비주얼(ILockOnPreviewVisualizer)은 이 값을 그대로 그리기만 합니다. 생성 후 바뀌지 않는 불변 값입니다.
    /// </summary>
    public sealed class LockOnTargetPreview
    {
        /// <summary>미리보기가 붙는 락온 대상.</summary>
        public RuntimeDataUnitGroup Target { get; }

        /// <summary>이 대상에서 예측 변화가 있는 유닛 종류(플래그 조합).</summary>
        public UnitType ChangedUnits { get; }

        /// <summary>Mass 변화 여부. true면 ScaleMultiplier/PredictedMass가 유효하다.</summary>
        public bool HasMass => (ChangedUnits & UnitType.Mass) != 0;

        /// <summary>예측 질량(큐 누적 후). Mass 변화가 없으면 0.</summary>
        public float PredictedMass { get; }

        /// <summary>
        /// 크기(스케일) 변화가 실제로 일어나는지 여부. Mass 마법의 적용기(Applicator)가 MassUnitApplicatorSO일 때만 스케일이 바뀌므로,
        /// 질량 값은 바뀌어도 크기는 그대로인 경우(다른 적용기가 연결된 Mass 마법)를 구분한다.
        /// </summary>
        public bool HasScaleChange { get; }

        /// <summary>예측 스케일 배율 = cbrt(질량 비율). 대상의 초기 스케일에 곱해진다. HasScaleChange가 false면 1.</summary>
        public float ScaleMultiplier { get; }

        /// <summary>Temperature 변화 여부. true면 PredictedTemperature가 유효하다.</summary>
        public bool HasTemperature => (ChangedUnits & UnitType.Temperature) != 0;

        /// <summary>예측 온도(큐 누적 후). 음수 = 얼음 방향(실제 적용기도 음수일 때만 얼음 머티리얼을 쓴다).</summary>
        public float PredictedTemperature { get; }

        public bool IsIce => HasTemperature && PredictedTemperature < 0f;

        public bool HasVolume => (ChangedUnits & UnitType.Volume) != 0;
        public bool HasVector => (ChangedUnits & UnitType.Vector) != 0;

        /// <summary>이 대상에 쌓인 (유효) 쌍의 개수.</summary>
        public int EntryCount { get; }

        /// <summary>사람이 읽는 요약(씬 계층/로그 확인용). 예: "Mass x2.00 (m=2), Temp -10 (Ice)".</summary>
        public string Description { get; }

        public LockOnTargetPreview(
            RuntimeDataUnitGroup target,
            UnitType changedUnits,
            float predictedMass,
            bool hasScaleChange,
            float scaleMultiplier,
            float predictedTemperature,
            int entryCount,
            string description)
        {
            Target = target;
            ChangedUnits = changedUnits;
            PredictedMass = predictedMass;
            HasScaleChange = hasScaleChange;
            ScaleMultiplier = scaleMultiplier;
            PredictedTemperature = predictedTemperature;
            EntryCount = entryCount;
            Description = description;
        }
    }
}
