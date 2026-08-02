using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace PlayerMovement
{
    public class MovementLifetimeScope : LifetimeScope
    {
        [SerializeField] private PureDataPlayerMovement pureData;
        [SerializeField] private PlayerMovementVisualizer visualizer;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(pureData);
            builder.Register<RuntimeDataPlayerInput>(Lifetime.Singleton);

            builder.RegisterComponent(visualizer).As<IPlayerMovementVisualizer>();

            builder.RegisterEntryPoint<PlayerInputSystem>();
            builder.RegisterEntryPoint<PlayerMovementSystem>();
        }
    }
}
