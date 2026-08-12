using System;
using Cysharp.Threading.Tasks;
using PipeLine.Contexts;
using PipeLine.PipeLineBase;
using UnityEngine;

namespace PipeLine.CharacterDamage.Steps
{
    [Serializable]
    public class EvasionStep : IPipeLineStep<DamageContext>
    {
        public async UniTask<DamageContext> Execute(DamageContext context)
        {
            if (context == null) return context;

            float hitChance = context.AttackerAccRate - context.VictimEvaRate;
            float randomVal = UnityEngine.Random.value;

            if (randomVal > hitChance)
            {
                context.IsEvaded = true;
                Debug.Log($"[EvasionStep] Evaded! (HitChance: {hitChance:F2}, Roll: {randomVal:F2})");
            }

            await UniTask.Yield();
            return context;
        }
    }
}
