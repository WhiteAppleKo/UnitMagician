using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using PipeLine.Contexts;
using PipeLine.CharacterDamage;
using PipeLine.UnitMagic;
using CharacterSystem;

namespace InteractionSystem.Logic
{
    /// <summary>
    /// 상호작용 신호 수신 후 CharacterDamagePipeLine 및 UnitMagicPipeLine 파이프라인 연산을 실행하고 스탯을 최종 적용하는 로직 시스템입니다.
    /// </summary>
    public class InteractionSystem : IInteractionService
    {
        private readonly CharacterDamagePipeLine damagePipeLine;
        private readonly UnitMagicPipeLine unitMagicPipeLine;

        public event Action<DamageContext> OnDamageProcessed;
        public event Action<DamageContext> OnHealProcessed;
        public event Action<UnitMagicContext> OnUnitMagicProcessed;

        public InteractionSystem(CharacterDamagePipeLine damagePipeLine = null, UnitMagicPipeLine unitMagicPipeLine = null)
        {
            this.damagePipeLine = damagePipeLine;
            this.unitMagicPipeLine = unitMagicPipeLine;
        }

        public async UniTask ProcessDamageAsync(DamageContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            await UniTask.Yield();

            // 파이프라인 연산 실행
            if (damagePipeLine != null)
            {
                await damagePipeLine.Run(context);
            }
            else
            {
                // 파이프라인 미지정 시 기본 연산
                if (!context.IsEvaded)
                {
                    context.FinalDamage = Mathf.Max(1, context.RawDamage - context.Defense);
                }
            }

            // 피격 스탯 차감 적용
            if (!context.IsEvaded && context.Victim != null && context.Victim.TryGetComponent<CharacterStatComponent>(out var statComponent))
            {
                statComponent.StatSystem?.TakeDamage(context.FinalDamage);
            }

            OnDamageProcessed?.Invoke(context);
        }

        public async UniTask ProcessHealAsync(DamageContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            await UniTask.Yield();

            if (context.Victim != null && context.Victim.TryGetComponent<CharacterStatComponent>(out var statComponent))
            {
                statComponent.StatSystem?.Heal(context.RawDamage);
            }

            OnHealProcessed?.Invoke(context);
        }

        public async UniTask ProcessUnitMagicAsync(UnitMagicContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            // 파이프라인 연산 실행
            if (unitMagicPipeLine != null)
            {
                await unitMagicPipeLine.Run(context);
            }

            // 파이프라인 성공 시 마나 소비 적용
            if (!context.ManaCheckFailed && !context.TargetInvalid)
            {
                if (context.Caster != null && context.Caster.TryGetComponent<CharacterStatComponent>(out var casterStat))
                {
                    casterStat.StatSystem?.UseMP(context.RequiredMana);
                }
                context.IsSuccess = true;
            }

            OnUnitMagicProcessed?.Invoke(context);
        }
    }
}
