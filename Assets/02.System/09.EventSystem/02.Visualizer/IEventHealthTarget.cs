using System;

namespace GameEvent
{
    /// <summary>
    /// 체력을 가진 이벤트 대상(예: 보스)의 규약입니다. IEventTarget에 HP 비율 조회/통지와 무적 여부를 더한 것으로, BossHpRatioTrigger/CoreHitCompletion 평가기가
    /// 구체 컴포넌트(CharacterStatComponent 등) 없이 보스의 체력 상태를 다루는 경로입니다. 대상 쪽 비주얼 컴포넌트(HealthEventTarget)가 구현합니다.
    /// </summary>
    public interface IEventHealthTarget : IEventTarget
    {
        /// <summary>현재 HP 비율(현재/최대). 최대 HP가 0 이하이면 1.</summary>
        float HpRatio { get; }

        /// <summary>대상이 무적 상태인지 여부(보스의 무적·회복 페이즈).</summary>
        bool IsInvincible { get; }

        /// <summary>HP가 바뀔 때마다 새 HP 비율(현재/최대)과 함께 발행됩니다.</summary>
        event Action<float> OnHpRatioChanged;
    }
}
