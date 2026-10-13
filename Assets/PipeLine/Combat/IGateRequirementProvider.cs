namespace PipeLine.Combat
{
    /// <summary>
    /// 피격 대상(victim)이 자신에게 요구되는 게이트 조건 값을 스스로 제공하게 하는 보조 인터페이스입니다(20번 기획서 §3-1).
    /// AttributeGateStep / SizeGateStep은 코드에 기준 숫자를 갖지 않고 대상의 이 값을 읽어 비교합니다.
    /// 모든 요구값은 대상의 PureData(ScriptableObject)에서 나와야 하며, 구현체는 그 값을 그대로 노출만 합니다.
    /// 구현하지 않은 대상은 해당 게이트가 요구되지 않는 대상으로 보아 게이트 결과가 통과로 기록됩니다
    /// (IHitStepFlagsProvider와 같은 하위 호환 패턴 - 일반 캐릭터 피격 동작이 변하지 않음).
    /// </summary>
    public interface IGateRequirementProvider
    {
        /// <summary>속성(온도) 게이트 통과 조건: 투사체의 온도가 이 값 미만이어야 통과합니다.</summary>
        float RequiredMaxTemperature { get; }

        /// <summary>크기 게이트 통과 조건: 투사체의 질량 비율(현재/원본)이 이 값 이상이어야 통과합니다.</summary>
        float RequiredMassRatio { get; }
    }
}
