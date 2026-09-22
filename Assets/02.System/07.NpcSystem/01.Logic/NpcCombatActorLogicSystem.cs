using System;
using UnityEngine;
using UnitSystem;

namespace NpcSystem
{
    /// <summary>
    /// INpcCombatActor의 순수 C# 로직 구현체([L] 호출형 순수 로직)입니다. MonoBehaviour를 상속하지 않으며,
    /// Transform/Rigidbody 등 구체 컴포넌트를 직접 참조하지 않습니다 - 실제 이동/모션 재생/런타임 스폰은
    /// 전부 INpcCombatActorVisualizer 인터페이스 호출로 위임합니다("판단"만 이 클래스가 수행).
    /// 공격이 투사체/근접/즉발 중 무엇으로 일어나는지는 PureDataNpcActor.AttackBehavior(NpcAttackBehaviorSO)에
    /// 위임하며, 이 클래스 자신은 그 방식을 모릅니다. 언제/누구를 공격할지는 이 클래스를 참조(주입)하는
    /// 두뇌 역할 시스템(이벤트 시퀀서, 보스 FSM)이 결정합니다 - 이 클래스를 상속해서 두뇌 로직이나 공격 방식을
    /// 얹지 마세요(컴포지션 원칙 - 공격 방식은 AttackBehavior 교체로).
    /// </summary>
    public class NpcCombatActorLogicSystem : INpcCombatActor
    {
        private readonly PureDataNpcActor pureData;
        private readonly RuntimeDataNpcActor runtimeData;
        private readonly INpcCombatActorVisualizer visualizer;

        public event Action OnAttackExecuted;
        public event Action OnMoveCompleted;

        /// <summary>다른 시스템이 읽기 전용으로 현재 상태(IsMoving 등)를 조회할 수 있도록 공개합니다.</summary>
        public RuntimeDataNpcActor RuntimeData => runtimeData;

        public NpcCombatActorLogicSystem(PureDataNpcActor pureData, RuntimeDataNpcActor runtimeData, INpcCombatActorVisualizer visualizer)
        {
            this.pureData = pureData ?? throw new ArgumentNullException(nameof(pureData));
            this.runtimeData = runtimeData ?? throw new ArgumentNullException(nameof(runtimeData));
            this.visualizer = visualizer ?? throw new ArgumentNullException(nameof(visualizer));

            this.runtimeData.OnMoveCompleted += HandleMoveCompleted;
            this.runtimeData.OnAttackExecuted += HandleAttackExecuted;
        }

        public void MoveTo(Vector3 destination)
        {
            runtimeData.BeginMove(destination);
        }

        public void Attack(GameObject target, PureDataUnit magic = null)
        {
            // 타겟이 없거나 이미 파괴된 경우 방어적으로 무시 (예외/실패 케이스 - 01번 문서 1.5절)
            if (target == null) return;

            // 공격 쿨타임 체크
            if (Time.time - runtimeData.LastAttackTime < pureData.AttackCooldown) return;
            runtimeData.RecordAttackTime(Time.time);

            // 이동 중 공격 명령이 들어오면 공격이 이동보다 우선한다 (정책 - 01번 문서 1.5절 확인 필요 항목에 대한 채택안)
            runtimeData.InterruptForAttack();

            // 실제 공격이 "어떤 방식"으로 일어나는지는 이 로직이 모른다 - PureData가 지정한 전략(SO)에 위임한다.
            // 투사체/근접/즉발 등 공격 방식을 바꾸고 싶으면 코드를 고치지 말고 PureDataNpcActor.AttackBehavior 에셋 참조를 교체할 것.
            // AttackBehavior가 비어있으면 실제로는 아무 공격도 일어나지 않은 것이므로,
            // 모션 재생/OnAttackExecuted 발행도 같이 스킵한다 (겉보기 상태와 실제 상태가 어긋나지 않도록).
            if (pureData.AttackBehavior == null)
            {
                string ownerName = visualizer.Owner != null ? visualizer.Owner.name : "(unknown)";
                Debug.LogWarning($"[NpcCombatActorLogicSystem] {ownerName}: AttackBehavior가 설정되지 않아 공격을 실행하지 않았습니다.");
                return;
            }

            pureData.AttackBehavior.Execute(visualizer, pureData, target);
            PlayMotion("Attack");
            runtimeData.NotifyAttackExecuted();
        }

        public void PlayMotion(string motionKey)
        {
            visualizer.PlayMotion(motionKey);
        }

        /// <summary>
        /// 매 프레임 이동 판정(목적지 도달 체크 등)을 수행합니다. 위치 비교는 visualizer.CurrentPosition(값 읽기)만
        /// 사용하며 Transform/Rigidbody 컴포넌트 자체는 참조하지 않습니다. Visualizer(MonoBehaviour)의 Update()에서
        /// 매 프레임 호출되어야 합니다.
        /// </summary>
        public void Tick()
        {
            if (!runtimeData.IsMoving) return;

            Vector3 toDestination = runtimeData.MoveDestination - visualizer.CurrentPosition;
            if (toDestination.magnitude <= pureData.ArrivalThreshold)
            {
                runtimeData.CompleteMove();
                return;
            }

            // MoveTowards로 프레임당 이동 거리를 목적지까지 남은 거리 이내로 clamp하여 목적지 주변에서 진동/오버슈트를 방지한다.
            visualizer.MoveTowards(runtimeData.MoveDestination, pureData.MoveSpeed);
        }

        private void HandleMoveCompleted()
        {
            OnMoveCompleted?.Invoke();
        }

        private void HandleAttackExecuted()
        {
            OnAttackExecuted?.Invoke();
        }
    }
}
