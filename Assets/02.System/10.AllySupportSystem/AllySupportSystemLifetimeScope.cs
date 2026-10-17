using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;
using VContainer.Unity;
using NpcSystem;
using GameEvent;

namespace AllySupportSystem
{
    /// <summary>
    /// AllySupportSystem(10.AllySupportSystem)의 VContainer LifetimeScope입니다. 02번 기획서가 규정하는 "얇은 배선" 그대로, 신규 RuntimeData/PureData 없이
    /// 씬에 이미 배치된 특수 아군 NPC(NpcCombatActorVisualizer)와 보스 대체 참조(GameObject)를 인스펙터로 고정 연결해
    /// 이벤트 투사체 발사 동작 핸들러(EventProjectileLaunchActionHandler)에 전달하고, 그 핸들러를 이벤트 시스템에 등록합니다(Goal 51).
    /// 돌문 사격(Goal 50)과 보스 50% 이벤트의 발사 트리거(Goal 51)는 모두 이벤트 시스템의 이벤트 SO가 일으키므로 이 스코프에는 신호 구독/타이머가 없습니다.
    ///
    /// [DefaultExecutionOrder] 필요 이유: allyVisualizer.CombatActor(NpcCombatActorLogicSystem)는 그 GameObject
    /// 자신의 Awake()(EnsureInitialized())가 먼저 실행되어야 준비됩니다. Unity는 서로 다른 GameObject 간
    /// Awake() 순서를 보장하지 않으므로, 09(EventSystemLifetimeScope)와 동일하게 이 스코프의 Awake()
    /// (Configure 포함)가 항상 나중에 실행되도록 순서를 뒤로 미룹니다.
    ///
    /// 이벤트 시스템과의 연동은 서로 다른 VContainer 스코프 트리에 있어 생성자 주입이 불가능하므로(09/10번 작업에서 반복 확인된 현상),
    /// 인스펙터 직접 참조(eventSystemScope) + Start() 사후 배선(핸들러 등록, 비행 로직 연결)을 그대로 재사용합니다.
    /// GameObject.Find/FindObjectOfType은 사용하지 않습니다.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public class AllySupportSystemLifetimeScope : LifetimeScope
    {
        [Header("Ally NPC Actor (Inspector Fixed Reference)")]
        [SerializeField] private NpcCombatActorVisualizer allyVisualizer;

        [Header("Event Projectile Target (Inspector Fixed Reference)")]
        [Tooltip("03(보스AI)이 아직 없어 씬의 임의 대체 참조(예: EnemyTest)를 임시로 연결합니다" +
                 "(Goal 프롬프트 '특히 주의할 점' 절 참고).")]
        [SerializeField] private GameObject bossTarget;

        [Header("Event System(09) Linkage - Optional")]
        [Tooltip("이벤트 시스템과 이 시스템을 연결하는 인스펙터 직접 참조입니다. 두 시스템은 서로 다른 " +
                 "VContainer 스코프 트리에 있어 생성자 주입이 불가능하므로, 씬에서 직접 드래그해 연결하고 Start()에서 수동 배선합니다.")]
        [FormerlySerializedAs("eventSequencerScope")]
        [SerializeField] private EventSystemLifetimeScope eventSystemScope;

        private bool configured;

        protected override void Configure(IContainerBuilder builder)
        {
            if (allyVisualizer == null)
            {
                Debug.LogError("[AllySupportSystemLifetimeScope] allyVisualizer가 인스펙터에 연결되지 않았습니다.");
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
                Debug.LogWarning("[AllySupportSystemLifetimeScope] bossTarget이 연결되지 않았습니다. 이벤트 투사체 " +
                                  "발사의 Attack 호출은 무시됩니다(03번 보스AI 미구현 임시 상태 - 씬의 EnemyTest 등으로 임시 연결하세요).");
            }

            // WithParameter(name, value)로 ally/bossTarget을 파라미터 이름으로 명시 바인딩하고, 나머지는 VContainer가
            // 자동 생성/생명주기 관리하도록 Register<T>()로 등록합니다(VContainer_가이드라인_v1.0.md "객체 생성" 원칙 -
            // new 직접 생성 대신 컨테이너 자동 생성).
            // AsSelf(): Start()에서 이벤트 시스템 스코프의 비행 로직을 사후 배선(BindFlightLogic)하기 위해 구체 타입으로도 Resolve 가능하게 함.
            builder.Register<EventProjectileLaunchActionHandler>(Lifetime.Scoped)
                .WithParameter("ally", combatActor)
                .WithParameter("bossTarget", bossTarget)
                .As<IEventActionHandler>()
                .AsSelf();

            configured = true;
        }

        /// <summary>
        /// 이벤트 시스템(09)과의 연동 배선입니다. 두 스코프의 Container가 모두 준비된 뒤인 Start()에서 한 번만 연결합니다
        /// (이벤트 시스템의 첫 Tick에서 이벤트가 시작되므로 그 전에 등록이 끝납니다). GameObject.Find/FindObjectOfType은 사용하지 않고
        /// 인스펙터에 직접 연결해둔 eventSystemScope 참조만 사용합니다(EventSystemLifetimeScope의 bossStatComponent와 동일한 패턴).
        /// </summary>
        private void Start()
        {
            if (!configured)
            {
                Debug.LogError("[AllySupportSystemLifetimeScope] Configure()가 실패해 이벤트 시스템 연동을 배선할 수 없습니다.");
                return;
            }

            if (eventSystemScope == null)
            {
                Debug.LogWarning("[AllySupportSystemLifetimeScope] eventSystemScope가 연결되지 않아 이벤트 시스템 연동 없이 동작합니다. " +
                                  "이벤트 투사체 발사 동작이 실행되지 않습니다.");
                return;
            }

            if (Container == null || eventSystemScope.Container == null)
            {
                Debug.LogError("[AllySupportSystemLifetimeScope] Container가 아직 준비되지 않아 이벤트 시스템 연동을 배선할 수 없습니다.");
                return;
            }

            var launchHandler = Container.Resolve<EventProjectileLaunchActionHandler>();
            launchHandler.BindFlightLogic(eventSystemScope.Container.Resolve<IEventFlightLogic>());

            var eventSystem = eventSystemScope.Container.Resolve<IEventSystem>();
            foreach (var handler in Container.Resolve<IEnumerable<IEventActionHandler>>())
            {
                eventSystem.RegisterActionHandler(handler);
            }
        }
    }
}
