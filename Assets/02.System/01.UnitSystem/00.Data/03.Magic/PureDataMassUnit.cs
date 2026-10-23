using UnityEngine;

namespace UnitSystem
{
    /// <summary>질량(Mass) 마법 PureData입니다. 연출 에셋은 마법 단위로 지정합니다(같은 타입이라도 에셋마다 다른 VFX 가능).</summary>
    [CreateAssetMenu(fileName = "PureDataMassUnit_", menuName = "UnitSystem/PureData/Mass")]
    public class PureDataMassUnit : PureDataUnit
    {
        [Header("Mass Specific Properties")]
        [SerializeField] private float massScaleMultiplier = 1.0f; // 이 마법 1회의 스케일 배율. 대상에 걸린 Mass 항목들의 배율을 합산한 값이 대상 배율(CurrentMass)이 된다(32번)
        [SerializeField] private GameObject scaleChangeVfxPrefab;  // 스케일 변화 시 스폰할 VFX(비면 스폰 안 함)

        public override UnitType UnitType => UnitType.Mass;
        public float MassScaleMultiplier => massScaleMultiplier;
        public GameObject ScaleChangeVfxPrefab => scaleChangeVfxPrefab;
    }
}
