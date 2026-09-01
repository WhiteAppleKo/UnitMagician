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

        [Header("Focus Gauge Settings")]
        [SerializeField] private int maxFocus = 100;
        [SerializeField] private int minFocus = 0;
        [SerializeField] private float focusDrainPerSecond = 20f;
        [SerializeField] private float focusRecoverPerSecond = 15f;
        [SerializeField] private float recoverDelaySeconds = 0.5f;
        [SerializeField] private int minFocusToActivate = 5;

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
