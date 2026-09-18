using System;
using UnityEngine;
using UnitSystem;

namespace NpcSystem
{
    /// <summary>
    /// 아군 NPC/보스 등 "이동하고 공격을 실행하는" 모든 NPC가 공유하는 공통 실행 규약(몸통/액추에이터)입니다.
    /// 두뇌 역할의 시스템(이벤트 시퀀서, 보스 FSM)은 이 인터페이스만 참조·호출하며,
    /// 구현체를 상속해서 확장하지 않습니다(컴포지션 원칙 — 01.NPC전투액터공통프레임워크.md 참고).
    /// </summary>
    public interface INpcCombatActor
    {
        /// <summary>목적지까지 이동을 시작합니다. 이동 중 Attack이 들어오면 이동은 즉시 중단됩니다.</summary>
        void MoveTo(Vector3 destination);

        /// <summary>대상을 공격합니다. target이 null이거나 이미 파괴된 경우 아무 동작도 하지 않습니다.</summary>
        void Attack(GameObject target, PureDataUnit magic = null);

        /// <summary>Animator가 있으면 해당 트리거를 재생합니다. 없으면 안전하게 무시됩니다.</summary>
        void PlayMotion(string motionKey);

        /// <summary>공격 실행(투사체 발사 등)이 실제로 일어난 시점에 발행됩니다.</summary>
        event Action OnAttackExecuted;

        /// <summary>MoveTo로 지정된 목적지에 도착한 시점에 발행됩니다.</summary>
        event Action OnMoveCompleted;
    }
}
