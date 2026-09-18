using System;
using Cysharp.Threading.Tasks;
using PipeLine.Contexts;
using PipeLine.PipeLineBase;
using UnityEngine;

namespace PipeLine.CharacterDamage.Steps
{
    /// <summary>
    /// 피격 파이프라인 스텝: 명중률 대비 회피율을 판정합니다.
    /// 상태를 갖지 않는 공유 싱글턴이며, 실제로 기다릴 비동기 작업이 없으므로 즉시 완료된 UniTask를 반환합니다.
    /// </summary>
    [Serializable]
    public class EvasionStep : IPipeLineStep<DamageContext>
    {
        public static readonly EvasionStep Instance = new EvasionStep();

        private EvasionStep() { }

        public UniTask<DamageContext> Execute(DamageContext context)
        {
            float hitChance = context.AttackerAccRate - context.VictimEvaRate;
            float randomVal = UnityEngine.Random.value;

            if (randomVal > hitChance)
            {
                context.IsEvaded = true;
                context.Aborted = true; // 회피 시 이후 피격 스텝(Defense 등)은 실행할 필요 없음 - CombatPipelineManager가 중단 처리
                Debug.Log($"[EvasionStep] Evaded! (HitChance: {hitChance:F2}, Roll: {randomVal:F2})");
            }

            return UniTask.FromResult(context);
        }
    }
}
