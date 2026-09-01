namespace CharacterSystem
{
    public interface ITimeSlowVisualizer
    {
        void SetTimeSlowEffect(bool isActive, float timeScale);
        void UpdateFocusGaugeUI(int current, int max);
    }
}
