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
        private Label hpLabel;
        private Label mpLabel;
        private VisualElement currentMagicIcon;
        private Label currentMagicName;

        private CharacterStatSystem statSystem;
        private UnitMagicSlotSystem magicSlotSystem;

        private void Awake()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }
        }

        private void OnEnable()
        {
            if (statSystem != null)
            {
                BindElements();
                RefreshAll();
            }
        }

        private void Start()
        {
            if (statSystem != null)
            {
                BindElements();
                RefreshAll();
            }
        }

        [Inject]
        public void Construct(CharacterStatSystem statSystem, UnitMagicSlotSystem magicSlotSystem)
        {
            Initialize(statSystem, magicSlotSystem);
        }

        public void Initialize(CharacterStatSystem statSystem, UnitMagicSlotSystem magicSlotSystem)
        {
            this.statSystem = statSystem;
            this.magicSlotSystem = magicSlotSystem;

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
            hpLabel = root.Q<Label>("HPText");
            mpLabel = root.Q<Label>("MPText");
            currentMagicIcon = root.Q<VisualElement>("CurrentMagicIcon");
            currentMagicName = root.Q<Label>("CurrentMagicName");
        }

        private void SubscribeEvents()
        {
            if (statSystem?.RuntimeData != null)
            {
                statSystem.RuntimeData.HP.OnValueChanged += UpdateHPUI;
                statSystem.RuntimeData.MP.OnValueChanged += UpdateMPUI;
            }

            if (magicSlotSystem != null)
            {
                magicSlotSystem.OnMagicChanged += UpdateMagicUI;
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
        }

        private void UpdateHPUI(int current, int max)
        {
            if (hpBar != null)
            {
                float ratio = max > 0 ? (float)current / max : 0f;
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
                float ratio = max > 0 ? (float)current / max : 0f;
                mpBar.style.width = Length.Percent(ratio * 100f);
            }

            if (mpLabel != null)
            {
                mpLabel.text = $"{current} / {max}";
            }
        }

        private void UpdateMagicUI(PureDataUnit magicData)
        {
            if (magicData == null)
            {
                if (currentMagicName != null) currentMagicName.text = "None";
                if (currentMagicIcon != null) currentMagicIcon.style.backgroundImage = null;
                return;
            }

            if (currentMagicName != null)
            {
                currentMagicName.text = magicData.UnitName;
            }

            if (currentMagicIcon != null && magicData.Icon != null)
            {
                currentMagicIcon.style.backgroundImage = new StyleBackground(magicData.Icon);
            }
        }
    }
}
