using System.Collections.Generic;
using Synty.AnimationBaseLocomotion.Samples.InputSystem;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using VContainer;

namespace CharacterSystem
{
    /// <summary>
    /// 캐릭터 인벤토리 창(33번 기획서 §3-2). 창 틀(제목·드래그 영역·탭 바·내용 영역)만 그리고, 탭은
    /// <see cref="IInventoryTabRegistry"/>에 등록된 <see cref="IInventoryTabView"/>들로 만든다(등록 순서대로 탭 버튼, 하드코딩된 탭 없음).
    /// 탭 내용은 선택된 탭 뷰가 내용 영역에 직접 그린다.
    ///
    /// 열기·닫기·시간정지 연동·입력 컨텍스트 전환은 기존 인벤토리 UI 컴포넌트(33번에서 삭제)의 동작을 그대로 옮겼다:
    /// 키 입력(기본 I, InputReader.onInventoryToggled)으로 열고 닫는 메뉴이며, 열려 있는 동안 일시정지를 요청하고
    /// UI 입력 컨텍스트를 올린다. 이벤트(3단계 감속) 진행 중에는 열지 않는다.
    ///
    /// 가변형 UI(UI Toolkit 가이드 §3): 제목 영역을 끌면 style.translate로 창이 따라 움직이고, 패널 경계 밖으로 나가지 않으며,
    /// 끌기 시작하면 창이 맨 앞으로 온다(BringToFront).
    /// </summary>
    public class InventoryUIComponent : MonoBehaviour, IInventoryTabRegistry
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private string windowElementName = "InventoryContainer";
        [SerializeField] private string titleBarElementName = "InventoryTitleBar";
        [SerializeField] private string tabBarElementName = "InventoryTabBar";
        [SerializeField] private string contentElementName = "InventoryContent";

        private static readonly Color SelectedTabBackground = new Color(1f, 0.85f, 0.2f, 1f);
        private static readonly Color SelectedTabText = Color.black;
        private static readonly Color DefaultTabBackground = new Color(0.16f, 0.16f, 0.2f, 0.95f);
        private static readonly Color DefaultTabText = new Color(0.85f, 0.85f, 0.85f, 1f);
        private static readonly Color TabBorderColor = new Color(0.6f, 0.6f, 0.6f, 1f);

        private readonly List<IInventoryTabView> tabs = new List<IInventoryTabView>();
        private readonly Dictionary<IInventoryTabView, Button> tabButtons = new Dictionary<IInventoryTabView, Button>();
        private IInventoryTabView selectedTab;

        private VisualElement window;
        private VisualElement titleBar;
        private VisualElement tabBar;
        private VisualElement content;

        // 이벤트(3단계 감속) 중 인벤토리 열기 차단용(선택적 주입 - 미주입이면 차단 없이 기존 동작).
        private RuntimeDataTimeSlow timeSlowData;
        // 17번 기획서: Time.timeScale은 직접 쓰지 않고 일시정지 요청만 올리고 내린다(요청자 집합으로 멱등 관리).
        private ITimeScaleService timeScaleService;
        private InputReader m_inputReader;
        private Common.InputSystem.IInputContextManager m_inputContextManager;

        private bool m_isOpen = false;

        // 드래그 상태
        private bool isDragging;
        private int dragPointerId = -1;
        private Vector2 dragStartPointer;
        private Vector2 dragStartTranslate;

        public bool IsOpen => m_isOpen;
        public IReadOnlyList<IInventoryTabView> Tabs => tabs;
        public IInventoryTabView SelectedTab => selectedTab;

        [Inject]
        public void Construct(
            InputReader inputReader = null,
            Common.InputSystem.IInputContextManager contextManager = null,
            RuntimeDataTimeSlow timeSlowData = null,
            ITimeScaleService timeScaleService = null)
        {
            UnbindEvents();

            m_inputReader = inputReader;
            m_inputContextManager = contextManager;
            this.timeSlowData = timeSlowData;
            this.timeScaleService = timeScaleService;

            BindEvents();
        }

        private void OnEnable()
        {
            BindEvents();
            InitContainer();
        }

        private void Start()
        {
            InitContainer();
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
            if (m_inputReader != null)
            {
                m_inputReader.onInventoryToggled -= ToggleInventory;
                m_inputReader.onInventoryToggled += ToggleInventory;
            }
        }

