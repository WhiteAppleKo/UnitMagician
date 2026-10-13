using UnityEngine;

namespace CharacterSystem
{
    [CreateAssetMenu(fileName = "PureDataTimeSlow_", menuName = "CharacterSystem/PureDataTimeSlow")]
    public class PureDataTimeSlow : ScriptableObject
    {
        [Header("Time Scale Settings")]
        [Tooltip("시간 정지/감속 시 적용할 TimeScale (0 = 완전 정지, 0.2 = 80% 감속 등 슬라이더로 조절 가능)")]
        [Range(0f, 1f)]
        [SerializeField] private float slowTimeScale = 0.0f;
        [SerializeField] private float defaultFixedDeltaTime = 0.02f;

        [Header("Event Slow (3-Stage Approach/Exit Curve)")]
        [Tooltip("이벤트 시퀀서 전용 감속(3단계)에서 접근->이탈 전환점의 최저 TimeScale. 0보다 큰 '거의 정지' 값(예: 0.02~0.05). " +
                 "0이면 투사체가 전환점에 영영 닿지 못하므로 허용하지 않습니다.")]
        [Range(0.001f, 1f)]
        [SerializeField] private float eventSlowMinTimeScale = 0.03f;
        [Tooltip("접근 구간 완급 곡선: x=접근 진행도 t(0~1), y=감속 비율(0=정상 속도, 1=최저 TimeScale). timeScale = lerp(1, min, curve(t)).")]
        [SerializeField] private AnimationCurve eventApproachCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [Tooltip("이탈 구간 완급 곡선: x=이탈 진행도 u(0~1), y=회복 비율(0=최저 TimeScale, 1=정상 속도). timeScale = lerp(min, 1, curve(u)).")]
        [SerializeField] private AnimationCurve eventExitCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Focus Gauge Settings")]
        [SerializeField] private int maxFocus = 100;
        [SerializeField] private int minFocus = 0;
        [Tooltip("초당 게이지 감소량 (0이면 무제한 정지)")]
        [SerializeField] private float focusDrainPerSecond = 0f;
        [SerializeField] private float focusRecoverPerSecond = 15f;
        [SerializeField] private float recoverDelaySeconds = 0.5f;
        [SerializeField] private int minFocusToActivate = 0;

        public float SlowTimeScale => slowTimeScale;
        public float DefaultFixedDeltaTime => defaultFixedDeltaTime;
        public float EventSlowMinTimeScale => Mathf.Max(0.001f, eventSlowMinTimeScale);
        public AnimationCurve EventApproachCurve => eventApproachCurve;
        public AnimationCurve EventExitCurve => eventExitCurve;
        public int MaxFocus => maxFocus;
        public int MinFocus => minFocus;
        public float FocusDrainPerSecond => focusDrainPerSecond;
        public float FocusRecoverPerSecond => focusRecoverPerSecond;
        public float RecoverDelaySeconds => recoverDelaySeconds;
        public int MinFocusToActivate => minFocusToActivate;
    }
}
