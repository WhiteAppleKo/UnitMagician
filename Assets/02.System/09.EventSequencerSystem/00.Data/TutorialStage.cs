namespace EventSequencerSystem
{
    /// <summary>
    /// 튜토리얼 1~4단계 진행 상태를 나타내는 열거형입니다([D] Pure Data 성격의 상수 정의).
    /// 이 열거형 자체는 이번 튜토리얼 전용 구성이며, 시퀀서 프레임워크(TutorialSequencerLogicSystem)는
    /// Stage 정의를 교체하면 다른 씬/연출에도 재사용 가능하도록 설계되어 있습니다(06번 기획서 6.1/6.2절).
    /// </summary>
    public enum TutorialStage
    {
        Stage1,
        Stage2,
        Stage3,
        Stage4Success,
        Stage4Fail,
        Victory
    }
}