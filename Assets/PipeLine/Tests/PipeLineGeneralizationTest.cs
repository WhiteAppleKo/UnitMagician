using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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
            TestManaCheckBreakAsync().Forget();
        }

        public async UniTaskVoid TestManaCheckBreakAsync()
        {
            Debug.Log("<color=green>=== [Test] ManaCheck ShouldBreak Verification ===</color>");

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
            Debug.Log($"[Test] Result: ManaCheckFailed={resultContext.ManaCheckFailed}, IsSuccess={resultContext.IsSuccess} => Passed: {passed}");
        }
    }
}
