namespace BossPhaseSystem
{
    /// <summary>
    /// 보스 페이즈 전환 연출([V] Visual Interface)입니다. 추상 메서드만 포함하며 구체 구현을 노출하지 않습니다
    /// (DIP 연결 고리 - 코드_가이드라인 1-3절). 체력바 애니메이션 자체는 기존 RuntimeStatData.HP.OnValueChanged
    /// 구독 UI 경로를 그대로 재사용하므로 이 인터페이스의 책임이 아니다(03번 기획서 3.8절 "시청각 전달" 서술).
    /// </summary>
    public interface IBossPhaseVisualizer
    {
        /// <summary>페이즈가 newPhase로 바뀐 시점에 호출됩니다. 3.4절의 페이즈별 연출(화염/포효/화면흔들림,
        /// 흡수 파티클, 실패 시 강조 등)을 재생합니다.</summary>
        void PlayPhaseTransitionEffect(BossPhaseType newPhase);
    }
}
