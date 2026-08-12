using PipeLine.Contexts;
using PipeLine.PipeLineBase;
using UnityEngine;

namespace PipeLine.CharacterDamage
{
    [CreateAssetMenu(fileName = "CharacterDamagePipeLine", menuName = "PipeLine/Character Damage PipeLine")]
    public class CharacterDamagePipeLine : PipeLineSo<DamageContext>
    {
        protected override bool ShouldBreak(DamageContext context)
        {
            if (context == null) return true;

            // 회피 발생 시 파이프라인 중단 (CriticalStep, DefenseStep, ApplyDamageStep 무효화)
            if (context.IsEvaded)
            {
                Debug.Log($"[{name}] CharacterDamagePipeLine broken: Target Evaded attack.");
                return true;
            }

            return false;
        }
    }
}
