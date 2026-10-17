namespace NpcSystem
{
    /// <summary>
    /// 이벤트 SO가 id로 가리킬 수 있는 NPC의 최소 규약입니다. NPC 쪽 비주얼 컴포넌트(NpcCombatActorVisualizer)가 구현하고,
    /// 동작 핸들러는 이 인터페이스로만 NPC를 다룹니다(구체 컴포넌트 직접 참조 금지).
    /// </summary>
    public interface INpcIdentity
    {
        /// <summary>이벤트 SO가 이 NPC를 가리킬 때 쓰는 id(비어 있으면 가리킬 수 없는 NPC).</summary>
        string NpcId { get; }

        /// <summary>이 NPC의 이동/공격 수행 규약. 초기화 전에는 null.</summary>
        INpcCombatActor CombatActor { get; }
    }
}
