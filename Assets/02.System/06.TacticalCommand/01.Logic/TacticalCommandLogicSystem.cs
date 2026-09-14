using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;
using CharacterSystem;
using UnitSystem;
using TimeSlowFilterSystem;
using Common.InputSystem;

namespace TacticalCommandSystem
{
    /// <summary>
    /// 전술 명령 큐잉 및 시간 정지 연쇄 마법 발동 로직을 담당하는 순수 C# Logic System입니다.
    /// MonoBehaviour를 상속받지 않으며, VContainer 생명주기 인터페이스를 구현합니다.
    /// </summary>
    public class TacticalCommandLogicSystem : IInitializable, ITickable, IDisposable
    {
        private readonly RuntimeDataTacticalQueue queueData;
        private RuntimeDataTimeSlow timeSlowData;
        private ICharacterStatService statService;
        private UnitQuickSlotUIComponent quickSlotUI;
        private UnitCasterSystem unitCaster;
        private readonly IUnitMagicSlotService magicSlotService;
        private readonly ITacticalCommandVisualizer visualizer;
        private ITargetStencilService stencilService;
        private readonly Synty.AnimationBaseLocomotion.Samples.InputSystem.InputReader inputReader;
        private CameraMovement.ICameraFollowService cameraFollowService;

        private Camera mainCamera;
        private GameObject currentHoverTarget;
        private float executionTimer;

        private Vector2 rightClickDownPosition;
        private bool isRightClickDown;
        private float maxRightDragDistance;
        private const float DRAG_CANCEL_THRESHOLD = 5f;

        [Inject]
        public TacticalCommandLogicSystem(
            RuntimeDataTacticalQueue queueData,
            ITacticalCommandVisualizer visualizer,
            RuntimeDataTimeSlow timeSlowData = null,
            ICharacterStatService statService = null,
            IUnitMagicSlotService magicSlotService = null,
            ITargetStencilService stencilService = null,
            Synty.AnimationBaseLocomotion.Samples.InputSystem.InputReader inputReader = null)
        {
            this.queueData = queueData ?? throw new ArgumentNullException(nameof(queueData));
            this.visualizer = visualizer ?? throw new ArgumentNullException(nameof(visualizer));
            this.timeSlowData = timeSlowData;
            this.statService = statService;
            this.magicSlotService = magicSlotService;
            this.stencilService = stencilService;
            this.inputReader = inputReader;
        }

        private CameraMovement.ICameraFollowService GetCameraFollowService()
        {
            if (cameraFollowService == null)
            {
                var camVis = UnityEngine.Object.FindAnyObjectByType<CameraMovement.CameraFollowVisualizer>();
                if (camVis != null)
                {
                    cameraFollowService = camVis.CameraFollowService;
                }
            }
            return cameraFollowService;
        }

        public void Initialize()
        {
            if (timeSlowData != null)
            {
                timeSlowData.OnSlowStateChanged += HandleTimeSlowChanged;
            }

            if (quickSlotUI == null)
            {
                quickSlotUI = UnityEngine.Object.FindAnyObjectByType<UnitQuickSlotUIComponent>();
            }

            if (unitCaster == null)
            {
                unitCaster = UnityEngine.Object.FindAnyObjectByType<UnitCasterSystem>();
            }

            GetCameraFollowService();
        }

        public void Dispose()
        {
            if (timeSlowData != null)
            {
                timeSlowData.OnSlowStateChanged -= HandleTimeSlowChanged;
            }

            InputContextManager.Instance?.PopContext(InputContextManager.Instance.TacticalContext);
            GetCameraFollowService()?.SetRequireRightClickToRotate(false);
            visualizer?.ClearAllVisuals();
            queueData?.Clear();
        }

        private void SetTacticalContext(bool isTactical)
        {
            if (isTactical)
            {
                InputContextManager.Instance?.PushContext(InputContextManager.Instance.TacticalContext);
            }
            else
            {
                InputContextManager.Instance?.PopContext(InputContextManager.Instance.TacticalContext);
            }
        }

