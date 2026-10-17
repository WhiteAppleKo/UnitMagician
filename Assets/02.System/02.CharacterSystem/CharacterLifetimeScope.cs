using System.Collections.Generic;
using CharacterSystem;
using GameEvent;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;
using VContainer.Unity;

public class CharacterLifetimeScope : LifetimeScope
{
    [Header("Character Stat Components")]
    [SerializeField] private CharacterStatComponent characterStatComponent;
    [SerializeField] private CharacterHUDUIView hudUIView;

    [Header("Player State & Time Slow Pure Data (Optional)")]
    [SerializeField] private PureDataPlayerState pureDataPlayerState;
    [SerializeField] private PureDataTimeSlow pureDataTimeSlow;

    [Header("Visualizers (Optional)")]
    [SerializeField] private PlayerStateVisualizer playerStateVisualizer;
    [SerializeField] private TimeSlowVisualizer timeSlowVisualizer;

    [Header("Input (Optional)")]
    [SerializeField] private Synty.AnimationBaseLocomotion.Samples.InputSystem.InputReader inputReader;
    [SerializeField] private Common.InputSystem.InputContextManager inputContextManager;

    [Header("Event System(09) Linkage - Optional")]
    [Tooltip("이벤트 시스템과 시간 정지 감속 모드(이벤트 감속 동작 핸들러, 비행 신호)를 연결하는 인스펙터 직접 참조입니다. " +
             "두 시스템은 서로 다른 VContainer 스코프 트리(부모 없는 루트 스코프)에 있어 생성자 주입이 불가능하므로, " +
             "씬에서 직접 드래그해 연결하고 Start()에서 수동 배선합니다.")]
    [FormerlySerializedAs("eventSequencerScope")]
    [SerializeField] private EventSystemLifetimeScope eventSystemScope;

    protected override void Configure(IContainerBuilder builder)
    {
        // 1. Character Stat Component & Service
        if (characterStatComponent != null)
        {
            builder.RegisterComponent(characterStatComponent);
            if (characterStatComponent.PureStatData != null)
            {
                builder.RegisterInstance(characterStatComponent.PureStatData);
            }
        }
        else
        {
            builder.RegisterComponentInHierarchy<CharacterStatComponent>();
        }

        // 19-A: 스태미나 지연 회복 틱을 위해 ITickable로도 구동한다(플레이어 스탯 서비스 한 개만 컨테이너가 틱한다).
        builder.RegisterEntryPoint<CharacterStatSystem>(Lifetime.Singleton).As<ICharacterStatService>().AsSelf();
        builder.Register<UnitMagicSlotSystem>(Lifetime.Singleton).As<IUnitMagicSlotService>().AsSelf();

        // 2. Pure Data 등록
        PureDataPlayerState statePureData = pureDataPlayerState != null ? pureDataPlayerState : ScriptableObject.CreateInstance<PureDataPlayerState>();
        builder.RegisterInstance(statePureData);

        PureDataTimeSlow slowPureData = pureDataTimeSlow != null ? pureDataTimeSlow : ScriptableObject.CreateInstance<PureDataTimeSlow>();
        builder.RegisterInstance(slowPureData);

        // 3. Runtime Data 등록
        builder.Register<RuntimeDataPlayerState>(Lifetime.Singleton);
        builder.Register<RuntimeDataTimeSlow>(Lifetime.Singleton);
        // 17번: Time.timeScale 합성 데이터(요청 배율 + 일시정지 요청자 집합)
        builder.Register<RuntimeDataTimeScale>(Lifetime.Singleton);

        // 3-1. Target Stencil Service 등록 (시간 정지 필터 서비스 연동)
        builder.Register<TimeSlowFilterSystem.TargetStencilService>(Lifetime.Singleton)
            .As<TimeSlowFilterSystem.ITargetStencilService>()
            .As<System.IDisposable>();

        // 4. Visualizer 등록
        if (playerStateVisualizer != null) builder.RegisterComponent(playerStateVisualizer).As<IPlayerStateVisualizer>();
        else builder.RegisterComponentInHierarchy<PlayerStateVisualizer>().As<IPlayerStateVisualizer>();

        if (timeSlowVisualizer != null) builder.RegisterComponent(timeSlowVisualizer).As<ITimeSlowVisualizer>();
        else builder.RegisterComponentInHierarchy<TimeSlowVisualizer>().As<ITimeSlowVisualizer>();

        // 4-1. InputReader 등록
        if (inputReader != null) builder.RegisterComponent(inputReader);
        else builder.RegisterComponentInHierarchy<Synty.AnimationBaseLocomotion.Samples.InputSystem.InputReader>();

        // 4-2. InputContextManager 등록 (IInputContextManager 인터페이스 바인딩)
        if (inputContextManager != null)
        {
            builder.RegisterComponent(inputContextManager).As<Common.InputSystem.IInputContextManager>();
        }
        else
        {
            var ctxMgr = FindAnyObjectByType<Common.InputSystem.InputContextManager>() ?? Common.InputSystem.InputContextManager.Instance;
            builder.RegisterComponent(ctxMgr).As<Common.InputSystem.IInputContextManager>();
        }

        // 5. Logic Systems 등록
        builder.RegisterEntryPoint<PlayerStateLogicSystem>(Lifetime.Singleton);
        // AsSelf(): 이벤트 감속 동작 핸들러가 주입받고 Start()에서 비행 신호를 사후 배선(BindEventFlight)하기 위해 구체 타입으로도 Resolve 가능하게 함.
        builder.RegisterEntryPoint<TimeSlowLogicSystem>(Lifetime.Singleton).AsSelf();
        // 이벤트 동작 핸들러(이벤트 감속). Start()에서 이벤트 시스템에 사후 등록한다(Goal 51).
        builder.Register<TimeSlowOverrideActionHandler>(Lifetime.Singleton).As<IEventActionHandler>();
        // 17번: 유효 배율 → TimeSlowVisualizer 적용 + UI 일시정지 요청 서비스(하위 스코프 UI가 부모 체인으로 ITimeScaleService 주입)
        builder.RegisterEntryPoint<TimeScaleLogicSystem>(Lifetime.Singleton).As<ITimeScaleService>();

        // 6. UI View 등록
        if (hudUIView != null) builder.RegisterComponent(hudUIView);
        else builder.RegisterComponentInHierarchy<CharacterHUDUIView>();
    }

