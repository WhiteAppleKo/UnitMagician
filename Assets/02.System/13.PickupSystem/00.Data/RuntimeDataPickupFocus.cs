using System;

namespace PickupSystem
{
    /// <summary>
    /// [RuntimeData] 줍기 대상 데이터를 담는 순수 C# 데이터입니다.
    /// - DetectedTarget: PickupDetectorComponent가 근접 판정으로 찾은 "반경 안에서 가장 가까운 아이템"(컨텍스트 무관).
    /// - CurrentTarget: PickupLogicSystem이 DetectedTarget과 입력 컨텍스트(Player일 때만 줍기 가능)를 합쳐 결정한
    ///   "실제로 줍기가 가능한 대상". PickupPromptUIComponent(프롬프트)와 줍기 입력 처리는 이 값만 사용합니다.
    /// 값이 실제로 바뀔 때만 이벤트를 발행하며 스스로 프레임 갱신(Tick)은 하지 않습니다.
    /// </summary>
    public class RuntimeDataPickupFocus
    {
        /// <summary>감지된(반경 안, 소유하지 않은 마법의) 가장 가까운 아이템. 입력 컨텍스트와 무관. 없으면 null.</summary>
        public IPickupTarget DetectedTarget { get; private set; }

        /// <summary>DetectedTarget이 바뀔 때 발행됩니다(인자: 새 감지 대상, 없으면 null).</summary>
        public event Action<IPickupTarget> OnDetectedChanged;

        /// <summary>현재 줍기가 실제로 가능한 대상(감지 대상 + Player 컨텍스트). 없으면 null.</summary>
        public IPickupTarget CurrentTarget { get; private set; }

        /// <summary>CurrentTarget이 바뀔 때 발행됩니다(인자: 새 대상, 없으면 null).</summary>
        public event Action<IPickupTarget> OnFocusChanged;

        public void SetDetectedTarget(IPickupTarget target)
        {
            if (ReferenceEquals(DetectedTarget, target)) return;

            DetectedTarget = target;
            OnDetectedChanged?.Invoke(DetectedTarget);
        }

        public void SetTarget(IPickupTarget target)
        {
            if (ReferenceEquals(CurrentTarget, target)) return;

            CurrentTarget = target;
            OnFocusChanged?.Invoke(CurrentTarget);
        }
    }
}
