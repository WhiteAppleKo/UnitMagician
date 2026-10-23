namespace UnitSystem
{
    /// <summary>
    /// 한 락온 대상에 대해 "지금 큐의 마법을 순서대로 모두 적용하면 대상이 어떻게 바뀌는가"의 예측 결과입니다(13-C).
    /// LockOnPreviewCalculator가 실제 적용과 같은 재계산(IUnitTargetStateCalculator.Recalculate - "현재 적용 목록 + 큐 임시 인스턴스"를 가진 이벤트 없는 복사본 상태, 31번)으로 만들고,
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

        /// <summary>예측 질량 배율(큐 누적 후, 초기 1 기준 스케일 배율). Mass 변화가 없으면 0.</summary>
        public float PredictedMass { get; }

        /// <summary>
        /// 크기(스케일) 변화가 실제로 일어나는지 여부. 큐에 Mass 마법(PureDataMassUnit → MassUnitApplicator)이 있을 때만 true이며,
        /// false면 ScaleMultiplier는 1이다.
        /// </summary>
        public bool HasScaleChange { get; }

        /// <summary>예측 스케일 배율 = 예측 질량 배율(PredictedMass와 같은 값, 32번). 대상의 초기 스케일에 곱해진다. HasScaleChange가 false면 1.</summary>
        public float ScaleMultiplier { get; }

        /// <summary>Temperature 변화 여부. true면 PredictedTemperature가 유효하다.</summary>
        public bool HasTemperature => (ChangedUnits & UnitType.Temperature) != 0;

        /// <summary>예측 온도(큐 누적 후). 음수 = 얼음 방향(실제 적용기도 음수일 때만 얼음 머티리얼을 쓴다).</summary>
        public float PredictedTemperature { get; }

        public bool IsIce => HasTemperature && PredictedTemperature < 0f;

        public bool HasVector => (ChangedUnits & UnitType.Vector) != 0;

        /// <summary>이 대상에 쌓인 (유효) 쌍의 개수.</summary>
        public int EntryCount { get; }

        /// <summary>사람이 읽는 요약(씬 계층/로그 확인용). 예: "Mass x6, Temp -15 (Ice), Vector (instant)".</summary>
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
