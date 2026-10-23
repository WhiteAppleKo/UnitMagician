using System;
using Cysharp.Threading.Tasks;
using PipeLine.Combat;
using PipeLine.Contexts;
using PipeLine.PipeLineBase;
using UnitSystem;
using UnityEngine;

namespace PipeLine.CharacterDamage.Steps
{
    /// <summary>
    /// 피격 파이프라인 스텝(크기 게이트, AttributeGateStep 다음): 가해 오브젝트(Source)의 질량 배율
    /// (마법 대상 RuntimeData의 CurrentMass - 초기 1에서 적용 마법 목록의 Mass 배율을 합산한 스케일 배율, 질량 속성이 없으면 1, 32번)이 피격 대상의 요구 비율(IGateRequirementProvider.RequiredMassRatio) 이상인지
    /// 확인해 DamageContext.SizeGatePassed에 기록합니다. 요구값은 대상의 PureData에서 오며 이 스텝에는 기준 숫자가 없습니다.
    /// 실패해도 Aborted는 세우지 않습니다 - 속성 게이트는 이미 통과했으므로 소비자(돌문/보스)가 "크기 부족"으로 개별 판정해
    /// 금 갔다 복구 같은 피드백을 내야 하기 때문입니다. 이미 Aborted(예: 속성 게이트 실패)면 실행하지 않습니다.
    /// IGateRequirementProvider가 없는 대상은 크기 게이트가 요구되지 않는 대상이므로 통과로 기록합니다.
    /// 상태를 갖지 않는 공유 싱글턴이며, 실제로 기다릴 비동기 작업이 없으므로 즉시 완료된 UniTask를 반환합니다.
    /// </summary>
    [Serializable]
    public class SizeGateStep : IPipeLineStep<DamageContext>
    {
        public static readonly SizeGateStep Instance = new SizeGateStep();

        private SizeGateStep() { }

        public UniTask<DamageContext> Execute(DamageContext context)
        {
            if (context.Aborted)
            {
                return UniTask.FromResult(context);
            }

            if (context.Victim == null || !context.Victim.TryGetComponent<IGateRequirementProvider>(out var requirement))
            {
                context.SizeGatePassed = true;
                return UniTask.FromResult(context);
            }

            var unitGroup = AttributeGateStep.ResolveSourceUnitGroup(context);
            // 투사체 마법 대상 RuntimeData의 질량 배율(32번). 질량 속성을 받을 수 없는 대상이면 1.
            var state = unitGroup != null ? unitGroup.UnitTarget : null;
            float massMultiplier = state != null && state.HasUnit(UnitType.Mass) ? state.CurrentMass : 1f;

            context.SizeGatePassed = massMultiplier >= requirement.RequiredMassRatio;

            Debug.Log($"[SizeGateStep] Gate {(context.SizeGatePassed ? "PASSED" : "FAILED")} - Mass: {massMultiplier:F2}, Required: {requirement.RequiredMassRatio:F2}");

            return UniTask.FromResult(context);
        }
    }
}
