using PipeLine.Contexts;

namespace PipeLine.Visualizers
{
    public interface IUnitMagicVisualizer
    {
        void ShowInsufficientManaWarning(UnitMagicContext context);
        void ShowUnitChangeEffect(UnitMagicContext context);
    }
}
