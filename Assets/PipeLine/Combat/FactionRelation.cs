using CharacterSystem;

namespace PipeLine.Combat
{
    /// <summary>
    /// 편 구분. 충돌 레이어 구성(몸체/공격 판정체 레이어 선택, 같은 편 충돌 제거)에 쓰는 구분이다.
    /// NeutralSide는 Neutral 진영끼리만 같은 편으로 묶는 레이어용 편이며 적대 판정에는 쓰이지 않는다(IsHostile 참조).
    /// </summary>
    public enum FactionSide
    {
        None = 0,
        PlayerSide = 1,
        EnemySide = 2,
        NeutralSide = 3
    }

    /// <summary>
    /// 진영 규칙표(순수 C# 정적 클래스)입니다. "같은 편/적대" 판정의 유일한 출처이며 다른 곳에 같은 규칙을 다시 쓰지 않습니다.
    /// - 편 정의: {Player, Ally} = 아군편, {Enemy} = 적편, {Neutral} = 중립편(레이어 목적).
    /// - IsSameSide(a, b): 같은 편(레이어 목적 - Neutral끼리도 같은 편이다. 중립 공격 판정체가 중립 몸체를 맞히지 않게 하기 위함).
    /// - IsHostile(a, b): 아군편과 적편 사이만 적대. 중립은 누구에게도 적대적이지 않다.
    /// Neutral(돌문 등)은 아군/적 공격 판정체에게 항상 피격 대상이다(충돌/피격 처리는 호출하는 쪽의 몫).
    /// </summary>
    public static class FactionRelation
    {
        public static FactionSide GetSide(FactionType faction)
        {
            switch (faction)
            {
                case FactionType.Player:
                case FactionType.Ally:
                    return FactionSide.PlayerSide;
                case FactionType.Enemy:
                    return FactionSide.EnemySide;
                case FactionType.Neutral:
                    return FactionSide.NeutralSide;
                default:
                    return FactionSide.None;
            }
        }

        public static bool IsSameSide(FactionType a, FactionType b)
        {
            FactionSide sideA = GetSide(a);
            return sideA != FactionSide.None && sideA == GetSide(b);
        }

        public static bool IsHostile(FactionType a, FactionType b)
        {
            // 적대는 아군편과 적편 사이에서만 성립한다(NeutralSide/None은 어느 쪽과도 적대가 아니다).
            FactionSide sideA = GetSide(a);
            FactionSide sideB = GetSide(b);
            return IsCombatSide(sideA) && IsCombatSide(sideB) && sideA != sideB;
        }

        private static bool IsCombatSide(FactionSide side)
        {
            return side == FactionSide.PlayerSide || side == FactionSide.EnemySide;
        }
    }
}
