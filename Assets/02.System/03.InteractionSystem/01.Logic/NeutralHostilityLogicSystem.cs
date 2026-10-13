using System;
using UnityEngine;
using VContainer.Unity;
using CharacterSystem;
using InteractionSystem.Data;
using PipeLine.Combat;
using PipeLine.Contexts;

namespace InteractionSystem.Logic
{
    /// <summary>
    /// 중립(Neutral) NPC 적대화 규칙(21번): 중립 피격자가 유효 시간 이내에 임계 횟수만큼 연속으로 피격되면 진영을 Enemy로 바꾼다.
    /// IInteractionService.OnDamageProcessed를 구독하며, 임계 횟수/유효 시간은 피격자의 IHostilityRule(PureStatData)이 제공한다.
    /// 진영 변경 후의 몸체 레이어 이동/충돌 규칙은 16번 경로(OnFactionChanged)가 처리하므로 여기서는 진영 설정만 지시한다.
    /// </summary>
    public class NeutralHostilityLogicSystem : IInitializable, IDisposable
    {
        private readonly IInteractionService interactionService;
        private readonly RuntimeDataHostility runtimeData;

        public NeutralHostilityLogicSystem(IInteractionService interactionService)
        {
            this.interactionService = interactionService ?? throw new ArgumentNullException(nameof(interactionService));
            runtimeData = new RuntimeDataHostility();
        }

        public void Initialize()
        {
            interactionService.OnDamageProcessed += HandleDamageProcessed;
        }

        public void Dispose()
        {
            interactionService.OnDamageProcessed -= HandleDamageProcessed;
            runtimeData.Clear();
        }

        private void HandleDamageProcessed(DamageContext context)
        {
            // 파괴/비활성화된 피격자의 기록을 정리한다(누수 방지).
            runtimeData.PruneInvalid();

            if (!context.HitRegistered) return;

            GameObject victim = context.Victim;
            GameObject attacker = context.Attacker;
            if (victim == null || attacker == null) return;

            // 피격자는 Neutral, 발사자는 Neutral이 아니어야 센다. 진영을 노출하지 않는 대상은 Neutral로 취급한다.
            if (!victim.TryGetComponent<IFactionProvider>(out var victimFaction) || victimFaction.Faction != FactionType.Neutral) return;
            if (!attacker.TryGetComponent<IFactionProvider>(out var attackerFaction) || attackerFaction.Faction == FactionType.Neutral) return;

            if (!victim.TryGetComponent<IHostilityRule>(out var rule) || rule.HostileHitThreshold <= 0) return;
            if (!victim.TryGetComponent<IFactionSettable>(out var settable)) return;

            float now = Time.time;
            int hitCount = 1;
            if (runtimeData.TryGetState(victim, out var state) && now - state.LastHitTime <= rule.HostileHitWindowSeconds)
            {
                hitCount = state.HitCount + 1;
            }

            if (hitCount >= rule.HostileHitThreshold)
            {
                runtimeData.Remove(victim);
                settable.SetFaction(FactionType.Enemy);
                Debug.Log($"<color=orange>[NeutralHostility]</color> {victim.name} became hostile after {hitCount} hits (attacker: {attacker.name})");
                return;
            }

            runtimeData.SetState(victim, hitCount, now);
        }
    }
}
