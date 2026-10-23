using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UnitSystem
{
    public class UnitChangeService : IUnitChangeService
    {
        private readonly IUnitCatalogService catalogService;
        private readonly IUnitApplicatorRegistry applicatorRegistry;
        private readonly IUnitTargetStateCalculator stateCalculator;

        public UnitChangeService(IUnitCatalogService catalogService, IUnitApplicatorRegistry applicatorRegistry, IUnitTargetStateCalculator stateCalculator)
        {
            this.catalogService = catalogService;
            this.applicatorRegistry = applicatorRegistry;
            this.stateCalculator = stateCalculator;
        }

        public bool ChangeUnit(RuntimeDataUnitGroup target, PureDataUnit spell, UnitCastSource source, CharacterSystem.RuntimeStatData manaPayer = null, PipeLine.UnitMagic.UnitMagicPipeLine pipeLine = null)
        {
            if (target == null)
            {
                Debug.LogError("[UnitChangeService] Target RuntimeDataUnitGroup is null.");
                return false;
            }

            if (spell == null)
            {
                Debug.LogError("[UnitChangeService] Provided PureDataUnit is null.");
                return false;
            }

            // 대상이 그 마법 유형의 속성을 갖지 않으면 적용 대상이 아니다(변경 전과 같이 조용히 건너뜀).
            var state = target.UnitTarget;
            if (state == null || !state.HasUnit(spell.UnitType)) return false;

            if (!catalogService.IsEquipped(spell))
            {
                Debug.LogWarning($"[UnitChangeService] Cannot change unit. Unit {spell.name} is not equipped.");
                return false;
            }

            var applicator = applicatorRegistry?.GetByDataType(spell.GetType());
            if (applicator == null)
            {
                Debug.LogWarning($"[UnitChangeService] No applicator registered for {spell.GetType().Name} ({spell.name}).");
                return false;
            }

            // 마법 한 번의 런타임 인스턴스(원본 + 시전자 영향을 거친 캐싱 수치)
            var instance = applicator.CreateInstance(spell, source ?? UnitCastSource.None);
            if (instance == null) return false;

            // 마나 코스트 계산 수식: Cost = BaseCost * |현재 값 - 추가 후 예상 값|. 예상 값은 "현재 적용 목록 + 새 인스턴스"를 가진
            // 이벤트 없는 복사본(CreateSimulation)을 실제 적용과 같은 재계산(IUnitTargetStateCalculator.Recalculate)으로 만든 값이다(실제 상태·목록은 바꾸지 않음).
            var simulation = state.CreateSimulation(new[] { instance });
            stateCalculator?.Recalculate(simulation);
            // 속성의 현재 값 읽기는 그 마법의 Applicator(ReadValue)가 정한다(미리보기와 같은 함수).
            float currentValue = applicator.ReadValue(state);
            float predictedValue = applicator.ReadValue(simulation);
            int baseCost = spell.BaseCost > 0 ? spell.BaseCost : 10;
            float valueDiff = Mathf.Abs(currentValue - predictedValue);
            int calculatedCost = Mathf.Max(Mathf.RoundToInt(baseCost * valueDiff), baseCost);

            var context = new PipeLine.Contexts.UnitMagicContext
            {
                Caster = source?.Caster,
                CasterStatData = manaPayer,
                TargetObject = target.gameObject,
                TargetUnitGroup = target,
                SelectedPureData = spell,
                UnitInstance = instance,
                IsInstantUnit = applicator.IsInstant,
                RequiredMana = calculatedCost
            };

            if (pipeLine != null)
            {
                // 목록 추가(즉시 마법이면 ApplyInstant)와 진영 변경은 ApplyUnitChangeStep이 단위 변경이 실제로 적용된 경우에만 처리한다(여기서 호출하지 않는다).
                pipeLine.Run(context).Forget();
                return true;
            }

            // 마나 수치 검증 및 차감 연동 (Fallback)
            if (manaPayer != null)
            {
                if (!manaPayer.TryConsumeMP(calculatedCost))
                {
                    Debug.LogWarning($"[UnitChangeService] Insufficient Mana! Required: {calculatedCost}, Current MP: {manaPayer.MP.CurrentValue}");
                    return false;
                }
            }

            string manaText = manaPayer != null
                ? $"Mana Cost Consumed: {calculatedCost}"
                : $"No mana consumed here (mana already paid by caller; single-cast cost would be {calculatedCost})";
            Debug.Log($"[UnitChangeService] {manaText}. Unit Applying: {spell.UnitType}({currentValue} -> {predictedValue}) by {spell.name}");

            // 지속 마법: 적용 마법 목록 끝에 추가 - 상태 재계산·시각/데미지 적용은 OnAppliedUnitsChanged 이벤트 경로(UnitApplyLogicSystem) 하나로만 일어난다(28번 §6-1, 31번 §3-2).
            // 즉시 마법(32번): 목록에 넣지 않고 ApplyInstant로 한 번만 적용한다(OnInstantUnitApplied 경로, 재계산 없음).
            bool applied = applicator.IsInstant ? state.ApplyInstant(instance) : state.AddApplied(instance);

            // 마나 소모와 적용이 성공한 뒤에만 대상 투사체의 진영을 시전자 진영으로 바꾼다(적 투사체 빼앗기)
            if (applied)
            {
                PipeLine.Combat.FactionChange.ApplyCasterFaction(target.gameObject, source?.Caster);
            }

            return applied;
        }
    }
}
