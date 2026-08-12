using System;
using Cysharp.Threading.Tasks;
using PipeLine.Contexts;
using PipeLine.PipeLineBase;
using UnityEngine;

namespace PipeLine.CharacterDamage.Steps
{
    [Serializable]
    public class CriticalStep : IPipeLineStep<DamageContext>
    {
        public async UniTask<DamageContext> Execute(DamageContext context)
        {
            if (context == null || context.IsEvaded) return context;

            float randomVal = UnityEngine.Random.value;
            if (randomVal <= context.CritRate)
            {
                context.IsCritical = true;
                context.RawDamage = Mathf.RoundToInt(context.RawDamage * context.CritMultiplier);
                Debug.Log($"[CriticalStep] Critical Hit! RawDamage increased to {context.RawDamage}");
            }

            await UniTask.Yield();
            return context;
        }
    }
}
