using UnityEngine;
using VContainer;
using VContainer.Unity;
using NpcSystem;
using CharacterSystem;
using EventSequencerSystem;

namespace BossPhaseSystem
{
    /// <summary>
    /// BossPhaseSystem(11.BossPhaseSystem)의 VContainer LifetimeScope입니다. bossStatComponent/bossActor는
    /// 인스펙터 고정 참조입니다 - 씬에 이미 배치된 EnemyTest(06/02번이 이미 보스 대체 참조로 써온 관례)에
    /// NpcCombatActorVisualizer를 추가해 같은 GameObject가 ICharacterStatService와 INpcCombatActor를 모두
    /// 제공하도록 연결합니다(03번 기획서 3.1절 "보스는 INpcCombatActor 구현체를 몸통으로 갖는다").
    ///
    /// [DefaultExecutionOrder] 필요 이유: bossStatComponent.StatService/bossActor.CombatActor는 그 GameObject
    /// 자신의 Awake()(EnsureInitialized())가 먼저 실행되어야 준비됩니다. Unity는 서로 다른 GameObject 간 Awake()
    /// 순서를 보장하지 않으므로, 09/10(EventSequencerSystemLifetimeScope/AllySupportSystemLifetimeScope)과
    /// 동일하게 이 스코프의 Awake()(Configure 포함)가 항상 나중에 실행되도록 순서를 뒤로 미룹니다.
    ///
    /// 06(이벤트 시퀀서)과의 연동은 서로 다른 VContainer 스코프 트리에 있어 생성자 주입이 불가능하므로(09/10/02번
    /// 작업에서 반복 확인된 현상), 인스펙터 직접 참조(eventSequencerScope) + Start() 사후 배선(BindEventSequencer)을
    /// 그대로 재사용합니다. GameObject.Find/FindObjectOfType은 사용하지 않습니다.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public class BossPhaseSystemLifetimeScope : LifetimeScope
    {
        [Header("Boss Reference (Inspector Fixed Reference - EnemyTest, 06/02번과 동일 관례)")]
        [SerializeField] private CharacterStatComponent bossStatComponent;
        [SerializeField] private NpcCombatActorVisualizer bossActor;

        [Header("Player Target (Inspector Fixed Reference)")]
        [SerializeField] private GameObject playerTarget;

        [Header("Pure Data (Optional - auto-created with defaults if empty)")]
        [SerializeField] private PureDataBossPattern pureDataBossPattern;

        [Header("Boss Phase Visualizer (Optional)")]
        [SerializeField] private BossPhaseVisualizer bossPhaseVisualizer;

        [Header("Event Sequencer(06) Linkage - Optional")]
        [Tooltip("06(이벤트 시퀀서)과 이 시스템을 연결하는 인스펙터 직접 참조입니다. 두 시스템은 서로 다른 " +
                 "VContainer 스코프 트리에 있어 생성자 주입이 불가능하므로, 09/10/02번과 동일한 패턴으로 씬에서 " +
                 "직접 드래그해 연결하고 Start()에서 수동 배선합니다.")]
        [SerializeField] private EventSequencerSystemLifetimeScope eventSequencerScope;

        private bool configured;

        protected override void Configure(IContainerBuilder builder)
        {
            if (bossStatComponent == null || bossActor == null)
            {
                Debug.LogError("[BossPhaseSystemLifetimeScope] bossStatComponent/bossActor가 인스펙터에 연결되지 " +
                                "않았습니다.");
                return;
            }

            if (bossStatComponent.StatService == null)
            {
                Debug.LogError("[BossPhaseSystemLifetimeScope] bossStatComponent.StatService가 아직 초기화되지 " +
                                "않았습니다(CharacterStatComponent.Awake() 미실행 또는 PureStatData 미지정).");
                return;
            }

            INpcCombatActor combatActor = bossActor.CombatActor;
            if (combatActor == null)
            {
                Debug.LogError("[BossPhaseSystemLifetimeScope] bossActor.CombatActor가 아직 초기화되지 " +
                                "않았습니다(NpcCombatActorVisualizer.Awake() 미실행 또는 pureData 미지정).");
                return;
            }

            if (playerTarget == null)
            {
                Debug.LogWarning("[BossPhaseSystemLifetimeScope] playerTarget이 연결되지 않았습니다. BasicAttack " +
                                  "상태에서 Attack이 대상 없이 호출됩니다.");
            }

            // 1. 보스 ICharacterStatService + 식별용 GameObject 등록 (EventSequencerSystemLifetimeScope와 동일 패턴)
            builder.RegisterInstance<ICharacterStatService>(bossStatComponent.StatService);
            builder.RegisterInstance(bossStatComponent.gameObject);

            // 2. Pure Data 등록 (인스펙터 미지정 시 기본값으로 생성)
            PureDataBossPattern pureData = pureDataBossPattern != null
                ? pureDataBossPattern
                : ScriptableObject.CreateInstance<PureDataBossPattern>();
            builder.RegisterInstance(pureData);

            // 3. Runtime Data 등록
            builder.Register<RuntimeDataBossPhase>(Lifetime.Scoped);

            // 4. Logic System 등록. bossActor(INpcCombatActor)/playerTarget(GameObject)은 이미 등록된
            // bossStatComponent.gameObject(GameObject)와 타입이 겹치므로 이름으로 명시 바인딩한다
            // (AllySupportSystemLifetimeScope와 동일한 이유).
            // AsSelf(): Start()에서 06 연동을 사후 배선(BindEventSequencer)하기 위해 구체 타입으로도 Resolve 가능하게 함.
            builder.Register<BossPhaseLogicSystem>(Lifetime.Scoped)
                .WithParameter("bossActor", combatActor)
                .WithParameter("playerTarget", playerTarget)
                .As<ITickable>()
                .As<System.IDisposable>()
                .AsSelf();

            // 5. Visualizer 등록
            if (bossPhaseVisualizer != null) builder.RegisterComponent(bossPhaseVisualizer).As<IBossPhaseVisualizer>();
            else builder.RegisterComponentInHierarchy<BossPhaseVisualizer>().As<IBossPhaseVisualizer>();

            configured = true;
        }

        /// <summary>
        /// 06(EventSequencerSystem)과의 연동 배선입니다. 두 스코프의 Container가 모두 준비된 뒤인 Start()에서
        /// 한 번만 연결합니다.
        /// </summary>
        private void Start()
        {
            if (!configured)
            {
                Debug.LogError("[BossPhaseSystemLifetimeScope] Configure()가 실패해 06 연동을 배선할 수 없습니다.");
                return;
            }

            if (eventSequencerScope == null)
            {
                Debug.LogWarning("[BossPhaseSystemLifetimeScope] eventSequencerScope가 연결되지 않아 06(이벤트 " +
                                  "시퀀서) 연동 없이 동작합니다. OnCoreHitSuccess/OnCoreHitFail 신호를 받지 못해 " +
                                  "InvulnerableHealing 상태에서 빠져나오지 못합니다.");
                return;
            }

            if (Container == null || eventSequencerScope.Container == null)
            {
                Debug.LogError("[BossPhaseSystemLifetimeScope] Container가 아직 준비되지 않아 06 연동을 배선할 " +
                                "수 없습니다.");
                return;
            }

            var bossPhaseLogic = Container.Resolve<BossPhaseLogicSystem>();
            var eventSequencer = eventSequencerScope.Container.Resolve<IEventSequencer>();
            bossPhaseLogic.BindEventSequencer(eventSequencer);
        }
    }
}
