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
        // 1. Character Stat & Unit Magic Slot
        if (characterStatComponent != null)
        {
            builder.RegisterComponent(characterStatComponent);

            if (characterStatComponent.StatSystem != null)
            {
                builder.RegisterInstance(characterStatComponent.StatSystem).As<ICharacterStatService>().AsSelf();
            }
            else if (characterStatComponent.PureStatData != null)
            {
                var statSystem = new CharacterStatSystem(characterStatComponent.PureStatData);
                characterStatComponent.Initialize(statSystem);
                builder.RegisterInstance(statSystem).As<ICharacterStatService>().AsSelf();
            }
            else
            {
                var statSystem = new CharacterStatSystem(ScriptableObject.CreateInstance<PureStatData>());
                characterStatComponent.Initialize(statSystem);
                builder.RegisterInstance(statSystem).As<ICharacterStatService>().AsSelf();
            }
        }
        else
        {
            var defaultStat = ScriptableObject.CreateInstance<PureStatData>();
            var statSystem = new CharacterStatSystem(defaultStat);
            builder.RegisterInstance(statSystem).As<ICharacterStatService>().AsSelf();
        }

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
        var stateVis = playerStateVisualizer;
        if (stateVis == null) stateVis = FindAnyObjectByType<PlayerStateVisualizer>();
        if (stateVis != null)
        {
            builder.RegisterComponent(stateVis).As<IPlayerStateVisualizer>();
        }

        var slowVis = timeSlowVisualizer;
        if (slowVis == null) slowVis = FindAnyObjectByType<TimeSlowVisualizer>();
        if (slowVis != null)
        {
            builder.RegisterComponent(slowVis).As<ITimeSlowVisualizer>();
        }

        // 4-1. InputReader 등록
        var inReader = inputReader;
        if (inReader == null) inReader = FindAnyObjectByType<Synty.AnimationBaseLocomotion.Samples.InputSystem.InputReader>();
        if (inReader != null)
        {
            builder.RegisterComponent(inReader);
        }

        // 5. Logic Systems 등록
        builder.RegisterEntryPoint<PlayerStateLogicSystem>(Lifetime.Singleton);
        builder.RegisterEntryPoint<TimeSlowLogicSystem>(Lifetime.Singleton);

        // 6. UI View 등록
        if (hudUIView != null)
        {
            builder.RegisterComponent(hudUIView);
        }
        else
        {
            builder.RegisterComponentInHierarchy<CharacterHUDUIView>();
        }
    }
}
