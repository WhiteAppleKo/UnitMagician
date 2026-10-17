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

        // 이벤트(3단계) 전용 감속 수치(최저 TimeScale, 접근/이탈 곡선)는 이벤트 SO(TimeSlowOverrideAction)로 이동했다(Goal 47 -> 51).
        // 감속 시스템은 IEventTimeSlowSettings로 읽는다. 이 SO에는 평상시 시간정지/감속 값만 남는다.

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
        public int MaxFocus => maxFocus;
        public int MinFocus => minFocus;
        public float FocusDrainPerSecond => focusDrainPerSecond;
        public float FocusRecoverPerSecond => focusRecoverPerSecond;
        public float RecoverDelaySeconds => recoverDelaySeconds;
        public int MinFocusToActivate => minFocusToActivate;
    }
}
