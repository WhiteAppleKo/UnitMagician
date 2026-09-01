using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using VContainer;
using CameraMovement;
using UI.Data;

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

        // Camera Mode Buttons
        private Button m_hybridBtn;
        private Button m_playerBtn;
        private Button m_mouseBtn;
        private Button m_firstPersonBtn;
        private Button m_shoulderBtn;

        // Action Buttons
        private Button m_resumeBtn;
        private Button m_lobbyBtn;
        private Button m_quitBtn;
        private Button m_closeBtn;

        private OptionTabType m_currentTab = OptionTabType.General;
        private bool m_isOpen = false;

        public bool IsOpen => m_isOpen;

        [Inject]
        public void Construct(ICameraFollowService cameraFollowService = null)
        {
            m_cameraFollowService = cameraFollowService;
        }

        private void OnEnable()
        {
            RegisterButtonCallbacks();
            if (m_cameraFollowService != null)
            {
                m_cameraFollowService.OnCameraModeChanged += UpdateCameraModeHighlights;
            }
        }

        private void OnDisable()
        {
            if (m_cameraFollowService != null)
            {
                m_cameraFollowService.OnCameraModeChanged -= UpdateCameraModeHighlights;
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
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
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

                // Camera Buttons
                m_hybridBtn = root.Q<Button>("Btn_HybridFocus");
                m_playerBtn = root.Q<Button>("Btn_PlayerOnly");
                m_mouseBtn = root.Q<Button>("Btn_MouseFocus");
                m_firstPersonBtn = root.Q<Button>("Btn_FirstPerson");
                m_shoulderBtn = root.Q<Button>("Btn_ThirdPersonShoulder");

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

                if (m_hybridBtn != null)
                {
                    m_hybridBtn.clicked -= OnHybridClicked;
                    m_hybridBtn.clicked += OnHybridClicked;
                }

                if (m_playerBtn != null)
                {
                    m_playerBtn.clicked -= OnPlayerClicked;
                    m_playerBtn.clicked += OnPlayerClicked;
                }

                if (m_mouseBtn != null)
                {
                    m_mouseBtn.clicked -= OnMouseClicked;
                    m_mouseBtn.clicked += OnMouseClicked;
                }

                if (m_firstPersonBtn != null)
                {
                    m_firstPersonBtn.clicked -= OnFirstPersonClicked;
                    m_firstPersonBtn.clicked += OnFirstPersonClicked;
                }

                if (m_shoulderBtn != null)
                {
                    m_shoulderBtn.clicked -= OnShoulderClicked;
                    m_shoulderBtn.clicked += OnShoulderClicked;
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

                FilterAllowedModeButtons();
            }
        }

        private void FilterAllowedModeButtons()
        {
            if (m_cameraFollowService == null || m_cameraFollowService.Setting == null) return;

            var setting = m_cameraFollowService.Setting;
            SetButtonVisibility(m_hybridBtn, setting.IsModeAllowed(CameraMode.HybridFocus));
            SetButtonVisibility(m_playerBtn, setting.IsModeAllowed(CameraMode.PlayerOnly));
            SetButtonVisibility(m_mouseBtn, setting.IsModeAllowed(CameraMode.MouseFocus));
            SetButtonVisibility(m_firstPersonBtn, setting.IsModeAllowed(CameraMode.FirstPerson));
            SetButtonVisibility(m_shoulderBtn, setting.IsModeAllowed(CameraMode.ThirdPersonShoulder));
        }

        private void SetButtonVisibility(Button btn, bool isAllowed)
        {
            if (btn == null) return;
            btn.style.display = isAllowed ? DisplayStyle.Flex : DisplayStyle.None;
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

        private void OnHybridClicked() => SetCameraMode(CameraMode.HybridFocus);
        private void OnPlayerClicked() => SetCameraMode(CameraMode.PlayerOnly);
        private void OnMouseClicked() => SetCameraMode(CameraMode.MouseFocus);
        private void OnFirstPersonClicked() => SetCameraMode(CameraMode.FirstPerson);
        private void OnShoulderClicked() => SetCameraMode(CameraMode.ThirdPersonShoulder);

        private void SetCameraMode(CameraMode mode)
        {
            m_cameraFollowService?.SetCameraMode(mode);

            if (m_isOpen)
            {
                UnityEngine.Cursor.lockState = CursorLockMode.None;
                UnityEngine.Cursor.visible = true;
            }
        }

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

            if (tabType == OptionTabType.Camera)
            {
                FilterAllowedModeButtons();
                if (m_cameraFollowService != null)
                {
                    UpdateCameraModeHighlights(m_cameraFollowService.CurrentMode);
                }
            }
        }

        private void UpdateCameraModeHighlights(CameraMode mode)
        {
            SetButtonActive(m_hybridBtn, mode == CameraMode.HybridFocus);
            SetButtonActive(m_playerBtn, mode == CameraMode.PlayerOnly);
            SetButtonActive(m_mouseBtn, mode == CameraMode.MouseFocus);
            SetButtonActive(m_firstPersonBtn, mode == CameraMode.FirstPerson);
            SetButtonActive(m_shoulderBtn, mode == CameraMode.ThirdPersonShoulder);
        }

        private void SetButtonActive(Button btn, bool isActive)
        {
            if (btn == null) return;
            if (colorData != null)
            {
                btn.style.backgroundColor = new StyleColor(isActive ? colorData.ActiveModeColor : colorData.InactiveModeColor);
            }
            else
            {
                btn.style.backgroundColor = isActive ? new Color(0.2f, 0.6f, 0.9f, 1f) : new Color(0.2f, 0.2f, 0.25f, 1f);
            }
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
                Time.timeScale = 0f;
            }

            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }

        public void CloseOption()
        {
            m_isOpen = false;
            if (m_optionPanel != null)
            {
                m_optionPanel.style.display = DisplayStyle.None;
            }

            Time.timeScale = 1f;

            if (m_cameraFollowService != null && m_cameraFollowService.CurrentMode == CameraMode.FirstPerson)
            {
                UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                UnityEngine.Cursor.visible = false;
            }
            else
            {
                UnityEngine.Cursor.lockState = CursorLockMode.None;
                UnityEngine.Cursor.visible = true;
            }
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

