using PipeLine.Contexts;
using PipeLine.PipeLineBase;
using UnityEngine;

namespace PipeLine.UnitMagic
{
    [CreateAssetMenu(fileName = "UnitMagicPipeLine", menuName = "PipeLine/Unit Magic PipeLine")]
    public class UnitMagicPipeLine : PipeLineSo<UnitMagicContext>
    {
        private void OnEnable()
        {
            if (steps == null || steps.Count == 0)
            {
                steps = new System.Collections.Generic.List<PipeLineBase.IPipeLineStep<UnitMagicContext>>
                {
                    new Steps.ManaCheckStep(),
                    new Steps.TargetValidStep(),
                    new Steps.ApplyUnitChangeStep()
                };
            }
        }

        protected override bool ShouldBreak(UnitMagicContext context)
        {
            if (context == null) return true;

            // 마나 검증 실패 또는 타겟 유효성 검증 실패 시 파이프라인 즉시 중단
            if (context.ManaCheckFailed || context.TargetInvalid)
            {
                Debug.Log($"[{name}] UnitMagicPipeLine broken: ManaCheckFailed={context.ManaCheckFailed}, TargetInvalid={context.TargetInvalid}");
                return true;
            }

            return false;
        }
    }
}
