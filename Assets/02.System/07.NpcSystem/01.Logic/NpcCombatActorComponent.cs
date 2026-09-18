using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using UnitSystem;

namespace NpcSystem
{
    /// <summary>
    /// INpcCombatActor의 기본 구현체입니다. 아군 NPC/보스 GameObject에 부착되어
    /// "이동"과 "공격 실행"만 담당하는 몸통 역할을 합니다. 공격이 투사체/근접/즉발 중 무엇으로 일어나는지는
    /// PureDataNpcActor.AttackBehavior(NpcAttackBehaviorSO)에 위임하며, 이 컴포넌트 자신은 그 방식을 모릅니다.
    /// 언제/누구를 공격할지는 이 컴포넌트를 참조(주입)하는 두뇌 역할 시스템(이벤트 시퀀서, 보스 FSM)이 결정합니다 —
    /// 이 클래스를 상속해서 두뇌 로직이나 공격 방식을 얹지 마세요(컴포지션 원칙 — 공격 방식은 AttackBehavior 교체로).
    /// </summary>
    public class NpcCombatActorComponent : MonoBehaviour, INpcCombatActor
    {
        [SerializeField] private PureDataNpcActor pureData;

        public event Action OnAttackExecuted;
        public event Action OnMoveCompleted;

        /// <summary>NpcAttackBehaviorSO 등 공격 전략이 기본 데미지 등 설정값을 읽을 수 있도록 공개합니다.</summary>
        public PureDataNpcActor PureData => pureData;

        private IObjectResolver resolver;
        private bool isMoving;
        private Vector3 moveDestination;
        private float lastAttackTime = float.NegativeInfinity;

        [Inject]
        public void Construct(IObjectResolver resolver)
        {
            this.resolver = resolver;
        }

        public void MoveTo(Vector3 destination)
        {
            moveDestination = destination;
            isMoving = true;
        }

        public void Attack(GameObject target, PureDataUnit magic = null)
        {
            // 타겟이 없거나 이미 파괴된 경우 방어적으로 무시 (예외/실패 케이스 - 01번 문서 1.5절)
            if (target == null) return;

            // 공격 쿨타임 체크
            if (Time.time - lastAttackTime < pureData.AttackCooldown) return;
            lastAttackTime = Time.time;

            // 이동 중 공격 명령이 들어오면 공격이 이동보다 우선한다 (정책 - 01번 문서 1.5절 확인 필요 항목에 대한 채택안)
            InterruptMovementForAttack();

            // 실제 공격이 "어떤 방식"으로 일어나는지는 이 컴포넌트가 모른다 - PureData가 지정한 전략(SO)에 위임한다.
            // 투사체/근접/즉발 등 공격 방식을 바꾸고 싶으면 코드를 고치지 말고 PureDataNpcActor.AttackBehavior 에셋 참조를 교체할 것.
            pureData.AttackBehavior?.Execute(this, target);
            PlayMotion("Attack");
            OnAttackExecuted?.Invoke();
        }

        /// <summary>
        /// "이동 중 공격이 들어오면 이동을 중단하고 공격을 우선 실행한다"는 정책을 이 한 메서드에만 담아둔다.
        /// 나중에 정책이 바뀌면(예: 이동 완료 후 공격 큐잉) 이 메서드만 교체하면 된다.
        /// </summary>
        private void InterruptMovementForAttack()
        {
            isMoving = false;
        }

        /// <summary>
        /// 런타임에 Instantiate한 오브젝트는 씬의 autoInjectGameObjects 대상이 아니므로
        /// NpcAttackBehaviorSO 구현체들은 직접 Instantiate하지 말고 이 헬퍼를 통해 스폰해야 한다.
        /// DI 주입에 대한 지식을 이 한 곳에만 두기 위한 헬퍼다.
        /// </summary>
        public GameObject SpawnAndInject(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;

            var instance = Instantiate(prefab, position, rotation);
            resolver?.InjectGameObject(instance);
            return instance;
        }

        public void PlayMotion(string motionKey)
        {
            // 아직 실제 애니메이션 리소스가 없을 수 있으므로 Animator 부재 시 조용히 무시 (방어적)
            if (TryGetComponent<Animator>(out var animator))
            {
                animator.SetTrigger(motionKey);
            }
        }

        private void Update()
        {
            if (!isMoving) return;

            Vector3 toDestination = moveDestination - transform.position;
            if (toDestination.magnitude <= pureData.ArrivalThreshold)
            {
                isMoving = false;
                OnMoveCompleted?.Invoke();
                return;
            }

            // MoveTowards로 프레임당 이동 거리를 목적지까지 남은 거리 이내로 clamp하여 목적지 주변에서 진동/오버슈트를 방지한다.
            transform.position = Vector3.MoveTowards(transform.position, moveDestination, pureData.MoveSpeed * Time.deltaTime);
        }
    }
}
