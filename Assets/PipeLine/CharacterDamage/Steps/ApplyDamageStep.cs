using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PipeLine.Contexts;
using PipeLine.PipeLineBase;
using PipeLine.Visualizers;
using UnityEngine;

namespace PipeLine.CharacterDamage.Steps
{
    [Serializable]
    public class ApplyDamageStep : IPipeLineStep<DamageContext>
    {
        public static readonly List<IDamageVisualizer> Visualizers = new List<IDamageVisualizer>();

        public async UniTask<DamageContext> Execute(DamageContext context)
        {
            if (context == null || context.IsEvaded) return context;

            Debug.Log($"[ApplyDamageStep] Applied FinalDamage: {context.FinalDamage} to victim: {context.Victim?.name}");

            foreach (var visualizer in Visualizers)
            {
                visualizer?.ShowDamageText(context);
            }

            await UniTask.Yield();
            return context;
        }
    }
}
