using UnityEngine;

namespace CharacterSystem
{
    [CreateAssetMenu(fileName = "PureDataTimeSlow_", menuName = "CharacterSystem/PureDataTimeSlow")]
    public class PureDataTimeSlow : ScriptableObject
    {
        [Header("Time Scale Settings")]
        [SerializeField] private float slowTimeScale = 0.2f;
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
