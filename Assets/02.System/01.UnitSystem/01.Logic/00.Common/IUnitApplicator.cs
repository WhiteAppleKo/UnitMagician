using System;
using PipeLine.Combat;

namespace UnitSystem
{
    /// <summary>
    /// 마법 타입 하나의 적용 로직([L] 호출형 순수 로직, 상태 없음)입니다. 같은 타입의 마법은 하나의 Applicator로 적용하고,
    /// 마법마다 다른 값과 연출은 PureData 서브클래스 에셋으로만 구분합니다(28번). 대상 조작은 UnitApplyTarget.Visualizer(IUnitTargetVisualizer)로만 지시합니다.
    /// 31번(순회 적용형): 마법 인스턴스 생성(CreateInstance), 재계산 순회 중 누적기에 항목 하나 적용(Accumulate),
    /// 완성된 대상 상태로 시각 적용(ApplyVisual)과 데미지 계산 자리(ApplyDamage)를 맡습니다.
    /// 재계산 자체(리셋 → 순회 → 확정)는 IUnitTargetStateCalculator 한 곳이 하며, 실제 적용과 미리보기가 같은 재계산을 씁니다.
    /// 32번: 마법은 지속 마법(적용 목록에 누적, 해제·재계산 대상: Mass·Temperature)과 즉시 마법(시전 시 한 번 적용하고 목록에 남기지 않음: Vector)으로 나뉩니다(IsInstant).
    /// </summary>
    public interface IUnitApplicator
    {
        /// <summary>이 Applicator가 담당하는 PureData 서브클래스 타입(인스턴스 생성 조회 키, 28번 §6-12).</summary>
        Type DataType { get; }

        /// <summary>이 Applicator가 담당하는 대상 속성(재계산 순회·시각·데미지 조회 키, 30번).</summary>
        UnitType UnitType { get; }

        /// <summary>
        /// 즉시 마법인지(32번). true면 시전 시 적용 목록에 추가하지 않고 RuntimeDataUnitTarget.ApplyInstant로 한 번만 적용하며(ApplyVisual에 그 인스턴스가 실려 옴),
        /// 해제·재계산·풀 재사용 때 되돌리지 않습니다. 이 경우 Accumulate·ReadValue는 의미가 없어 아무것도 하지 않거나 0을 반환합니다.
        /// </summary>
        bool IsInstant { get; }

        /// <summary>원본 마법 + 시전자 정보로 마법 한 번의 런타임 인스턴스를 만든다(시전자 영향 계산 자리를 거침). 타입이 맞지 않으면 null.</summary>
        RuntimeDataUnit CreateInstance(PureDataUnit data, UnitCastSource source);

        /// <summary>재계산 순회 중 이 항목 하나를 누적기에 적용한다(Mass: 배율 합에 더함, Temperature: 목표 온도 설정, 즉시 마법: 아무것도 하지 않음).</summary>
        void Accumulate(UnitStateAccumulator acc, RuntimeDataUnit instance);

        /// <summary>
        /// 완성된 대상 상태로 시각을 적용한다. added가 이 타입 인스턴스일 때(추가)만 이펙트를 스폰한다(해제면 null).
        /// 즉시 마법은 added(이번에 적용된 인스턴스)의 값으로 한 번 적용한다.
        /// </summary>
        void ApplyVisual(UnitApplyTarget target, RuntimeDataUnitTarget state, RuntimeDataUnit added);

        /// <summary>
        /// 데미지 계산 자리. 지금까지 계산된 데미지(currentDamage, BaseDamage에서 시작)를 받아 완성된 대상 상태로 새 데미지를 반환한다.
        /// 저장하지 않는다(최종값은 호출부가 SetCurrentDamage로 한 번만 저장). 기본 구현은 입력값을 그대로 반환하는 빈 자리다.
        /// </summary>
        int ApplyDamage(int currentDamage, RuntimeDataAttackDamage damage, RuntimeDataUnitTarget state);

        /// <summary>대상 상태에서 이 속성의 현재 값을 읽는다(Mass = CurrentMass(배율), Temperature = CurrentTemperature, 즉시 마법 = 0). 비용 계산과 미리보기가 쓴다.</summary>
        float ReadValue(RuntimeDataUnitTarget state);
    }
}
