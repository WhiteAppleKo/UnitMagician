using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Pooling;
using GameEvent;

namespace NpcSystem
{
    /// <summary>
    /// NpcSystem(07.NpcSystem)의 VContainer LifetimeScope입니다.
    /// NpcCombatActorLogicSystem(순수 C# 로직)은 NPC마다 NpcCombatActorVisualizer.Awake()가 직접 new로 생성해
    /// 보유하므로(RuntimeDataUnitGroup.Awake() 패턴 - NPC는 다중 인스턴스이므로 VContainer 싱글턴 등록 대상이 아니다)
    /// 여기서 등록하지 않는다. 다만 Attack 실행 시 런타임 스폰 오브젝트(투사체)에
    /// IObjectResolver.InjectGameObject를 명시적으로 호출해야 하므로, NpcCombatActorVisualizer(MonoBehaviour)가
    /// [Inject] Construct(IObjectResolver)를 통해 컨테이너의 리졸버를 주입받아야 한다.
    ///
    /// [버그 정정 - 튜토리얼 씬 통합 조립 중 발견] 이 주입은 과거 builder.RegisterComponentInHierarchy&lt;NpcCombatActorVisualizer&gt;()로
    /// 처리했으나, VContainer 소스(FindComponentProvider.cs)를 확인한 결과 이 메서드는 "씬 하이어라키의 모든 인스턴스를
    /// 등록한다"는 게 아니라 씬에서 가장 먼저 찾은 인스턴스 단 하나만 등록/주입한다. 그 결과 씬에 NpcCombatActorVisualizer가
    /// 둘 이상(보스용 EnemyTest, 아군용 NpcCombatActor_Dummy 등) 있으면 하이어라키 순서상 먼저인 쪽만 주입받고 나머지는
    /// resolver가 null로 남아 SpawnAndInject -> resolver.InjectGameObject가 실패한다.
    ///
    /// [추가 발견 - Play Mode 실검증 중 확인] 처음에는 LifetimeScope.autoInjectGameObjects(인스펙터 직렬화 GameObject
    /// 리스트)로 교체를 시도했으나, 이 메커니즘은 IObjectResolver.InjectGameObject(GameObject)를 호출해 대상
    /// GameObject에 붙은 "모든" MonoBehaviour를 재귀적으로 주입한다. 그런데 EnemyTest/NpcCombatActor_Dummy에는
    /// NpcCombatActorVisualizer 외에 CharacterStatComponent(02.CharacterSystem)도 함께 붙어 있고, 이 컴포넌트는
    /// [Inject] Construct(CharacterStatSystem)을 요구한다. CharacterStatSystem은 이 스코프(NpcSystemLifetimeScope,
    /// 부모=InteractionSystemLifetimeScope)의 컨테이너가 아니라 보스/캐릭터별로 별도 관리되는(BossPhaseSystemLifetimeScope가
    /// bossStatComponent.StatService를 인스펙터 고정 참조로 직접 읽는 방식) 값이라 이 스코프 트리에서는 전혀 등록되지 않는다.
    /// 그 결과 autoInjectGameObjects를 쓰면 Awake() 시점에 실제로
    /// "VContainerException: Failed to resolve CharacterSystem.CharacterStatComponent :
    /// No such registration of type: CharacterSystem.CharacterStatSystem" 예외가 발생해 AutoInjectAll() 전체가
    /// 중단되고, 그 결과 두 NpcCombatActorVisualizer 모두 resolver를 주입받지 못하는(=버그가 전혀 해결되지 않는)
    /// 것을 Play Mode 실행으로 직접 확인했다. 따라서 GameObject 전체를 재귀 주입하는 autoInjectGameObjects 대신,
    /// VContainer_가이드라인 2-1/3-3절이 이미 쓰는 "컴포넌트 단위 인스펙터 등록"(builder.RegisterComponent(instance))
    /// 패턴을 NpcCombatActorVisualizer 각 인스턴스에 그대로 적용한다 - 이 방식은 등록된 그 컴포넌트 인스턴스만
    /// 주입하고 같은 GameObject의 다른 컴포넌트(CharacterStatComponent 등)는 건드리지 않는다.
    /// (GameObject.Find/FindObjectOfType는 여전히 금지 - 인스펙터에 직접 등록한 컴포넌트 리스트만 사용)
    ///
    /// ProjectileAttackBehaviorSO.Execute -> visualizer.SpawnAndInject -> resolver.InjectGameObject(projectile) 경로에서
    /// 투사체의 CollisionDamageTrigger.Construct(IInteractionService)가 실제로 해석되려면, 이 스코프의 컨테이너가
    /// IInteractionService/ICombatPipelineManager를 등록한 InteractionSystemLifetimeScope의 자식 컨테이너여야 한다
    /// (VContainer는 자식 스코프가 부모 스코프의 등록을 상속하지만, 형제 스코프끼리는 서로의 등록을 보지 못한다).
    /// 그래서 부모 스코프를 InteractionSystemLifetimeScope로 명시적으로 지정한다.
    /// </summary>
    public class NpcSystemLifetimeScope : LifetimeScope
    {
        [Header("NpcCombatActorVisualizer 인스턴스 (씬에 배치된 모든 인스턴스를 직접 등록 - 인스펙터 전용)")]
        [SerializeField] private List<NpcCombatActorVisualizer> npcCombatActorVisualizers = new List<NpcCombatActorVisualizer>();