    /// <summary>
    /// 이벤트 시스템(09)과의 연동 배선. VContainer의 Awake()/Build() 타이밍(스크립트 실행 순서 무관하게
    /// "모든 Awake는 어떤 Start보다 먼저 실행된다"는 유니티 보장)을 이용해, 두 스코프의 Container가 모두 준비된
    /// 뒤인 Start()에서 한 번만 연결한다. GameObject.Find/FindObjectOfType은 사용하지 않고 인스펙터에 직접
    /// 연결해둔 eventSystemScope 참조만 사용한다(EventSystemLifetimeScope의 bossStatComponent와 동일한 패턴).
    /// </summary>
    private void Start()
    {
        if (eventSystemScope == null)
        {
            Debug.LogWarning("[CharacterLifetimeScope] eventSystemScope가 연결되지 않아 이벤트 시스템 연동 없이 동작합니다. " +
                              "감속 모드(ActivateEventSlow)는 이벤트 SO의 감속 동작 없이는 트리거되지 않습니다.");
            return;
        }

        if (Container == null || eventSystemScope.Container == null)
        {
            Debug.LogError("[CharacterLifetimeScope] Container가 아직 준비되지 않아 이벤트 시스템 연동을 배선할 수 없습니다.");
            return;
        }

        // 이벤트 투사체 비행 구간/진행도 신호(IEventFlightLogic)로 이벤트 감속의 timeScale을 구동한다.
        Container.Resolve<TimeSlowLogicSystem>().BindEventFlight(eventSystemScope.Container.Resolve<IEventFlightLogic>());

        // 이벤트 감속 동작 핸들러를 이벤트 시스템에 등록한다. 감속 수치(최저 timeScale, 곡선)는 감속 시작 동작이 실행될 때 이벤트 SO에서 넘어온다.
        var eventSystem = eventSystemScope.Container.Resolve<IEventSystem>();
        foreach (var handler in Container.Resolve<IEnumerable<IEventActionHandler>>())
        {
            eventSystem.RegisterActionHandler(handler);
        }
    }
}
