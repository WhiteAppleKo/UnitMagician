using UnityEngine;

namespace CharacterSystem
{
    public class TimeSlowVisualizer : MonoBehaviour, ITimeSlowVisualizer
    {
        [Header("Time Settings")]
        [SerializeField] private float defaultFixedDeltaTime = 0.02f;

        [Header("Audio & Feedback")]
        [SerializeField] private AudioSource slowAudioSource;
        [SerializeField] private AudioClip slowEnterSound;
        [SerializeField] private AudioClip slowExitSound;
        [SerializeField] private float slowPitch = 0.5f;

        [Header("Visual Effects (Optional)")]
        [SerializeField] private GameObject slowVfxObject;

        public event System.Action<bool> OnSlowStateChanged;

        private void Awake()
        {
            if (slowAudioSource == null)
            {
                slowAudioSource = GetComponentInChildren<AudioSource>();
            }
        }

        private void OnDisable()
        {
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = defaultFixedDeltaTime;
        }

        public void SetTimeSlowEffect(bool isActive, float targetScale)
        {
            OnSlowStateChanged?.Invoke(isActive);

            // TimeScale 및 FixedDeltaTime 제어 전담 (DLV 원칙 준수)
            Time.timeScale = targetScale;
            Time.fixedDeltaTime = targetScale > 0f ? (defaultFixedDeltaTime * targetScale) : defaultFixedDeltaTime;

            if (slowVfxObject != null)
            {
                slowVfxObject.SetActive(isActive);
            }

            if (slowAudioSource != null)
            {
                if (isActive)
                {
                    if (slowEnterSound != null) slowAudioSource.PlayOneShot(slowEnterSound);
                    slowAudioSource.pitch = slowPitch;
                }
                else
                {
                    if (slowExitSound != null) slowAudioSource.PlayOneShot(slowExitSound);
                    slowAudioSource.pitch = 1.0f;
                }
            }

            Debug.Log($"<color=yellow>[TimeSlowVisualizer] TimeSlow Effect: {(isActive ? "ACTIVE" : "INACTIVE")}, TimeScale: {targetScale:F2}</color>");
        }

        public void UpdateFocusGaugeUI(int current, int max)
        {
            // UI 직접 제어는 UIView(CharacterHUDUIView)에서 모델 이벤트를 구독하여 수행합니다.
        }
    }
}
