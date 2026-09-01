using UnityEngine;

namespace CharacterSystem
{
    public class TimeSlowVisualizer : MonoBehaviour, ITimeSlowVisualizer
    {
        [Header("Audio & Feedback")]
        [SerializeField] private AudioSource slowAudioSource;
        [SerializeField] private AudioClip slowEnterSound;
        [SerializeField] private AudioClip slowExitSound;
        [SerializeField] private float slowPitch = 0.5f;

        [Header("Visual Effects (Optional)")]
        [SerializeField] private GameObject slowVfxObject;

        private void Awake()
        {
            if (slowAudioSource == null)
            {
                slowAudioSource = GetComponentInChildren<AudioSource>();
            }
        }

        public void SetTimeSlowEffect(bool isActive, float timeScale)
        {
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

            Debug.Log($"<color=yellow>[TimeSlowVisualizer] TimeSlow Effect: {(isActive ? "ACTIVE" : "INACTIVE")}, TimeScale: {timeScale:F2}</color>");
        }

        public void UpdateFocusGaugeUI(int current, int max)
        {
            // UI 직접 제어는 UIView(CharacterHUDUIView)에서 모델 이벤트를 구독하여 수행합니다.
        }
    }
}
