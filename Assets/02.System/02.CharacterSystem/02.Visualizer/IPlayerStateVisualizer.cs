namespace CharacterSystem
{
    public interface IPlayerStateVisualizer
    {
        void OnStateChanged(PlayerStateType previousState, PlayerStateType newState);
        void PlayCastingMotion(bool isCasting);
        void TriggerCastEffect();
    }
}
