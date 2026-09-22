using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using PipeLine.Combat;
using PipeLine.Contexts;
using PipeLine.UnitMagic;
using CharacterSystem;

namespace InteractionSystem.Logic
{
    /// <summary>
    /// 상호작용 신호 수신 후 CombatPipelineManager(공격/피격 파이프라인 분리 + 캐싱)와 UnitMagicPipeLine 파이프라인
    /// 연산을 실행하고 스탯을 최종 적용하는 로직 시스템입니다.
    /// </summary>
    public class InteractionSystem : IInteractionService
    {
        // 버프/무기 시스템이 아직 없어서 공격자/피격자 둘 다 우선 기본 플래그를 사용합니다.
        // 실제 버프/장비에 따른 플래그 배선(FlagRefCounter/CombatFlagState 연동)은 이번 작업 범위 밖의 후속 작업입니다.
        private const AttackStepFlags DefaultAttackFlags = AttackStepFlags.Critical;
        private const HitStepFlags DefaultHitFlags = HitStepFlags.Evasion | HitStepFlags.Defense;

        private readonly ICombatPipelineManager combatPipelineManager;
        private readonly UnitMagicPipeLine unitMagicPipeLine;

        public event Action<DamageContext> OnDamageProcessed;
        public event Action<DamageContext> OnHealProcessed;
        public event Action<UnitMagicContext> OnUnitMagicProcessed;

        public InteractionSystem(ICombatPipelineManager combatPipelineManager = null, UnitMagicPipeLine unitMagicPipeLine = null)
        {
            this.combatPipelineManager = combatPipelineManager;
            this.unitMagicPipeLine = unitMagicPipeLine;
        }

        public async UniTask ProcessDamageAsync(DamageContext context)
        {
            DamageContext result = context;

            if (combatPipelineManager != null)
            {
                // victim이 IHitStepFlagsProvider를 구현하면(예: 돌문 - AttributeGate만 필요) 그 값을 사용하고,
                // 구현하지 않으면(기존 캐릭터) 회귀 없이 기본값(Evasion|Defense)을 그대로 사용합니다.
                HitStepFlags hitFlags = DefaultHitFlags;
                if (result.Victim != null && result.Victim.TryGetComponent<IHitStepFlagsProvider>(out var hitFlagsProvider))
                {
                    hitFlags = hitFlagsProvider.CurrentHitFlags;
                }

                // 공격 파이프라인 -> HitRegistered 설정 -> 피격 파이프라인 -> ApplyDamageStep(고정, 항상 실행) 순으로 실행.
                // HP 차감은 ApplyDamageStep이 victim의 IDamageable을 통해 이미 직접 처리하므로 여기서 중복 적용하지 않습니다.
                result = await combatPipelineManager.RunFullPipeline(result, DefaultAttackFlags, hitFlags);
            }
            else
            {
                // 파이프라인 미지정 시 기본 연산 + 직접 데미지 적용 (안전망)
                if (!result.IsEvaded)
                {
                    result.FinalDamage = Mathf.Max(1, result.RawDamage - result.Defense);
                }

                if (!result.IsEvaded && result.Victim != null && result.Victim.TryGetComponent<IDamageable>(out var fallbackDamageable))
                {
                    fallbackDamageable.ApplyDamage(result.FinalDamage);
                }
            }

            // 피격 스탯 조회 및 디버그 출력 (HP 차감 자체는 위에서 이미 완료됨)
            if (result.IsEvaded)
            {
                Debug.Log($"<color=yellow>[InteractionSystem.Damage]</color> Victim: {result.Victim?.name} EVADED attack from {result.Attacker?.name}");
            }
            else if (result.Aborted)
            {
                // 회피가 아닌 게이트 실패(예: 돌문의 AttributeGate) - 대상이 CharacterStatComponent가 없는 것은
                // 버그가 아니라 의도된 설계(비-캐릭터 오브젝트)이므로 경고 대신 정보 로그만 남깁니다.
                Debug.Log($"<color=cyan>[InteractionSystem.Damage]</color> Victim: {result.Victim?.name} gate BLOCKED attack from {result.Attacker?.name} (no damage applied)");
            }
            else if (result.Victim != null && result.Victim.TryGetComponent<CharacterStatComponent>(out var statComponent))
            {
                var statService = statComponent.StatService ?? (ICharacterStatService)statComponent.StatSystem;
                int currentHp = statService?.RuntimeData?.HP?.CurrentValue ?? 0;
                int maxHp = statService?.RuntimeData?.HP?.MaxValue ?? 0;
                Debug.Log($"<color=red>[InteractionSystem.Damage]</color> Attacker: {result.Attacker?.name} -> Victim: {result.Victim.name} | Raw: {result.RawDamage} | Final: {result.FinalDamage} | Crit: {result.IsCritical} | HP: {currentHp}/{maxHp}");
            }
            else if (result.Victim != null)
            {
                Debug.LogWarning($"<color=orange>[InteractionSystem.Damage]</color> Victim: {result.Victim.name} does NOT have CharacterStatComponent!");
            }

            OnDamageProcessed?.Invoke(result);
        }

        public UniTask ProcessHealAsync(DamageContext context)
        {
            if (context.Victim != null && context.Victim.TryGetComponent<CharacterStatComponent>(out var statComponent))
            {
                var statService = statComponent.StatService ?? (ICharacterStatService)statComponent.StatSystem;
                statService?.Heal(context.RawDamage);
                int currentHp = statService?.RuntimeData?.HP?.CurrentValue ?? 0;
                int maxHp = statService?.RuntimeData?.HP?.MaxValue ?? 0;
                Debug.Log($"<color=green>[InteractionSystem.Heal]</color> Victim: {context.Victim.name} Healed +{context.RawDamage} | HP: {currentHp}/{maxHp}");
            }

            OnHealProcessed?.Invoke(context);
            return UniTask.CompletedTask;
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
