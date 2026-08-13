using CharacterSystem;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class CharacterLifetimeScope : LifetimeScope
{
    [SerializeField] private CharacterStatComponent characterStatComponent;
    [SerializeField] private CharacterHUDUIView hudUIView;

    protected override void Configure(IContainerBuilder builder)
    {
        if (characterStatComponent != null)
        {
            builder.RegisterComponent(characterStatComponent);

            if (characterStatComponent.StatSystem != null)
            {
                builder.RegisterInstance(characterStatComponent.StatSystem);
            }
            else if (characterStatComponent.PureStatData != null)
            {
                var statSystem = new CharacterStatSystem(characterStatComponent.PureStatData);
                characterStatComponent.Initialize(statSystem);
                builder.RegisterInstance(statSystem);
            }
        }

        builder.Register<UnitMagicSlotSystem>(Lifetime.Singleton);

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
