using UnityEngine;
using VContainer;
using VContainer.Unity;
using OptionSystem;
using Synty.AnimationBaseLocomotion.Samples.InputSystem;

public class OptionLifetimeScope : LifetimeScope
{
    [SerializeField] private OptionUIComponent optionUIComponent;
    [SerializeField] private InputReader inputReader;

    protected override void Configure(IContainerBuilder builder)
    {
        // 1. OptionUIComponent 등록
        if (optionUIComponent != null) builder.RegisterComponent(optionUIComponent);
        else builder.RegisterComponentInHierarchy<OptionUIComponent>();

        // 2. InputReader 등록
        if (inputReader != null) builder.RegisterComponent(inputReader);
        else builder.RegisterComponentInHierarchy<InputReader>();
    }
}