        [Header("BlockerNPC (이벤트가 길을 막고 서 있게 하는 일반 NPC - 씬 배치 인스턴스를 직접 등록, 인스펙터 전용)")]
        [SerializeField] private List<BlockerNPC> blockerNpcs = new List<BlockerNPC>();

        [Header("Event System(09) Linkage - Optional")]
        [Tooltip("이벤트 시스템과 이 시스템을 연결하는 인스펙터 직접 참조입니다. 두 시스템은 서로 다른 VContainer 스코프 트리에 있어 생성자 주입이 불가능하므로, " +
                 "다른 시스템과 같은 패턴으로 Start()에서 NPC 동작 핸들러를 사후 등록합니다.")]
        [SerializeField] private EventSystemLifetimeScope eventSystemScope;

        [Header("15-B 아군 투사체 풀링")]
        [SerializeField] private PureDataNpcProjectile projectileSettings;
        [SerializeField] private NpcProjectilePool projectilePool;

        protected override void Awake()
        {
            parentReference = ParentReference.Create<InteractionSystem.InteractionSystemLifetimeScope>();
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            // 15-B: 아군 투사체 풀. 풀 컴포넌트는 인터페이스로만 등록해 NpcCombatActorVisualizer/NpcProjectile이 주입받는다.
            // 풀 크기/빗나감 반환 기준은 PureData(projectileSettings)가 소유한다.
            builder.RegisterInstance(projectileSettings);
            builder.RegisterComponent(projectilePool).As<IPoolService<NpcProjectile, NpcProjectileLaunchData>>();

            // 같은 구체 타입(NpcCombatActorVisualizer)을 키 없이 둘 이상 RegisterComponent하면 VContainer가
            // "동일 구현 타입 + Singleton" 조합을 컬렉션(IEnumerable<T>) 등록의 중복으로 간주해
            // VContainerException("Conflict implementation type")을 던진다(Play Mode 실검증 중 확인).
            // 각 인스턴스를 자기 자신으로 Keyed()해 서로 다른 키를 부여함으로써 이 충돌을 피한다 - 어차피 이
            // 스코프에서는 아무도 NpcCombatActorVisualizer를 타입으로 Resolve하지 않고, 강제 주입 실행만 필요하다.
            foreach (var visualizer in npcCombatActorVisualizers)
            {
                if (visualizer != null)
                {
                    builder.RegisterComponent(visualizer).Keyed(visualizer);
                }
            }

            ConfigureEventActions(builder);
        }

