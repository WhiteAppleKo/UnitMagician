using UnityEngine;

namespace UnitSystem
{
    [CreateAssetMenu(fileName = "PureDataUnit_", menuName = "UnitSystem/PureDataUnit")]
    public class PureDataUnit : ScriptableObject
    {
        [Header("Common Metadata")]
        [SerializeField] private UnitType unitType;
        [SerializeField] private string unitName;
        [SerializeField] private int baseCost = 10;
        [SerializeField] private Sprite icon;
        [SerializeField] private UnitVisualApplicatorSO applicator;

        [Header("Volume / Liquid Specific Properties")]
        [SerializeField] private Material overlayMaterial;

        [Header("Mass Specific Properties")]
        [SerializeField] private float massScaleMultiplier = 1.0f;

        public UnitType UnitType => unitType;
        public string UnitName => unitName;
        public int BaseCost => baseCost;
        public Sprite Icon => icon;
        public IUnitVisualApplicator Applicator => applicator;

        public Material OverlayMaterial => overlayMaterial;
        public float MassScaleMultiplier => massScaleMultiplier;
    }
}
