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

            // 피격 스탯 차감 적용 및 디버그 출력
            if (!context.IsEvaded && context.Victim != null && context.Victim.TryGetComponent<CharacterStatComponent>(out var statComponent))
            {
                var statService = statComponent.StatService ?? (ICharacterStatService)statComponent.StatSystem;
                statService?.TakeDamage(context.FinalDamage);
                int currentHp = statService?.RuntimeData?.HP?.CurrentValue ?? 0;
                int maxHp = statService?.RuntimeData?.HP?.MaxValue ?? 0;
                Debug.Log($"<color=red>[InteractionSystem.Damage]</color> Attacker: {context.Attacker?.name} -> Victim: {context.Victim.name} | Raw: {context.RawDamage} | Final: {context.FinalDamage} | Crit: {context.IsCritical} | HP: {currentHp}/{maxHp}");
            }
            else if (context.IsEvaded)
            {
                Debug.Log($"<color=yellow>[InteractionSystem.Damage]</color> Victim: {context.Victim?.name} EVADED attack from {context.Attacker?.name}");
            }
            else if (context.Victim != null)
            {
                Debug.LogWarning($"<color=orange>[InteractionSystem.Damage]</color> Victim: {context.Victim.name} does NOT have CharacterStatComponent!");
            }

            OnDamageProcessed?.Invoke(context);
        }

        public async UniTask ProcessHealAsync(DamageContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            await UniTask.Yield();

            if (context.Victim != null && context.Victim.TryGetComponent<CharacterStatComponent>(out var statComponent))
            {
                var statService = statComponent.StatService ?? (ICharacterStatService)statComponent.StatSystem;
                statService?.Heal(context.RawDamage);
                int currentHp = statService?.RuntimeData?.HP?.CurrentValue ?? 0;
                int maxHp = statService?.RuntimeData?.HP?.MaxValue ?? 0;
                Debug.Log($"<color=green>[InteractionSystem.Heal]</color> Victim: {context.Victim.name} Healed +{context.RawDamage} | HP: {currentHp}/{maxHp}");
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
                    var statService = casterStat.StatService ?? (ICharacterStatService)casterStat.StatSystem;
                    statService?.UseMP(context.RequiredMana);
                }
                context.IsSuccess = true;
            }

            OnUnitMagicProcessed?.Invoke(context);
        }
    }
}
