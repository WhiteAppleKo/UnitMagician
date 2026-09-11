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

        // 3. Target Stencil Service 등록
        builder.Register<TargetStencilService>(Lifetime.Singleton)
            .As<ITargetStencilService>()
            .As<System.IDisposable>();

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
