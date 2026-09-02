using UnityEngine;
using VContainer;
using VContainer.Unity;
using CameraMovement;
using CameraMovement.UI;
using Movement.RefactoredLocomotion;
using Synty.AnimationBaseLocomotion.Samples.InputSystem;

namespace PlayerMovement
{
    public class MovementLifetimeScope : LifetimeScope
    {
        [Header("Locomotion (New System)")]
        [SerializeField] private PureDataLocomotion locomotionPureData;
        [SerializeField] private LocomotionVisualizer locomotionVisualizer;
        [SerializeField] private LocomotionConfig locomotionConfig;
        [SerializeField] private InputReader inputReader;

        [Header("Camera System (6 Modes)")]
        [SerializeField] private PureDataCameraSetting cameraSetting;
        [SerializeField] private CameraFollowVisualizer cameraVisualizer;
        [SerializeField] private CameraOptionUIController cameraOptionUI;

        [Header("Legacy Compatibility (Optional)")]
        [SerializeField] private PureDataPlayerMovement legacyPureData;
        [SerializeField] private PlayerMovementVisualizer legacyVisualizer;

        protected override void Configure(IContainerBuilder builder)
        {
            // 1. Locomotion Config 자동 감지
            if (locomotionConfig == null)
            {
                locomotionConfig = GetComponent<LocomotionConfig>();
            }

            // 2. Data 등록
            PureDataLocomotion pData = locomotionPureData;
            if (pData == null && locomotionConfig != null) pData = locomotionConfig.PureData;
            if (pData != null)
            {
                builder.RegisterInstance(pData);
            }

            PureDataCameraSetting camSetting = cameraSetting;
            if (camSetting == null && locomotionConfig != null) camSetting = locomotionConfig.CameraPureData;
            if (camSetting == null) camSetting = ScriptableObject.CreateInstance<PureDataCameraSetting>();
            builder.RegisterInstance(camSetting);

            // 3. Runtime Data 등록
            builder.Register<RuntimeDataLocomotion>(Lifetime.Singleton);

            // 4. Mouse Position Provider 및 Camera Strategies 등록
            builder.Register<IMouseWorldPositionProvider, MouseWorldPositionProvider>(Lifetime.Singleton);
            builder.Register<ICameraModeCalculationStrategy, FirstPersonCameraStrategy>(Lifetime.Singleton);
            builder.Register<ICameraModeCalculationStrategy, ThirdPersonShoulderCameraStrategy>(Lifetime.Singleton);
            builder.Register<ICameraModeCalculationStrategy, TopViewMouseFocusStrategy>(Lifetime.Singleton);
            builder.Register<ICameraModeCalculationStrategy, HybridFocusCameraStrategy>(Lifetime.Singleton);
            builder.Register<ICameraModeCalculationStrategy, PlayerOnlyCameraStrategy>(Lifetime.Singleton);

            // 5. Camera Service 등록 (ILateTickable)
            builder.RegisterEntryPoint<CameraFollowService>(Lifetime.Singleton)
                .AsSelf()
                .As<ICameraFollowService>()
                .As<ILateTickable>();

            // 6. Visualizer 등록
            if (locomotionVisualizer != null) builder.RegisterComponent(locomotionVisualizer).As<ILocomotionVisualizer>();
            else builder.RegisterComponentInHierarchy<LocomotionVisualizer>().As<ILocomotionVisualizer>();

            if (cameraVisualizer != null) builder.RegisterComponent(cameraVisualizer).AsSelf().As<ICameraFollowVisualizer>();
            else builder.RegisterComponentInHierarchy<CameraFollowVisualizer>().AsSelf().As<ICameraFollowVisualizer>();

            if (cameraOptionUI != null) builder.RegisterComponent(cameraOptionUI);
            else builder.RegisterComponentInHierarchy<CameraOptionUIController>();

            // 6. Input Reader 등록
            if (inputReader != null) builder.RegisterComponent(inputReader);
            else builder.RegisterComponentInHierarchy<InputReader>();

            // 7. Locomotion Logic System 등록
            builder.RegisterEntryPoint<LocomotionLogicSystem>(Lifetime.Singleton);

            // 8. 하위 호환 레거시 등록 (필요시)
            if (legacyPureData != null)
            {
                builder.RegisterInstance(legacyPureData);
                builder.Register<RuntimeDataPlayerInput>(Lifetime.Singleton);
                if (legacyVisualizer != null) builder.RegisterComponent(legacyVisualizer).As<IPlayerMovementVisualizer>();
                else builder.RegisterComponentInHierarchy<PlayerMovementVisualizer>().As<IPlayerMovementVisualizer>();
                builder.RegisterEntryPoint<PlayerInputSystem>();
                builder.RegisterEntryPoint<PlayerMovementSystem>();
            }
        }
    }
}

