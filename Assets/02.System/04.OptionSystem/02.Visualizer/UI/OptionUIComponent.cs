using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using VContainer;
using CameraMovement;
using UI.Data;
using Synty.AnimationBaseLocomotion.Samples.InputSystem;
using Common.InputSystem;

namespace OptionSystem
{
    public enum OptionSceneContext
    {
        InGame = 0,
        Lobby = 1
    }

    public enum OptionTabType
    {
        General = 0,
        Camera = 1,
        Sound = 2,
        Graphics = 3
    }

    public class OptionUIComponent : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private OptionSceneContext sceneContext = OptionSceneContext.InGame;
        [SerializeField] private PureColorData colorData;
        private ICameraFollowService m_cameraFollowService;
        private InputReader m_inputReader;
        private VisualElement m_optionPanel;
        
        // Tab Buttons
        private Button m_tabGeneralBtn;
        private Button m_tabCameraBtn;
        private Button m_tabSoundBtn;
        private Button m_tabGraphicsBtn;

        // Content Panels
        private VisualElement m_contentGeneral;
        private VisualElement m_contentCamera;
        private VisualElement m_contentSound;
        private VisualElement m_contentGraphics;

        // Action Buttons
        private Button m_resumeBtn;
        private Button m_lobbyBtn;
        private Button m_quitBtn;
        private Button m_closeBtn;

        private OptionTabType m_currentTab = OptionTabType.General;
        private bool m_isOpen = false;

        // 17번 기획서: Time.timeScale은 직접 쓰지 않고 일시정지 요청만 올리고 내린다(요청자 집합으로 멱등 관리).
        private CharacterSystem.ITimeScaleService m_timeScaleService;

        private IInputContextManager m_inputContextManager;
        private IInputContextManager ContextManager => m_inputContextManager;

        public bool IsOpen => m_isOpen;

        [Inject]
        public void Construct(
            ICameraFollowService cameraFollowService = null,
            InputReader inputReader = null,
            Common.InputSystem.IInputContextManager contextManager = null,
            CharacterSystem.ITimeScaleService timeScaleService = null)
        {
            m_cameraFollowService = cameraFollowService;
            m_inputReader = inputReader;
            m_inputContextManager = contextManager;
            m_timeScaleService = timeScaleService;

            if (isActiveAndEnabled)
            {
                SubscribeEvents();
            }
        }

        public void Initialize(ICameraFollowService cameraFollowService, InputReader inputReader = null)
        {
            UnsubscribeEvents();
            m_cameraFollowService = cameraFollowService;
            m_inputReader = inputReader;

            if (isActiveAndEnabled)
            {
                SubscribeEvents();
            }
        }

        private void OnEnable()
        {
            RegisterButtonCallbacks();
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            if (m_inputReader != null)
            {
                m_inputReader.onOptionToggled -= ToggleOption;
                m_inputReader.onOptionToggled += ToggleOption;
            }
        }

        private void UnsubscribeEvents()
        {
            if (m_inputReader != null)
            {
                m_inputReader.onOptionToggled -= ToggleOption;
            }
        }

        private void Start()
        {
            RegisterButtonCallbacks();
            ApplySceneContext(sceneContext);
            CloseOption();
        }

        private void Update()
        {
            // InputReader 미주입 시 폴백 처리
            if (m_inputReader == null && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ToggleOption();
            }
        }

