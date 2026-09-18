using System;
using System.Collections.Generic;

namespace PipeLine.Combat
{
    /// <summary>
    /// [Flags] enum 기반 비트 플래그를 여러 버프/무기 등 서로 다른 소스가 동시에 요구하더라도
    /// 안전하게 Add/Remove 할 수 있도록 비트별 참조 카운트로 관리하는 제네릭 컨테이너입니다.
    ///
    /// - 추가 카운터(additive): 특정 비트를 켜고 싶어하는 소스 수를 셉니다. 카운트가 0보다 크면 그 비트가 켜집니다.
    /// - 강제 제외 카운터(suppress): 특정 비트를 강제로 끄고 싶어하는 소스 수를 셉니다.
    ///   카운트가 0보다 크면 그 비트는 추가 카운터보다 우선해서 항상 꺼집니다.
    ///
    /// 최종 플래그 = (기본 플래그 | 추가카운터&gt;0인 비트들) &amp; ~(제외카운터&gt;0인 비트들)
    ///
    /// CurrentFlags는 Add/Remove/Suppress/Unsuppress가 호출될 때만 재계산되고, 그 사이에는 캐시된 값을
    /// 즉시 반환합니다. 따라서 매 타격마다 조회해도 추가 GC 할당이나 반복 연산이 발생하지 않습니다.
    /// </summary>
    /// <typeparam name="TFlags">int 기반 [Flags] enum. (byte/long backing 등 다른 underlying type은 지원 대상이 아닙니다.)</typeparam>
    public class FlagRefCounter<TFlags> where TFlags : struct, Enum
    {
        private readonly Dictionary<int, int> additiveCounters = new Dictionary<int, int>();
        private readonly Dictionary<int, int> suppressCounters = new Dictionary<int, int>();

        private int baseFlagsRaw;
        private TFlags cachedCurrentFlags;
        private bool dirty = true;

        public TFlags BaseFlags
        {
            get => ToEnum(baseFlagsRaw);
            set
            {
                int raw = ToInt(value);
                if (raw == baseFlagsRaw) return;
                baseFlagsRaw = raw;
                dirty = true;
            }
        }

        public FlagRefCounter(TFlags baseFlags = default)
        {
            baseFlagsRaw = ToInt(baseFlags);
        }

        /// <summary>
        /// (기본 플래그 | 추가 카운터가 살아있는 비트들) &amp; ~(제외 카운터가 살아있는 비트들)로 계산된 최종 플래그입니다.
        /// </summary>
        public TFlags CurrentFlags
        {
            get
            {
                if (dirty) Recompute();
                return cachedCurrentFlags;
            }
        }

        public void Add(TFlags flags) => AdjustBits(additiveCounters, ToInt(flags), +1);
        public void Remove(TFlags flags) => AdjustBits(additiveCounters, ToInt(flags), -1);
        public void Suppress(TFlags flags) => AdjustBits(suppressCounters, ToInt(flags), +1);
        public void Unsuppress(TFlags flags) => AdjustBits(suppressCounters, ToInt(flags), -1);

        private void AdjustBits(Dictionary<int, int> counters, int mask, int delta)
        {
            if (mask == 0) return;

            int remaining = mask;
            while (remaining != 0)
            {
                int lowestBit = remaining & (-remaining);
                counters.TryGetValue(lowestBit, out int count);
                count += delta;

                if (count <= 0) counters.Remove(lowestBit);
                else counters[lowestBit] = count;

                remaining &= ~lowestBit;
            }

            dirty = true;
        }

        private void Recompute()
        {
            int result = baseFlagsRaw;

            foreach (var kvp in additiveCounters)
            {
                if (kvp.Value > 0) result |= kvp.Key;
            }

            foreach (var kvp in suppressCounters)
            {
                if (kvp.Value > 0) result &= ~kvp.Key;
            }

            cachedCurrentFlags = ToEnum(result);
            dirty = false;
        }

        private static int ToInt(TFlags flags) => Convert.ToInt32(flags);
        private static TFlags ToEnum(int value) => (TFlags)Enum.ToObject(typeof(TFlags), value);
    }

    /// <summary>
    /// 공격자/피격자 개별 엔티티가 들고 있는 AttackStepFlags/HitStepFlags를 하나로 묶어 관리하는 편의 홀더입니다.
    /// 내부적으로 FlagRefCounter&lt;AttackStepFlags&gt; / FlagRefCounter&lt;HitStepFlags&gt;를 사용합니다.
    /// 버프/무기 시스템이 아직 없는 현재는 CollisionDamageTrigger 등에서 기본 플래그만 사용하지만,
    /// 이후 버프/장비가 추가되면 이 홀더를 엔티티(RuntimeData 등)에 보관해 참조 카운트를 갱신하면 됩니다.
    /// </summary>
    public class CombatFlagState
    {
        private readonly FlagRefCounter<AttackStepFlags> attackFlags;
        private readonly FlagRefCounter<HitStepFlags> hitFlags;

        public CombatFlagState(AttackStepFlags baseAttackFlags = AttackStepFlags.None, HitStepFlags baseHitFlags = HitStepFlags.None)
        {
            attackFlags = new FlagRefCounter<AttackStepFlags>(baseAttackFlags);
            hitFlags = new FlagRefCounter<HitStepFlags>(baseHitFlags);
        }

        public AttackStepFlags CurrentAttackFlags => attackFlags.CurrentFlags;
        public HitStepFlags CurrentHitFlags => hitFlags.CurrentFlags;

        public void AddAttack(AttackStepFlags flags) => attackFlags.Add(flags);
        public void RemoveAttack(AttackStepFlags flags) => attackFlags.Remove(flags);
        public void SuppressAttack(AttackStepFlags flags) => attackFlags.Suppress(flags);
        public void UnsuppressAttack(AttackStepFlags flags) => attackFlags.Unsuppress(flags);

        public void AddHit(HitStepFlags flags) => hitFlags.Add(flags);
        public void RemoveHit(HitStepFlags flags) => hitFlags.Remove(flags);
        public void SuppressHit(HitStepFlags flags) => hitFlags.Suppress(flags);
        public void UnsuppressHit(HitStepFlags flags) => hitFlags.Unsuppress(flags);
    }
}
