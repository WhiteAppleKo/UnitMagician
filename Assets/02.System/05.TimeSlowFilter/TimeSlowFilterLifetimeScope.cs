using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace TimeSlowFilterSystem
{
    public class TimeSlowFilterLifetimeScope : LifetimeScope
    {
        [Header("Data (Optional)")]
        [SerializeField] private PureDataTimeSlowFilter pureDataTimeSlowFilter;

        [Header("Visualizer (Optional)")]
        [SerializeField] private TimeSlowFilterVisualizer timeSlowFilterVisualizer;

        protected override void Configure(IContainerBuilder builder)
        {
            // 1. Pure Data 등록
            PureDataTimeSlowFilter pureData = pureDataTimeSlowFilter != null
                ? pureDataTimeSlowFilter
                : ScriptableObject.CreateInstance<PureDataTimeSlowFilter>();
            builder.RegisterInstance(pureData);

            // 2. Visualizer 등록 (인터페이스 바인딩)
            if (timeSlowFilterVisualizer != null)
            {
                builder.RegisterComponent(timeSlowFilterVisualizer).As<ITimeSlowFilterVisualizer>();
            }
            else
            {
                builder.RegisterComponentInHierarchy<TimeSlowFilterVisualizer>().As<ITimeSlowFilterVisualizer>();
            }

            // 3. Logic System 등록
            builder.RegisterEntryPoint<TimeSlowFilterLogicSystem>(Lifetime.Scoped);
        }
    }
}
