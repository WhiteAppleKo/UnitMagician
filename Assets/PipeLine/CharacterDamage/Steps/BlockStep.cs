using Cysharp.Threading.Tasks;
using PipeLine;
using UnityEngine;

public class BlockStep : IPipeLineStep<BaseDamageContext>
{
    public UniTask<BaseDamageContext> Execute(BaseDamageContext context)
    {
        if (context == null) return UniTask.FromResult(context);
        
        // 예외 처리 (총합 0 이하 시 기본 공격 성공)
        if (context.VictimBlockChance <= 0f)
        {
            context.IsEvaded = false;
            return UniTask.FromResult(context);
        }

        // 무작위값 피격자 회피율 미만 시 회피 성공
        bool isBlock = Random.Range(0f, context.VictimBlockChance) < context.VictimBlockChance;

        context.IsEvaded = isBlock;
        context.AttackFailed = isBlock;

        return UniTask.FromResult(context);
    }
}
