using System;
using System.Collections.Generic;
using UnityEngine;
using UnitSystem;
using CharacterSystem;
using Movement.Visualizer;
using Movement.RefactoredLocomotion;
using VContainer;

namespace UnitSystem
{
    /// <summary>
    /// 플레이어 자식 오브젝트 MagicLockOn에 부착되는 시간 정지 마법 다중 락온 컴포넌트
    /// 일반 락온과 100% 동일한 탐색 및 가중치 알고리즘으로 예비 락온을 표시하며, 휠 클릭 시 목록에 누적합니다.
    /// </summary>
    public class MagicLockOnComponent : MonoBehaviour, ILockOnComponent
    {
        [Header("Settings")]
        [SerializeField] private float scanRadius = 25f;

        private RuntimeDataMultiLockOn _multiLockOnData;
        private IMultiLockOnVisualizer _visualizer;
        private ILocomotionVisualizer _locomotionVisualizer;
        private UnitChangeService _changeService;
        private CharacterStatSystem _playerStatSystem;
        private UnitQuickSlotUIComponent _quickSlotUI;

        private RuntimeDataUnitGroup _currentHoverTarget;
        private readonly Collider[] _scanColliderBuffer = new Collider[32];

        [Inject]
        public void Construct(
            RuntimeDataMultiLockOn multiLockOnData,
            IMultiLockOnVisualizer visualizer,
            UnitChangeService changeService,
            CharacterStatSystem playerStatSystem = null)
        {
            _multiLockOnData = multiLockOnData;
            _visualizer = visualizer;
            _changeService = changeService;
            _playerStatSystem = playerStatSystem;
        }

        private void Awake()
        {
            if (_multiLockOnData == null) _multiLockOnData = new RuntimeDataMultiLockOn();
            if (_visualizer == null) _visualizer = GetComponentInParent<IMultiLockOnVisualizer>();
            if (_visualizer == null) _visualizer = UnityEngine.Object.FindFirstObjectByType<MultiLockOnVisualizer>();
            if (_locomotionVisualizer == null) _locomotionVisualizer = GetComponentInParent<ILocomotionVisualizer>();
            if (_quickSlotUI == null) _quickSlotUI = UnityEngine.Object.FindFirstObjectByType<UnitQuickSlotUIComponent>();
            GetPlayerStatSystem();
        }

        private void OnEnable()
        {
            if (_visualizer == null) _visualizer = GetComponentInParent<IMultiLockOnVisualizer>();
            if (_visualizer == null) _visualizer = UnityEngine.Object.FindFirstObjectByType<MultiLockOnVisualizer>();
            if (_locomotionVisualizer == null) _locomotionVisualizer = GetComponentInParent<ILocomotionVisualizer>();
            GetPlayerStatSystem();

            if (_visualizer != null)
            {
                _visualizer.SetCrosshairVisible(true);
                _visualizer.UpdateLockOnCount(0);
            }

            _multiLockOnData?.ClearTargets();
            _currentHoverTarget = null;
        }

        private CharacterStatSystem GetPlayerStatSystem()
        {
            if (_playerStatSystem != null) return _playerStatSystem;

            var statComp = GetComponentInParent<CharacterStatComponent>();
            if (statComp == null)
            {
                statComp = UnityEngine.Object.FindFirstObjectByType<CharacterStatComponent>();
            }

            if (statComp != null)
            {
                _playerStatSystem = statComp.StatSystem;
            }

            return _playerStatSystem;
        }

        private void OnDisable()
        {
            // 예비 타겟 마커 끄기
            if (_currentHoverTarget != null && (_multiLockOnData == null || !_multiLockOnData.Contains(_currentHoverTarget)))
            {
                _currentHoverTarget.Highlight(false, false);
                _currentHoverTarget = null;
            }

            // 시간 정지 해제 순간 누적된 대상들에게 일괄 마법 변환 실행
            if (_multiLockOnData != null && _multiLockOnData.TargetCount > 0)
            {
                ExecuteBatchCast();
            }
            else
            {
                ClearAllLockOns();
            }

            if (_visualizer != null)
            {
                _visualizer.SetCrosshairVisible(false);
            }
        }

