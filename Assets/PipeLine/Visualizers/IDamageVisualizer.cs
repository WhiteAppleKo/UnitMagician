using PipeLine.Contexts;

namespace PipeLine.Visualizers
{
    public interface IDamageVisualizer
    {
        void ShowDamageText(DamageContext context);
        void ShowEvadeText(DamageContext context);
    }
}
