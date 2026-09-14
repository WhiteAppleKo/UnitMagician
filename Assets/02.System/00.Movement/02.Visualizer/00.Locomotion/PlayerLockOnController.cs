using UnityEngine;
using CharacterSystem;
using Movement.Visualizer;
using UnitSystem;
using VContainer;

namespace Movement.Visualizer
{
    public interface ILockOnComponent
    {
        void ToggleLockOn();
    }

    /// <summary>
    /// 플레이어 루트에 부착되어 T키(시간 정지) 상태에 따라 자식 NormalLockOn / MagicLockOn 오브젝트를 활성화/비활성화 전환하는 컨트롤러
    /// </summary>
    public class PlayerLockOnController : MonoBehaviour, ILockOnController
    {
        [Header("Child LockOn Objects")]
        [SerializeField] private GameObject normalLockOnObject;
        [SerializeField] private GameObject magicLockOnObject;

        private NormalCombatLockOnComponent _normalCombatLockOn;
        private MagicLockOnComponent _magicLockOn;
        private ILockOnComponent _currentActiveLockOn;
        private TimeSlowVisualizer _timeSlowVisualizer;
        private CameraMovement.ICameraFollowService _cameraFollowService;

        [Inject]
        public void Construct(IObjectResolver resolver = null)
        {
            if (resolver != null)
            {
                if (resolver.TryResolve<TimeSlowVisualizer>(out var timeSlowVis))
                {
                    Initialize(timeSlowVis);
                }
                if (resolver.TryResolve<CameraMovement.ICameraFollowService>(out var camService))
                {
                    _cameraFollowService = camService;
                }
            }
            EnsureDependencies();
        }

        public void Initialize(TimeSlowVisualizer timeSlowVisualizer)
        {
            if (_timeSlowVisualizer != null)
            {
                _timeSlowVisualizer.OnSlowStateChanged -= HandleSlowStateChanged;
            }

            _timeSlowVisualizer = timeSlowVisualizer;

            if (_timeSlowVisualizer != null && isActiveAndEnabled)
            {
                _timeSlowVisualizer.OnSlowStateChanged += HandleSlowStateChanged;
            }
        }

        private void EnsureDependencies()
        {
            if (_timeSlowVisualizer == null)
            {
                _timeSlowVisualizer = GetComponentInParent<TimeSlowVisualizer>();
                if (_timeSlowVisualizer == null)
                {
                    _timeSlowVisualizer = UnityEngine.Object.FindAnyObjectByType<TimeSlowVisualizer>();
                }

                if (_timeSlowVisualizer != null)
                {
                    _timeSlowVisualizer.OnSlowStateChanged -= HandleSlowStateChanged;
                    _timeSlowVisualizer.OnSlowStateChanged += HandleSlowStateChanged;
                }
            }

            if (_cameraFollowService == null)
            {
                var camVis = UnityEngine.Object.FindAnyObjectByType<CameraMovement.CameraFollowVisualizer>();
                if (camVis != null)
                {
                    _cameraFollowService = camVis.CameraFollowService;
                }
            }
        }

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

            EnsureDependencies();

            // 기본 상태: 일반 락온 ON, 마법 락온 OFF
            SetSlowMode(false);
        }

        private void Start()
        {
            EnsureDependencies();
            SetSlowMode(false);
        }

        private void OnEnable()
        {
            EnsureDependencies();
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
            EnsureDependencies();

            if (isSlowActive)
            {
                Common.InputSystem.InputContextManager.Instance?.PushContext(Common.InputSystem.InputContextManager.Instance.TacticalContext);
                _cameraFollowService?.SetRequireRightClickToRotate(true);

                if (magicLockOnObject != null) magicLockOnObject.SetActive(true);
                if (normalLockOnObject != null) normalLockOnObject.SetActive(false);
                _currentActiveLockOn = _magicLockOn;
            }
            else
            {
                Common.InputSystem.InputContextManager.Instance?.PopContext(Common.InputSystem.InputContextManager.Instance.TacticalContext);
                _cameraFollowService?.SetRequireRightClickToRotate(false);

                if (magicLockOnObject != null) magicLockOnObject.SetActive(false);
                if (normalLockOnObject != null) normalLockOnObject.SetActive(true);
                _currentActiveLockOn = _normalCombatLockOn;
            }
        }

        public void ToggleLockOn()
        {
            // 마우스 휠 클릭 시 조건 검사 없이 현재 장착된 락온 무기 직격 호출
            _currentActiveLockOn?.ToggleLockOn();
        }
    }
}
