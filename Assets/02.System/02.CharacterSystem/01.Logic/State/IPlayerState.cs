namespace CharacterSystem
{
    public interface IPlayerState
    {
        PlayerStateType StateType { get; }
        void Enter();
        void Tick(float deltaTime);
        void Exit();
    }
}
