using UnityEngine;
using VContainer;
using VContainer.Unity;
using InteractionSystem.Logic;
using PipeLine.Combat;
using PipeLine.UnitMagic;

namespace InteractionSystem
{
    public class InteractionSystemLifetimeScope : LifetimeScope
    {
        [Header("PipeLine Assets")]
        [SerializeField] private UnitMagicPipeLine unitMagicPipeLine;

        protected override void Configure(IContainerBuilder builder)
        {
            if (unitMagicPipeLine != null) builder.RegisterInstance(unitMagicPipeLine);

            // CombatPipelineManager는 플래그->스텝 배열 캐시를 세션 내내 유지해야 하므로 Singleton으로 등록합니다.
            builder.Register<CombatPipelineManager>(Lifetime.Singleton).As<ICombatPipelineManager>().AsSelf();

            builder.Register<Logic.InteractionSystem>(Lifetime.Scoped).As<IInteractionService>().AsSelf();
        }
    }
}
