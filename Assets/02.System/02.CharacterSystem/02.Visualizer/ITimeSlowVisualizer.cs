namespace CharacterSystem
{
    public interface ITimeSlowVisualizer
    {
        event System.Action<bool> OnSlowStateChanged;
        /// <summary>시간정지/감속의 부수 효과(OnSlowStateChanged, 오디오, VFX)만 수행합니다. Time.timeScale은 쓰지 않습니다.</summary>
        void SetTimeSlowEffect(bool isActive, float targetScale);
        /// <summary>합성된 유효 배율을 Time.timeScale/Time.fixedDeltaTime에 적용합니다(프로젝트에서 이 값을 쓰는 유일한 지점).</summary>
        void ApplyTimeScale(float effectiveScale);
        void UpdateFocusGaugeUI(int current, int max);
    }
}
