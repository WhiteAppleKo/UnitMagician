using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using CameraMovement;
using CharacterSystem;

namespace UnitSystem
{
    /// <summary>
    /// 단위 마법 시점별 조작 시스템의 전략 패턴 Context 역할을 수행하는 컴포넌트입니다.
    /// ICameraFollowService의 카메라 모드 변경 이벤트에 반응하여
    /// TopViewMouseCastingStrategy와 AimLockOnCastingStrategy를 스위칭하며,
    /// Update()에서는 현재 전략의 Update()만을 단일 위임 실행합니다.
    /// </summary>
    public class UnitCasterSystem : MonoBehaviour
    {
        [Header("Targeting Settings")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private UIDocument uiDocument;

        private UnitChangeService changeService;
        private UnitQuickSlotUIComponent quickSlotUI;
        private RuntimeDataMultiLockOn multiLockOnData;
        private RuntimeDataTimeSlow timeSlowData;
        private ICameraFollowService cameraFollowService;
        private IMultiLockOnVisualizer multiLockOnVisualizer;
        private CharacterStatSystem playerStatSystem;

        private IUnitCastingStrategy currentStrategy;
        private TopViewMouseCastingStrategy topViewStrategy;
        private AimLockOnCastingStrategy aimLockOnStrategy;
        private bool isInitialized = false;

        [Inject]
        public void Construct(
            UnitChangeService changeService,
            UnitQuickSlotUIComponent quickSlotUI,
            RuntimeDataMultiLockOn multiLockOnData,
            IObjectResolver resolver = null)
        {
            this.changeService = changeService;
            this.quickSlotUI = quickSlotUI;
            this.multiLockOnData = multiLockOnData;

            if (resolver != null)
            {
                if (resolver.TryResolve<ICameraFollowService>(out var camService))
                {
                    this.cameraFollowService = camService;
                }

                if (resolver.TryResolve<RuntimeDataTimeSlow>(out var slowData))
                {
                    this.timeSlowData = slowData;
                }

                if (resolver.TryResolve<IMultiLockOnVisualizer>(out var multiVis))
                {
                    this.multiLockOnVisualizer = multiVis;
                }

                if (resolver.TryResolve<CharacterStatSystem>(out var statSystem))
                {
                    this.playerStatSystem = statSystem;
                }
            }

            EnsureStrategiesInitialized();
        }

        private void Awake()
        {
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            if (mainCamera == null) mainCamera = Camera.main;
        }

        private void Start()
        {
            EnsureStrategiesInitialized();
            BindCameraEvents();
        }

        private void EnsureStrategiesInitialized()
        {
            if (isInitialized) return;

            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            if (mainCamera == null) mainCamera = Camera.main;

            if (quickSlotUI == null)
            {
                quickSlotUI = FindFirstObjectByType<UnitQuickSlotUIComponent>();
            }

            if (cameraFollowService == null)
            {
                var camVis = FindFirstObjectByType<CameraFollowVisualizer>();
                if (camVis != null)
                {
                    cameraFollowService = camVis.CameraFollowService;
                }
            }

            if (timeSlowData == null)
            {
                timeSlowData = new RuntimeDataTimeSlow(null);
            }

            if (multiLockOnData == null)
            {
                multiLockOnData = new RuntimeDataMultiLockOn();
            }

            if (multiLockOnVisualizer == null)
            {
                multiLockOnVisualizer = FindFirstObjectByType<MultiLockOnVisualizer>();
            }

            if (playerStatSystem == null)
            {
                var playerStatComp = FindFirstObjectByType<CharacterStatComponent>();
                if (playerStatComp != null)
                {
                    playerStatSystem = playerStatComp.StatSystem;
                }
            }

            topViewStrategy = new TopViewMouseCastingStrategy(
                mainCamera,
                uiDocument,
                changeService,
                quickSlotUI,
                gameObject
            );

            aimLockOnStrategy = new AimLockOnCastingStrategy(
                multiLockOnData,
                timeSlowData,
                changeService,
                quickSlotUI,
                multiLockOnVisualizer,
                playerStatSystem
            );

            isInitialized = true;

            // 초기 카메라 모드에 맞춰 전략 설정
            if (cameraFollowService != null)
            {
                SwitchByCameraMode(cameraFollowService.CurrentMode);
            }
            else
            {
                SwitchStrategy(topViewStrategy);
            }
        }

        private void BindCameraEvents()
        {
            if (cameraFollowService == null)
            {
                var camVis = FindFirstObjectByType<CameraFollowVisualizer>();
                if (camVis != null)
                {
                    cameraFollowService = camVis.CameraFollowService;
                }
            }

            if (cameraFollowService != null)
            {
                cameraFollowService.OnCameraModeChanged -= HandleCameraModeChanged;
                cameraFollowService.OnCameraModeChanged += HandleCameraModeChanged;
                SwitchByCameraMode(cameraFollowService.CurrentMode);
            }
        }

        private void OnEnable()
        {
            BindCameraEvents();
        }

        private void OnDisable()
        {
            if (cameraFollowService != null)
            {
                cameraFollowService.OnCameraModeChanged -= HandleCameraModeChanged;
            }

            currentStrategy?.Exit();
        }

        private void OnDestroy()
        {
            if (cameraFollowService != null)
            {
                cameraFollowService.OnCameraModeChanged -= HandleCameraModeChanged;
            }

            currentStrategy?.Exit();
            topViewStrategy?.Dispose();
            aimLockOnStrategy?.Dispose();
        }

        private void HandleCameraModeChanged(CameraMode mode)
        {
            SwitchByCameraMode(mode);
        }

        private void SwitchByCameraMode(CameraMode mode)
        {
            EnsureStrategiesInitialized();

            switch (mode)
            {
                case CameraMode.FirstPerson:
                case CameraMode.ThirdPersonShoulder:
                    // 1인칭 및 3인칭 숄더뷰 마법 락온은 자식 오브젝트(MagicLockOnComponent)가 시간 정지 시 전담합니다.
                    SwitchStrategy(null);
                    break;

                case CameraMode.HybridFocus:
                case CameraMode.PlayerOnly:
                case CameraMode.MouseFocus:
                default:
                    SwitchStrategy(topViewStrategy);
                    break;
            }
        }

        private void SwitchStrategy(IUnitCastingStrategy newStrategy)
        {
            if (currentStrategy == newStrategy) return;

            currentStrategy?.Exit();
            currentStrategy = newStrategy;
            currentStrategy?.Enter();
        }

        private void Update()
        {
            // 전략 패턴: 매 프레임 if 조건문 분기 없이 현재 활성화된 전략의 Update만 단일 위임 호출
            currentStrategy?.Update();
        }
    }
}
