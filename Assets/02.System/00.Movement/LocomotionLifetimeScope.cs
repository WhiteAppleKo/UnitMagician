using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using CameraMovement;
using CameraMovement.UI;
using Movement.Visualizer;
using EventSequencerSystem;

namespace Movement.RefactoredLocomotion
{
    [RequireComponent(typeof(LocomotionConfig))]
    public class LocomotionLifetimeScope : LifetimeScope
    {
        [SerializeField] private LocomotionConfig config;

        [Header("Event Sequencer(06) Linkage - Optional")]
        [Tooltip("06(이벤트 시퀀서)과 신규 이벤트 카메라(EventCameraTriggerLogicSystem)를 연결하는 인스펙터 직접 참조입니다. " +
                 "두 시스템은 서로 다른 VContainer 스코프 트리(EventSequencerSystemLifetimeScope의 부모는 " +
                 "InteractionSystemLifetimeScope)에 있어 생성자 주입이 불가능하므로, 09번(CharacterLifetimeScope의 " +
                 "eventSequencerScope)과 동일한 패턴으로 씬에서 직접 드래그해 연결하고 Start()에서 수동 배선합니다.")]
        [SerializeField] private EventSequencerSystemLifetimeScope eventSequencerScope;

        [Header("Event Target Tracking Test Objects (Optional - 02/03 미구현으로 임시 대체)")]
        [Tooltip("아군 발사 위치를 대신할 씬의 임의 테스트 오브젝트(02.CharacterSystem 미구현으로 임시 대체, Goal 프롬프트 '제외' 절).")]
        [SerializeField] private Transform allyLaunchPositionTestObject;
        [Tooltip("보스 위치를 대신할 씬의 임의 테스트 오브젝트(03.BossAI 미구현으로 임시 대체, Goal 프롬프트 '제외' 절).")]
        [SerializeField] private Transform bossPositionTestObject;

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

            // 3. Mouse Position Provider 및 Camera Strategies 등록
            builder.Register<IMouseWorldPositionProvider, MouseWorldPositionProvider>(Lifetime.Singleton);
            builder.Register<ICameraModeCalculationStrategy, FirstPersonCameraStrategy>(Lifetime.Singleton);
            builder.Register<ICameraModeCalculationStrategy, ThirdPersonShoulderCameraStrategy>(Lifetime.Singleton);
            builder.Register<ICameraModeCalculationStrategy, TopViewMouseFocusStrategy>(Lifetime.Singleton);
            builder.Register<ICameraModeCalculationStrategy, HybridFocusCameraStrategy>(Lifetime.Singleton);
            builder.Register<ICameraModeCalculationStrategy, PlayerOnlyCameraStrategy>(Lifetime.Singleton);
            builder.Register<ICameraModeCalculationStrategy, EventTargetTrackingCameraStrategy>(Lifetime.Singleton);

            // 3-1. 이벤트 추적 카메라(EventTargetTracking) 대상 Provider 등록 (선택 - 02/03 미구현 시 미등록)
            // 이 등록이 EventTargetTrackingCameraStrategy보다 먼저 존재해야 그 생성자([Inject])가
            // IEventCameraTargetProvider를 정상적으로 주입받는다(VContainer는 등록 시점 순서와 무관하게
            // Build() 시점에 전체 그래프를 해석하므로 실제로는 순서 무관하지만, 가독성을 위해 앞에 둔다).
            if (allyLaunchPositionTestObject != null && bossPositionTestObject != null)
            {
                builder.RegisterInstance<IEventCameraTargetProvider>(
                    new EventCameraTargetProvider(allyLaunchPositionTestObject, bossPositionTestObject));
            }

            // 4. Camera Service 등록 (ILateTickable 프레임 루프 자동 구동)
            builder.RegisterEntryPoint<CameraFollowService>(Lifetime.Singleton)
                .AsSelf()
                .As<ICameraFollowService>()
                .As<ILateTickable>();

            // 4-1. 이벤트 카메라 트리거 로직 등록 (설계 정정 이후 IInitializable 불필요 - 3단계 이벤트 카메라 판정 전용)
            // AsSelf(): Start()에서 06 연동을 사후 배선(BindEventSequencer)하기 위해 구체 타입으로도 Resolve 가능하게 함.
            builder.Register<EventCameraTriggerLogicSystem>(Lifetime.Singleton).AsSelf().As<IDisposable>();

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
        /// 06(EventSequencerSystem)과의 연동 배선. VContainer의 Awake()/Build() 타이밍(스크립트 실행 순서와
        /// 무관하게 "모든 Awake는 어떤 Start보다 먼저 실행된다"는 유니티 보장)을 이용해, 두 스코프의 Container가
        /// 모두 준비된 뒤인 Start()에서 한 번만 연결한다. GameObject.Find/FindObjectOfType은 사용하지 않고
        /// 인스펙터에 직접 연결해둔 eventSequencerScope 참조만 사용한다(CharacterLifetimeScope와 동일한 패턴).
        /// </summary>
        private void Start()
        {
            if (eventSequencerScope == null)
            {
                Debug.LogWarning("[LocomotionLifetimeScope] eventSequencerScope가 연결되지 않아 06(이벤트 시퀀서) 연동 없이 동작합니다. " +
                                  "EventTargetTracking 모드는 06의 자동 발동 없이는 트리거되지 않습니다(기본 3인칭 숄더뷰 조작은 정상 동작).");
                return;
            }

            if (Container == null || eventSequencerScope.Container == null)
            {
                Debug.LogError("[LocomotionLifetimeScope] Container가 아직 준비되지 않아 06 연동을 배선할 수 없습니다.");
                return;
            }

            var eventCameraTrigger = Container.Resolve<EventCameraTriggerLogicSystem>();
            var eventSequencer = eventSequencerScope.Container.Resolve<IEventSequencer>();
            eventCameraTrigger.BindEventSequencer(eventSequencer);
        }
    }
}
