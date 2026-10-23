namespace UnitSystem
{
    /// <summary>
    /// 마법 대상 상태 재계산([L] 호출형 순수 로직, 31번)입니다. 실제 적용(UnitApplyLogicSystem)과 미리보기·비용 예측(복사본 상태)이
    /// 같은 함수를 써서 두 결과가 항상 같게 합니다.
    /// </summary>
    public interface IUnitTargetStateCalculator
    {
        /// <summary>
        /// 대상 상태를 "초기 상태(질량 배율 1, 온도 = PureData 값)로 리셋 → 적용 마법 목록을 처음부터 순서대로 순회 적용 → 확정"으로 다시 만든다.
        /// 결과는 대상 RuntimeData의 전용 변경 메서드(SetMass/SetTemperature)로 기록한다. 목록은 바꾸지 않는다.
        /// </summary>
        void Recalculate(RuntimeDataUnitTarget state);
    }
}
