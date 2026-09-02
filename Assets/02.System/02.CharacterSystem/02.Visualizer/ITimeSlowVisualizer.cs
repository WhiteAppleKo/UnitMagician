namespace CharacterSystem
{
    public interface ITimeSlowVisualizer
    {
        void SetTimeSlowEffect(bool isActive, float targetScale);
        void UpdateFocusGaugeUI(int current, int max);
    }
}
