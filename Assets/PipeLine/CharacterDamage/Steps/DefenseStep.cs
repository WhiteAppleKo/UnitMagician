using System;
using Cysharp.Threading.Tasks;
using PipeLine.Contexts;
using PipeLine.PipeLineBase;
using UnityEngine;

namespace PipeLine.CharacterDamage.Steps
{
    [Serializable]
    public class DefenseStep : IPipeLineStep<DamageContext>
    {
        public async UniTask<DamageContext> Execute(DamageContext context)
        {
            if (context == null || context.IsEvaded) return context;

            int calculatedDamage = Mathf.Max(1, context.RawDamage - context.Defense);
            context.FinalDamage = calculatedDamage;

            Debug.Log($"[DefenseStep] RawDamage: {context.RawDamage}, Defense: {context.Defense} => FinalDamage: {context.FinalDamage}");

            await UniTask.Yield();
            return context;
        }
    }
}
