namespace TimeSlowFilterSystem
{
    public interface ITimeSlowFilterVisualizer
    {
        void SetFilterIntensity(float intensity);
        void PlayFilterTransition(bool isEnter);
        void PlayFilterTransition(bool isEnter, float duration);
    }
}
