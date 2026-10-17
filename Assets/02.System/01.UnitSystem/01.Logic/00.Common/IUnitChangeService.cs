using UnityEngine;

namespace UnitSystem
{
    public interface IUnitChangeService
    {
        bool ChangeUnit(GameObject targetObject, RuntimeDataUnit targetRuntimeData, PureDataUnit newUnitData, float newValue, CharacterSystem.RuntimeStatData casterStatData = null, PipeLine.UnitMagic.UnitMagicPipeLine pipeLine = null, GameObject casterGameObject = null);
    }
}
