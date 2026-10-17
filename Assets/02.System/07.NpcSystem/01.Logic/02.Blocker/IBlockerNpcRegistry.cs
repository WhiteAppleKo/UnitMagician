using System.Collections.Generic;

namespace NpcSystem
{
    /// <summary>씬에 등록된 길막이 NPC(IBlockerNpc)를 그룹 id로 조회하는 호출형 규약입니다.</summary>
    public interface IBlockerNpcRegistry
    {
        /// <summary>그룹 id에 속한 NPC들을 돌려줍니다. 없으면 빈 목록.</summary>
        IReadOnlyList<IBlockerNpc> GetGroup(string groupId);
    }
}
