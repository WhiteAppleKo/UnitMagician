namespace DestructibleSystem
{
    /// <summary>
    /// DestructibleDoorLogicSystem(순수 C# 로직)이 실제 연출/콜라이더 조작을 수행시키기 위해 참조하는
    /// "수동적 수행자" 인터페이스입니다. 로직은 이 인터페이스만 호출하며, Transform/Collider/ParticleSystem 등
    /// 구체 컴포넌트를 직접 참조·수정하지 않습니다(DLV Rule 2 - Blind Logic).
    /// </summary>
    public interface IDestructibleDoorVisualizer
    {
        /// <summary>
        /// 2차 게이트(데미지 &gt;= HP) 통과 시 재생: 분열 애니메이션 + 파편 파티클 + 파괴음 재생,
        /// 콜라이더 비활성화로 통로를 개방합니다.
        /// </summary>
        void PlayDestroy();

        /// <summary>
        /// 얼음 속성이지만 데미지가 HP에 못 미쳤을 때 재생: 금 가는 이펙트 + 복구 파티클/사운드만 재생하고
        /// 파괴되지 않습니다(매 호출 독립 판정 - 누적된 손상 상태를 표현하지 않음).
        /// </summary>
        void PlayCrackAndRepair();
    }
}
