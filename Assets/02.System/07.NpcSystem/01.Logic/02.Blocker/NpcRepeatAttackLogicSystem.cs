using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

namespace NpcSystem
{
    /// <summary>
    /// 이벤트가 시킨 NPC 반복 공격을 실행하는 [L] 유니티 생명주기 연동 로직(ITickable)입니다. 매 프레임 RuntimeDataNpcRepeatAttack의 경과 시간을 진행시키고
    /// 발사 차례가 된 NPC에게 INpcCombatActor.Attack을 지시할 뿐, 투사체 생성/속도/데미지는 NPC 자신의 공격 전략(NpcAttackBehaviorSO)이 정합니다.
    /// 반복 간격 값은 이벤트 SO(NpcRepeatAttackAction.IntervalSeconds)가 Begin 인자로 넘겨주며 이 클래스에는 수치가 없습니다.
    /// 시간 진행은 Time.deltaTime(기존 돌문 사격 타이머와 동일 - 시간 정지 중에는 같이 느려짐)을 씁니다.
    /// </summary>
    public class NpcRepeatAttackLogicSystem : INpcRepeatAttackController, ITickable
    {
        private readonly RuntimeDataNpcRepeatAttack runtimeData;
        private readonly List<RuntimeDataNpcRepeatAttack.Session> dueSessions = new List<RuntimeDataNpcRepeatAttack.Session>();

        public NpcRepeatAttackLogicSystem(RuntimeDataNpcRepeatAttack runtimeData)
        {
            this.runtimeData = runtimeData ?? throw new System.ArgumentNullException(nameof(runtimeData));
        }

        public void Begin(INpcCombatActor actor, GameObject target, float intervalSeconds)
        {
            runtimeData.Begin(actor, target, intervalSeconds);
        }

        public void End(INpcCombatActor actor)
        {
            runtimeData.End(actor);
        }

        public void Tick()
        {
            if (runtimeData.ActiveCount == 0) return;

            runtimeData.Advance(Time.deltaTime, dueSessions);
            foreach (var session in dueSessions)
            {
                session.Actor.Attack(session.Target);
            }
        }
    }
}
