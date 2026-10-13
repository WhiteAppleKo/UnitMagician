using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace PickupSystem
{
    /// <summary>
    /// 화면 하단 중앙 "[E] 줍기" 프롬프트(UI Toolkit)입니다. RuntimeDataPickupFocus.OnFocusChanged를 구독해
    /// CurrentTarget이 있을 때만 표시하며, 자체 계산 로직은 없습니다.
    /// </summary>
    public class PickupPromptUIComponent : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private string promptPanelName = "PickupPromptPanel";

        private VisualElement promptPanel;
        private RuntimeDataPickupFocus focus;

        [Inject]
        public void Construct(RuntimeDataPickupFocus focus)
        {
            if (this.focus != null)
            {
                this.focus.OnFocusChanged -= HandleFocusChanged;
            }

            this.focus = focus;
            this.focus.OnFocusChanged += HandleFocusChanged;
        }

        private void OnEnable()
        {
            BindUI();
        }

        // UIDocument의 rootVisualElement가 OnEnable 시점에 아직 준비되지 않을 수 있어 Start에서 한 번 더 바인딩한다
        // (UnitUnlockPopupUIComponent와 동일한 이유/패턴).
        private void Start()
        {
            BindUI();
        }

        private void BindUI()
        {
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();

            var root = uiDocument != null ? uiDocument.rootVisualElement : null;
            if (root == null) return;

            root.pickingMode = PickingMode.Ignore;
            promptPanel = root.Q<VisualElement>(promptPanelName);

            Refresh();
        }

        private void OnDestroy()
        {
            if (focus != null)
            {
                focus.OnFocusChanged -= HandleFocusChanged;
            }
        }

        private void HandleFocusChanged(IPickupTarget target)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (promptPanel == null) return;

            bool hasTarget = focus != null && focus.CurrentTarget != null;
            promptPanel.style.display = hasTarget ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
