using UnityEngine;

namespace GameEvent
{
    /// <summary>
    /// 이벤트 대상(체력을 가진 IEventHealthTarget, 예: 보스)의 HP 비율(현재/최대)이 ratio 이하가 되면 충족되는 시작 조건입니다.
    /// 평가 시점에 이미 ratio 이하이면 즉시 충족됩니다. 이 비율이 보스 이벤트의 유일한 HP 임계값입니다(보스 무적·회복 페이즈 진입도 이 이벤트가 일으킵니다).
    /// </summary>
    [CreateAssetMenu(fileName = "BossHpRatioTrigger_", menuName = "GameEvent/Trigger/BossHpRatio")]
    public class BossHpRatioTrigger : EventTriggerSO
    {
        [Tooltip("HP를 지켜볼 이벤트 대상의 id(IEventTarget.TargetId, IEventHealthTarget을 구현한 대상).")]
        [SerializeField] private string targetId;
        [Tooltip("HP 비율(현재/최대)이 이 값 이하가 되면 충족됩니다.")]
        [Range(0f, 1f)]
        [SerializeField] private float ratio = 0.5f;

        public string TargetId => targetId;
        public float Ratio => ratio;
    }
}
