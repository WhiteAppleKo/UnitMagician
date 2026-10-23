using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PipeLine.Contexts;
using PipeLine.PipeLineBase;
using PipeLine.Visualizers;
using UnityEngine;

namespace PipeLine.UnitMagic.Steps
{
    [Serializable]
    public class ApplyUnitChangeStep : IPipeLineStep<UnitMagicContext>
    {
        public static readonly List<IUnitMagicVisualizer> Visualizers = new List<IUnitMagicVisualizer>();

        public async UniTask<UnitMagicContext> Execute(UnitMagicContext context)
        {
            if (context == null || context.ManaCheckFailed || context.TargetInvalid) return context;

            if (context.TargetUnitGroup != null && context.UnitInstance != null && context.TargetObject != null)
            {
                // 마법 인스턴스를 대상의 적용 마법 목록 끝에 추가한다(30번 - 같은 속성이어도 교체하지 않고 누적).
                // 속성 재계산·시각/데미지 적용은 추가가 발행하는 OnAppliedUnitsChanged 이벤트 경로(UnitApplyLogicSystem) 하나로만 일어난다(28번 §6-1 - 직접 Apply 제거).
                // 즉시 마법(IsInstantUnit, 32번)은 목록에 넣지 않고 ApplyInstant로 한 번만 적용한다(OnInstantUnitApplied 경로, 재계산 없음).
                var unitTarget = context.TargetUnitGroup.UnitTarget;
                bool applied = unitTarget != null && (context.IsInstantUnit
                    ? unitTarget.ApplyInstant(context.UnitInstance)
                    : unitTarget.AddApplied(context.UnitInstance));

                // 단위 변경이 실제로 적용된 경우에만 대상 투사체의 진영을 시전자 진영으로 바꾼다(적 투사체 빼앗기).
                // 조기 반환 경로(ManaCheckFailed/TargetInvalid)와 적용 조건 미충족(목록 추가 실패 포함) 시에는 호출되지 않는다.
                if (applied)
                {
                    PipeLine.Combat.FactionChange.ApplyCasterFaction(context.TargetObject, context.Caster);
                }
            }

            context.IsSuccess = true;
            Debug.Log($"[ApplyUnitChangeStep] Unit Magic successfully applied to target: {context.TargetObject?.name}");

            foreach (var visualizer in Visualizers)
            {
                visualizer?.ShowUnitChangeEffect(context);
            }

            await UniTask.Yield();
            return context;
        }
    }
}