        private void HandleTimeSlowChanged(bool isActive)
        {
            if (isActive)
            {
                queueData.SetTacticalMode(true);
                visualizer.ShowTacticalVisuals(true);
                SetTacticalContext(true);
                GetCameraFollowService()?.SetRequireRightClickToRotate(true);
                Debug.Log("<color=cyan>[TacticalCommand] Mode Activated: Free Cursor & Right-Click Rotate Enabled</color>");
            }
            else
            {
                SetTacticalContext(false);
                GetCameraFollowService()?.SetRequireRightClickToRotate(false);
                isRightClickDown = false;
                maxRightDragDistance = 0f;
                Debug.Log("<color=yellow>[TacticalCommand] Mode Deactivated: Normal Rotate Restored</color>");

                // 시간 정지가 풀렸을 때 예약된 큐가 있으면 순차 발동 시작
                if (queueData.CommandQueue.Count > 0 && !queueData.IsExecuting)
                {
                    StartQueueExecution();
                }
                else
                {
                    queueData.SetTacticalMode(false);
                    visualizer.ShowTacticalVisuals(false);
                    visualizer.ClearAllVisuals();
                }
            }
        }

        public void Tick()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            // 1. 순차 격발 실행 상태 처리
            if (queueData.IsExecuting)
            {
                ProcessQueueExecution();
                return;
            }

            // 시간 정지 상태 동기화 (Time.timeScale == 0f 절대 금지, 순수 데이터 플래그 기반)
            bool isTimeStopped = timeSlowData != null && timeSlowData.IsSlowActive;

            if (isTimeStopped && !queueData.IsTacticalModeActive)
            {
                queueData.SetTacticalMode(true);
                visualizer.ShowTacticalVisuals(true);
                SetTacticalContext(true);
                GetCameraFollowService()?.SetRequireRightClickToRotate(true);
            }
            else if (!isTimeStopped && queueData.IsTacticalModeActive)
            {
                SetTacticalContext(false);
                GetCameraFollowService()?.SetRequireRightClickToRotate(false);
                isRightClickDown = false;
                maxRightDragDistance = 0f;

                if (queueData.CommandQueue.Count > 0 && !queueData.IsExecuting)
                {
                    StartQueueExecution();
                }
                else
                {
                    queueData.SetTacticalMode(false);
                    visualizer.ShowTacticalVisuals(false);
                    visualizer.ClearAllVisuals();
                }
            }

            // 2. 전술 모드 (시간 정지 상태) 조작 처리
            if (queueData.IsTacticalModeActive)
            {
                ProcessTacticalTargeting();
            }
        }

        private void ProcessTacticalTargeting()
        {
            // UI 메뉴 등이 열려 있는 경우 입력 누수 방지
            var currentContext = InputContextManager.Instance?.CurrentContext;
            if (currentContext != null && currentContext.ContextType != InputContextType.Tactical)
            {
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null || mainCamera == null) return;

            Vector2 mousePos = mouse.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(mousePos);

            float searchDist = queueData.PureData != null ? queueData.PureData.MaxTargetDistance : 50f;
            LayerMask mask = queueData.PureData != null ? queueData.PureData.TargetLayerMask : ~0;

            RaycastHit[] hits = Physics.SphereCastAll(ray, 0.4f, searchDist, mask);
            GameObject validTarget = null;

            if (hits != null && hits.Length > 0)
            {
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var hit in hits)
                {
                    var unitGroup = hit.collider.GetComponentInParent<RuntimeDataUnitGroup>();
                    if (unitGroup != null && unitGroup.IsTargetable)
                    {
                        validTarget = unitGroup.gameObject;
                        break;
                    }
                }
            }

            // 호버 타깃 갱신
            if (currentHoverTarget != validTarget)
            {
                currentHoverTarget = validTarget;
                visualizer.SetHoverTarget(currentHoverTarget);
            }

            // 좌클릭: 예약 명령 추가
            if (mouse.leftButton.wasPressedThisFrame && validTarget != null)
            {
                TryEnqueueCurrentMagic(validTarget);
            }

