using UnityEngine;
using UnityEngine.UIElements;
using UnitSystem;
using VContainer;

namespace CharacterSystem
{
    [RequireComponent(typeof(UIDocument))]
    public class CharacterHUDUIView : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private VisualElement hpBar;
        private VisualElement mpBar;
        private VisualElement focusBar;
        private Label hpLabel;
        private Label mpLabel;
        private Label focusLabel;
        private VisualElement currentMagicIcon;
        private Label currentMagicName;

        private ICharacterStatService statSystem;
        private IUnitMagicSlotService magicSlotSystem;
        private RuntimeDataTimeSlow timeSlowData;

        private void Awake()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }
        }

        private void OnEnable()
        {
            if (statSystem != null || timeSlowData != null || magicSlotSystem != null)
            {
                BindElements();
                RefreshAll();
            }
        }

        private void Start()
        {
            if (statSystem != null || timeSlowData != null || magicSlotSystem != null)
            {
                BindElements();
                RefreshAll();
            }
        }

        [Inject]
        public void Construct(
            ICharacterStatService statSystem,
            IUnitMagicSlotService magicSlotSystem,
            RuntimeDataTimeSlow timeSlowData = null)
        {
            Initialize(statSystem, magicSlotSystem, timeSlowData);
        }

        public void Initialize(
            ICharacterStatService statSystem,
            IUnitMagicSlotService magicSlotSystem,
            RuntimeDataTimeSlow timeSlowData = null)
        {
            this.statSystem = statSystem;
            this.magicSlotSystem = magicSlotSystem;
            this.timeSlowData = timeSlowData;

            SubscribeEvents();
            BindElements();
            RefreshAll();
        }

        private void BindElements()
        {
            if (uiDocument == null || uiDocument.rootVisualElement == null) return;

            var root = uiDocument.rootVisualElement;

            // UI - 백그라운드 클릭 방지 레이아웃 설정
            root.pickingMode = PickingMode.Ignore;

            hpBar = root.Q<VisualElement>("HPBarFill");
            mpBar = root.Q<VisualElement>("MPBarFill");
            focusBar = root.Q<VisualElement>("FocusBarFill");
            hpLabel = root.Q<Label>("HPText");
            mpLabel = root.Q<Label>("MPText");
            focusLabel = root.Q<Label>("FocusText");
            currentMagicIcon = root.Q<VisualElement>("CurrentMagicIcon");
            currentMagicName = root.Q<Label>("CurrentMagicName");
        }

        private void SubscribeEvents()
        {
            UnsubscribeEvents();

            if (statSystem?.RuntimeData != null)
            {
                statSystem.RuntimeData.HP.OnValueChanged += UpdateHPUI;
                statSystem.RuntimeData.MP.OnValueChanged += UpdateMPUI;
            }

            if (magicSlotSystem != null)
            {
                magicSlotSystem.OnMagicChanged += UpdateMagicUI;
            }

            if (timeSlowData?.FocusGauge != null)
            {
                timeSlowData.FocusGauge.OnValueChanged += UpdateFocusUI;
            }
        }

        private void UnsubscribeEvents()
        {
            if (statSystem?.RuntimeData != null)
            {
                statSystem.RuntimeData.HP.OnValueChanged -= UpdateHPUI;
                statSystem.RuntimeData.MP.OnValueChanged -= UpdateMPUI;
            }

            if (magicSlotSystem != null)
            {
                magicSlotSystem.OnMagicChanged -= UpdateMagicUI;
            }

            if (timeSlowData?.FocusGauge != null)
            {
                timeSlowData.FocusGauge.OnValueChanged -= UpdateFocusUI;
            }
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void RefreshAll()
        {
            if (statSystem?.RuntimeData != null)
            {
                UpdateHPUI(statSystem.RuntimeData.HP.CurrentValue, statSystem.RuntimeData.HP.MaxValue);
                UpdateMPUI(statSystem.RuntimeData.MP.CurrentValue, statSystem.RuntimeData.MP.MaxValue);
            }

            if (magicSlotSystem != null)
            {
                UpdateMagicUI(magicSlotSystem.CurrentMagic);
            }

            if (timeSlowData?.FocusGauge != null)
            {
                UpdateFocusUI(timeSlowData.FocusGauge.CurrentValue, timeSlowData.FocusGauge.MaxValue);
            }
        }

        private void UpdateHPUI(int current, int max)
        {
            if (hpBar != null)
            {
                float ratio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
                hpBar.style.width = Length.Percent(ratio * 100f);
            }

            if (hpLabel != null)
            {
                hpLabel.text = $"{current} / {max}";
            }
        }

        private void UpdateMPUI(int current, int max)
        {
            if (mpBar != null)
            {
                float ratio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
                mpBar.style.width = Length.Percent(ratio * 100f);
            }

            if (mpLabel != null)
            {
                mpLabel.text = $"{current} / {max}";
            }
        }

        private void UpdateFocusUI(int current, int max)
        {
            if (focusBar != null)
            {
                float ratio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
                focusBar.style.width = Length.Percent(ratio * 100f);
            }

            if (focusLabel != null)
            {
                focusLabel.text = $"{current} / {max}";
            }
        }

        private void UpdateMagicUI(PureDataUnit magicData)
        {
            if (magicData == null)
            {
                if (currentMagicName != null) currentMagicName.text = "None";
                if (currentMagicIcon != null) currentMagicIcon.style.backgroundImage = StyleKeyword.Null;
                return;
            }

            if (currentMagicName != null)
            {
                currentMagicName.text = magicData.UnitName;
            }

            if (currentMagicIcon != null)
            {
                currentMagicIcon.style.backgroundImage = magicData.Icon != null
                    ? new StyleBackground(magicData.Icon)
                    : StyleKeyword.Null;
            }
        }
    }
}
