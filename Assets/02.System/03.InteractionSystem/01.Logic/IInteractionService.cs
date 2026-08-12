using Cysharp.Threading.Tasks;
using PipeLine.Contexts;

namespace InteractionSystem.Logic
{
    /// <summary>
    /// 상호작용 통합 수신 및 PipeLine 연산 실행 서비스 인터페이스입니다.
    /// </summary>
    public interface IInteractionService
    {
        UniTask ProcessDamageAsync(DamageContext context);
        UniTask ProcessHealAsync(DamageContext context);
        UniTask ProcessUnitMagicAsync(UnitMagicContext context);
    }
}
