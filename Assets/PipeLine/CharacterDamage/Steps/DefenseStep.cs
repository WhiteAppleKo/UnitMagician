using System;
using Cysharp.Threading.Tasks;
using PipeLine.Contexts;
using PipeLine.PipeLineBase;
using UnityEngine;

namespace PipeLine.CharacterDamage.Steps
{
    /// <summary>
    /// 피격 파이프라인 스텝: 방어력을 반영해 FinalDamage를 계산합니다.
    /// 상태를 갖지 않는 공유 싱글턴이며, 실제로 기다릴 비동기 작업이 없으므로 즉시 완료된 UniTask를 반환합니다.
    /// </summary>
    [Serializable]
    public class DefenseStep : IPipeLineStep<DamageContext>
    {
        public static readonly DefenseStep Instance = new DefenseStep();

        private DefenseStep() { }

        public UniTask<DamageContext> Execute(DamageContext context)
        {
            // 회피 등으로 인한 중단 판정은 CombatPipelineManager의 피격 루프가 각 스텝 실행 직후
            // context.Aborted를 확인해서 중앙에서 처리한다 - 이 스텝이 개별적으로 IsEvaded를 재검사할 필요 없음.

            int calculatedDamage = Mathf.Max(1, context.RawDamage - context.Defense);
            context.FinalDamage = calculatedDamage;

            Debug.Log($"[DefenseStep] RawDamage: {context.RawDamage}, Defense: {context.Defense} => FinalDamage: {context.FinalDamage}");

            return UniTask.FromResult(context);
        }
    }
}
