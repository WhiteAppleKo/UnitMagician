using PipeLine;
using UnityEngine;

[CreateAssetMenu(fileName = "BaseDamagePipeLineSO", menuName = "Scriptable Objects/BaseDamagePipeLineSO")]
public class BaseDamagePipeLineSO : PipeLineSo<BaseDamageContext>
{
    protected override bool ShouldBreak(BaseDamageContext context)
    {
        if (context == null) return true;
        
        // 회피 발생 시 이후 데미지 계산 단계 무효화
        if (context.IsEvaded)
        {
            return true;
        }

        return false;
    }
}
