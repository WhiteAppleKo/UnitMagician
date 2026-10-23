using UnityEngine;

namespace UnitSystem
{
    /// <summary>온도(Temperature) 마법 PureData입니다. 얼음 방향(음수) 연출 에셋은 마법 단위로 지정합니다.</summary>
    [CreateAssetMenu(fileName = "PureDataTemperatureUnit_", menuName = "UnitSystem/PureData/Temperature")]
    public class PureDataTemperatureUnit : PureDataUnit
    {
        [Header("Temperature Specific Properties")]
        [SerializeField] private float temperatureTarget = 0f; // 음수 = 얼음(냉각) 방향, 양수 = 가열 방향
        [SerializeField] private Material iceMaterial;          // 얼음 방향일 때 교체할 머티리얼(비면 교체 안 함)
        [SerializeField] private GameObject frostParticlePrefab; // 얼음 방향일 때 스폰할 서리 파티클(비면 스폰 안 함)

        public override UnitType UnitType => UnitType.Temperature;
        public float TemperatureTarget => temperatureTarget;
        public Material IceMaterial => iceMaterial;
        public GameObject FrostParticlePrefab => frostParticlePrefab;
    }
}
