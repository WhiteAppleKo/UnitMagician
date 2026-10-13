using System;
using VContainer;
using VContainer.Unity;

namespace CharacterSystem
{
    /// <summary>
    /// RuntimeDataTimeScale의 유효 배율이 바뀔 때 ITimeSlowVisualizer(Time.timeScale/fixedDeltaTime의 유일한 쓰기 지점)에
    /// 배율 적용을 지시하고, UI의 일시정지 요청(ITimeScaleService)을 RuntimeDataTimeScale에 전달합니다(17번 기획서 §3-2).
    /// </summary>
    public class TimeScaleLogicSystem : ITimeScaleService, IInitializable, IDisposable
    {
        private readonly RuntimeDataTimeScale runtimeData;
        private readonly ITimeSlowVisualizer visualizer;

        [Inject]
        public TimeScaleLogicSystem(RuntimeDataTimeScale runtimeData, ITimeSlowVisualizer visualizer = null)
        {
            this.runtimeData = runtimeData ?? throw new ArgumentNullException(nameof(runtimeData));
            this.visualizer = visualizer;
        }

        public bool IsPaused => runtimeData.IsPaused;
        public float EffectiveScale => runtimeData.EffectiveScale;

        public event Action<bool> OnPauseStateChanged
        {
            add => runtimeData.OnPauseStateChanged += value;
            remove => runtimeData.OnPauseStateChanged -= value;
        }

        public void Initialize()
        {
            runtimeData.OnEffectiveScaleChanged += HandleEffectiveScaleChanged;
        }

        public void Dispose()
        {
            runtimeData.OnEffectiveScaleChanged -= HandleEffectiveScaleChanged;
        }

        public void SetPaused(object requester, bool paused)
        {
            runtimeData.SetPaused(requester, paused);
        }

        private void HandleEffectiveScaleChanged(float effectiveScale)
        {
            visualizer?.ApplyTimeScale(effectiveScale);
        }
    }
}
