using System.Collections.Generic;
using UnityEngine;

namespace NpcSystem
{
    /// <summary>
    /// NpcSystemLifetimeScope 인스펙터에 등록된 NPC 중 id가 있는 것(INpcIdentity)을 id로 조회해 주는 [L] 호출형 순수 로직입니다.
    /// id가 비어 있는 NPC는 이벤트가 가리킬 수 없으므로 건너뛰고, id가 중복이면 에러 로그 후 그 항목을 건너뜁니다.
    /// </summary>
    public class NpcActorRegistry : INpcActorRegistry
    {
        private readonly Dictionary<string, INpcIdentity> npcs = new Dictionary<string, INpcIdentity>();

        public NpcActorRegistry(INpcIdentity[] identities)
        {
            if (identities == null) return;

            foreach (var identity in identities)
            {
                if (identity == null || string.IsNullOrEmpty(identity.NpcId)) continue;

                if (!npcs.TryAdd(identity.NpcId, identity))
                {
                    Debug.LogError($"[NpcActorRegistry] NpcId '{identity.NpcId}'가 중복되어 등록하지 않았습니다.");
                }
            }
        }

        public bool TryGetActor(string npcId, out INpcCombatActor actor)
        {
            actor = null;
            return !string.IsNullOrEmpty(npcId) && npcs.TryGetValue(npcId, out var identity) && (actor = identity.CombatActor) != null;
        }
    }
}
