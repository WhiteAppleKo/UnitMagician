using UnityEngine;

namespace UnitSystem
{
    /// <summary>
    /// 온도(Temperature) 마법 적용 로직입니다.
    /// - 순회 적용(31번): 항목의 목표 온도(TargetTemperature)를 누적기에 설정한다. 순서대로 덮어쓰므로 가장 나중 항목의 값이 남고(30번 §6-2),
    ///   항목이 없으면 확정 시 원본 온도(대상 PureData의 baseTemperature). 결합 규칙은 이 함수 한 곳만 바꾸면 된다.
    /// - 시각: 대상 현재 온도의 부호가 얼음 방향(음수)이면 목록의 가장 나중 Temperature 항목 에셋의 얼음 머티리얼로 교체하고,
    ///   이 타입 마법이 추가될 때만 그 에셋의 서리 파티클을 온도 크기 배율로 스폰한다. 얼음 머티리얼 원복은 범위 밖이다(28번 §7).
    /// 얼음/불 상성 반응(파지직 판정 등)은 04/05번 시스템 소관이며 이 클래스의 책임이 아닙니다. 데미지 계산 자리는 비어 있습니다.
    /// </summary>
    public class TemperatureUnitApplicator : UnitApplicator<PureDataTemperatureUnit>
    {
        public override UnitType UnitType => UnitType.Temperature;

        protected override RuntimeDataUnit CreateInstance(PureDataTemperatureUnit data, UnitCastSource source)
        {
            return new RuntimeDataTemperatureUnit(data, ApplyCasterInfluence(data, data.TemperatureTarget, source));
        }

        protected override void AccumulateInstance(UnitStateAccumulator acc, RuntimeDataUnit instance)
        {
            if (instance is RuntimeDataTemperatureUnit temperature)
            {
                acc.SetTemperature(temperature.TargetTemperature);
            }
        }

        protected override void ApplyVisualToTarget(UnitApplyTarget target, RuntimeDataUnitTarget state, RuntimeDataUnit added)
        {
            float temperature = state.CurrentTemperature;
            bool isIceDirection = temperature < 0f;

            if (isIceDirection && target.Visualizer != null)
            {
                var latest = FindLatest<RuntimeDataTemperatureUnit>(state.AppliedUnits);
                if (latest != null && latest.Data.IceMaterial != null)
                {
                    target.Visualizer.ApplyMaterial(latest.Data.IceMaterial);
                }

                if (added is RuntimeDataTemperatureUnit addedTemperature && addedTemperature.Data.FrostParticlePrefab != null)
                {
                    // 온도 크기에 비례한 서리 연출 강도 (부호는 이미 얼음 방향으로 확정된 상태) - 배율 값만 계산해 넘긴다(28번 §6-11)
                    float intensity = Mathf.Clamp(Mathf.Abs(temperature) / 10f, 0.5f, 3f);
                    target.Visualizer.SpawnEffect(addedTemperature.Data.FrostParticlePrefab, intensity);
                }
            }

            Debug.Log($"[TemperatureUnitApplicator] Applied Temperature ({(added != null ? added.Source.name : "recalculated")}). Value: {temperature}, IsIceDirection: {isIceDirection}, Entries: {CountOf<RuntimeDataTemperatureUnit>(state.AppliedUnits)}");
        }

        protected override float ReadStateValue(RuntimeDataUnitTarget state) => state.CurrentTemperature;
    }
}
