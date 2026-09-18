using System;
using Cysharp.Threading.Tasks;
using PipeLine.Contexts;
using PipeLine.PipeLineBase;
using UnityEngine;

namespace PipeLine.CharacterDamage.Steps
{
    /// <summary>
    /// 공격 파이프라인 스텝: 치명타 여부를 판정하고 RawDamage에 배율을 반영합니다.
    /// 상태를 갖지 않는 공유 싱글턴이며, 실제로 기다릴 비동기 작업이 없으므로 즉시 완료된 UniTask를 반환합니다.
    /// </summary>
    [Serializable]
    public class CriticalStep : IPipeLineStep<DamageContext>
    {
        public static readonly CriticalStep Instance = new CriticalStep();

        private CriticalStep() { }

        public UniTask<DamageContext> Execute(DamageContext context)
        {
            // CriticalStep은 공격 파이프라인 단계에서 실행되고 EvasionStep은 그 뒤 피격 파이프라인 단계에서 실행되므로
            // (CombatPipelineManager.RunFullPipeline 참고), 이 시점의 context.IsEvaded는 항상 기본값(false)이다.
            // IsEvaded 분기 검사는 필요 없다.

            float randomVal = UnityEngine.Random.value;
            if (randomVal <= context.CritRate)
            {
                context.IsCritical = true;
                context.RawDamage = Mathf.RoundToInt(context.RawDamage * context.CritMultiplier);
                Debug.Log($"[CriticalStep] Critical Hit! RawDamage increased to {context.RawDamage}");
            }

            return UniTask.FromResult(context);
        }
    }
}
