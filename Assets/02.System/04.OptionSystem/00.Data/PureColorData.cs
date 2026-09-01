using UnityEngine;

namespace UI.Data
{
    [CreateAssetMenu(fileName = "PureColorData", menuName = "UI/Data/ColorData")]
    public class PureColorData : ScriptableObject
    {
        [Header("Button Highlight Colors")]
        [SerializeField] private Color activeModeColor = new Color(0.2f, 0.6f, 0.9f, 1f);
        [SerializeField] private Color inactiveModeColor = new Color(0.2f, 0.2f, 0.25f, 1f);

        [Header("Tab Button Colors")]
        [SerializeField] private Color activeTabBgColor = new Color(0.2f, 0.2f, 0.28f, 1f);
        [SerializeField] private Color inactiveTabBgColor = new Color(0.14f, 0.14f, 0.19f, 1f);
        [SerializeField] private Color activeTabTextColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color inactiveTabTextColor = new Color(0.7f, 0.7f, 0.7f, 1f);

        public Color ActiveModeColor => activeModeColor;
        public Color InactiveModeColor => inactiveModeColor;
        public Color ActiveTabBgColor => activeTabBgColor;
        public Color InactiveTabBgColor => inactiveTabBgColor;
        public Color ActiveTabTextColor => activeTabTextColor;
        public Color InactiveTabTextColor => inactiveTabTextColor;
    }
}
