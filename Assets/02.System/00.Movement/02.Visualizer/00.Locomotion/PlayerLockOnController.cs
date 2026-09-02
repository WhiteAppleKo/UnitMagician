using UnityEngine;
using CharacterSystem;
using Movement.Visualizer;
using UnitSystem;

namespace Movement.Visualizer
{
    public interface ILockOnComponent
    {
        void ToggleLockOn();
    }

    /// <summary>
    /// 플레이어 루트에 부착되어 T키(시간 정지) 상태에 따라 자식 NormalLockOn / MagicLockOn 오브젝트를 활성화/비활성화 전환하는 컨트롤러
    /// </summary>
    public class PlayerLockOnController : MonoBehaviour
    {
        [Header("Child LockOn Objects")]
        [SerializeField] private GameObject normalLockOnObject;
        [SerializeField] private GameObject magicLockOnObject;

        private NormalCombatLockOnComponent _normalCombatLockOn;
        private MagicLockOnComponent _magicLockOn;
        private ILockOnComponent _currentActiveLockOn;
        private TimeSlowVisualizer _timeSlowVisualizer;

        private void Awake()
        {
            // 자식 오브젝트 자동 탐색
            if (normalLockOnObject == null)
            {
                var normalComp = GetComponentInChildren<NormalCombatLockOnComponent>(true);
                if (normalComp != null)
                {
                    normalLockOnObject = normalComp.gameObject;
                    _normalCombatLockOn = normalComp;
                }
            }
            else
            {
                _normalCombatLockOn = normalLockOnObject.GetComponent<NormalCombatLockOnComponent>();
            }

            if (magicLockOnObject == null)
            {
                var magicComp = GetComponentInChildren<MagicLockOnComponent>(true);
                if (magicComp != null)
                {
                    magicLockOnObject = magicComp.gameObject;
                    _magicLockOn = magicComp;
                }
            }
            else
            {
                _magicLockOn = magicLockOnObject.GetComponent<MagicLockOnComponent>();
            }

            _timeSlowVisualizer = GetComponentInParent<TimeSlowVisualizer>();
            if (_timeSlowVisualizer == null)
            {
                _timeSlowVisualizer = UnityEngine.Object.FindFirstObjectByType<TimeSlowVisualizer>();
            }

            // 기본 상태: 일반 락온 ON, 마법 락온 OFF
            SetSlowMode(false);
        }

        private void Start()
        {
            SetSlowMode(false);
        }

        private void OnEnable()
        {
            if (_timeSlowVisualizer != null)
            {
                _timeSlowVisualizer.OnSlowStateChanged += HandleSlowStateChanged;
            }
        }

        private void OnDisable()
        {
            if (_timeSlowVisualizer != null)
            {
                _timeSlowVisualizer.OnSlowStateChanged -= HandleSlowStateChanged;
            }
        }

        private void HandleSlowStateChanged(bool isSlowActive)
        {
            SetSlowMode(isSlowActive);
        }

        public void SetSlowMode(bool isSlowActive)
        {
            // FPS 무기 교체처럼 자식 오브젝트 SetActive 전환 및 현재 활성 락온 레퍼런스 단 1회 교체
            if (normalLockOnObject != null)
            {
                normalLockOnObject.SetActive(!isSlowActive);
            }

            if (magicLockOnObject != null)
            {
                magicLockOnObject.SetActive(isSlowActive);
            }

            _currentActiveLockOn = isSlowActive ? (ILockOnComponent)_magicLockOn : _normalCombatLockOn;
        }

        public void ToggleLockOn()
        {
            // 마우스 휠 클릭 시 조건 검사 없이 현재 장착된 락온 무기 직격 호출
            _currentActiveLockOn?.ToggleLockOn();
        }
    }
}
