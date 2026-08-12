using PipeLine.CharacterDamage.Steps;
using PipeLine.Contexts;
using UnityEngine;

namespace PipeLine.Visualizers
{
    public class EvadeTextEffect : MonoBehaviour, IDamageVisualizer
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

        public void ShowDamageText(DamageContext context) { }

        public void ShowEvadeText(DamageContext context)
        {
            if (context == null || !context.IsEvaded) return;

            Debug.Log($"<color=yellow>[EvadeTextEffect]</color> EVADE text effect on {context.Victim?.name}");
        }
    }
}
