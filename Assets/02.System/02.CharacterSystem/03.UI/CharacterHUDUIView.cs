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
        private VisualElement mpPreviewFill;
        private Label mpPreviewLabel;
        private VisualElement staminaBar;
        private Label staminaLabel;
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
            mpPreviewFill = root.Q<VisualElement>("MPPreviewFill");
            mpPreviewLabel = root.Q<Label>("MPPreviewText");
            staminaBar = root.Q<VisualElement>("StaminaBarFill");
            staminaLabel = root.Q<Label>("StaminaText");
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
                statSystem.OnStaminaChanged += UpdateStaminaUI;
                statSystem.RuntimeData.OnManaUnlimitedChanged += HandleManaUnlimitedChanged;
                statSystem.RuntimeData.OnPreviewManaCostChanged += HandlePreviewManaCostChanged;
            }

            if (magicSlotSystem != null)
            {
                magicSlotSystem.OnMagicChanged += UpdateMagicUI;
            }

            if (timeSlowData?.FocusGauge != null)
            {
                timeSlowData.FocusGauge.OnValueChanged += UpdateFocusUI;
            }

            if (timeSlowData != null)
            {
                timeSlowData.OnGaugeOverrideChanged += HandleGaugeOverrideChanged;
            }
        }

        private void UnsubscribeEvents()
        {
            if (statSystem?.RuntimeData != null)
            {
                statSystem.RuntimeData.HP.OnValueChanged -= UpdateHPUI;
                statSystem.RuntimeData.MP.OnValueChanged -= UpdateMPUI;
                statSystem.OnStaminaChanged -= UpdateStaminaUI;
                statSystem.RuntimeData.OnManaUnlimitedChanged -= HandleManaUnlimitedChanged;
                statSystem.RuntimeData.OnPreviewManaCostChanged -= HandlePreviewManaCostChanged;
            }

            if (magicSlotSystem != null)
            {
                magicSlotSystem.OnMagicChanged -= UpdateMagicUI;
            }

            if (timeSlowData?.FocusGauge != null)
            {
                timeSlowData.FocusGauge.OnValueChanged -= UpdateFocusUI;
            }

            if (timeSlowData != null)
            {
                timeSlowData.OnGaugeOverrideChanged -= HandleGaugeOverrideChanged;
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
                UpdateStaminaUI(statSystem.Stamina, statSystem.MaxStamina);
                RefreshManaPreview();
            }

            if (magicSlotSystem != null)
            {
                UpdateMagicUI(magicSlotSystem.CurrentMagic);
            }

            if (timeSlowData?.FocusGauge != null)
            {
                UpdateFocusUI(timeSlowData.FocusGauge.CurrentValue, timeSlowData.FocusGauge.MaxValue);
            }

            if (timeSlowData != null && timeSlowData.IsGaugeOverride)
            {
                HandleGaugeOverrideChanged(true);
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

            // 현재 MP가 바뀌면 예상 비용 구간(바 오른쪽 끝)과 부족 판정도 다시 그린다.
            RefreshManaPreview();
        }

        /// <summary>스태미나 바(19-A). OnStaminaChanged를 받아 비율과 라벨만 갱신한다(계산 로직 없음).</summary>
        private void UpdateStaminaUI(float current, float max)
        {
            if (staminaBar != null)
            {
                float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
                staminaBar.style.width = Length.Percent(ratio * 100f);
            }

            if (staminaLabel != null)
            {
                staminaLabel.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            }
        }

        private void HandlePreviewManaCostChanged(int cost)
        {
            RefreshManaPreview();
        }

        /// <summary>
        /// 락온 큐의 예상 총비용 표시(13-C). RuntimeStatData.PreviewManaCost가 0보다 크면 현재 MP 바의 오른쪽 끝에서
        /// 비용만큼 겹쳐 칠하고 "-비용" 라벨을 보인다. 현재 MP보다 크면(부족) 바 전체를 붉게 칠하고 라벨에 LOW를 덧붙인다.
        /// 비용이 0이거나(큐 비움) 마나 무제한(3단계 이벤트: 실제 차감 0)이면 표시를 숨긴다.
        /// </summary>
        private void RefreshManaPreview()
        {
            if (mpPreviewFill == null && mpPreviewLabel == null) return;

            var data = statSystem?.RuntimeData;
            int cost = data != null ? data.PreviewManaCost : 0;
            bool show = data != null && cost > 0 && !data.IsManaUnlimited;

            DisplayStyle display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (mpPreviewFill != null) mpPreviewFill.style.display = display;
            if (mpPreviewLabel != null) mpPreviewLabel.style.display = display;
            if (!show)
            {
                if (mpPreviewLabel != null) mpPreviewLabel.text = string.Empty;
                return;
            }

            int current = data.MP.CurrentValue;
            int max = data.MP.MaxValue;
            bool insufficient = cost > current;

            if (mpPreviewFill != null)
            {
                float visibleCost = Mathf.Min(cost, current);
                float leftRatio = max > 0 ? Mathf.Clamp01((current - visibleCost) / max) : 0f;
                float widthRatio = max > 0 ? Mathf.Clamp01(visibleCost / max) : 0f;
                mpPreviewFill.style.left = Length.Percent(leftRatio * 100f);
                mpPreviewFill.style.width = Length.Percent(widthRatio * 100f);
                mpPreviewFill.EnableInClassList("mp-preview-fill--insufficient", insufficient);
            }

            if (mpPreviewLabel != null)
            {
                mpPreviewLabel.text = insufficient ? $"-{cost} LOW" : $"-{cost}";
                mpPreviewLabel.EnableInClassList("mp-preview-label--insufficient", insufficient);
            }
        }

        private void HandleManaUnlimitedChanged(bool isManaUnlimited)
        {
            if (isManaUnlimited)
            {
                if (mpBar != null)
                {
                    mpBar.style.width = Length.Percent(100f);
                }
            }
            else if (statSystem?.RuntimeData != null)
            {
                UpdateMPUI(statSystem.RuntimeData.MP.CurrentValue, statSystem.RuntimeData.MP.MaxValue);
            }

            // 마나 무제한이면 실제 차감이 0이므로 예상 비용 표시를 숨기고, 해제되면 다시 판단한다.
            RefreshManaPreview();
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

        /// <summary>
        /// 게이지 오버라이드 On/Off (09번 기획서 9.8절). 오버라이드가 켜지는 시점의 실제 FocusGauge가
        /// 부분값일 수 있으므로(8번 기획서의 MP 오버라이드와 동일한 이유), true 수신 시 실값과 무관하게
        /// 항상 100%로 강제 표시하고, false 수신 시 즉시 실제 값 기준으로 복귀합니다.
        /// </summary>
        private void HandleGaugeOverrideChanged(bool isOverride)
        {
            if (isOverride)
            {
                if (focusBar != null)
                {
                    focusBar.style.width = Length.Percent(100f);
                }
            }
            else if (timeSlowData?.FocusGauge != null)
            {
                UpdateFocusUI(timeSlowData.FocusGauge.CurrentValue, timeSlowData.FocusGauge.MaxValue);
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
