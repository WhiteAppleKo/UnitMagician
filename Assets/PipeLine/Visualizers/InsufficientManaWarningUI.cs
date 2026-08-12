using PipeLine.Contexts;
using PipeLine.UnitMagic.Steps;
using UnityEngine;

namespace PipeLine.Visualizers
{
    public class InsufficientManaWarningUI : MonoBehaviour, IUnitMagicVisualizer
    {
        private void OnEnable()
        {
            if (!ManaCheckStep.Visualizers.Contains(this))
            {
                ManaCheckStep.Visualizers.Add(this);
            }
        }

        private void OnDisable()
        {
            ManaCheckStep.Visualizers.Remove(this);
        }

        public void ShowInsufficientManaWarning(UnitMagicContext context)
        {
            if (context == null || !context.ManaCheckFailed) return;

            Debug.LogWarning($"<color=orange>[InsufficientManaWarningUI]</color> Warning Popup: Insufficient Mana! (Required: {context.RequiredMana}, Current: {context.CurrentMana})");
        }

        public void ShowUnitChangeEffect(UnitMagicContext context) { }
    }
}
