namespace BossPhaseSystem
{
    /// <summary>
    /// 보스 페이즈 FSM(BossPhaseLogicSystem)이 외부에 노출하는 지시 창구입니다. 이벤트 시스템의 동작 핸들러(BossInvulnerableAction / BossRecoverAction /
    /// BossDefeatAction)가 이 인터페이스로만 보스 페이즈를 바꿉니다(구체 클래스 직접 참조 금지).
    /// </summary>
    public interface IBossPhaseController
    {
        /// <summary>무적·회복 페이즈로 진입합니다(무적 + 속성/크기 게이트 피격 플래그). 이미 무적·회복이거나 사망이면 무시합니다.</summary>
        /// <param name="healPerSecond">무적 동안의 초당 회복량(HP).</param>
        /// <param name="healCapHpRatio">무적 동안의 회복 상한 비율(현재/최대).</param>
        void EnterInvulnerableHealing(float healPerSecond, float healCapHpRatio);

        /// <summary>실패 회복: 보스 HP를 recoverHpRatio(현재/최대)까지 채우고 무적을 풀어 일반 공격 순환으로 복귀합니다. 사망이면 무시합니다.</summary>
        void RecoverFromFail(float recoverHpRatio);

        /// <summary>처치: 보스 HP를 0으로 반영하고 사망 페이즈로 전환해 사망 모션을 재생합니다. 이미 사망이면 무시합니다.</summary>
        void Defeat();
    }
}
