using UnityEngine;
using VContainer;
using VContainer.Unity;
using EventSequencerSystem;

namespace AreaFreezeSystem
{
    /// <summary>
    /// AreaFreezeSystem(12.AreaFreezeSystem)의 VContainer LifetimeScope입니다. bossGameObjectRef는 인스펙터
    /// 고정 참조입니다 - 09/11번과 동일한 관례로 씬에 이미 배치된 보스 대체 참조(EnemyTest)를 연결합니다.
    ///
    /// 이 스코프는 bossGameObjectRef의 어떤 컴포넌트 상태(StatService 초기화 등)에도 의존하지 않고 GameObject
    /// 참조 자체만 필요하므로, 09/10/11번과 달리 [DefaultExecutionOrder]가 필요하지 않습니다. Unity는 씬의
    /// 모든 GameObject의 Awake()가 끝난 뒤에야 비로소 어떤 GameObject의 Start()도 호출하지 않으므로(초기화 1회
    /// 보장), 기본 실행 순서에서도 Start() 시점에는 eventSequencerScope.Container가 항상 준비되어 있습니다.
    ///
    /// 06(이벤트 시퀀서)과의 연동은 서로 다른 VContainer 스코프 트리에 있어 생성자 주입이 불가능하므로(09/10/02/
    /// 03번 작업에서 반복 확인된 현상), 인스펙터 직접 참조(eventSequencerScope) + Start() 사후 배선
    /// (BindEventSequencer)을 그대로 재사용합니다. GameObject.Find/FindObjectOfType은 사용하지 않습니다.
    /// </summary>
    public class AreaFreezeSystemLifetimeScope : LifetimeScope
    {
        [Header("Boss Reference (Inspector Fixed Reference - EnemyTest, 09/11번과 동일 관례)")]
        [Tooltip("광역 빙결 폭발 중심(Physics.OverlapSphere 중심)으로 사용할 보스 GameObject입니다. " +
                 "DamageContext에 명중 좌표 필드가 없어 대신 이 GameObject의 transform.position을 사용합니다" +
                 "(05번 기획서 5.8절).")]
        [SerializeField] private GameObject bossGameObjectRef;

        [Header("Pure Data (Optional - auto-created with defaults if empty)")]
        [SerializeField] private PureDataAreaFreeze pureDataAreaFreeze;

        [Header("Area Freeze Visualizer (Optional)")]
        [SerializeField] private AreaFreezeVisualizer areaFreezeVisualizer;

        [Header("Event Sequencer(06) Linkage - Optional")]
        [Tooltip("06(이벤트 시퀀서)과 이 시스템을 연결하는 인스펙터 직접 참조입니다. 두 시스템은 서로 다른 " +
                 "VContainer 스코프 트리에 있어 생성자 주입이 불가능하므로, 09/10/11번과 동일한 패턴으로 씬에서 " +
                 "직접 드래그해 연결하고 Start()에서 수동 배선합니다.")]
        [SerializeField] private EventSequencerSystemLifetimeScope eventSequencerScope;

        private bool configured;

        protected override void Configure(IContainerBuilder builder)
        {
            if (bossGameObjectRef == null)
            {
                Debug.LogError("[AreaFreezeSystemLifetimeScope] bossGameObjectRef가 인스펙터에 연결되지 " +
                                "않았습니다. 씬의 보스 대체 참조(예: EnemyTest)를 연결하세요.");
                return;
            }

            // 1. Pure Data 등록 (인스펙터 미지정 시 기본값으로 생성)
            PureDataAreaFreeze pureData = pureDataAreaFreeze != null
                ? pureDataAreaFreeze
                : ScriptableObject.CreateInstance<PureDataAreaFreeze>();
            builder.RegisterInstance(pureData);

            // 2. Visualizer 등록 (구체 클래스 직접 주입 금지 - 인터페이스 단위로만 등록)
            if (areaFreezeVisualizer != null) builder.RegisterComponent(areaFreezeVisualizer).As<IAreaFreezeVisualizer>();
            else builder.RegisterComponentInHierarchy<AreaFreezeVisualizer>().As<IAreaFreezeVisualizer>();

            // 3. Logic System 등록. bossGameObjectRef(GameObject)는 파라미터 이름으로 명시 바인딩한다
            // (BossPhaseSystemLifetimeScope/AllySupportSystemLifetimeScope와 동일한 이유 - GameObject 타입은
            // 이름으로 구분해야 함). AsSelf(): Start()에서 06 연동을 사후 배선(BindEventSequencer)하기 위해
            // 구체 타입으로도 Resolve 가능하게 함.
            builder.Register<AreaFreezeTriggerLogicSystem>(Lifetime.Scoped)
                .WithParameter("bossGameObject", bossGameObjectRef)
                .As<System.IDisposable>()
                .AsSelf();

            configured = true;
        }

        /// <summary>
        /// 06(EventSequencerSystem)과의 연동 배선입니다. 두 스코프의 Container가 모두 준비된 뒤인 Start()에서
        /// 한 번만 연결합니다. GameObject.Find/FindObjectOfType은 사용하지 않고 인스펙터에 직접 연결해둔
        /// eventSequencerScope 참조만 사용합니다(BossPhaseSystemLifetimeScope와 동일한 패턴).
        /// </summary>
        private void Start()
        {
            if (!configured)
            {
                Debug.LogError("[AreaFreezeSystemLifetimeScope] Configure()가 실패해 06 연동을 배선할 수 없습니다.");
                return;
            }

            if (eventSequencerScope == null)
            {
                Debug.LogWarning("[AreaFreezeSystemLifetimeScope] eventSequencerScope가 연결되지 않아 06(이벤트 " +
                                  "시퀀서) 연동 없이 동작합니다. OnCoreHitSuccess 신호를 받지 못해 광역 빙결이 " +
                                  "트리거되지 않습니다.");
                return;
            }

            if (Container == null || eventSequencerScope.Container == null)
            {
                Debug.LogError("[AreaFreezeSystemLifetimeScope] Container가 아직 준비되지 않아 06 연동을 배선할 " +
                                "수 없습니다.");
                return;
            }

            var triggerLogic = Container.Resolve<AreaFreezeTriggerLogicSystem>();
            var eventSequencer = eventSequencerScope.Container.Resolve<IEventSequencer>();
            triggerLogic.BindEventSequencer(eventSequencer);
        }
    }
}
