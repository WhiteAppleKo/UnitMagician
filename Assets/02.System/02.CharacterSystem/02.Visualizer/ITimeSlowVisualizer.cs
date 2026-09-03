namespace CharacterSystem
{
    public interface ITimeSlowVisualizer
    {
        event System.Action<bool> OnSlowStateChanged;
        void SetTimeSlowEffect(bool isActive, float targetScale);
        void UpdateFocusGaugeUI(int current, int max);
    }
}
