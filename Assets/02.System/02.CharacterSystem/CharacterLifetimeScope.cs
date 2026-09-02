using CharacterSystem;
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

        // 4. Visualizer 등록
        if (playerStateVisualizer != null) builder.RegisterComponent(playerStateVisualizer).As<IPlayerStateVisualizer>();
        else builder.RegisterComponentInHierarchy<PlayerStateVisualizer>().As<IPlayerStateVisualizer>();

        if (timeSlowVisualizer != null) builder.RegisterComponent(timeSlowVisualizer).As<ITimeSlowVisualizer>();
        else builder.RegisterComponentInHierarchy<TimeSlowVisualizer>().As<ITimeSlowVisualizer>();

        // 4-1. InputReader 등록
        if (inputReader != null) builder.RegisterComponent(inputReader);
        else builder.RegisterComponentInHierarchy<Synty.AnimationBaseLocomotion.Samples.InputSystem.InputReader>();

        // 5. Logic Systems 등록
        builder.RegisterEntryPoint<PlayerStateLogicSystem>(Lifetime.Singleton);
        builder.RegisterEntryPoint<TimeSlowLogicSystem>(Lifetime.Singleton);

        // 6. UI View 등록
        if (hudUIView != null) builder.RegisterComponent(hudUIView);
        else builder.RegisterComponentInHierarchy<CharacterHUDUIView>();
    }
}
