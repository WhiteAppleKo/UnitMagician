using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using CharacterSystem;

namespace TacticalCommandSystem
{
    /// <summary>
    /// 전술 명령 시스템의 화면 UI 연출을 담당하는 UI View 컴포넌트입니다.
    /// DLV 아키텍처 규칙에 따라 자체 계산 로직을 전혀 포함하지 않으며,
    /// 오직 RuntimeDataTacticalQueue 및 CharacterStatService의 이벤트를 구독하여 화면을 수동 갱신합니다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class TacticalCommandUIView : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private RuntimeDataTacticalQueue queueData;
        private ICharacterStatService statService;

        private VisualElement rootElement;
        private VisualElement tacticalHUDContainer;
        private Label tacticalTitleLabel;
        private Label estimatedManaLabel;
        private Label instructionGuideLabel;
        private VisualElement queueListContainer;

        private void Awake()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }
        }

        [Inject]
        public void Construct(RuntimeDataTacticalQueue queueData, ICharacterStatService statService = null)
        {
            UnbindEvents();
            this.queueData = queueData;
            this.statService = statService;
            BindEvents();

            InitUIElements();
            RefreshAll();
        }

        private void OnEnable()
        {
            InitUIElements();
            BindEvents();
            RefreshAll();
        }

        private void OnDisable()
        {
            UnbindEvents();
        }

        private void OnDestroy()
        {
            UnbindEvents();
        }

        private void InitUIElements()
        {
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null || uiDocument.rootVisualElement == null) return;

            rootElement = uiDocument.rootVisualElement;
            rootElement.pickingMode = PickingMode.Ignore;

            // 기존에 동적으로 생성된 컨테이너가 있으면 재사용, 없으면 생성
            tacticalHUDContainer = rootElement.Q<VisualElement>("TacticalHUDContainer");
            if (tacticalHUDContainer == null)
            {
                tacticalHUDContainer = new VisualElement { name = "TacticalHUDContainer", pickingMode = PickingMode.Ignore };
                tacticalHUDContainer.style.position = Position.Absolute;
                tacticalHUDContainer.style.top = 20;
                tacticalHUDContainer.style.left = Length.Percent(25);
                tacticalHUDContainer.style.right = Length.Percent(25);
                tacticalHUDContainer.style.alignItems = Align.Center;
                tacticalHUDContainer.style.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.85f);
                tacticalHUDContainer.style.borderTopWidth = 1;
                tacticalHUDContainer.style.borderBottomWidth = 1;
                tacticalHUDContainer.style.borderLeftWidth = 1;
                tacticalHUDContainer.style.borderRightWidth = 1;
                tacticalHUDContainer.style.borderTopColor = new Color(0.2f, 0.8f, 1f, 0.8f);
                tacticalHUDContainer.style.borderBottomColor = new Color(0.2f, 0.8f, 1f, 0.8f);
                tacticalHUDContainer.style.borderLeftColor = new Color(0.2f, 0.8f, 1f, 0.8f);
                tacticalHUDContainer.style.borderRightColor = new Color(0.2f, 0.8f, 1f, 0.8f);
                tacticalHUDContainer.style.borderTopLeftRadius = 10;
                tacticalHUDContainer.style.borderTopRightRadius = 10;
                tacticalHUDContainer.style.borderBottomLeftRadius = 10;
                tacticalHUDContainer.style.borderBottomRightRadius = 10;
                tacticalHUDContainer.style.paddingTop = 10;
                tacticalHUDContainer.style.paddingBottom = 10;
                tacticalHUDContainer.style.paddingLeft = 16;
                tacticalHUDContainer.style.paddingRight = 16;
                tacticalHUDContainer.style.display = DisplayStyle.None;

                tacticalTitleLabel = new Label("TACTICAL TIME STOP MODE") { name = "TacticalTitleLabel", pickingMode = PickingMode.Ignore };
                tacticalTitleLabel.style.fontSize = 18;
                tacticalTitleLabel.style.color = new Color(0.3f, 0.9f, 1f);
                tacticalTitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                tacticalTitleLabel.style.marginBottom = 6;
                tacticalHUDContainer.Add(tacticalTitleLabel);

                estimatedManaLabel = new Label("예상 마나 소모: 0 MP") { name = "EstimatedManaLabel", pickingMode = PickingMode.Ignore };
                estimatedManaLabel.style.fontSize = 14;
                estimatedManaLabel.style.color = new Color(1f, 0.85f, 0.3f);
                estimatedManaLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                estimatedManaLabel.style.marginBottom = 6;
                tacticalHUDContainer.Add(estimatedManaLabel);

                queueListContainer = new VisualElement { name = "QueueListContainer", pickingMode = PickingMode.Ignore };
                queueListContainer.style.flexDirection = FlexDirection.Row;
                queueListContainer.style.flexWrap = Wrap.Wrap;
                queueListContainer.style.justifyContent = Justify.Center;
                queueListContainer.style.marginBottom = 6;
                tacticalHUDContainer.Add(queueListContainer);

                instructionGuideLabel = new Label("[좌클릭] 타깃 예약  |  [우클릭] 예약 취소  |  [T키] 연쇄 격발") { name = "InstructionGuideLabel", pickingMode = PickingMode.Ignore };
                instructionGuideLabel.style.fontSize = 12;
                instructionGuideLabel.style.color = new Color(0.8f, 0.8f, 0.8f, 0.9f);
                tacticalHUDContainer.Add(instructionGuideLabel);

                rootElement.Add(tacticalHUDContainer);
            }
            else
            {
                tacticalTitleLabel = tacticalHUDContainer.Q<Label>("TacticalTitleLabel");
                estimatedManaLabel = tacticalHUDContainer.Q<Label>("EstimatedManaLabel");
                queueListContainer = tacticalHUDContainer.Q<VisualElement>("QueueListContainer");
                instructionGuideLabel = tacticalHUDContainer.Q<Label>("InstructionGuideLabel");
            }
        }

        private void BindEvents()
        {
            if (queueData != null)
            {
                queueData.OnTacticalModeStateChanged += HandleTacticalModeChanged;
                queueData.OnQueueChanged += HandleQueueChanged;
                queueData.OnEstimatedManaCostChanged += HandleEstimatedManaChanged;
            }
        }

        private void UnbindEvents()
        {
            if (queueData != null)
            {
                queueData.OnTacticalModeStateChanged -= HandleTacticalModeChanged;
                queueData.OnQueueChanged -= HandleQueueChanged;
                queueData.OnEstimatedManaCostChanged -= HandleEstimatedManaChanged;
            }
        }

        private void HandleTacticalModeChanged(bool isActive)
        {
            if (tacticalHUDContainer != null)
            {
                tacticalHUDContainer.style.display = isActive ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void HandleQueueChanged(IReadOnlyList<TacticalCommandEntry> queue)
        {
            if (queueListContainer == null) return;

            queueListContainer.Clear();

            if (queue == null || queue.Count == 0)
            {
                var emptyLabel = new Label("예약된 마법 없음") { pickingMode = PickingMode.Ignore };
                emptyLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                emptyLabel.style.fontSize = 12;
                queueListContainer.Add(emptyLabel);
                return;
            }

            for (int i = 0; i < queue.Count; i++)
            {
                var entry = queue[i];
                var badge = new VisualElement { pickingMode = PickingMode.Ignore };
                badge.style.flexDirection = FlexDirection.Row;
                badge.style.alignItems = Align.Center;
                badge.style.backgroundColor = new Color(0.15f, 0.2f, 0.3f, 0.9f);
                badge.style.borderTopWidth = 1;
                badge.style.borderBottomWidth = 1;
                badge.style.borderLeftWidth = 1;
                badge.style.borderRightWidth = 1;
                badge.style.borderTopColor = new Color(0.3f, 0.8f, 1f);
                badge.style.borderBottomColor = new Color(0.3f, 0.8f, 1f);
                badge.style.borderLeftColor = new Color(0.3f, 0.8f, 1f);
                badge.style.borderRightColor = new Color(0.3f, 0.8f, 1f);
                badge.style.borderTopLeftRadius = 4;
                badge.style.borderTopRightRadius = 4;
                badge.style.borderBottomLeftRadius = 4;
                badge.style.borderBottomRightRadius = 4;
                badge.style.paddingLeft = 6;
                badge.style.paddingRight = 6;
                badge.style.paddingTop = 3;
                badge.style.paddingBottom = 3;
                badge.style.marginLeft = 4;
                badge.style.marginRight = 4;

                var text = new Label($"#{entry.OrderIndex} {entry.MagicData?.UnitName} ({entry.ManaCost}MP)") { pickingMode = PickingMode.Ignore };
                text.style.color = Color.white;
                text.style.fontSize = 12;
                badge.Add(text);

                queueListContainer.Add(badge);
            }
        }

        private void HandleEstimatedManaChanged(int totalCost)
        {
            if (estimatedManaLabel != null)
            {
                int currentMana = statService != null ? statService.CurrentMP : 100;
                estimatedManaLabel.text = $"예상 마나 소모: {totalCost} / 현재 마나: {currentMana} MP";
                estimatedManaLabel.style.color = totalCost > currentMana ? new Color(1f, 0.3f, 0.3f) : new Color(1f, 0.85f, 0.3f);
            }
        }

        private void RefreshAll()
        {
            if (queueData != null)
            {
                HandleTacticalModeChanged(queueData.IsTacticalModeActive);
                HandleQueueChanged(queueData.CommandQueue);
                HandleEstimatedManaChanged(queueData.TotalEstimatedManaCost);
            }
        }
    }
}
