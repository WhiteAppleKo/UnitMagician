using UnityEngine;
using UnityEngine.UIElements;

namespace UnitSystem
{
    /// <summary>
    /// 1인칭 및 3인칭 숄더뷰 다중 락온 조준선(크로스헤어) 및 락온 수량 표시를 담당하는 Visualizer입니다.
    /// </summary>
    public class MultiLockOnVisualizer : MonoBehaviour, IMultiLockOnVisualizer
    {
        [SerializeField] private UIDocument uiDocument;

        private VisualElement crosshairContainer;
        private Label lockOnCountLabel;

        private void Awake()
        {
            EnsureUI();
        }

        private void EnsureUI()
        {
            if (this == null) return;
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            if (uiDocument != null && uiDocument.rootVisualElement != null)
            {
                var root = uiDocument.rootVisualElement;
                crosshairContainer = root.Q<VisualElement>("MultiLockOnCrosshair");
                lockOnCountLabel = root.Q<Label>("MultiLockOnCountLabel");
            }
        }

        public void SetCrosshairVisible(bool visible)
        {
            EnsureUI();
            if (crosshairContainer != null)
            {
                crosshairContainer.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void UpdateLockOnCount(int count)
        {
            EnsureUI();
            if (lockOnCountLabel != null)
            {
                lockOnCountLabel.text = count > 0 ? $"Lock-On x{count}" : string.Empty;
                lockOnCountLabel.style.display = count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void PlayBatchCastEffect()
        {
            Debug.Log("<color=cyan>[MultiLockOnVisualizer] Batch Cast Effect Triggered!</color>");
        }
    }
}
