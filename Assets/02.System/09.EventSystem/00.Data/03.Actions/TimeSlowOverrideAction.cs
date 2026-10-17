using UnityEngine;

namespace GameEvent
{
    /// <summary>
    /// 이벤트 감속(시간 개입)을 시작/해제하는 동작입니다. 시작(release = false)하면 투사체의 비행 구간/진행도로 timeScale이 구동되어
    /// 접근 구간에서 최저 timeScale까지 내려갔다가 이탈 구간에서 회복합니다. 해제(release = true)는 감속을 끝내고 정상 속도로 되돌립니다.
    /// 해제 동작은 값을 쓰지 않으므로 해제용 에셋은 값 없이 release만 켜서 씁니다.
    /// </summary>
    [CreateAssetMenu(fileName = "TimeSlowOverrideAction_", menuName = "GameEvent/Action/TimeSlowOverride")]
    public class TimeSlowOverrideAction : EventActionSO, IEventTimeSlowSettings
    {
        [Tooltip("켜면 이 동작은 이벤트 감속을 해제합니다(아래 값은 쓰지 않음).")]
        [SerializeField] private bool release;

        [Header("Event Slow (3-Stage Approach/Exit Curve)")]
        [Tooltip("접근->이탈 전환점(투사체가 플레이어를 스치는 순간)의 최저 TimeScale. 0보다 큰 '거의 정지' 값(예: 0.02~0.05). 0이면 투사체가 전환점에 영영 닿지 못하므로 허용하지 않습니다.")]
        [Range(0.001f, 1f)]
        [SerializeField] private float minTimeScale = 0.03f;
        [Tooltip("접근 구간 완급 곡선: x=접근 진행도 t(0~1), y=감속 비율(0=정상 속도, 1=최저 TimeScale). timeScale = lerp(1, min, curve(t)).")]
        [SerializeField] private AnimationCurve approachCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [Tooltip("이탈 구간 완급 곡선: x=이탈 진행도 u(0~1), y=회복 비율(0=최저 TimeScale, 1=정상 속도). timeScale = lerp(min, 1, curve(u)).")]
        [SerializeField] private AnimationCurve exitCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        public bool Release => release;
        public float EventSlowMinTimeScale => Mathf.Max(0.001f, minTimeScale);
        public AnimationCurve EventApproachCurve => approachCurve;
        public AnimationCurve EventExitCurve => exitCurve;
    }
}
