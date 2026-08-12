using PipeLine.Contexts;
using PipeLine.UnitMagic.Steps;
using UnityEngine;

namespace PipeLine.Visualizers
{
    public class UnitChangeEffectVisualizer : MonoBehaviour, IUnitMagicVisualizer
    {
        private void OnEnable()
        {
            if (!ApplyUnitChangeStep.Visualizers.Contains(this))
            {
                ApplyUnitChangeStep.Visualizers.Add(this);
            }
        }

        private void OnDisable()
        {
            ApplyUnitChangeStep.Visualizers.Remove(this);
        }

        public void ShowInsufficientManaWarning(UnitMagicContext context) { }

        public void ShowUnitChangeEffect(UnitMagicContext context)
        {
            if (context == null || !context.IsSuccess) return;

            Debug.Log($"<color=cyan>[UnitChangeEffectVisualizer]</color> Playing Unit Change Shader & Particle FX on {context.TargetObject?.name}");
        }
    }
}
