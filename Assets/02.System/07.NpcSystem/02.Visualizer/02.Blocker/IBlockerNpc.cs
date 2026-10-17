using UnityEngine;

namespace NpcSystem
{
    /// <summary>
    /// 이벤트가 길을 막는 일반 NPC(BlockerNPC)를 다루는 접점 규약입니다. 이벤트 동작 핸들러는 이 인터페이스로 NPC의 그룹/위치 값을 읽고
    /// CombatActor를 통해 명령(MoveTo/Attack)만 내립니다(구체 컴포넌트 직접 참조 금지).
    /// </summary>
    public interface IBlockerNpc
    {
        /// <summary>이벤트 SO(동작)가 NPC들을 묶어 가리키는 그룹 id.</summary>
        string GroupId { get; }

        /// <summary>길을 막고 있는 위치(씬 고정 월드 좌표).</summary>
        Vector3 BlockPosition { get; }

        /// <summary>비켜설 위치(씬 고정 월드 좌표). 비켜서기 동작이 이 위치로 MoveTo 합니다.</summary>
        Vector3 StepAsidePosition { get; }

        /// <summary>이 NPC의 공통 전투 액터(MoveTo/Attack 명령 창구).</summary>
        INpcCombatActor CombatActor { get; }
    }
}
