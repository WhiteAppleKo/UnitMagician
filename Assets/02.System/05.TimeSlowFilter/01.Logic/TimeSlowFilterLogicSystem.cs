using System;
using CharacterSystem;
using VContainer;
using VContainer.Unity;

namespace TimeSlowFilterSystem
{
    public class TimeSlowFilterLogicSystem : IInitializable, IDisposable
    {
        private readonly PureDataTimeSlowFilter pureData;
        private readonly ITimeSlowFilterVisualizer filterVisualizer;
        private readonly ITargetStencilService targetStencilService;
        private readonly ITimeSlowVisualizer timeSlowVisualizer;
        private readonly RuntimeDataTimeSlow runtimeDataTimeSlow;

        [Inject]
        public TimeSlowFilterLogicSystem(
            PureDataTimeSlowFilter pureData,
            ITimeSlowFilterVisualizer filterVisualizer,
            ITargetStencilService targetStencilService = null,
            ITimeSlowVisualizer timeSlowVisualizer = null,
            RuntimeDataTimeSlow runtimeDataTimeSlow = null)
        {
            this.pureData = pureData ?? throw new ArgumentNullException(nameof(pureData));
            this.filterVisualizer = filterVisualizer ?? throw new ArgumentNullException(nameof(filterVisualizer));
            this.targetStencilService = targetStencilService;
            this.timeSlowVisualizer = timeSlowVisualizer;
            this.runtimeDataTimeSlow = runtimeDataTimeSlow;
        }

        public void Initialize()
        {
            if (timeSlowVisualizer != null)
            {
                timeSlowVisualizer.OnSlowStateChanged += HandleSlowStateChanged;
            }
            else if (runtimeDataTimeSlow != null)
            {
                runtimeDataTimeSlow.OnSlowStateChanged += HandleSlowStateChanged;
            }

            bool isInitiallyActive = runtimeDataTimeSlow != null && runtimeDataTimeSlow.IsSlowActive;
            filterVisualizer.SetFilterIntensity(isInitiallyActive ? 1f : 0f);
            targetStencilService?.SetActive(isInitiallyActive);
        }

        public void Dispose()
        {
            if (timeSlowVisualizer != null)
            {
                timeSlowVisualizer.OnSlowStateChanged -= HandleSlowStateChanged;
            }

            if (runtimeDataTimeSlow != null)
            {
                runtimeDataTimeSlow.OnSlowStateChanged -= HandleSlowStateChanged;
            }

            filterVisualizer.SetFilterIntensity(0f);
            targetStencilService?.SetActive(false);
        }

        private void HandleSlowStateChanged(bool isSlowActive)
        {
            targetStencilService?.SetActive(isSlowActive);
            float duration = pureData.TransitionDuration;
            filterVisualizer.PlayFilterTransition(isSlowActive, duration);
        }
    }
}
