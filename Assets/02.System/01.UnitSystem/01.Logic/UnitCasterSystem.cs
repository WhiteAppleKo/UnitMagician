using System;
using System.Collections.Generic;
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
        private IUnitBatchCastingService batchCastingService;
        private UnitQuickSlotUIComponent quickSlotUI;
        private RuntimeDataMultiLockOn multiLockOnData;
        private RuntimeDataTimeSlow timeSlowData;
        private ICameraFollowService cameraFollowService;
        private IMultiLockOnVisualizer multiLockOnVisualizer;
        private CharacterStatSystem playerStatSystem;

        public RuntimeDataMultiLockOn MultiLockOnData
        {
            get
            {
                if (multiLockOnData == null) multiLockOnData = new RuntimeDataMultiLockOn();
                return multiLockOnData;
            }
        }

        private IUnitCastingStrategy currentStrategy;
        private TopViewMouseCastingStrategy topViewStrategy;
        private AimLockOnCastingStrategy aimLockOnStrategy;
        private bool isInitialized = false;

        [Inject]
        public void Construct(
            UnitChangeService changeService,
            UnitQuickSlotUIComponent quickSlotUI,
            RuntimeDataMultiLockOn multiLockOnData,
            IUnitBatchCastingService batchCastingService = null,
            IObjectResolver resolver = null)
        {
            this.changeService = changeService;
            this.quickSlotUI = quickSlotUI;
            this.multiLockOnData = multiLockOnData;
            this.batchCastingService = batchCastingService;

            if (resolver != null)
            {
                if (this.batchCastingService == null && resolver.TryResolve<IUnitBatchCastingService>(out var resolvedBatchService))
                {
                    this.batchCastingService = resolvedBatchService;
                }

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

                if (this.playerStatSystem == null && resolver.TryResolve<CharacterStatSystem>(out var statSystem))
                {
                    this.playerStatSystem = statSystem;
                }
            }

            EnsureStrategiesInitialized();
            BindMultiLockOnEvents();
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
            BindMultiLockOnEvents();
        }

        public void EnsureStrategiesInitialized()
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

            EnsurePlayerStatBound();

            if (batchCastingService == null && changeService != null)
            {
                batchCastingService = new UnitBatchCastingService(changeService);
            }

            topViewStrategy = new TopViewMouseCastingStrategy(
                mainCamera,
                uiDocument,
                changeService,
                quickSlotUI,
                gameObject,
                batchCastingService
            );

            aimLockOnStrategy = new AimLockOnCastingStrategy(
                multiLockOnData,
                timeSlowData,
                changeService,
                quickSlotUI,
                multiLockOnVisualizer,
                playerStatSystem,
                batchCastingService
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

        private void BindMultiLockOnEvents()
        {
            if (multiLockOnData != null)
            {
                multiLockOnData.OnBatchCastRequested -= HandleBatchCastRequested;
                multiLockOnData.OnBatchCastRequested += HandleBatchCastRequested;
            }
        }

        private void UnbindMultiLockOnEvents()
        {
            if (multiLockOnData != null)
            {
                multiLockOnData.OnBatchCastRequested -= HandleBatchCastRequested;
            }
        }

        private void OnEnable()
        {
            BindCameraEvents();
            BindMultiLockOnEvents();
        }

        private void OnDisable()
        {
            if (cameraFollowService != null)
            {
                cameraFollowService.OnCameraModeChanged -= HandleCameraModeChanged;
            }
            UnbindMultiLockOnEvents();

            currentStrategy?.Exit();
        }

        private void OnDestroy()
        {
            if (cameraFollowService != null)
            {
                cameraFollowService.OnCameraModeChanged -= HandleCameraModeChanged;
            }
            UnbindMultiLockOnEvents();

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

        private void HandleBatchCastRequested()
        {
            ExecuteBatchCast();
        }

        private void EnsurePlayerStatBound()
        {
            if (playerStatSystem != null) return;

            var statComp = GetComponentInParent<CharacterStatComponent>() ?? GetComponent<CharacterStatComponent>();
            if (statComp == null)
            {
                var playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) statComp = playerObj.GetComponent<CharacterStatComponent>();
            }

            if (statComp != null)
            {
                playerStatSystem = statComp.StatSystem;
            }
        }

        public void ExecuteBatchCast()
        {
            int targetCount = multiLockOnData != null ? multiLockOnData.TargetCount : 0;
            if (targetCount == 0)
            {
                ClearAllLockOns();
                return;
            }

            if (quickSlotUI == null)
            {
                quickSlotUI = FindFirstObjectByType<UnitQuickSlotUIComponent>();
            }

            PureDataUnit selectedUnitData = quickSlotUI != null ? quickSlotUI.CurrentSelectedUnit : null;
            if (selectedUnitData == null && quickSlotUI != null && quickSlotUI.CatalogService != null)
            {
                var unlocked = quickSlotUI.CatalogService.GetUnlockedUnits();
                if (unlocked != null && unlocked.Count > 0)
                {
                    selectedUnitData = unlocked[0];
                }
            }

            if (selectedUnitData == null)
            {
                Debug.LogWarning("[UnitCasterSystem] Batch Cast Aborted: No Unit selected.");
                ClearAllLockOns();
                return;
            }

            var targets = new List<RuntimeDataUnitGroup>(multiLockOnData.LockedTargets);
            multiLockOnData.ClearTargets();
            multiLockOnVisualizer?.UpdateLockOnCount(0);

            EnsurePlayerStatBound();

            RuntimeStatData casterStat = playerStatSystem?.RuntimeData;

            if (changeService == null && quickSlotUI != null && quickSlotUI.CatalogService != null)
            {
                changeService = new UnitChangeService(quickSlotUI.CatalogService);
            }

            if (batchCastingService == null && changeService != null)
            {
                batchCastingService = new UnitBatchCastingService(changeService);
            }

            GameObject casterObj = gameObject;

            bool success = batchCastingService != null && batchCastingService.ExecuteBatchCast(targets, selectedUnitData, casterStat, casterObj);
            if (success)
            {
                multiLockOnVisualizer?.PlayBatchCastEffect();
            }
        }

        private float CalculateNewValueForUnit(RuntimeDataUnit targetUnit, PureDataUnit spellUnit)
        {
            if (batchCastingService != null)
            {
                return batchCastingService.CalculateNewValue(targetUnit, spellUnit);
            }

            if (targetUnit == null || spellUnit == null) return 0f;

            float originalVal = targetUnit.OriginalValue > 0f ? targetUnit.OriginalValue : 1.0f;

            switch (spellUnit.UnitType)
            {
                case UnitType.Mass:
                    return originalVal * spellUnit.MassScaleMultiplier;
                case UnitType.Volume:
                    return originalVal;
                case UnitType.Vector:
                    return -targetUnit.CurrentValue;
                default:
                    return targetUnit.CurrentValue;
            }
        }

        private void ClearAllLockOns()
        {
            if (multiLockOnData == null) return;

            var targets = new List<RuntimeDataUnitGroup>(multiLockOnData.LockedTargets);
            foreach (var target in targets)
            {
                if (target != null)
                {
                    target.Highlight(false, false);
                }
            }

            multiLockOnData.ClearTargets();
            multiLockOnVisualizer?.UpdateLockOnCount(0);
        }
    }
}
