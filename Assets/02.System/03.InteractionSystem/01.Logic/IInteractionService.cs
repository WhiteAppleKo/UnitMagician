using System;
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

        /// <summary>피격 파이프라인 처리가 끝난 시점에 발행됩니다. 구현체(InteractionSystem)가 이미 방송 중인 이벤트를 DIP 인터페이스로 노출합니다.</summary>
        event Action<DamageContext> OnDamageProcessed;
    }
}
