using System;
using UnityEngine;
using VContainer.Unity;

namespace UnitSystem
{
    /// <summary>
    /// 마법 적용의 단일 호출 주체([L] 순수 C#, 28번 §6-6 (b), 31번 §3-2 4)입니다. RuntimeDataUnitGroup의 정적 이벤트(OnAnyUnitApplyRequested)를 구독해,
    /// 적용 마법 목록이 바뀔 때마다
    ///   1) 대상 상태를 "PureData 값으로 리셋 → 목록 순회 적용 → 확정"으로 다시 만들고(IUnitTargetStateCalculator.Recalculate - 미리보기와 같은 함수),
    ///   2) 목록이 바뀐 속성의 Applicator로 시각을 적용하고(ApplyVisual),
    ///   3) 대상에 공격 데미지가 있으면 BaseDamage에서 시작한 지역 값으로 대상이 지원하는 속성의 Applicator마다 데미지 계산 자리(ApplyDamage)를 거친 뒤
    ///      최종값을 SetCurrentDamage로 한 번만 저장한다
    ///      (속성 하나만 바뀌어도 데미지가 다른 속성 결과를 잃지 않게, 31번 §6-4).
    /// 추가와 해제가 같은 경로로 처리되므로 누적과 해제가 같은 규칙을 씁니다.
    /// 즉시 마법(Vector, 32번)은 OnAnyInstantUnitApplyRequested로 따로 받아, 재계산·데미지 계산 없이 그 인스턴스의 Applicator로 시각만 한 번 적용합니다.
    /// 대상 오브젝트는 풀링되는 프리팹이라 개별 MonoBehaviour에 DI 주입하기 어려우므로, 대상은 이벤트에 실려 온 UnitApplyTarget으로만 다룹니다.
    /// </summary>
    public class UnitApplyLogicSystem : IInitializable, IDisposable
    {
        private static readonly UnitType[] AllUnitTypes = BuildUnitTypes();

        private readonly IUnitApplicatorRegistry registry;
        private readonly IUnitTargetStateCalculator stateCalculator;

        public UnitApplyLogicSystem(IUnitApplicatorRegistry registry, IUnitTargetStateCalculator stateCalculator)
        {
            this.registry = registry;
            this.stateCalculator = stateCalculator;
        }

        public void Initialize()
        {
            RuntimeDataUnitGroup.OnAnyUnitApplyRequested += HandleUnitApplyRequested;
            RuntimeDataUnitGroup.OnAnyInstantUnitApplyRequested += HandleInstantUnitApplyRequested;
        }

        public void Dispose()
        {
            RuntimeDataUnitGroup.OnAnyUnitApplyRequested -= HandleUnitApplyRequested;
            RuntimeDataUnitGroup.OnAnyInstantUnitApplyRequested -= HandleInstantUnitApplyRequested;
        }

        private void HandleInstantUnitApplyRequested(UnitApplyTarget target, RuntimeDataUnitTarget state, RuntimeDataUnit instance)
        {
            if (target == null || state == null || instance == null) return;

            var applicator = registry?.GetByUnitType(instance.UnitType);
            if (applicator == null)
            {
                Debug.LogWarning($"[UnitApplyLogicSystem] No applicator registered for unit type {instance.UnitType}.");
                return;
            }

            applicator.ApplyVisual(target, state, instance);
        }

        private void HandleUnitApplyRequested(UnitApplyTarget target, RuntimeDataUnitTarget state, UnitType changed, RuntimeDataUnit added)
        {
            if (state == null) return;

            stateCalculator?.Recalculate(state);

            if (target == null) return;

            var applicator = registry?.GetByUnitType(changed);
            if (applicator == null)
            {
                Debug.LogWarning($"[UnitApplyLogicSystem] No applicator registered for unit type {changed}.");
            }
            else
            {
                applicator.ApplyVisual(target, state, added);
            }

            var damage = target.AttackDamage;
            if (damage == null) return;

            // BaseDamage에서 시작한 지역 값으로 계산 자리를 모두 거친 뒤 최종값을 한 번만 저장한다(값이 그대로면 이벤트 없음).
            int result = damage.BaseDamage;
            for (int i = 0; i < AllUnitTypes.Length; i++)
            {
                if (!state.HasUnit(AllUnitTypes[i])) continue;

                var damageApplicator = registry?.GetByUnitType(AllUnitTypes[i]);
                if (damageApplicator != null) result = damageApplicator.ApplyDamage(result, damage, state);
            }
            damage.SetCurrentDamage(result);
        }

        private static UnitType[] BuildUnitTypes()
        {
            var values = (UnitType[])Enum.GetValues(typeof(UnitType));
            return Array.FindAll(values, t => t != UnitType.None);
        }
    }
}