        /// <summary>
        /// 이벤트가 NPC에게 시키는 동작(반복 사격 / 사격 중단 / 비켜서기 / 순간이동)의 로직과 핸들러 등록입니다. BlockerNPC 목록은 인터페이스(IBlockerNpc) 배열로,
        /// id가 있는 NPC(NpcCombatActorVisualizer.NpcId)는 인터페이스(INpcIdentity) 배열로 레지스트리에 넘기고, 핸들러는 IEventActionHandler로 등록해
        /// Start()에서 이벤트 시스템에 일괄 등록합니다.
        /// </summary>
        private void ConfigureEventActions(IContainerBuilder builder)
        {
            var blockers = new List<IBlockerNpc>();
            foreach (var blocker in blockerNpcs)
            {
                if (blocker != null) blockers.Add(blocker);
            }

            var identities = new List<INpcIdentity>();
            foreach (var visualizer in npcCombatActorVisualizers)
            {
                if (visualizer != null) identities.Add(visualizer);
            }

            builder.Register<NpcActorRegistry>(Lifetime.Scoped)
                .WithParameter<INpcIdentity[]>(identities.ToArray())
                .As<INpcActorRegistry>();

            builder.Register<BlockerNpcRegistry>(Lifetime.Scoped)
                .WithParameter<IBlockerNpc[]>(blockers.ToArray())
                .As<IBlockerNpcRegistry>();
            builder.Register<RuntimeDataNpcRepeatAttack>(Lifetime.Scoped);
            builder.Register<NpcRepeatAttackLogicSystem>(Lifetime.Scoped)
                .As<INpcRepeatAttackController>()
                .As<ITickable>();

            builder.Register<NpcRepeatAttackActionHandler>(Lifetime.Scoped).As<IEventActionHandler>();
            builder.Register<NpcStopAttackActionHandler>(Lifetime.Scoped).As<IEventActionHandler>();
            builder.Register<NpcStepAsideActionHandler>(Lifetime.Scoped).As<IEventActionHandler>();
            // AsSelf(): Start()에서 이벤트 시스템 스코프의 비행 로직(발사 지점 L 공급원)을 사후 배선(BindFlightLogic)하기 위해 구체 타입으로도 Resolve 가능하게 함.
            builder.Register<NpcTeleportActionHandler>(Lifetime.Scoped).As<IEventActionHandler>().AsSelf();
        }

        /// <summary>
        /// 이벤트 시스템(09)과의 연동 배선입니다. 두 스코프의 Container가 모두 준비된 뒤인 Start()에서 NPC 동작 핸들러를 이벤트 시스템에 등록합니다
        /// (이벤트 시스템의 첫 Tick에서 이벤트가 시작되므로 그 전에 등록이 끝납니다). Find*는 쓰지 않고 인스펙터 직접 참조만 씁니다.
        /// </summary>
        private void Start()
        {
            if (eventSystemScope == null)
            {
                Debug.LogWarning("[NpcSystemLifetimeScope] eventSystemScope가 연결되지 않아 이벤트 시스템 연동 없이 동작합니다. NPC 이벤트 동작(반복 사격/비켜서기)이 실행되지 않습니다.");
                return;
            }

            if (Container == null || eventSystemScope.Container == null)
            {
                Debug.LogError("[NpcSystemLifetimeScope] Container가 아직 준비되지 않아 이벤트 시스템 연동을 배선할 수 없습니다.");
                return;
            }

            var eventSystem = eventSystemScope.Container.Resolve<IEventSystem>();
            Container.Resolve<NpcTeleportActionHandler>().BindFlightLogic(eventSystemScope.Container.Resolve<IEventFlightLogic>());
            foreach (var handler in Container.Resolve<IEnumerable<IEventActionHandler>>())
            {
                eventSystem.RegisterActionHandler(handler);
            }
        }
    }
}
