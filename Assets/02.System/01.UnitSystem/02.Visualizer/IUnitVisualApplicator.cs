using UnityEngine;

namespace UnitSystem
{
    public interface IUnitVisualApplicator
    {
        void Apply(GameObject target, RuntimeDataUnit runtimeData);
    }

    public abstract class UnitVisualApplicatorSO : ScriptableObject, IUnitVisualApplicator
    {
        public abstract void Apply(GameObject target, RuntimeDataUnit runtimeData);
    }
}
