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

            if (context.TargetRuntimeData != null && context.SelectedPureData != null && context.TargetObject != null)
            {
                if (context.SelectedPureData.UnitType == UnitSystem.UnitType.Vector)
                {
                    context.TargetRuntimeData.SetVectorData(context.VectorDirection, context.VectorSpeed);
                }
                else
                {
                    context.TargetRuntimeData.UpdateUnitData(context.SelectedPureData.UnitType, context.NewValue, context.SelectedPureData);
                }

                if (context.SelectedPureData.Applicator != null)
                {
                    context.SelectedPureData.Applicator.Apply(context.TargetObject, context.TargetRuntimeData);
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