        private void RegisterButtonCallbacks()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            if (uiDocument != null && uiDocument.rootVisualElement != null)
            {
                VisualElement root = uiDocument.rootVisualElement;
                m_optionPanel = root.Q<VisualElement>("OptionPanel");

                // Tabs
                m_tabGeneralBtn = root.Q<Button>("Tab_General");
                m_tabCameraBtn = root.Q<Button>("Tab_Camera");
                m_tabSoundBtn = root.Q<Button>("Tab_Sound");
                m_tabGraphicsBtn = root.Q<Button>("Tab_Graphics");

                // Contents
                m_contentGeneral = root.Q<VisualElement>("Content_General");
                m_contentCamera = root.Q<VisualElement>("Content_Camera");
                m_contentSound = root.Q<VisualElement>("Content_Sound");
                m_contentGraphics = root.Q<VisualElement>("Content_Graphics");

                // Actions
                m_resumeBtn = root.Q<Button>("Btn_Resume");
                m_lobbyBtn = root.Q<Button>("Btn_Lobby");
                m_quitBtn = root.Q<Button>("Btn_Quit");
                m_closeBtn = root.Q<Button>("Btn_Close");

                if (m_tabGeneralBtn != null)
                {
                    m_tabGeneralBtn.clicked -= OnGeneralTabClicked;
                    m_tabGeneralBtn.clicked += OnGeneralTabClicked;
                }

                if (m_tabCameraBtn != null)
                {
                    m_tabCameraBtn.clicked -= OnCameraTabClicked;
                    m_tabCameraBtn.clicked += OnCameraTabClicked;
                }

                if (m_tabSoundBtn != null)
                {
                    m_tabSoundBtn.clicked -= OnSoundTabClicked;
                    m_tabSoundBtn.clicked += OnSoundTabClicked;
                }

                if (m_tabGraphicsBtn != null)
                {
                    m_tabGraphicsBtn.clicked -= OnGraphicsTabClicked;
                    m_tabGraphicsBtn.clicked += OnGraphicsTabClicked;
                }

                if (m_resumeBtn != null)
                {
                    m_resumeBtn.clicked -= CloseOption;
                    m_resumeBtn.clicked += CloseOption;
                }

                if (m_closeBtn != null)
                {
                    m_closeBtn.clicked -= CloseOption;
                    m_closeBtn.clicked += CloseOption;
                }

                if (m_quitBtn != null)
                {
                    m_quitBtn.clicked -= OnQuitClicked;
                    m_quitBtn.clicked += OnQuitClicked;
                }
            }
        }

        public void ApplySceneContext(OptionSceneContext context)
        {
            this.sceneContext = context;
            if (m_resumeBtn != null)
            {
                m_resumeBtn.style.display = (context == OptionSceneContext.InGame) ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (m_lobbyBtn != null)
            {
                m_lobbyBtn.style.display = (context == OptionSceneContext.InGame) ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void OnGeneralTabClicked() => SelectTab(OptionTabType.General);
        private void OnCameraTabClicked() => SelectTab(OptionTabType.Camera);
        private void OnSoundTabClicked() => SelectTab(OptionTabType.Sound);
        private void OnGraphicsTabClicked() => SelectTab(OptionTabType.Graphics);

        public void SelectTab(OptionTabType tabType)
        {
            m_currentTab = tabType;

            if (m_contentGeneral != null) m_contentGeneral.style.display = tabType == OptionTabType.General ? DisplayStyle.Flex : DisplayStyle.None;
            if (m_contentCamera != null) m_contentCamera.style.display = tabType == OptionTabType.Camera ? DisplayStyle.Flex : DisplayStyle.None;
            if (m_contentSound != null) m_contentSound.style.display = tabType == OptionTabType.Sound ? DisplayStyle.Flex : DisplayStyle.None;
            if (m_contentGraphics != null) m_contentGraphics.style.display = tabType == OptionTabType.Graphics ? DisplayStyle.Flex : DisplayStyle.None;

            SetTabActive(m_tabGeneralBtn, tabType == OptionTabType.General);
            SetTabActive(m_tabCameraBtn, tabType == OptionTabType.Camera);
            SetTabActive(m_tabSoundBtn, tabType == OptionTabType.Sound);
            SetTabActive(m_tabGraphicsBtn, tabType == OptionTabType.Graphics);
        }

        private void SetTabActive(Button tabBtn, bool isActive)
        {
            if (tabBtn == null) return;
            if (colorData != null)
            {
                tabBtn.style.backgroundColor = new StyleColor(isActive ? colorData.ActiveTabBgColor : colorData.InactiveTabBgColor);
                tabBtn.style.color = new StyleColor(isActive ? colorData.ActiveTabTextColor : colorData.InactiveTabTextColor);
            }
            else
            {
                tabBtn.style.backgroundColor = isActive ? new Color(0.2f, 0.2f, 0.28f, 1f) : new Color(0.14f, 0.14f, 0.19f, 1f);
                tabBtn.style.color = isActive ? new Color(1f, 1f, 1f, 1f) : new Color(0.7f, 0.7f, 0.7f, 1f);
            }
        }

        public void ToggleOption()
        {
            if (m_isOpen)
            {
                CloseOption();
            }
            else
            {
                OpenOption();
            }
        }

        public void OpenOption()
        {
            m_isOpen = true;
            if (m_optionPanel != null)
            {
                m_optionPanel.style.display = DisplayStyle.Flex;
            }

            SelectTab(OptionTabType.General);

            if (sceneContext == OptionSceneContext.InGame)
            {
                m_timeScaleService?.SetPaused(this, true);
            }

            var manager = ContextManager;
            manager?.PushContext(manager.UIContext);
        }

        public void CloseOption()
        {
            m_isOpen = false;
            if (m_optionPanel != null)
            {
                m_optionPanel.style.display = DisplayStyle.None;
            }

            m_timeScaleService?.SetPaused(this, false);

            var manager = ContextManager;
            manager?.PopContext(manager.UIContext);
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}

