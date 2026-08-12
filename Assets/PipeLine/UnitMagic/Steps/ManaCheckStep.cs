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
    public class ManaCheckStep : IPipeLineStep<UnitMagicContext>
    {
        public static readonly List<IUnitMagicVisualizer> Visualizers = new List<IUnitMagicVisualizer>();

        public async UniTask<UnitMagicContext> Execute(UnitMagicContext context)
        {
            if (context == null) return context;

            if (context.CasterStatData != null)
            {
                if (!context.CasterStatData.TryConsumeMP(context.RequiredMana))
                {
                    context.ManaCheckFailed = true;
                    Debug.LogWarning($"<color=red>[ManaCheckStep] Insufficient Mana in RuntimeStatData! (Required: {context.RequiredMana}, Current MP: {context.CasterStatData.MP.CurrentValue} / {context.CasterStatData.MP.MaxValue})</color>");

                    foreach (var visualizer in Visualizers)
                    {
                        visualizer?.ShowInsufficientManaWarning(context);
                    }
                }
                else
                {
                    Debug.Log($"<color=green>[ManaCheckStep] MP Deducted Successfully! (Consumed: {context.RequiredMana}, Remaining MP: {context.CasterStatData.MP.CurrentValue} / {context.CasterStatData.MP.MaxValue})</color>");
                }
            }
            else if (context.CurrentMana < context.RequiredMana)
            {
                context.ManaCheckFailed = true;
                Debug.LogWarning($"[ManaCheckStep] Insufficient Mana! (Required: {context.RequiredMana}, Current: {context.CurrentMana})");

                foreach (var visualizer in Visualizers)
                {
                    visualizer?.ShowInsufficientManaWarning(context);
                }
            }
            else
            {
                context.CurrentMana -= context.RequiredMana;
                Debug.Log($"[ManaCheckStep] Mana deducted. Remaining Mana: {context.CurrentMana}");
            }

            await UniTask.Yield();
            return context;
        }
    }
}
