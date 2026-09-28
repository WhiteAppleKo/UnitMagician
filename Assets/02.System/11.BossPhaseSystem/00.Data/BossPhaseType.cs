namespace BossPhaseSystem
{
    /// <summary>
    /// 보스 페이즈 FSM("두뇌")의 상태입니다(03번 기획서 3.2/3.8절).
    /// Idle -> BasicAttack(순환) -> (HP 50% 도달) -> InvulnerableHealing -> (06의 판정) -> Dead 또는 BasicAttack 복귀.
    /// </summary>
    public enum BossPhaseType
    {
        Idle,
        BasicAttack,
        InvulnerableHealing,
        Dead
    }
}
