using UnityEngine;

namespace UnitSystem
{
    [CreateAssetMenu(fileName = "PureDataUnit_", menuName = "UnitSystem/PureDataUnit")]
    public class PureDataUnit : ScriptableObject
    {
        [Header("Common Metadata")]
        [SerializeField] private UnitType unitType;
        [SerializeField] private string unitName;
        [SerializeField, TextArea(2, 6)] private string description = string.Empty; // 인벤토리 목록/상세에만 노출(비면 설명 요소 미생성)
        [SerializeField] private int baseCost = 10;
        [SerializeField] private Sprite icon;
        [SerializeField] private UnitVisualApplicatorSO applicator;

        [Header("Volume / Liquid Specific Properties")]
        [SerializeField] private Material overlayMaterial;

        [Header("Mass Specific Properties")]
        [SerializeField] private float massScaleMultiplier = 1.0f;

        [Header("Temperature Specific Properties")]
        [SerializeField] private float temperatureTarget = 0f; // 음수 = 얼음(냉각) 방향, 양수 = 가열 방향

        public UnitType UnitType => unitType;
        public string UnitName => unitName;
        public string Description => description;
        public int BaseCost => baseCost;
        public Sprite Icon => icon;
        public IUnitVisualApplicator Applicator => applicator;

        public Material OverlayMaterial => overlayMaterial;
        public float MassScaleMultiplier => massScaleMultiplier;
        public float TemperatureTarget => temperatureTarget;
    }
}
