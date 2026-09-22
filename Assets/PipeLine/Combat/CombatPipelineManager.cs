using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PipeLine.CharacterDamage.Steps;
using PipeLine.Contexts;
using PipeLine.PipeLineBase;
using UnityEngine;

namespace PipeLine.Combat
{
    /// <summary>
    /// CombatPipelineManager의 DIP 인터페이스입니다. VContainer 등록/모킹 편의를 위해 분리되어 있습니다.
    /// </summary>
    public interface ICombatPipelineManager
    {
        /// <summary>
        /// 공격 파이프라인(attackFlags) → HitRegistered 설정 → 피격 파이프라인(hitFlags) → ApplyDamageStep(고정)
        /// 순으로 전체 데미지 파이프라인을 실행합니다.
        /// </summary>
        UniTask<DamageContext> RunFullPipeline(DamageContext context, AttackStepFlags attackFlags, HitStepFlags hitFlags);

        IPipeLineStep<DamageContext>[] ResolveAttackSteps(AttackStepFlags attackFlags);
        IPipeLineStep<DamageContext>[] ResolveHitSteps(HitStepFlags hitFlags);
    }

    /// <summary>
    /// 공격 파이프라인(공격자 스탯 기반)과 피격 파이프라인(피격자 스탯 기반)을 분리해서 실행하는 관리자입니다.
    ///
    /// AttackStepFlags/HitStepFlags 조합 → 조립된 IPipeLineStep&lt;DamageContext&gt;[] 변환 결과를 세션(앱 라이프사이클)
    /// 내내 두 개의 Dictionary에 캐싱(메모이제이션)해서, 동일 플래그 조합에 대해서는 최초 1회만 조립하고
    /// 이후에는 캐시 히트로 즉시 배열을 재사용합니다. 매 타격마다 반복되는 조립 연산과 그로 인한 GC 할당을 없애는 것이 목적입니다.
    ///
    /// ApplyDamageStep은 플래그 게이트 대상이 아니라 파이프라인 종료 시 항상 실행되는 고정 마지막 스텝으로 별도 처리됩니다.
    /// VContainer에는 Singleton으로 등록해야 캐시가 세션 내내 유지됩니다. (InteractionSystemLifetimeScope 참고)
    /// </summary>
    public class CombatPipelineManager : ICombatPipelineManager
    {
        // 공격 파이프라인 정준(canonical) 순서. 새 AttackStepFlags 비트가 생기면 여기에도 추가해야 실제로 반영됩니다.
        private static readonly (AttackStepFlags flag, IPipeLineStep<DamageContext> step)[] AttackStepOrder =
        {
            (AttackStepFlags.Critical, CriticalStep.Instance),
        };

        // 피격 파이프라인 정준 순서: AttributeGate(비-캐릭터 오브젝트 속성 게이트) → Evasion → Defense.
        // 새 HitStepFlags 비트가 생기면 여기에도 추가해야 합니다.
        private static readonly (HitStepFlags flag, IPipeLineStep<DamageContext> step)[] HitStepOrder =
        {
            (HitStepFlags.AttributeGate, AttributeGateStep.Instance),
            (HitStepFlags.Evasion, EvasionStep.Instance),
            (HitStepFlags.Defense, DefenseStep.Instance),
        };

        private static readonly IPipeLineStep<DamageContext>[] EmptySteps = Array.Empty<IPipeLineStep<DamageContext>>();

        private readonly Dictionary<AttackStepFlags, IPipeLineStep<DamageContext>[]> attackStepCache =
            new Dictionary<AttackStepFlags, IPipeLineStep<DamageContext>[]>();

        private readonly Dictionary<HitStepFlags, IPipeLineStep<DamageContext>[]> hitStepCache =
            new Dictionary<HitStepFlags, IPipeLineStep<DamageContext>[]>();

        public IPipeLineStep<DamageContext>[] ResolveAttackSteps(AttackStepFlags attackFlags)
        {
            if (attackStepCache.TryGetValue(attackFlags, out var cached))
            {
                return cached;
            }

            var resolved = BuildSteps(AttackStepOrder, attackFlags);
            attackStepCache[attackFlags] = resolved;
            Debug.Log($"[CombatPipelineManager] Attack step cache MISS -> assembled {resolved.Length} step(s) for flags: {attackFlags} (cache size: {attackStepCache.Count})");
            return resolved;
        }

        public IPipeLineStep<DamageContext>[] ResolveHitSteps(HitStepFlags hitFlags)
        {
            if (hitStepCache.TryGetValue(hitFlags, out var cached))
            {
                return cached;
            }

            var resolved = BuildSteps(HitStepOrder, hitFlags);
            hitStepCache[hitFlags] = resolved;
            Debug.Log($"[CombatPipelineManager] Hit step cache MISS -> assembled {resolved.Length} step(s) for flags: {hitFlags} (cache size: {hitStepCache.Count})");
            return resolved;
        }

        public async UniTask<DamageContext> RunFullPipeline(DamageContext context, AttackStepFlags attackFlags, HitStepFlags hitFlags)
        {
            var attackSteps = ResolveAttackSteps(attackFlags);
            for (int i = 0; i < attackSteps.Length; i++)
            {
                context = await attackSteps[i].Execute(context);
                if (context.Aborted) break; // 스텝이 중단을 요청하면 이후 공격 스텝은 실행하지 않음
            }

            // "피격 판정"과 "데미지 판정"을 분리해서 기록 (공격 파이프라인 종료 시점)
            context.HitRegistered = true;

            if (!context.Aborted)
            {
                var hitSteps = ResolveHitSteps(hitFlags);
                for (int i = 0; i < hitSteps.Length; i++)
                {
                    context = await hitSteps[i].Execute(context);
                    if (context.Aborted) break; // 예: EvasionStep이 회피 판정 시 이후 DefenseStep 등은 스킵
                }
            }

            // ApplyDamageStep은 플래그 게이트 대상이 아닌 고정 마지막 스텝
            context = await ApplyDamageStep.Instance.Execute(context);

            return context;
        }

        private static IPipeLineStep<DamageContext>[] BuildSteps<TFlags>(
            (TFlags flag, IPipeLineStep<DamageContext> step)[] canonicalOrder,
            TFlags flags) where TFlags : struct, Enum
        {
            List<IPipeLineStep<DamageContext>> buffer = null;

            foreach (var entry in canonicalOrder)
            {
                if (HasFlag(flags, entry.flag))
                {
                    (buffer ??= new List<IPipeLineStep<DamageContext>>(canonicalOrder.Length)).Add(entry.step);
                }
            }

            return buffer?.ToArray() ?? EmptySteps;
        }

        private static bool HasFlag<TFlags>(TFlags value, TFlags flag) where TFlags : struct, Enum
        {
            int v = Convert.ToInt32(value);
            int f = Convert.ToInt32(flag);
            return (v & f) == f;
        }
    }
}
