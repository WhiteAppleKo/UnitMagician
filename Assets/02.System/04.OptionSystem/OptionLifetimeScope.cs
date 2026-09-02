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
        // 1. OptionUIComponent 등록 (인스펙터 -> 씬 탐색 -> Hierarchy 탐색)
        OptionUIComponent optUI = optionUIComponent;
        if (optUI == null)
        {
            optUI = GetComponent<OptionUIComponent>();
        }
        if (optUI == null)
        {
            optUI = FindAnyObjectByType<OptionUIComponent>();
        }

        if (optUI != null)
        {
            builder.RegisterComponent(optUI);
        }
        else
        {
            builder.RegisterComponentInHierarchy<OptionUIComponent>();
        }

        // 2. InputReader 등록 (존재 시)
        InputReader inReader = inputReader;
        if (inReader == null)
        {
            inReader = FindAnyObjectByType<InputReader>();
        }

        if (inReader != null)
        {
            builder.RegisterComponent(inReader);
        }
    }
}
