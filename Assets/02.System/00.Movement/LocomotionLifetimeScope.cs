using System;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;
using VContainer.Unity;
using CameraMovement;
using CameraMovement.UI;
using Movement.Visualizer;
using GameEvent;

namespace Movement.RefactoredLocomotion
{
    [RequireComponent(typeof(LocomotionConfig))]
    public class LocomotionLifetimeScope : LifetimeScope
    {
        [SerializeField] private LocomotionConfig config;

        [Header("Event System(09) Linkage - Optional")]
        [Tooltip("이벤트 시스템과 이벤트 카메라/입력 락 동작 핸들러(CameraOverrideActionHandler, InputLockActionHandler)를 연결하는 인스펙터 직접 참조입니다. " +
                 "두 시스템은 서로 다른 VContainer 스코프 트리(EventSystemLifetimeScope의 부모는 " +
                 "InteractionSystemLifetimeScope)에 있어 생성자 주입이 불가능하므로, 씬에서 직접 드래그해 연결하고 Start()에서 수동 배선합니다.")]
        [FormerlySerializedAs("eventSequencerScope")]
        [SerializeField] private EventSystemLifetimeScope eventSystemScope;

        protected override void Awake()
        {
            parentReference = ParentReference.Create<CharacterLifetimeScope>();
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            if (config == null)
            {
                config = GetComponent<LocomotionConfig>();
            }

            if (config == null) return;

            // 1. Pure Data 등록 (로코모션 & 카메라)
            if (config.PureData != null)
            {
                builder.RegisterInstance(config.PureData);
            }

            PureDataCameraSetting cameraSetting = config.CameraPureData != null 
                ? config.CameraPureData 
                : ScriptableObject.CreateInstance<PureDataCameraSetting>();
            builder.RegisterInstance(cameraSetting);

            // 2. Runtime Data 등록
            builder.Register<RuntimeDataLocomotion>(Lifetime.Singleton);

            // 3. Camera Strategy 등록 (숄더뷰 고정 — 숄더 전략 하나만 사용, 22번 기획서)
            builder.Register<ICameraModeCalculationStrategy, ThirdPersonShoulderCameraStrategy>(Lifetime.Singleton);

            // 3-1. 이벤트 시선 대상 등록 — CameraFollowService가 IEventCameraLookTarget(위치 값)만 주입받는다.
            // 실제 위치 공급원(09의 IEventFlightTracker)은 다른 스코프 트리라 Start()에서 사후 배선(Bind)한다.
            builder.Register<EventProjectileLookTarget>(Lifetime.Singleton).AsSelf().As<IEventCameraLookTarget>();

            // 4. Camera Service 등록 (ILateTickable 프레임 루프 자동 구동)
            builder.RegisterEntryPoint<CameraFollowService>(Lifetime.Singleton)
                .AsSelf()
                .As<ICameraFollowService>()
                .As<ILateTickable>();

            // 4-1. 이벤트 동작 핸들러 등록 (카메라 오버라이드 / 입력 락). Start()에서 이벤트 시스템에 사후 등록한다(Goal 51).
            // AsSelf(): 부모 스코프(CharacterLifetimeScope)의 핸들러까지 IEnumerable로 섞여 중복 등록되지 않도록 Start()에서 구체 타입으로 직접 Resolve해 등록한다.
            builder.Register<CameraOverrideActionHandler>(Lifetime.Singleton).AsSelf().As<IEventActionHandler>();
            builder.Register<InputLockActionHandler>(Lifetime.Singleton).AsSelf().As<IEventActionHandler>();

            // 5. Visualizer 등록
            if (config.Visualizer != null) builder.RegisterComponent(config.Visualizer).As<ILocomotionVisualizer>();
            else builder.RegisterComponentInHierarchy<LocomotionVisualizer>().As<ILocomotionVisualizer>();

            builder.RegisterComponentInHierarchy<PlayerLockOnController>().As<ILockOnController>();

            if (config.CameraVisualizer != null) builder.RegisterComponent(config.CameraVisualizer).AsSelf().As<ICameraFollowVisualizer>();
            else builder.RegisterComponentInHierarchy<CameraFollowVisualizer>().AsSelf().As<ICameraFollowVisualizer>();

            if (config.CameraOptionUI != null) builder.RegisterComponent(config.CameraOptionUI);
            else builder.RegisterComponentInHierarchy<CameraOptionUIController>();

            // 5. Input Reader 등록
            if (config.InputReader != null) builder.RegisterComponent(config.InputReader);
            else builder.RegisterComponentInHierarchy<Synty.AnimationBaseLocomotion.Samples.InputSystem.InputReader>();

            // 6. Locomotion Logic System 등록 (ITickable, IDisposable)
            builder.RegisterEntryPoint<LocomotionLogicSystem>(Lifetime.Singleton);
        }

        /// <summary>
        /// 이벤트 시스템(09)과의 연동 배선. VContainer의 Awake()/Build() 타이밍(스크립트 실행 순서와
        /// 무관하게 "모든 Awake는 어떤 Start보다 먼저 실행된다"는 유니티 보장)을 이용해, 두 스코프의 Container가
        /// 모두 준비된 뒤인 Start()에서 한 번만 연결한다. GameObject.Find/FindObjectOfType은 사용하지 않고
        /// 인스펙터에 직접 연결해둔 eventSystemScope 참조만 사용한다(CharacterLifetimeScope와 동일한 패턴).
        /// </summary>
        private void Start()
        {
            if (eventSystemScope == null)
            {
                Debug.LogWarning("[LocomotionLifetimeScope] eventSystemScope가 연결되지 않아 이벤트 시스템 연동 없이 동작합니다. " +
                                  "이벤트 카메라(대상 오버라이드)는 이벤트 SO의 카메라 오버라이드 동작 없이는 트리거되지 않습니다(기본 3인칭 숄더뷰 조작은 정상 동작).");
                return;
            }

            if (Container == null || eventSystemScope.Container == null)
            {
                Debug.LogError("[LocomotionLifetimeScope] Container가 아직 준비되지 않아 이벤트 시스템 연동을 배선할 수 없습니다.");
                return;
            }

            // 이벤트 카메라 동작 핸들러(카메라 오버라이드 / 입력 락)를 이벤트 시스템에 등록한다. 이벤트 카메라 수치(블렌드/곡선/FOV/눈 높이)는
            // 카메라 오버라이드 동작이 실행될 때 이벤트 SO에서 카메라 서비스로 넘어온다.
            var eventSystem = eventSystemScope.Container.Resolve<IEventSystem>();
            eventSystem.RegisterActionHandler(Container.Resolve<CameraOverrideActionHandler>());
            eventSystem.RegisterActionHandler(Container.Resolve<InputLockActionHandler>());

            // 이벤트 카메라가 바라볼 투사체 위치 공급원(09의 IEventFlightTracker)도 같은 방식으로 사후 배선한다.
            var eventLookTarget = Container.Resolve<EventProjectileLookTarget>();
            eventLookTarget.Bind(eventSystemScope.Container.Resolve<IEventFlightTracker>());
        }
    }
}
