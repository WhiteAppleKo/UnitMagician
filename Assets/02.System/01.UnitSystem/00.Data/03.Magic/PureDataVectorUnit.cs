using UnityEngine;

namespace UnitSystem
{
    /// <summary>벡터(Vector) 마법 PureData입니다(범위 밖 보존용 - 특화 필드 없음, 씬에서 접근 불가).</summary>
    [CreateAssetMenu(fileName = "PureDataVectorUnit_", menuName = "UnitSystem/PureData/Vector")]
    public class PureDataVectorUnit : PureDataUnit
    {
        public override UnitType UnitType => UnitType.Vector;
    }
}
