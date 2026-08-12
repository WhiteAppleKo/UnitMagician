using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PipeLine.CharacterDamage;
using PipeLine.CharacterDamage.Steps;
using PipeLine.Contexts;
using PipeLine.PipeLineBase;
using PipeLine.UnitMagic;
using PipeLine.UnitMagic.Steps;
using UnityEngine;

namespace PipeLine.Tests
{
    public class PipeLineGeneralizationTest : MonoBehaviour
    {
        [ContextMenu("Run All Pipeline Generalization Tests")]
        public void RunAllTests()
        {
            TestEvasionBreakAsync().Forget();
            TestManaCheckBreakAsync().Forget();
            TestAsyncChaining100TimesAsync().Forget();
        }

        public async UniTaskVoid TestEvasionBreakAsync()
        {
            Debug.Log("<color=green>=== [Test 1] Evasion ShouldBreak Verification ===</color>");

            var pipeline = ScriptableObject.CreateInstance<CharacterDamagePipeLine>();
            // Add EvasionStep, CriticalStep, DefenseStep, ApplyDamageStep
            var stepsField = typeof(PipeLineSo<DamageContext>).GetField("steps", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var steps = new List<IPipeLineStep<DamageContext>>
            {
                new EvasionStep(),
                new CriticalStep(),
                new DefenseStep(),
                new ApplyDamageStep()
            };
            stepsField?.SetValue(pipeline, steps);

            var context = new DamageContext
            {
                AttackerAccRate = 0.0f,
                VictimEvaRate = 1.0f, // 100% evasion
                RawDamage = 100
            };

            var resultContext = await pipeline.Run(context);

            bool passed = resultContext.IsEvaded && resultContext.FinalDamage == 0;
            Debug.Log($"[Test 1] Result: IsEvaded={resultContext.IsEvaded}, FinalDamage={resultContext.FinalDamage} => Passed: {passed}");
        }

        public async UniTaskVoid TestManaCheckBreakAsync()
        {
            Debug.Log("<color=green>=== [Test 2] ManaCheck ShouldBreak Verification ===</color>");

            var pipeline = ScriptableObject.CreateInstance<UnitMagicPipeLine>();
            var stepsField = typeof(PipeLineSo<UnitMagicContext>).GetField("steps", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var steps = new List<IPipeLineStep<UnitMagicContext>>
            {
                new ManaCheckStep(),
                new TargetValidStep(),
                new ApplyUnitChangeStep()
            };
            stepsField?.SetValue(pipeline, steps);

            var context = new UnitMagicContext
            {
                RequiredMana = 50,
                CurrentMana = 20 // Insufficient Mana
            };

            var resultContext = await pipeline.Run(context);

            bool passed = resultContext.ManaCheckFailed && !resultContext.IsSuccess;
            Debug.Log($"[Test 2] Result: ManaCheckFailed={resultContext.ManaCheckFailed}, IsSuccess={resultContext.IsSuccess} => Passed: {passed}");
        }

        public async UniTaskVoid TestAsyncChaining100TimesAsync()
        {
            Debug.Log("<color=green>=== [Test 3] UniTask 100 Continuous Execution Test ===</color>");

            var pipeline = ScriptableObject.CreateInstance<CharacterDamagePipeLine>();
            var stepsField = typeof(PipeLineSo<DamageContext>).GetField("steps", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var steps = new List<IPipeLineStep<DamageContext>>
            {
                new EvasionStep(),
                new CriticalStep(),
                new DefenseStep(),
                new ApplyDamageStep()
            };
            stepsField?.SetValue(pipeline, steps);

            int successCount = 0;
            for (int i = 0; i < 100; i++)
            {
                var context = new DamageContext
                {
                    AttackerAccRate = 1.0f,
                    VictimEvaRate = 0.0f,
                    RawDamage = 50,
                    Defense = 10
                };

                var res = await pipeline.Run(context);
                if (res.FinalDamage > 0)
                {
                    successCount++;
                }
            }

            Debug.Log($"[Test 3] 100 Chaining Completed. Success Count: {successCount}/100");
        }
    }
}
