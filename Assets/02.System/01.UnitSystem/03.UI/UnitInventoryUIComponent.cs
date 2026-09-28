using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using VContainer;
using Synty.AnimationBaseLocomotion.Samples.InputSystem;

namespace UnitSystem
{
    /// <summary>
    /// 소유 중인 유닛(마법) 인벤토리를 아이콘 그리드로 표시하고, 클릭으로 장착/해제를 토글하는 UI 컴포넌트입니다.
    /// 장착된 유닛은 강조 테두리, 미장착이지만 장착 가능한 유닛은 기본 테두리,
    /// 조건 미달로 장착 불가능한 유닛(현재는 스텁 체커라 발생하지 않음)은 회색+비활성으로 표시합니다.
    ///
    /// 다크소울3 레퍼런스에 맞춰 상시 표시 HUD가 아니라, 키 입력(기본 I)으로 열고 닫는 메뉴로 동작합니다.
    /// (Assets/02.System/04.OptionSystem/02.Visualizer/UI/OptionUIComponent.cs의 여닫는 메뉴 패턴을 그대로 따름)
    /// </summary>
    public class UnitInventoryUIComponent : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private string containerElementName = "InventoryContainer";

        private static readonly Color EquippedBorderColor = new Color(1f, 0.85f, 0.2f, 1f);
        private static readonly Color DefaultBorderColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        private static readonly Color DisabledTint = new Color(0.35f, 0.35f, 0.35f, 0.8f);
        private static readonly Color EnabledTint = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        private VisualElement inventoryContainer;
        private IUnitCatalogService catalogService;
        private IUnitEquipRequirementChecker requirementChecker;
        private InputReader m_inputReader;
        private Common.InputSystem.IInputContextManager m_inputContextManager;

        private bool m_isOpen = false;

        // 인벤토리 메뉴가 씬에 둘 이상 존재해 동시에 InputReader.onInventoryToggled를 구독하는 경우에도,
        // Time.timeScale은 전역 자원이므로 인스턴스별 캐시가 아니라 정적 참조 카운트로 관리해
        // 마지막으로 닫힌 시점에만 원래 값으로 복원한다.
        // OptionUIComponent의 s_pauseRefCount/s_cachedTimeScale과는 별개의 정적 필드로 관리한다
        // (두 메뉴를 동시에 열 때의 Time.timeScale 상호작용은 이번 범위 밖의 알려진 한계).
        private static int s_pauseRefCount = 0;
        private static float s_cachedTimeScale = 1f;

        public bool IsOpen => m_isOpen;

        [Inject]
        public void Construct(
            IUnitCatalogService catalogService,
            IUnitEquipRequirementChecker requirementChecker = null,
            InputReader inputReader = null,
            Common.InputSystem.IInputContextManager contextManager = null)
        {
            UnbindEvents();

            this.catalogService = catalogService;
            this.requirementChecker = requirementChecker;
            m_inputReader = inputReader;
            m_inputContextManager = contextManager;

            BindEvents();
            RefreshInventory();
        }

        private void OnEnable()
        {
            BindEvents();
            InitContainer();
            RefreshInventory();
        }

        private void Start()
        {
            InitContainer();
            RefreshInventory();
            CloseInventory();
        }

        private void Update()
        {
            // InputReader 미주입 시 폴백 처리
            if (m_inputReader == null && Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
            {
                ToggleInventory();
            }
        }

        private void InitContainer()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            if (uiDocument != null && uiDocument.rootVisualElement != null)
            {
                uiDocument.rootVisualElement.pickingMode = PickingMode.Ignore;
                inventoryContainer = uiDocument.rootVisualElement.Q<VisualElement>(containerElementName);
            }
        }

        private void OnDisable()
        {
            UnbindEvents();
        }

        private void OnDestroy()
        {
            UnbindEvents();
        }

        private void BindEvents()
        {
            if (catalogService != null)
            {
                catalogService.OnUnitAcquired -= HandleCatalogChanged;
                catalogService.OnUnitAcquired += HandleCatalogChanged;
                catalogService.OnUnitEquipped -= HandleCatalogChanged;
                catalogService.OnUnitEquipped += HandleCatalogChanged;
                catalogService.OnUnitUnequipped -= HandleCatalogChanged;
                catalogService.OnUnitUnequipped += HandleCatalogChanged;
            }

            if (m_inputReader != null)
            {
                m_inputReader.onInventoryToggled -= ToggleInventory;
                m_inputReader.onInventoryToggled += ToggleInventory;
            }
        }

