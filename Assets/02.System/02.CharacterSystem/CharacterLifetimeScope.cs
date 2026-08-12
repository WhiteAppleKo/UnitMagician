using CharacterSystem;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class CharacterLifetimeScope : LifetimeScope
{
    [SerializeField] private PureStatData defaultStatData;
    [SerializeField] private CharacterStatComponent characterStatComponent;
    [SerializeField] private CharacterHUDUIView hudUIView;

    protected override void Configure(IContainerBuilder builder)
    {
        if (defaultStatData != null)
        {
            builder.RegisterInstance(defaultStatData);
            builder.Register<CharacterStatSystem>(Lifetime.Singleton);
            builder.Register<UnitMagicSlotSystem>(Lifetime.Singleton);
        }

        if (characterStatComponent != null)
        {
            builder.RegisterComponent(characterStatComponent);
        }

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
