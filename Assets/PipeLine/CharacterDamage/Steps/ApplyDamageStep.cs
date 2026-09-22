using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PipeLine.Combat;
using PipeLine.Contexts;
using PipeLine.PipeLineBase;
using PipeLine.Visualizers;
using UnityEngine;

namespace PipeLine.CharacterDamage.Steps
{
    /// <summary>
    /// 공격/피격 파이프라인이 끝나면 항상 실행되는 고정 마지막 스텝입니다. (플래그 게이트 대상이 아님 - CombatPipelineManager 참고)
    /// 회피 시에는 회피 시각효과만 재생하고, 그렇지 않으면 victim의 IDamageable에 FinalDamage를 전달한 뒤 데미지 시각효과를 재생합니다.
    /// 상태를 갖지 않는 공유 싱글턴이며, 실제로 기다릴 비동기 작업이 없으므로 즉시 완료된 UniTask를 반환합니다.
    /// </summary>
    [Serializable]
    public class ApplyDamageStep : IPipeLineStep<DamageContext>
    {
        public static readonly ApplyDamageStep Instance = new ApplyDamageStep();
        public static readonly List<IDamageVisualizer> Visualizers = new List<IDamageVisualizer>();

        private ApplyDamageStep() { }

        public UniTask<DamageContext> Execute(DamageContext context)
        {
            if (context.IsEvaded)
            {
                for (int i = 0; i < Visualizers.Count; i++)
                {
                    Visualizers[i]?.ShowEvadeText(context);
                }

                return UniTask.FromResult(context);
            }

            // 회피가 아닌 다른 게이트 스텝(예: AttributeGateStep)이 Aborted를 세운 경우 - 회피와 달리
            // 어떤 연출도 재생하지 않고 조용히 종료한다(투사체 자체의 물리 튕김은 파이프라인과 무관하게 이미 일어남).
            if (context.Aborted)
            {
                Debug.Log($"[ApplyDamageStep] Aborted (non-evasion gate) - no damage applied to victim: {context.Victim?.name}");
                return UniTask.FromResult(context);
            }

            // DefenseStep 등 이전 스텝을 거치지 않은 단독 실행 시 초기 RawDamage 수치 그대로 FinalDamage에 반영
            if (context.FinalDamage <= 0 && context.RawDamage > 0)
            {
                context.FinalDamage = context.RawDamage;
            }

            if (context.Victim != null && context.Victim.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.ApplyDamage(context.FinalDamage);
            }

            Debug.Log($"[ApplyDamageStep] Applied FinalDamage: {context.FinalDamage} to victim: {context.Victim?.name}");

            for (int i = 0; i < Visualizers.Count; i++)
            {
                Visualizers[i]?.ShowDamageText(context);
            }

            return UniTask.FromResult(context);
        }
    }
}
