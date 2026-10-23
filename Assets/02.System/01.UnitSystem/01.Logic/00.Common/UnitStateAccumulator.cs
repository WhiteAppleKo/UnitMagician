namespace UnitSystem
{
    /// <summary>
    /// 마법 대상 상태 재계산 중의 임시 작업 공간([L] 순수 C#, 31번)입니다. UnitTargetStateCalculator가 재계산마다 새로 만들고,
    /// 적용 마법 목록을 순서대로 돌며 각 항목의 Applicator.Accumulate가 이 누적기에 값을 더하거나 덮어씁니다.
    /// 순회가 끝나면 계산기가 이 값으로 대상 상태를 확정합니다. 계산 중간값(배율 합 등)을 RuntimeData에 두지 않기 위한 Logic 쪽 공간입니다.
    /// 즉시 마법(Vector)은 목록에 없으므로 여기에 값을 두지 않습니다(32번).
    /// </summary>
    public class UnitStateAccumulator
    {
        /// <summary>Mass 항목 곱 배율의 합(같은 속성 곱 배율은 먼저 합산한다, 30번). 확정 시 이 합이 그대로 질량 배율이 된다(×2 3개 → 6).</summary>
        public float MassMultiplierSum { get; private set; }

        /// <summary>순회한 Mass 항목 수(0이면 질량 배율 1 유지).</summary>
        public int MassEntryCount { get; private set; }

        /// <summary>목표 온도(순서대로 덮어쓰므로 가장 나중 항목의 값이 남는다). 항목이 없으면 null.</summary>
        public float? Temperature { get; private set; }

        public void AddMassMultiplier(float multiplier)
        {
            MassMultiplierSum += multiplier;
            MassEntryCount++;
        }

        public void SetTemperature(float temperature)
        {
            Temperature = temperature;
        }
    }
}