        private void UnbindEvents()
        {
            if (m_inputReader != null)
            {
                m_inputReader.onInventoryToggled -= ToggleInventory;
            }
        }

        private void InitContainer()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            if (uiDocument == null || uiDocument.rootVisualElement == null) return;

            var root = uiDocument.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;

            var newWindow = root.Q<VisualElement>(windowElementName);
            if (newWindow == window && window != null) return;

            window = newWindow;
            titleBar = window?.Q<VisualElement>(titleBarElementName);
            tabBar = window?.Q<VisualElement>(tabBarElementName);
            content = window?.Q<VisualElement>(contentElementName);

            if (titleBar != null)
            {
                titleBar.RegisterCallback<PointerDownEvent>(OnTitlePointerDown);
                titleBar.RegisterCallback<PointerMoveEvent>(OnTitlePointerMove);
                titleBar.RegisterCallback<PointerUpEvent>(OnTitlePointerUp);
                titleBar.RegisterCallback<PointerCaptureOutEvent>(OnTitlePointerCaptureOut);
            }

            RebuildTabBar();
            if (m_isOpen) ShowSelectedTab();
        }

        // ───────────────────────── 탭 레지스트리 ─────────────────────────

        public bool RegisterTab(IInventoryTabView tab)
        {
            if (tab == null || tabs.Contains(tab)) return false;

            for (int i = 0; i < tabs.Count; i++)
            {
                if (tabs[i].ItemType == tab.ItemType)
                {
                    Debug.LogWarning($"[InventoryUIComponent] ItemType {tab.ItemType} 탭이 이미 등록돼 있어 '{tab.TabName}' 탭을 등록하지 않습니다.");
                    return false;
                }
            }

            tabs.Add(tab);
            if (selectedTab == null) selectedTab = tab;

            RebuildTabBar();
            if (m_isOpen && selectedTab == tab) ShowSelectedTab();
            return true;
        }

        public void UnregisterTab(IInventoryTabView tab)
        {
            if (tab == null || !tabs.Remove(tab)) return;

            if (selectedTab == tab)
            {
                if (m_isOpen) tab.Hide();
                ClearContent();
                selectedTab = tabs.Count > 0 ? tabs[0] : null;
                if (m_isOpen) ShowSelectedTab();
            }

            RebuildTabBar();
        }

        /// <summary>등록된 탭 하나를 선택해 내용 영역을 그 탭에 맡긴다.</summary>
        public void SelectTab(IInventoryTabView tab)
        {
            if (tab == null || !tabs.Contains(tab) || tab == selectedTab) return;

            if (m_isOpen) selectedTab?.Hide();
            selectedTab = tab;
            RefreshTabButtonStyles();
            if (m_isOpen) ShowSelectedTab();
        }

        private void RebuildTabBar()
        {
            if (tabBar == null) return;

            tabBar.Clear();
            tabButtons.Clear();

            for (int i = 0; i < tabs.Count; i++)
            {
                var tab = tabs[i];
                var button = new Button(() => SelectTab(tab));
                button.name = $"InventoryTab_{tab.ItemType}";
                button.text = tab.TabName;
                button.style.marginLeft = 0;
                button.style.marginRight = 6;
                button.style.paddingLeft = 14;
                button.style.paddingRight = 14;
                button.style.paddingTop = 4;
                button.style.paddingBottom = 4;
                button.style.fontSize = 13;
                button.style.unityFontStyleAndWeight = FontStyle.Bold;
                button.style.borderTopWidth = 1;
                button.style.borderBottomWidth = 1;
                button.style.borderLeftWidth = 1;
                button.style.borderRightWidth = 1;
                button.style.borderTopColor = TabBorderColor;
                button.style.borderBottomColor = TabBorderColor;
                button.style.borderLeftColor = TabBorderColor;
                button.style.borderRightColor = TabBorderColor;
                button.style.borderTopLeftRadius = 6;
                button.style.borderTopRightRadius = 6;
                button.style.borderBottomLeftRadius = 6;
                button.style.borderBottomRightRadius = 6;

                tabButtons.Add(tab, button);
                tabBar.Add(button);
            }

            RefreshTabButtonStyles();
        }

        private void RefreshTabButtonStyles()
        {
            foreach (var pair in tabButtons)
            {
                bool selected = pair.Key == selectedTab;
                pair.Value.style.backgroundColor = new StyleColor(selected ? SelectedTabBackground : DefaultTabBackground);
                pair.Value.style.color = new StyleColor(selected ? SelectedTabText : DefaultTabText);
            }
        }

        private void ShowSelectedTab()
        {
            if (content == null) return;

            ClearContent();
            selectedTab?.Show(content);
        }

        private void ClearContent()
        {
            content?.Clear();
        }

        // ───────────────────────── 열기 / 닫기 ─────────────────────────

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
            // 13번 기획서 §3 "이벤트 중 차단": 이벤트(3단계 감속) 진행 중에는 인벤토리를 열지 않는다.
            // (이미 열려 있는 경우의 닫기는 CloseInventory가 그대로 허용한다.)
            if (timeSlowData != null && timeSlowData.IsEventTriggered) return;

            m_isOpen = true;

            InitContainer();
            ShowSelectedTab();

            if (window != null)
            {
                window.style.display = DisplayStyle.Flex;
                // 화면 크기가 바뀌었을 수 있으므로 다음 레이아웃 뒤 경계 안으로 되돌린다.
                window.schedule.Execute(ClampWindowIntoPanel);
            }

            timeScaleService?.SetPaused(this, true);

            var manager = m_inputContextManager;
            manager?.PushContext(manager.UIContext);
        }

        public void CloseInventory()
        {
            m_isOpen = false;
            EndDrag();

            selectedTab?.Hide();
            ClearContent();

            if (window != null)
            {
                window.style.display = DisplayStyle.None;
            }

            timeScaleService?.SetPaused(this, false);

            var manager = m_inputContextManager;
            manager?.PopContext(manager.UIContext);
        }

        // ───────────────────────── 드래그(가변형 UI) ─────────────────────────

        private void OnTitlePointerDown(PointerDownEvent evt)
        {
            if (window == null || evt.button != 0) return;

            isDragging = true;
            dragPointerId = evt.pointerId;
            dragStartPointer = evt.position;
            dragStartTranslate = CurrentTranslate();

            window.BringToFront();
            titleBar.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnTitlePointerMove(PointerMoveEvent evt)
        {
            if (!isDragging || evt.pointerId != dragPointerId) return;

            Vector2 delta = (Vector2)evt.position - dragStartPointer;
            SetClampedTranslate(dragStartTranslate + delta);
            evt.StopPropagation();
        }

        private void OnTitlePointerUp(PointerUpEvent evt)
        {
            if (!isDragging || evt.pointerId != dragPointerId) return;

            EndDrag();
            evt.StopPropagation();
        }

        private void OnTitlePointerCaptureOut(PointerCaptureOutEvent evt)
        {
            isDragging = false;
            dragPointerId = -1;
        }

        private void EndDrag()
        {
            if (titleBar != null && dragPointerId >= 0 && titleBar.HasPointerCapture(dragPointerId))
            {
                titleBar.ReleasePointer(dragPointerId);
            }

            isDragging = false;
            dragPointerId = -1;
        }

        private Vector2 CurrentTranslate()
        {
            if (window == null) return Vector2.zero;
            Vector3 translate = window.resolvedStyle.translate;
            return new Vector2(translate.x, translate.y);
        }

        private void ClampWindowIntoPanel()
        {
            SetClampedTranslate(CurrentTranslate());
        }

        /// <summary>원하는 translate를 패널 경계 안에 들어오도록 제한해 적용한다(창 전체가 화면 안에 남음).</summary>
        private void SetClampedTranslate(Vector2 desired)
        {
            if (window == null || window.panel == null) return;

            Rect bounds = window.panel.visualTree.worldBound;
            Rect current = window.worldBound;
            Vector2 currentTranslate = CurrentTranslate();
            if (float.IsNaN(current.width) || float.IsNaN(bounds.width)) return;

            // translate 0일 때의 창 위치(현재 위치 - 현재 translate)를 기준으로 허용 범위를 계산한다.
            Vector2 untranslatedMin = current.min - currentTranslate;
            float minX = bounds.xMin - untranslatedMin.x;
            float maxX = bounds.xMax - current.width - untranslatedMin.x;
            float minY = bounds.yMin - untranslatedMin.y;
            float maxY = bounds.yMax - current.height - untranslatedMin.y;

            float x = maxX < minX ? minX : Mathf.Clamp(desired.x, minX, maxX);
            float y = maxY < minY ? minY : Mathf.Clamp(desired.y, minY, maxY);

            window.style.translate = new Translate(x, y);
        }
    }
}
