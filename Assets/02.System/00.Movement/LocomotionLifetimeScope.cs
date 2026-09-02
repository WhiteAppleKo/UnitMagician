using UnityEngine;
using VContainer;
using VContainer.Unity;
using CameraMovement;
using CameraMovement.UI;
using Movement.Visualizer;

namespace Movement.RefactoredLocomotion
{
    [RequireComponent(typeof(LocomotionConfig))]
    public class LocomotionLifetimeScope : LifetimeScope
    {
        [SerializeField] private LocomotionConfig config;

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

            // 4. Camera Service 등록 (ILateTickable 프레임 루프 자동 구동)
            builder.RegisterEntryPoint<CameraFollowService>(Lifetime.Singleton)
                .AsSelf()
                .As<ICameraFollowService>()
                .As<ILateTickable>();

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
    }
}
