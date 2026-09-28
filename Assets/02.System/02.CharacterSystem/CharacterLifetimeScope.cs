using CharacterSystem;
using EventSequencerSystem;
using UnityEngine;
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

    [Header("Event Sequencer(06) Linkage - Optional")]
    [Tooltip("06(이벤트 시퀀서)과 09(시간 정지 감속 모드)를 연결하는 인스펙터 직접 참조입니다. " +
             "두 시스템은 서로 다른 VContainer 스코프 트리(부모 없는 루트 스코프)에 있어 생성자 주입이 불가능하므로, " +
             "06의 bossStatComponent와 동일한 패턴으로 씬에서 직접 드래그해 연결하고 Start()에서 수동 배선합니다.")]
    [SerializeField] private EventSequencerSystemLifetimeScope eventSequencerScope;

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

        builder.Register<CharacterStatSystem>(Lifetime.Singleton).As<ICharacterStatService>().AsSelf();
        builder.Register<UnitMagicSlotSystem>(Lifetime.Singleton).As<IUnitMagicSlotService>().AsSelf();

        // 2. Pure Data 등록
        PureDataPlayerState statePureData = pureDataPlayerState != null ? pureDataPlayerState : ScriptableObject.CreateInstance<PureDataPlayerState>();
        builder.RegisterInstance(statePureData);

        PureDataTimeSlow slowPureData = pureDataTimeSlow != null ? pureDataTimeSlow : ScriptableObject.CreateInstance<PureDataTimeSlow>();
        builder.RegisterInstance(slowPureData);

        // 3. Runtime Data 등록
        builder.Register<RuntimeDataPlayerState>(Lifetime.Singleton);
        builder.Register<RuntimeDataTimeSlow>(Lifetime.Singleton);

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
        // AsSelf(): Start()에서 06 연동을 사후 배선(BindEventSequencer)하기 위해 구체 타입으로도 Resolve 가능하게 함.
        builder.RegisterEntryPoint<TimeSlowLogicSystem>(Lifetime.Singleton).AsSelf();

        // 6. UI View 등록
        if (hudUIView != null) builder.RegisterComponent(hudUIView);
        else builder.RegisterComponentInHierarchy<CharacterHUDUIView>();
    }

    /// <summary>
    /// 06(EventSequencerSystem)과의 연동 배선. VContainer의 Awake()/Build() 타이밍(스크립트 실행 순서 무관하게
    /// "모든 Awake는 어떤 Start보다 먼저 실행된다"는 유니티 보장)을 이용해, 두 스코프의 Container가 모두 준비된
    /// 뒤인 Start()에서 한 번만 연결한다. GameObject.Find/FindObjectOfType은 사용하지 않고 인스펙터에 직접
    /// 연결해둔 eventSequencerScope 참조만 사용한다(06의 bossStatComponent와 동일한 패턴).
    /// </summary>
    private void Start()
    {
        if (eventSequencerScope == null)
        {
            Debug.LogWarning("[CharacterLifetimeScope] eventSequencerScope가 연결되지 않아 06(이벤트 시퀀서) 연동 없이 동작합니다. " +
                              "감속 모드(ActivateEventSlow)는 06의 자동 발동 없이는 트리거되지 않습니다.");
            return;
        }

        if (Container == null || eventSequencerScope.Container == null)
        {
            Debug.LogError("[CharacterLifetimeScope] Container가 아직 준비되지 않아 06 연동을 배선할 수 없습니다.");
            return;
        }

        var timeSlowLogicSystem = Container.Resolve<TimeSlowLogicSystem>();
        var eventSequencer = eventSequencerScope.Container.Resolve<IEventSequencer>();
        var pureDataTutorialSequence = eventSequencerScope.Container.Resolve<PureDataTutorialSequence>();
        timeSlowLogicSystem.BindEventSequencer(eventSequencer, pureDataTutorialSequence);
    }
}
