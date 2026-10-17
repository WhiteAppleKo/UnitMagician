using System.Collections.Generic;
using UnityEngine;

namespace NpcSystem
{
    /// <summary>
    /// 이벤트가 시킨 "반복 공격" 진행 상태([D] Runtime Data)입니다. 반복 공격 중인 NPC별로 대상/발사 간격/경과 시간을 보관하고,
    /// private set + 전용 메서드(Begin/End/Advance)로만 바꿉니다. 스스로 프레임을 갱신하지 않으며, 시간 경과는 로직이 Advance로 넘겨줍니다.
    /// </summary>
    public class RuntimeDataNpcRepeatAttack
    {
        /// <summary>반복 공격 중인 NPC 한 마리의 진행 상태.</summary>
        public sealed class Session
        {
            public INpcCombatActor Actor { get; }
            public GameObject Target { get; }
            public float IntervalSeconds { get; }
            public float ElapsedSeconds { get; private set; }

            public Session(INpcCombatActor actor, GameObject target, float intervalSeconds)
            {
                Actor = actor;
                Target = target;
                IntervalSeconds = intervalSeconds;
            }

            /// <summary>경과 시간을 더하고, 발사 간격에 도달했으면 경과 시간을 0으로 되돌리고 true를 돌려줍니다.</summary>
            public bool AdvanceAndCheckDue(float deltaTime)
            {
                ElapsedSeconds += deltaTime;
                if (ElapsedSeconds < IntervalSeconds) return false;

                ElapsedSeconds = 0f;
                return true;
            }
        }

        private readonly List<Session> sessions = new List<Session>();

        public int ActiveCount => sessions.Count;

        /// <summary>NPC의 반복 공격을 시작합니다. 이미 반복 중이면 새 설정으로 교체합니다(경과 시간 0부터).</summary>
        public void Begin(INpcCombatActor actor, GameObject target, float intervalSeconds)
        {
            End(actor);
            sessions.Add(new Session(actor, target, intervalSeconds));
        }

        /// <summary>NPC의 반복 공격을 멈춥니다. 반복 중이 아니면 아무 일도 하지 않습니다.</summary>
        public void End(INpcCombatActor actor)
        {
            sessions.RemoveAll(session => session.Actor == actor);
        }

        /// <summary>모든 반복 공격의 시간을 진행시키고, 이번에 발사할 차례가 된 세션을 due에 담습니다(due는 먼저 비워집니다).</summary>
        public void Advance(float deltaTime, List<Session> due)
        {
            due.Clear();
            foreach (var session in sessions)
            {
                if (session.AdvanceAndCheckDue(deltaTime)) due.Add(session);
            }
        }
    }
}
