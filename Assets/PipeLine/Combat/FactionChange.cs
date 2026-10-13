using CharacterSystem;
using UnityEngine;

namespace PipeLine.Combat
{
    /// <summary>
    /// 마법으로 대상 투사체의 진영을 시전자 진영으로 바꾸는 정적 순수 C# 헬퍼입니다.
    /// 시전자의 IFactionProvider를 읽어 대상의 부모/자식에 있는 모든 IFactionSettable에 설정하며, 구체 클래스를 모릅니다.
    /// 발사자(attacker)는 바꾸지 않습니다. 시전이 성공한(단위 변경이 실제로 적용된) 경우에만 호출해야 합니다.
    /// </summary>
    public static class FactionChange
    {
        /// <summary>시전자 진영을 대상(부모/자식 포함)의 모든 IFactionSettable에 설정한다.</summary>
        public static void ApplyCasterFaction(GameObject target, GameObject caster)
        {
            if (target == null || caster == null) return;
            if (!caster.TryGetComponent<IFactionProvider>(out var casterFaction)) return;

            FactionType faction = casterFaction.Faction;

            // 부모 쪽과 자식 쪽 컴포넌트는 target 자신에서 겹칠 수 있으므로 한 번씩만 설정한다.
            var parents = target.GetComponentsInParent<IFactionSettable>(true);
            foreach (var settable in parents)
            {
                settable.SetFaction(faction);
                Debug.Log($"<color=cyan>[FactionChange]</color> {DescribeSettable(settable)} faction -> {faction} (caster: {caster.name})");
            }

            var children = target.GetComponentsInChildren<IFactionSettable>(true);
            foreach (var settable in children)
            {
                if (System.Array.IndexOf(parents, settable) >= 0) continue;
                settable.SetFaction(faction);
                Debug.Log($"<color=cyan>[FactionChange]</color> {DescribeSettable(settable)} faction -> {faction} (caster: {caster.name})");
            }
        }

        private static string DescribeSettable(IFactionSettable settable)
        {
            return settable is Component component ? component.name : settable.GetType().Name;
        }
    }
}