            // 우클릭: 드래그와 단발 탭(취소) 구분
            ProcessRightClickCancel(mouse);
        }

        private void ProcessRightClickCancel(Mouse mouse)
        {
            if (mouse == null) return;

            // 우클릭 누른 프레임
            if (mouse.rightButton.wasPressedThisFrame)
            {
                isRightClickDown = true;
                rightClickDownPosition = mouse.position.ReadValue();
                maxRightDragDistance = 0f;
            }

            // 우클릭 누르고 있는 동안 드래그 이동거리 누적
            if (isRightClickDown && mouse.rightButton.isPressed)
            {
                Vector2 currentPos = mouse.position.ReadValue();
                float dist = Vector2.Distance(currentPos, rightClickDownPosition);
                if (dist > maxRightDragDistance)
                {
                    maxRightDragDistance = dist;
                }
            }

            // 우클릭을 뗐을 때
            if (mouse.rightButton.wasReleasedThisFrame && isRightClickDown)
            {
                Vector2 releasePos = mouse.position.ReadValue();
                float finalDistance = Vector2.Distance(releasePos, rightClickDownPosition);

                // 드래그 없이 제자리에서 뗐을 때만(이동거리 5px 미만) 마지막 예약 취소 실행
                if (finalDistance < DRAG_CANCEL_THRESHOLD && maxRightDragDistance < DRAG_CANCEL_THRESHOLD)
                {
                    CancelLastCommand();
                }

                isRightClickDown = false;
                maxRightDragDistance = 0f;
            }
        }

        private void TryEnqueueCurrentMagic(GameObject target)
        {
            if (target == null) return;

            // 이미 예약된 대상 중복 등록 완전 차단
            if (queueData.ContainsTarget(target))
            {
                Debug.LogWarning($"<color=yellow>[TacticalCommand] Target {target.name} is already in queue! Duplicate blocked.</color>");
                visualizer.PlayManaInsufficientFeedback();
                return;
            }

            PureDataUnit selectedMagic = GetCurrentSelectedMagic();
            if (selectedMagic == null)
            {
                Debug.LogWarning("[TacticalCommandLogicSystem] No magic selected to queue!");
                visualizer.PlayManaInsufficientFeedback();
                return;
            }

            int availableMana = statService != null ? statService.CurrentMP : 100;

            if (queueData.TryEnqueue(target, selectedMagic, availableMana, out var newEntry))
            {
                Debug.Log($"<color=cyan>[TacticalCommand] Enqueued:</color> #{newEntry.OrderIndex} {newEntry.MagicData.UnitName} on {target.name} (Cost: {newEntry.ManaCost}, Total Est: {queueData.TotalEstimatedManaCost}/{availableMana})");

                // 타깃 컬러 보존을 위해 스텐실 서비스에 등록
                stencilService?.RegisterFromComponent(target.transform);

                visualizer.AddTargetIndicator(newEntry);
                visualizer.UpdateTrajectoryLines(queueData.CommandQueue, GetOriginPosition());
            }
            else
            {
                Debug.LogWarning($"<color=red>[TacticalCommand] Enqueue Blocked!</color> Insufficient Mana or Queue Full. (Est: {queueData.TotalEstimatedManaCost} + Cost: {selectedMagic.BaseCost} > Available: {availableMana})");
                visualizer.PlayManaInsufficientFeedback();
            }
        }

        private void CancelLastCommand()
        {
            var removed = queueData.RemoveLast();
            if (removed != null)
            {
                Debug.Log($"<color=yellow>[TacticalCommand] Cancelled #{removed.OrderIndex} on {removed.TargetObject?.name}</color>");
                visualizer.RemoveTargetIndicator(removed);
                visualizer.UpdateTrajectoryLines(queueData.CommandQueue, GetOriginPosition());
            }
        }

        private void StartQueueExecution()
        {
            Debug.Log($"<color=green>[TacticalCommand] Starting Sequential Execution of {queueData.CommandQueue.Count} Commands! (Total Mana: {queueData.TotalEstimatedManaCost})</color>");

            // 기존 구조대로 시간 정지 해제 시점에 총 마나를 한 번에 일괄 차감
            if (statService != null && queueData.TotalEstimatedManaCost > 0)
            {
                statService.UseMP(queueData.TotalEstimatedManaCost);
            }

            queueData.SetExecuting(true);
            executionTimer = 0f;
        }

        private void ProcessQueueExecution()
        {
            float delay = queueData.PureData != null ? queueData.PureData.ExecutionDelay : 0.25f;
            executionTimer += Time.deltaTime;

            if (executionTimer >= delay)
            {
                executionTimer = 0f;

                if (queueData.CommandQueue.Count > 0)
                {
                    var entry = queueData.Dequeue();
                    if (entry != null)
                    {
                        ExecuteSingleCommand(entry);
                        visualizer.RemoveTargetIndicator(entry);
                        visualizer.UpdateTrajectoryLines(queueData.CommandQueue, GetOriginPosition());
                    }
                }

                if (queueData.CommandQueue.Count == 0)
                {
                    CompleteQueueExecution();
                }
            }
        }

        private void ExecuteSingleCommand(TacticalCommandEntry entry)
        {
            if (entry == null) return;

            Debug.Log($"<color=yellow>[TacticalCommand] Executing #{entry.OrderIndex}:</color> {entry.MagicData?.UnitName} -> {entry.TargetObject?.name}");

            if (unitCaster == null)
            {
                unitCaster = UnityEngine.Object.FindAnyObjectByType<UnitCasterSystem>();
            }

            var changeService = unitCaster != null ? unitCaster.ChangeService : null;
            var batchService = unitCaster != null ? unitCaster.BatchCastingService : null;

            if (changeService != null && entry.TargetObject != null && entry.MagicData != null)
            {
                var unitGroup = entry.TargetObject.GetComponentInParent<RuntimeDataUnitGroup>();
                if (unitGroup != null)
                {
                    var matchingUnit = unitGroup.GetMatchingUnitData(entry.MagicData.UnitType);
                    if (matchingUnit != null)
                    {
                        float newVal = batchService != null
                            ? batchService.CalculateNewValue(matchingUnit, entry.MagicData)
                            : (matchingUnit.OriginalValue > 0f ? matchingUnit.OriginalValue : 1.0f) * (entry.MagicData.MassScaleMultiplier > 0f ? entry.MagicData.MassScaleMultiplier : 1.0f);

                        // 이미 해제 시점에 총 마나를 일괄 차감했으므로 casterStatData는 null로 전달
                        changeService.ChangeUnit(unitGroup.gameObject, matchingUnit, entry.MagicData, newVal, null, null, null);
                        Debug.Log($"<color=green>[TacticalCommand] Successfully Changed Unit:</color> {unitGroup.name} {matchingUnit.CurrentUnit} -> NewValue: {newVal}");
                    }
                    else
                    {
                        Debug.LogWarning($"[TacticalCommand] No matching unit type ({entry.MagicData.UnitType}) on {unitGroup.name}");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[TacticalCommand] Execution skipped: changeService is null ({changeService == null}) or entry invalid.");
            }

            // 연쇄 격발 피격/시각 연출
            visualizer.PlayCommandExecutionEffect(entry);
        }

        private void CompleteQueueExecution()
        {
            Debug.Log("<color=green>[TacticalCommand] All Queued Commands Executed Successfully.</color>");
            queueData.SetExecuting(false);
            queueData.SetTacticalMode(false);
            visualizer.ShowTacticalVisuals(false);
            visualizer.ClearAllVisuals();
            SetTacticalContext(false);
            cameraFollowService?.SetRequireRightClickToRotate(false);
            isRightClickDown = false;
            maxRightDragDistance = 0f;
        }

        private PureDataUnit GetCurrentSelectedMagic()
        {
            if (quickSlotUI == null)
            {
                quickSlotUI = UnityEngine.Object.FindAnyObjectByType<UnitQuickSlotUIComponent>();
            }

            if (quickSlotUI != null && quickSlotUI.CurrentSelectedUnit != null)
            {
                return quickSlotUI.CurrentSelectedUnit;
            }

            if (magicSlotService != null && magicSlotService.CurrentMagic != null)
            {
                return magicSlotService.CurrentMagic;
            }

            return null;
        }

        private Vector3 GetOriginPosition()
        {
            if (mainCamera != null)
            {
                return mainCamera.transform.position;
            }
            return Vector3.zero;
        }
    }
}
