using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;
using CharacterSystem;
using UnitSystem;
using TimeSlowFilterSystem;

namespace TacticalCommandSystem
{
    /// <summary>
    /// 전술 명령 큐잉 및 시간 정지 연쇄 마법 발동 로직을 담당하는 순수 C# Logic System입니다.
    /// MonoBehaviour를 상속받지 않으며, VContainer 생명주기 인터페이스를 구현합니다.
    /// </summary>
    public class TacticalCommandLogicSystem : IInitializable, ITickable, IDisposable
    {
        private readonly RuntimeDataTacticalQueue queueData;
        private readonly RuntimeDataTimeSlow timeSlowData;
        private readonly ICharacterStatService statService;
        private readonly RuntimeDataUnitQuickSlot quickSlotData;
        private readonly IUnitMagicSlotService magicSlotService;
        private readonly ITacticalCommandVisualizer visualizer;
        private readonly ITargetStencilService stencilService;
        private readonly IUnitChangeService unitChangeService;
        private readonly Synty.AnimationBaseLocomotion.Samples.InputSystem.InputReader inputReader;

        private Camera mainCamera;
        private GameObject currentHoverTarget;
        private float executionTimer;

        [Inject]
        public TacticalCommandLogicSystem(
            RuntimeDataTacticalQueue queueData,
            ITacticalCommandVisualizer visualizer,
            RuntimeDataTimeSlow timeSlowData = null,
            ICharacterStatService statService = null,
            RuntimeDataUnitQuickSlot quickSlotData = null,
            IUnitMagicSlotService magicSlotService = null,
            ITargetStencilService stencilService = null,
            IUnitChangeService unitChangeService = null,
            Synty.AnimationBaseLocomotion.Samples.InputSystem.InputReader inputReader = null)
        {
            this.queueData = queueData ?? throw new ArgumentNullException(nameof(queueData));
            this.visualizer = visualizer ?? throw new ArgumentNullException(nameof(visualizer));
            this.timeSlowData = timeSlowData ?? new RuntimeDataTimeSlow(null);
            this.statService = statService;
            this.quickSlotData = quickSlotData;
            this.magicSlotService = magicSlotService;
            this.stencilService = stencilService;
            this.unitChangeService = unitChangeService;
            this.inputReader = inputReader;
        }

        public void Initialize()
        {
            if (timeSlowData != null)
            {
                timeSlowData.OnSlowStateChanged += HandleTimeSlowChanged;
            }
        }

        public void Dispose()
        {
            if (timeSlowData != null)
            {
                timeSlowData.OnSlowStateChanged -= HandleTimeSlowChanged;
            }

            visualizer?.ClearAllVisuals();
            queueData?.Clear();
        }

        private void HandleTimeSlowChanged(bool isActive)
        {
            if (isActive)
            {
                queueData.SetTacticalMode(true);
                visualizer.ShowTacticalVisuals(true);
            }
            else
            {
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

            // 2. 전술 모드 (시간 정지 상태) 조작 처리
            if (queueData.IsTacticalModeActive)
            {
                ProcessTacticalTargeting();
            }
        }

        private void ProcessTacticalTargeting()
        {
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
                    // RuntimeDataUnitGroup 또는 Collider 소유 대상 탐색
                    var unitGroup = hit.collider.GetComponentInParent<RuntimeDataUnitGroup>();
                    if (unitGroup != null && unitGroup.IsTargetable)
                    {
                        validTarget = unitGroup.gameObject;
                        break;
                    }

                    // 일반 적 또는 반응 오브젝트
                    if (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Interactable") || hit.collider.GetComponent<Rigidbody>() != null)
                    {
                        validTarget = hit.collider.gameObject;
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

            // 우클릭: 마지막 예약 명령 취소
            if (mouse.rightButton.wasPressedThisFrame)
            {
                CancelLastCommand();
            }
        }

        private void TryEnqueueCurrentMagic(GameObject target)
        {
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
            Debug.Log($"<color=green>[TacticalCommand] Starting Sequential Execution of {queueData.CommandQueue.Count} Commands!</color>");
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

            // 실제 마나 차감
            if (statService != null && entry.ManaCost > 0)
            {
                statService.UseMP(entry.ManaCost);
            }

            // 마법 적용 (RuntimeDataUnitGroup 매칭 및 유닛 변환)
            if (unitChangeService != null && entry.TargetObject != null && entry.MagicData != null)
            {
                var unitGroup = entry.TargetObject.GetComponentInParent<RuntimeDataUnitGroup>();
                if (unitGroup != null)
                {
                    var matchingUnit = unitGroup.GetMatchingUnitData(entry.MagicData.UnitType);
                    if (matchingUnit != null)
                    {
                        float originalVal = matchingUnit.OriginalValue > 0f ? matchingUnit.OriginalValue : 1.0f;
                        float multiplier = entry.MagicData.MassScaleMultiplier > 0f ? entry.MagicData.MassScaleMultiplier : 1.0f;
                        float newVal = originalVal * multiplier;
                        unitChangeService.ChangeUnit(unitGroup.gameObject, matchingUnit, entry.MagicData, newVal);
                    }
                }
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
        }

        private PureDataUnit GetCurrentSelectedMagic()
        {
            if (quickSlotData != null && quickSlotData.SelectedUnit != null)
            {
                return quickSlotData.SelectedUnit;
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
