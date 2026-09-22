using System;
using UnityEngine;

namespace NpcSystem
{
    /// <summary>
    /// NpcCombatActorLogicSystem이 보유하는 NPC 전투 액터의 런타임 상태([RuntimeData])입니다.
    /// 모든 속성은 private set이며, 전용 메서드(BeginMove/CompleteMove/InterruptForAttack/RecordAttackTime)를
    /// 통해서만 상태가 바뀝니다. 스스로 프레임을 갱신(Tick)하지 않으며, 상태가 바뀌는 시점에 이벤트만 발행합니다.
    /// </summary>
    public class RuntimeDataNpcActor
    {
        public bool IsMoving { get; private set; }
        public Vector3 MoveDestination { get; private set; }
        public float LastAttackTime { get; private set; } = float.NegativeInfinity;

        /// <summary>공격 실행(투사체 발사 등)이 실제로 일어난 시점에 발행됩니다.</summary>
        public event Action OnAttackExecuted;

        /// <summary>MoveTo로 지정된 목적지에 도착한 시점에 발행됩니다.</summary>
        public event Action OnMoveCompleted;

        public void BeginMove(Vector3 destination)
        {
            MoveDestination = destination;
            IsMoving = true;
        }

        /// <summary>목적지 도달 판정 시 호출합니다. 이동 중이었을 때만 OnMoveCompleted를 발행합니다.</summary>
        public void CompleteMove()
        {
            if (!IsMoving) return;

            IsMoving = false;
            OnMoveCompleted?.Invoke();
        }

        /// <summary>
        /// "이동 중 공격이 들어오면 이동을 중단하고 공격을 우선 실행한다"는 정책을 이 한 메서드에만 담아둔다.
        /// 나중에 정책이 바뀌면(예: 이동 완료 후 공격 큐잉) 이 메서드만 교체하면 된다.
        /// </summary>
        public void InterruptForAttack()
        {
            IsMoving = false;
        }

        public void RecordAttackTime(float time)
        {
            LastAttackTime = time;
        }

        /// <summary>공격이 실제로 실행된 시점에 로직이 호출하여 OnAttackExecuted를 발행시킵니다.</summary>
        public void NotifyAttackExecuted()
        {
            OnAttackExecuted?.Invoke();
        }
    }
}
