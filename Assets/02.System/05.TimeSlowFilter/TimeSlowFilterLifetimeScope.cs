using UnityEngine;
using VContainer;
using VContainer.Unity;
using TimeSlowFilterSystem;

public class TimeSlowFilterLifetimeScope : LifetimeScope
{
    [Header("Data References")]
    [SerializeField] private PureDataTimeSlowFilter pureDataTimeSlowFilter;
    [SerializeField] private PureDataStencilMask pureDataStencilMask;

    [Header("Visualizer (Optional)")]
    [SerializeField] private TimeSlowFilterVisualizer filterVisualizer;

    [Header("Additional Target Renderers (Optional)")]
    [SerializeField] private Renderer[] additionalTargetRenderers;

    protected override void Awake()
    {
        parentReference = ParentReference.Create<CharacterLifetimeScope>();
        base.Awake();
    }

    protected override void Configure(IContainerBuilder builder)
    {
        // 1. PureData 등록
        PureDataTimeSlowFilter timeSlowData = pureDataTimeSlowFilter != null
            ? pureDataTimeSlowFilter
            : ScriptableObject.CreateInstance<PureDataTimeSlowFilter>();
        builder.RegisterInstance(timeSlowData);

        PureDataStencilMask stencilData = pureDataStencilMask != null
            ? pureDataStencilMask
            : ScriptableObject.CreateInstance<PureDataStencilMask>();
        builder.RegisterInstance(stencilData);

        // 2. Visualizer 등록
        if (filterVisualizer != null)
        {
            builder.RegisterComponent(filterVisualizer).As<ITimeSlowFilterVisualizer>();
        }
        else
        {
            builder.RegisterComponentInHierarchy<TimeSlowFilterVisualizer>().As<ITimeSlowFilterVisualizer>();
        }

        // 3. Target Stencil Service는 CharacterLifetimeScope(공통 부모 스코프)에서 단일 등록되어
        // 상위 스코프 체인을 통해 주입된다. TacticalCommandSystem(UnitSystem 하위)과 동일 인스턴스를
        // 공유해야 하므로 이 스코프에서 별도로 재등록하지 않는다.

        // 4. Logic System 등록
        builder.RegisterEntryPoint<TimeSlowFilterLogicSystem>(Lifetime.Singleton);

        // 5. 추가 타깃 렌더러가 설정된 경우 빌드 후 등록 콜백
        if (additionalTargetRenderers != null && additionalTargetRenderers.Length > 0)
        {
            builder.RegisterBuildCallback(container =>
            {
                var stencilService = container.Resolve<ITargetStencilService>();
                stencilService.RegisterRenderers(additionalTargetRenderers);
            });
        }
    }
}
