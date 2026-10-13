using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using CharacterSystem;

namespace UnitSystem
{
    /// <summary>
    /// 단위 마법 일괄 시전(ExecuteBatchCast)을 수행하는 컴포넌트입니다.
    /// 시점은 3인칭 숄더뷰 하나로 고정이므로(22번 기획서) 카메라 모드에 따른 시전 전략 전환은 없으며,
    /// 마법 락온은 항상 자식 오브젝트(MagicLockOnComponent)가 시간 정지 시 전담합니다.
    /// 탑뷰 마우스 즉시 시전 전략(TopViewMouseCastingStrategy)은 01.UnitSystem/99.Legacy로 격리되었습니다.
    /// </summary>
    public class UnitCasterSystem : MonoBehaviour
    {
        private IUnitChangeService changeService;
        private IUnitBatchCastingService batchCastingService;
        private RuntimeDataMultiLockOn multiLockOnData;
        private IMultiLockOnVisualizer multiLockOnVisualizer;
        private CharacterStatSystem playerStatSystem;

        public RuntimeDataMultiLockOn MultiLockOnData => multiLockOnData;

        public IUnitChangeService ChangeService => changeService;
        public IUnitBatchCastingService BatchCastingService => batchCastingService;

        private CharacterSystem.ICharacterStatService playerStatService;

        [Inject]
        public void Construct(
            IUnitChangeService changeService,
            RuntimeDataMultiLockOn multiLockOnData,
            IUnitBatchCastingService batchCastingService,
            IMultiLockOnVisualizer multiLockOnVisualizer = null)
        {
            this.changeService = changeService;
            this.multiLockOnData = multiLockOnData;
            this.batchCastingService = batchCastingService;
            this.multiLockOnVisualizer = multiLockOnVisualizer;

            BindMultiLockOnEvents();
        }

        private void Start()
        {
            BindMultiLockOnEvents();
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
            BindMultiLockOnEvents();
        }

        private void OnDisable()
        {
            UnbindMultiLockOnEvents();
        }

        private void OnDestroy()
        {
            UnbindMultiLockOnEvents();
        }

        private void HandleBatchCastRequested()
        {
            ExecuteBatchCast();
        }

        public void ExecuteBatchCast()
        {
            // 13번 기획서: (대상, 마법) 쌍 큐(Entries)를 쌍마다 자기 마법으로 처리한다. 지정 대기 중이던 락온은 버려진다.
            if (multiLockOnData == null || multiLockOnData.EntryCount == 0)
            {
                ClearAllLockOns();
                return;
            }

            var entries = new List<LockOnCastEntry>(multiLockOnData.Entries);
            var pendingTarget = multiLockOnData.PendingTarget;
            multiLockOnData.ClearEntries();
            if (pendingTarget != null) pendingTarget.Highlight(false, false);
            multiLockOnVisualizer?.UpdateLockOnCount(0);

            // 장착 해제/대상 소실 쌍 제외, 총비용 계산, UseMP 한 번 차감, 쌍별 ChangeUnit은 모두 IUnitBatchCastingService의
            // 단일 규칙이 처리한다(HUD 예상 비용과 같은 계산). 여기서는 따로 필터링/계산하지 않는다(15-A).
            var statComp = GetComponentInParent<CharacterSystem.CharacterStatComponent>()
                        ?? GetComponent<CharacterSystem.CharacterStatComponent>()
                        ?? CharacterSystem.CharacterStatComponent.PlayerStat;
            CharacterSystem.ICharacterStatService statService = statComp?.StatService ?? playerStatService ?? playerStatSystem;

            GameObject casterObj = gameObject;

            bool success = batchCastingService != null && batchCastingService.ExecuteBatchCast(entries, statService, casterObj);
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

            foreach (var entry in multiLockOnData.Entries)
            {
                if (entry.Target != null)
                {
                    entry.Target.Highlight(false, false);
                }
            }

            var pendingTarget = multiLockOnData.PendingTarget;
            if (pendingTarget != null)
            {
                pendingTarget.Highlight(false, false);
            }

            multiLockOnData.ClearEntries();
            multiLockOnVisualizer?.UpdateLockOnCount(0);
        }
    }
}