        private void UnbindEvents()
        {
            if (catalogService != null)
            {
                catalogService.OnUnitAcquired -= HandleCatalogChanged;
                catalogService.OnUnitEquipped -= HandleCatalogChanged;
                catalogService.OnUnitUnequipped -= HandleCatalogChanged;
            }

            if (m_inputReader != null)
            {
                m_inputReader.onInventoryToggled -= ToggleInventory;
            }
        }

        private void HandleCatalogChanged(PureDataUnit unitData)
        {
            RefreshInventory();
        }

        public void RefreshInventory()
        {
            if (catalogService == null) return;
            if (inventoryContainer == null) return;

            var possessedUnits = catalogService.GetPossessedUnits();
            Debug.Log($"[UnitInventoryUIComponent] Inventory Refreshed. Possessed Count: {possessedUnits.Count}");

            inventoryContainer.Clear();
            foreach (var unitData in possessedUnits)
            {
                if (unitData == null) continue;

                inventoryContainer.Add(BuildUnitElement(unitData));
            }
        }

        private VisualElement BuildUnitElement(PureDataUnit unitData)
        {
            bool isEquipped = catalogService.IsEquipped(unitData);
            bool canEquip = isEquipped || requirementChecker == null || requirementChecker.CanEquip(unitData, out _);

            var unitElement = new VisualElement();
            unitElement.style.width = 48;
            unitElement.style.height = 48;
            unitElement.style.marginRight = 8;
            unitElement.style.marginBottom = 8;
            unitElement.style.backgroundColor = new StyleColor(canEquip ? EnabledTint : DisabledTint);
            unitElement.style.opacity = canEquip ? 1f : 0.5f;

            unitElement.style.borderTopLeftRadius = 4;
            unitElement.style.borderTopRightRadius = 4;
            unitElement.style.borderBottomLeftRadius = 4;
            unitElement.style.borderBottomRightRadius = 4;

            Color borderColor = isEquipped ? EquippedBorderColor : DefaultBorderColor;
            float borderWidth = isEquipped ? 3f : 1f;
            unitElement.style.borderTopWidth = borderWidth;
            unitElement.style.borderBottomWidth = borderWidth;
            unitElement.style.borderLeftWidth = borderWidth;
            unitElement.style.borderRightWidth = borderWidth;
            unitElement.style.borderTopColor = borderColor;
            unitElement.style.borderBottomColor = borderColor;
            unitElement.style.borderLeftColor = borderColor;
            unitElement.style.borderRightColor = borderColor;

            if (unitData.Icon != null)
            {
                unitElement.style.backgroundImage = new StyleBackground(unitData.Icon);
            }

            PureDataUnit captureUnit = unitData;
            unitElement.RegisterCallback<ClickEvent>(_ => OnUnitClicked(captureUnit));

            return unitElement;
        }

        private void OnUnitClicked(PureDataUnit unitData)
        {
            if (catalogService == null || unitData == null) return;

            if (catalogService.IsEquipped(unitData))
            {
                catalogService.UnequipUnit(unitData);
                return;
            }

            if (!catalogService.TryEquipUnit(unitData, out var failReason))
            {
                Debug.LogWarning($"[UnitInventoryUIComponent] Cannot equip {unitData.name}: {failReason}");
            }
        }

        public void ToggleInventory()
        {
            if (m_isOpen)
            {
                CloseInventory();
            }
            else
            {
                OpenInventory();
            }
        }

        public void OpenInventory()
        {
            bool wasOpen = m_isOpen;
            m_isOpen = true;

            RefreshInventory();

            if (inventoryContainer != null)
            {
                inventoryContainer.style.display = DisplayStyle.Flex;
            }

            if (!wasOpen)
            {
                if (s_pauseRefCount == 0)
                {
                    s_cachedTimeScale = Time.timeScale;
                }
                s_pauseRefCount++;
            }

            Time.timeScale = 0f;

            var manager = m_inputContextManager;
            manager?.PushContext(manager.UIContext);
        }

        public void CloseInventory()
        {
            bool wasOpen = m_isOpen;
            m_isOpen = false;

            if (inventoryContainer != null)
            {
                inventoryContainer.style.display = DisplayStyle.None;
            }

            if (wasOpen)
            {
                s_pauseRefCount = Mathf.Max(0, s_pauseRefCount - 1);
            }

            if (s_pauseRefCount == 0)
            {
                Time.timeScale = s_cachedTimeScale;
            }

            var manager = m_inputContextManager;
            manager?.PopContext(manager.UIContext);
        }
    }
}
