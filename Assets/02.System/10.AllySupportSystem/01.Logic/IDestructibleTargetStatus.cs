namespace AllySupportSystem
{
    /// <summary>
    /// 10(AllySupportSystem)이 04(DestructibleSystem)의 파괴 상태를 조회하기 위해 정의한 최소 인터페이스입니다.
    /// 소비자(이 시스템)가 필요한 만큼만 규약을 정의하는 인터페이스 분리 원칙(ISP)에 따라, 04번 시스템의 기존
    /// "명령"용 IDestructibleDoorVisualizer(PlayDestroy/PlayCrackAndRepair)는 전혀 건드리지 않고, 별도의
    /// 읽기 전용 "조회" 인터페이스로 분리했습니다. DestructibleDoorComponent가 이 인터페이스를 구현해
    /// IsDestroyed를 외부에 노출합니다. AllySupportTriggerLogicSystem은 이 인터페이스만 참조하며
    /// DestructibleDoorComponent(MonoBehaviour) 구체 타입을 직접 참조하지 않습니다
    /// (VContainer_가이드라인_v1.0.md "인터페이스 주입 원칙").
    /// </summary>
    public interface IDestructibleTargetStatus
    {
        /// <summary>파괴 판정(PlayDestroy 호출)이 이미 내려졌으면 true.</summary>
        bool IsDestroyed { get; }
    }
}
