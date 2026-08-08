using CameraMovement;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace PlayerMovement
{
    public class MovementLifetimeScope : LifetimeScope
    {
        [Header("Player Movement")]
        [SerializeField] private PureDataPlayerMovement pureData;
        [SerializeField] private PlayerMovementVisualizer visualizer;

        [Header("Camera System")]
        [SerializeField] private CameraSettingSO cameraSetting;
        [SerializeField] private CameraFollowVisualizer cameraVisualizer;
        [SerializeField] private CameraModeUIComponent cameraModeUI;

        protected override void Configure(IContainerBuilder builder)
        {
            // Player Movement
            builder.RegisterInstance(pureData);
            builder.Register<RuntimeDataPlayerInput>(Lifetime.Singleton);
            builder.RegisterComponent(visualizer).As<IPlayerMovementVisualizer>();
            builder.RegisterEntryPoint<PlayerInputSystem>();
            builder.RegisterEntryPoint<PlayerMovementSystem>();

            // Camera System
            if (cameraSetting != null)
            {
                builder.RegisterInstance(cameraSetting);
            }
            builder.RegisterEntryPoint<CameraFollowService>().As<ICameraFollowService>();
            if (cameraVisualizer != null)
            {
                builder.RegisterComponent(cameraVisualizer);
            }
            if (cameraModeUI != null)
            {
                builder.RegisterComponent(cameraModeUI);
            }
        }
    }
}