        private void Update()
        {
            if (_locomotionVisualizer == null)
            {
                _locomotionVisualizer = GetComponentInParent<ILocomotionVisualizer>();
                if (_locomotionVisualizer == null) return;
            }

            // 일반 락온과 100% 동일한 25m 구체 탐색
            int hitCount = Physics.OverlapSphereNonAlloc(_locomotionVisualizer.Position, scanRadius, _scanColliderBuffer);

            RuntimeDataUnitGroup newBestTarget = null;
            float bestScore = float.MinValue;

            Vector3 camPos = _locomotionVisualizer.GetCameraPosition();
            Vector3 camForward = _locomotionVisualizer.GetCameraForward();

            for (int i = 0; i < hitCount; i++)
            {
                var col = _scanColliderBuffer[i];
                if (col == null) continue;

                if (_locomotionVisualizer.Transform != null && col.transform.root == _locomotionVisualizer.Transform.root) continue;

                // 오직 마법 대상(RuntimeDataUnitGroup && IsTargetable)만 검사
                var unitGroup = col.GetComponentInParent<RuntimeDataUnitGroup>();
                if (unitGroup == null || !unitGroup.IsTargetable) continue;

                // 이미 확정 락온된 대상이 아니고, 이전 호버 대상과 다르면 일단 꺼둠
                if (_multiLockOnData == null || !_multiLockOnData.Contains(unitGroup))
                {
                    if (unitGroup != _currentHoverTarget)
                    {
                        unitGroup.Highlight(false, false);
                    }
                }

                Vector3 targetPos = unitGroup.transform.position + Vector3.up * 1.0f;
                Vector3 toTargetFromCam = targetPos - camPos;
                float distFromCam = toTargetFromCam.magnitude;
                if (distFromCam < 0.1f) continue;

                Vector3 dirFromCam = toTargetFromCam / distFromCam;
                float dot = Vector3.Dot(camForward, dirFromCam);

                // 카메라 전방 내적 0.25 이상
                if (dot < 0.25f) continue;

                float distFromPlayer = Vector3.Distance(_locomotionVisualizer.Position, unitGroup.transform.position);

                // 가중치 점수 계산 (시야각 + 거리)
                float angleScore = dot * 60f;
                float distanceScore = (1f / Mathf.Max(distFromPlayer, 1.0f)) * 40f;
                float totalScore = angleScore + distanceScore;

                // 벽 차폐 검사
                if (Physics.Linecast(camPos, targetPos, out RaycastHit hit))
                {
                    if (hit.collider.transform.root != unitGroup.transform.root && !hit.collider.isTrigger)
                    {
                        continue;
                    }
                }

                if (totalScore > bestScore)
                {
                    bestScore = totalScore;
                    newBestTarget = unitGroup;
                }
            }

            // 이전 예비 대상이 바뀌었으면 이전 대상 끄기
            if (_currentHoverTarget != null && _currentHoverTarget != newBestTarget)
            {
                if (_multiLockOnData == null || !_multiLockOnData.Contains(_currentHoverTarget))
                {
                    _currentHoverTarget.Highlight(false, false);
                }
            }

            _currentHoverTarget = newBestTarget;

            // 새로운 최적 대상에게 예비 락온 표시(노란색) 켜기
            if (_currentHoverTarget != null)
            {
                if (_multiLockOnData == null || !_multiLockOnData.Contains(_currentHoverTarget))
                {
                    _currentHoverTarget.Highlight(true, false);
                }
            }
        }

