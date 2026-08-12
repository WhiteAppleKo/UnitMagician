using System;
using Cysharp.Threading.Tasks;
using PipeLine.Contexts;
using PipeLine.PipeLineBase;
using UnityEngine;

namespace PipeLine.UnitMagic.Steps
{
    [Serializable]
    public class TargetValidStep : IPipeLineStep<UnitMagicContext>
    {
        public async UniTask<UnitMagicContext> Execute(UnitMagicContext context)
        {
            if (context == null || context.ManaCheckFailed) return context;

            if (context.TargetObject == null)
            {
                context.TargetInvalid = true;
                Debug.LogWarning("[TargetValidStep] TargetObject is null or invalid.");
            }

            await UniTask.Yield();
            return context;
        }
    }
}
