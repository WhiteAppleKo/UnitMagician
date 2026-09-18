using UnityEngine;

namespace NpcSystem
{
    /// <summary>
    /// NPC의 "공격이 실제로 어떤 방식으로 일어나는지"를 결정하는 교체 가능한 전략입니다.
    /// (투사체/근접/즉발 등) PureDataNpcActor.AttackBehavior에 원하는 구현체 에셋을 물려서 선택합니다.
    /// NpcCombatActorComponent는 이 전략을 상속받아 확장하지 않고 참조·호출만 합니다(컴포지션 원칙).
    /// </summary>
    public abstract class NpcAttackBehaviorSO : ScriptableObject
    {
        public abstract void Execute(NpcCombatActorComponent actor, GameObject target);
    }
}