        /// <summary>
        /// 마우스 휠 클릭 시 현재 예비 락온 대상을 확정 락온 목록에 누적/토글합니다.
        /// </summary>
        public void ToggleLockOn()
        {
            if (_currentHoverTarget == null || _multiLockOnData == null) return;

            if (_multiLockOnData.Contains(_currentHoverTarget))
            {
                // 이미 확정 락온된 대상을 다시 클릭하면 락온 해제 -> 예비 락온(노란색)으로 복귀
                _multiLockOnData.RemoveTarget(_currentHoverTarget);
                _currentHoverTarget.Highlight(true, false);
            }
            else
            {
                // 새로운 대상 확정 락온 추가 -> 빨간색 마커 켜기 및 리스트 누적
                _multiLockOnData.AddTarget(_currentHoverTarget);
                _currentHoverTarget.Highlight(true, true);
            }

            _visualizer?.UpdateLockOnCount(_multiLockOnData.TargetCount);
        }

        private void ExecuteBatchCast()
        {
            int targetCount = _multiLockOnData != null ? _multiLockOnData.TargetCount : 0;
            if (targetCount == 0)
            {
                ClearAllLockOns();
                return;
            }

            // 시간 정지 해제 시점에 최종 선택된 마법 단위 확인
            if (_quickSlotUI == null) _quickSlotUI = UnityEngine.Object.FindFirstObjectByType<UnitQuickSlotUIComponent>();
            PureDataUnit selectedUnitData = _quickSlotUI != null ? _quickSlotUI.CurrentSelectedUnit : null;
            if (selectedUnitData == null && _quickSlotUI != null && _quickSlotUI.CatalogService != null)
            {
                var unlocked = _quickSlotUI.CatalogService.GetUnlockedUnits();
                if (unlocked != null && unlocked.Count > 0)
                {
                    selectedUnitData = unlocked[0];
                }
            }

            if (selectedUnitData == null)
            {
                Debug.LogWarning("[MagicLockOnComponent] Batch Cast Aborted: No Unit selected.");
                ClearAllLockOns();
                return;
            }

            var targets = new List<RuntimeDataUnitGroup>(_multiLockOnData.LockedTargets);
            _multiLockOnData.ClearTargets();
            _visualizer?.UpdateLockOnCount(0);

            var statSystem = GetPlayerStatSystem();
            RuntimeStatData casterStat = statSystem?.RuntimeData;

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
                    totalCost += Mathf.RoundToInt(selectedUnitData.BaseCost * diff);
                }
            }

            // 2. 마나 검증 및 부족 시 안전 예외 처리
            if (casterStat != null && casterStat.MP.CurrentValue < totalCost)
            {
                Debug.LogWarning($"[MagicLockOnComponent] Insufficient Mana. (Required: {totalCost}, Current MP: {casterStat.MP.CurrentValue})");
                casterStat.TryConsumeMP(totalCost);
                foreach (var target in targets)
                {
                    if (target != null) target.Highlight(false, false);
                }
                return;
            }

            if (_changeService == null && _quickSlotUI != null && _quickSlotUI.CatalogService != null)
            {
                _changeService = new UnitChangeService(_quickSlotUI.CatalogService);
            }

            GameObject casterObj = _locomotionVisualizer?.Transform != null
                ? _locomotionVisualizer.Transform.root.gameObject
                : transform.root.gameObject;

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
                    _changeService?.ChangeUnit(targetGroup.gameObject, matchingUnitData, selectedUnitData, newValue, casterStat, null, casterObj);
                }
            }

            _visualizer?.PlayBatchCastEffect();
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
            if (_multiLockOnData == null) return;

            var targets = new List<RuntimeDataUnitGroup>(_multiLockOnData.LockedTargets);
            foreach (var target in targets)
            {
                if (target != null)
                {
                    target.Highlight(false, false);
                }
            }

            _multiLockOnData.ClearTargets();
            _visualizer?.UpdateLockOnCount(0);
        }
    }
}
