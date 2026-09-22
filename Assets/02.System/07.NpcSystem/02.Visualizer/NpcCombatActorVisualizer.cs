using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace NpcSystem
{
    /// <summary>
    /// INpcCombatActorVisualizer의 기본 구현체([V] World Visualizer)입니다. 아군 NPC/보스 GameObject에 부착되어
    /// 실제 transform 이동, Animator.SetTrigger, Instantiate+IObjectResolver.InjectGameObject를 전부 수행하는
    /// "수동적 수행자"입니다. 언제/무엇을 할지는 스스로 판단하지 않고 NpcCombatActorLogicSystem(순수 C# 로직)이 결정합니다.
    /// NPC는 여러 마리 존재하므로 LogicSystem을 VContainer 싱글턴으로 등록하지 않고,
    /// RuntimeDataUnitGroup.Awake()와 동일하게 자기 자신의 PureData/RuntimeData로 인스턴스마다 직접 new로 생성해 보유합니다.
    /// CombatActor(INpcCombatActor)를 외부(두뇌 역할의 이벤트 시퀀서/보스 FSM)가 참조·호출할 수 있도록 공개 프로퍼티로 노출합니다
    /// (CharacterStatComponent.StatService => StatSystem 패턴 참고). 이 클래스를 상속해서 두뇌 로직이나 공격 방식을
    /// 얹지 마세요(컴포지션 원칙 - 공격 방식은 PureDataNpcActor.AttackBehavior 교체로).
    /// </summary>
    public class NpcCombatActorVisualizer : MonoBehaviour, INpcCombatActorVisualizer
    {
        [SerializeField] private PureDataNpcActor pureData;

        public NpcCombatActorLogicSystem LogicSystem { get; private set; }
        public INpcCombatActor CombatActor => LogicSystem;

        public Vector3 CurrentPosition => transform.position;
        public GameObject Owner => gameObject;

        private IObjectResolver resolver;

        private void Awake()
        {
            EnsureInitialized();
        }

        [Inject]
        public void Construct(IObjectResolver resolver)
        {
            this.resolver = resolver;
        }

        private void EnsureInitialized()
        {
            if (LogicSystem == null && pureData != null)
            {
                LogicSystem = new NpcCombatActorLogicSystem(pureData, new RuntimeDataNpcActor(), this);
            }
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

        public void MoveTowards(Vector3 destination, float speed)
        {
            // 프레임당 이동 거리를 목적지까지 남은 거리 이내로 clamp하여 목적지 주변에서 진동/오버슈트를 방지한다.
            transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
        }

        private void Update()
        {
            EnsureInitialized();
            LogicSystem?.Tick();
        }
    }
}
