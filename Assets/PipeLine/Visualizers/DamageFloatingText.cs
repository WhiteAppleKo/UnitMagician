using PipeLine.CharacterDamage.Steps;
using PipeLine.Contexts;
using UnityEngine;

namespace PipeLine.Visualizers
{
    public class DamageFloatingText : MonoBehaviour, IDamageVisualizer
    {
        private void OnEnable()
        {
            if (!ApplyDamageStep.Visualizers.Contains(this))
            {
                ApplyDamageStep.Visualizers.Add(this);
            }
        }

        private void OnDisable()
        {
            ApplyDamageStep.Visualizers.Remove(this);
        }

        public void ShowDamageText(DamageContext context)
        {
            if (context == null) return;

            string critText = context.IsCritical ? " [CRITICAL!]" : "";
            Debug.Log($"<color=red>[DamageFloatingText]</color> Floating Text on {context.Victim?.name}: -{context.FinalDamage}{critText}");
        }

        public void ShowEvadeText(DamageContext context) { }
    }
}
