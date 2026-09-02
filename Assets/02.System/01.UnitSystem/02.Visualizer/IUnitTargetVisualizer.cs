using UnityEngine;

namespace UnitSystem
{
    public interface IUnitTargetVisualizer
    {
        void ApplyMassScale(float mass, float scaleMultiplier);
        void ApplyVelocity(Vector3 direction, float speed);
        void ApplyMaterial(Material material);
        Vector3 InitialScale { get; }
        void ApplyVisuals(RuntimeDataUnit runtimeData);
    }
}
