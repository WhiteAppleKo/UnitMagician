using UnityEngine;
using VContainer;
using VContainer.Unity;
using NpcSystem;
using DestructibleSystem;
using EventSequencerSystem;

namespace AllySupportSystem
{
    /// <summary>
    /// AllySupportSystem(10.AllySupportSystem)의 VContainer LifetimeScope입니다. 02번 기획서가 규정하는
    /// "얇은 배선" 그대로, 신규 RuntimeData/PureData 없이 씬에 이미 배치된 아군 NPC(NpcCombatActorVisualizer),
    /// 돌문(DestructibleDoorComponent), 보스 대체 참조(GameObject)를 인스펙터로 고정 연결해
    /// AllySupportTriggerLogicSystem에 전달합니다(02번 기획서 2.9절 폴더 구조).
    ///
    /// [DefaultExecutionOrder] 필요 이유: allyVisualizer.CombatActor(NpcCombatActorLogicSystem)는 그 GameObject
    /// 자신의 Awake()(EnsureInitialized())가 먼저 실행되어야 준비됩니다. Unity는 서로 다른 GameObject 간
    /// Awake() 순서를 보장하지 않으므로, 09(EventSequencerSystemLifetimeScope)와 동일하게 이 스코프의 Awake()
    /// (Configure 포함)가 항상 나중에 실행되도록 순서를 뒤로 미룹니다.
    ///
    /// 06(이벤트 시퀀서)과의 연동은 서로 다른 VContainer 스코프 트리에 있어 생성자 주입이 불가능하므로
    /// (09/10번 작업에서 반복 확인된 현상), CharacterLifetimeScope/LocomotionLifetimeScope와 동일하게 인스펙터
    /// 직접 참조(eventSequencerScope) + Start() 사후 배선(BindEventSequencer)을 그대로 재사용합니다.
    /// GameObject.Find/FindObjectOfType은 사용하지 않습니다.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public class AllySupportSystemLifetimeScope : LifetimeScope
    {
        [Header("Ally NPC Actor (Inspector Fixed Reference)")]
        [SerializeField] private NpcCombatActorVisualizer allyVisualizer;

        [Header("1-Stage Attack Target (Inspector Fixed Reference)")]
        [SerializeField] private GameObject stoneDoorTarget;
        [SerializeField] private DestructibleDoorComponent doorComponent;

        [Header("3-Stage Attack Target (Inspector Fixed Reference)")]
        [Tooltip("03(보스AI)이 아직 없어 씬의 임의 대체 참조(예: EnemyTest)를 임시로 연결합니다" +
                 "(Goal 프롬프트 '특히 주의할 점' 절 참고).")]
        [SerializeField] private GameObject bossTarget;

        [Header("1단계 반복 타이머 설정")]
        [Tooltip("stoneDoorTarget을 향한 Attack 반복 호출 간격(초). 02번 기획서 2.2절 - 3~4초 권장.")]
        [SerializeField] private float attackIntervalSeconds = 3.5f;

        [Header("Event Sequencer(06) Linkage - Optional")]
        [Tooltip("06(이벤트 시퀀서)과 이 시스템을 연결하는 인스펙터 직접 참조입니다. 두 시스템은 서로 다른 " +
                 "VContainer 스코프 트리에 있어 생성자 주입이 불가능하므로, 09/10번과 동일한 패턴으로 씬에서 " +
                 "직접 드래그해 연결하고 Start()에서 수동 배선합니다.")]
        [SerializeField] private EventSequencerSystemLifetimeScope eventSequencerScope;

        private bool configured;

        protected override void Configure(IContainerBuilder builder)
        {
            if (allyVisualizer == null || stoneDoorTarget == null || doorComponent == null)
            {
                Debug.LogError("[AllySupportSystemLifetimeScope] allyVisualizer/stoneDoorTarget/doorComponent가 " +
                                "인스펙터에 연결되지 않았습니다.");
                return;
            }

            INpcCombatActor combatActor = allyVisualizer.CombatActor;
            if (combatActor == null)
            {
                Debug.LogError("[AllySupportSystemLifetimeScope] allyVisualizer.CombatActor가 아직 초기화되지 " +
                                "않았습니다(NpcCombatActorVisualizer.Awake() 미실행 또는 pureData 미지정).");
                return;
            }

            if (bossTarget == null)
            {
                Debug.LogWarning("[AllySupportSystemLifetimeScope] bossTarget이 연결되지 않았습니다. 3단계 지원 " +
                                  "사격/재도전 신호는 정상 구독되지만 Attack 호출은 무시됩니다(03번 보스AI 미구현 " +
                                  "임시 상태 - 씬의 EnemyTest 등으로 임시 연결하세요).");
            }

            // WithParameter(name, value)로 stoneDoorTarget/bossTarget(둘 다 GameObject 타입 - 타입만으로는
            // 구분 불가)을 파라미터 이름으로 명시 바인딩하고, 나머지는 VContainer가 자동 생성/생명주기 관리하도록
            // Register<T>()로 등록합니다(VContainer_가이드라인_v1.0.md "객체 생성" 원칙 - new 직접 생성 대신
            // 컨테이너 자동 생성).
            // AsSelf(): Start()에서 06 연동을 사후 배선(BindEventSequencer)하기 위해 구체 타입으로도 Resolve 가능하게 함.
            builder.Register<AllySupportTriggerLogicSystem>(Lifetime.Scoped)
                .WithParameter("ally", combatActor)
                .WithParameter("stoneDoorTarget", stoneDoorTarget)
                .WithParameter("bossTarget", bossTarget)
                .WithParameter<IDestructibleTargetStatus>(doorComponent)
                .WithParameter("attackIntervalSeconds", attackIntervalSeconds)
                .As<ITickable>()
                .As<System.IDisposable>()
                .AsSelf();

            configured = true;
        }

        /// <summary>
        /// 06(EventSequencerSystem)과의 연동 배선입니다. 두 스코프의 Container가 모두 준비된 뒤인 Start()에서
        /// 한 번만 연결합니다. GameObject.Find/FindObjectOfType은 사용하지 않고 인스펙터에 직접 연결해둔
        /// eventSequencerScope 참조만 사용합니다(EventSequencerSystemLifetimeScope의 bossStatComponent와 동일한 패턴).
        /// </summary>
        private void Start()
        {
            if (!configured)
            {
                Debug.LogError("[AllySupportSystemLifetimeScope] Configure()가 실패해 06 연동을 배선할 수 없습니다.");
                return;
            }

            if (eventSequencerScope == null)
            {
                Debug.LogWarning("[AllySupportSystemLifetimeScope] eventSequencerScope가 연결되지 않아 06(이벤트 " +
                                  "시퀀서) 연동 없이 동작합니다. 3단계 지원 사격/재도전 Attack은 트리거되지 않습니다" +
                                  "(1단계 돌문 반복 공격은 06과 무관하게 정상 동작).");
                return;
            }

            if (Container == null || eventSequencerScope.Container == null)
            {
                Debug.LogError("[AllySupportSystemLifetimeScope] Container가 아직 준비되지 않아 06 연동을 배선할 " +
                                "수 없습니다.");
                return;
            }

            var triggerLogic = Container.Resolve<AllySupportTriggerLogicSystem>();
            var eventSequencer = eventSequencerScope.Container.Resolve<IEventSequencer>();
            triggerLogic.BindEventSequencer(eventSequencer);
        }
    }
}
