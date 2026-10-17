namespace NpcSystem
{
    /// <summary>씬에 등록된 NPC(INpcIdentity)를 id로 조회하는 호출형 규약입니다.</summary>
    public interface INpcActorRegistry
    {
        /// <summary>id에 해당하는 NPC의 전투 액터를 돌려줍니다. 없으면 false.</summary>
        bool TryGetActor(string npcId, out INpcCombatActor actor);
    }
}
