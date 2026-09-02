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

            EnsurePlayerStatBound();

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

            // 1. 총 필요 마나 사전 계산
            int totalCost = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                var targetGroup = targets[i];
                if (targetGroup == null) continue;

                var matchingUnitData = targetGroup.GetMatchingUnitData(selectedUnitData.UnitType);
                if (matchingUnitData != null)
                {
                    float newValue = CalculateNewValueForUnit(matchingUnitData, selectedUnitData);
                    float diff = Mathf.Abs(matchingUnitData.CurrentValue - newValue);
                    int baseCost = selectedUnitData.BaseCost > 0 ? selectedUnitData.BaseCost : 10;
                    totalCost += Mathf.Max(Mathf.RoundToInt(baseCost * diff), baseCost);
                }
            }

            // 2. 마나 검증 및 부족 시 안전 예외 처리
            if (casterStat != null && casterStat.MP.CurrentValue < totalCost)
            {
                Debug.LogWarning($"[UnitCasterSystem] Insufficient Mana. (Required: {totalCost}, Current MP: {casterStat.MP.CurrentValue})");
                casterStat.TryConsumeMP(totalCost);
                foreach (var target in targets)
                {
                    if (target != null) target.Highlight(false, false);
                }
                return;
            }

            if (changeService == null && quickSlotUI != null && quickSlotUI.CatalogService != null)
            {
                changeService = new UnitChangeService(quickSlotUI.CatalogService);
            }

            GameObject casterObj = gameObject;

            // 3. 순차적으로 마커를 즉시 끄면서 마법 변환 및 소유권 갱신 적용
            for (int i = 0; i < targets.Count; i++)
            {
                var targetGroup = targets[i];
                if (targetGroup == null) continue;

                // 마커 즉시 소등
                targetGroup.Highlight(false, false);

                var matchingUnitData = targetGroup.GetMatchingUnitData(selectedUnitData.UnitType);
                if (matchingUnitData != null)
                {
                    float newValue = CalculateNewValueForUnit(matchingUnitData, selectedUnitData);
                    changeService?.ChangeUnit(targetGroup.gameObject, matchingUnitData, selectedUnitData, newValue, casterStat, null, casterObj);
                }
            }

            // 4. 시각 효과 재생
            multiLockOnVisualizer?.PlayBatchCastEffect();
        }

        private float CalculateNewValueForUnit(RuntimeDataUnit targetUnit, PureDataUnit spellUnit)
        {
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
