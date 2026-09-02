using UnityEngine;
using VContainer;
using VContainer.Unity;
using InteractionSystem.Logic;
using PipeLine.CharacterDamage;
using PipeLine.UnitMagic;

namespace InteractionSystem
{
    public class InteractionSystemLifetimeScope : LifetimeScope
    {
        [Header("PipeLine Assets")]
        [SerializeField] private CharacterDamagePipeLine damagePipeLine;
        [SerializeField] private UnitMagicPipeLine unitMagicPipeLine;

        protected override void Configure(IContainerBuilder builder)
        {
            if (damagePipeLine != null) builder.RegisterInstance(damagePipeLine);
            if (unitMagicPipeLine != null) builder.RegisterInstance(unitMagicPipeLine);

            builder.Register<Logic.InteractionSystem>(Lifetime.Scoped).As<IInteractionService>().AsSelf();
        }
    }
}
