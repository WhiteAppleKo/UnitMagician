using System.Collections.Generic;

namespace NpcSystem
{
    /// <summary>NpcSystemLifetimeScope 인스펙터에 등록된 길막이 NPC 목록을 그룹 id별로 묶어 조회해 주는 [L] 호출형 순수 로직입니다.</summary>
    public class BlockerNpcRegistry : IBlockerNpcRegistry
    {
        private static readonly IReadOnlyList<IBlockerNpc> Empty = new List<IBlockerNpc>();

        private readonly Dictionary<string, List<IBlockerNpc>> groups = new Dictionary<string, List<IBlockerNpc>>();

        public BlockerNpcRegistry(IBlockerNpc[] blockerNpcs)
        {
            if (blockerNpcs == null) return;

            foreach (var npc in blockerNpcs)
            {
                if (npc == null || string.IsNullOrEmpty(npc.GroupId)) continue;

                if (!groups.TryGetValue(npc.GroupId, out var group))
                {
                    group = new List<IBlockerNpc>();
                    groups.Add(npc.GroupId, group);
                }

                group.Add(npc);
            }
        }

        public IReadOnlyList<IBlockerNpc> GetGroup(string groupId)
        {
            return !string.IsNullOrEmpty(groupId) && groups.TryGetValue(groupId, out var group) ? group : Empty;
        }
    }
}
