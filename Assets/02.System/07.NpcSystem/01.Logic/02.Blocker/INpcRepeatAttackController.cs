using UnityEngine;

namespace NpcSystem
{
    /// <summary>NPC의 반복 공격을 시작/중단시키는 호출형 규약입니다. 이벤트 동작 핸들러(시작/중단)가 이 인터페이스만 보고 호출합니다.</summary>
    public interface INpcRepeatAttackController
    {
        /// <summary>NPC가 target을 intervalSeconds마다 반복 공격하게 합니다. 이미 반복 중이면 새 설정으로 교체합니다.</summary>
        void Begin(INpcCombatActor actor, GameObject target, float intervalSeconds);

        /// <summary>NPC의 반복 공격을 멈춥니다. 반복 중이 아니면 아무 일도 하지 않습니다.</summary>
        void End(INpcCombatActor actor);
    }
}
