using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace CameraMovement
{
    [System.Obsolete("CameraModeUIComponent is deprecated. Use OptionSystem.OptionUIComponent instead.")]
    public class CameraModeUIComponent : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private ICameraFollowService m_cameraFollowService;
        private Button m_hybridBtn;
        private Button m_playerBtn;
        private Button m_mouseBtn;
        private Button m_firstPersonBtn;

        [Inject]
        public void Construct(ICameraFollowService cameraFollowService = null)
        {
            m_cameraFollowService = cameraFollowService;
        }

        public void Initialize(ICameraFollowService cameraFollowService)
        {
            m_cameraFollowService = cameraFollowService;
        }

        private void OnEnable()
        {
            RegisterButtonCallbacks();
            if (m_cameraFollowService != null)
            {
                m_cameraFollowService.OnCameraModeChanged += UpdateButtonHighlights;
            }
        }

        private void OnDisable()
        {
            if (m_cameraFollowService != null)
            {
                m_cameraFollowService.OnCameraModeChanged -= UpdateButtonHighlights;
            }
        }

        private void Start()
        {
            RegisterButtonCallbacks();
            if (m_cameraFollowService != null)
            {
                UpdateButtonHighlights(m_cameraFollowService.CurrentMode);
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
                m_hybridBtn = root.Q<Button>("Btn_HybridFocus");
                m_playerBtn = root.Q<Button>("Btn_PlayerOnly");
                m_mouseBtn = root.Q<Button>("Btn_MouseFocus");
                m_firstPersonBtn = root.Q<Button>("Btn_FirstPerson");

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
            }
        }

        private void OnHybridClicked() => SetMode(CameraMode.HybridFocus);
        private void OnPlayerClicked() => SetMode(CameraMode.PlayerOnly);
        private void OnMouseClicked() => SetMode(CameraMode.MouseFocus);
        private void OnFirstPersonClicked() => SetMode(CameraMode.FirstPerson);

        private void SetMode(CameraMode mode)
        {
            if (m_cameraFollowService != null)
            {
                m_cameraFollowService.SetCameraMode(mode);
            }
        }

        private void UpdateButtonHighlights(CameraMode mode)
        {
            SetButtonActive(m_hybridBtn, mode == CameraMode.HybridFocus);
            SetButtonActive(m_playerBtn, mode == CameraMode.PlayerOnly);
            SetButtonActive(m_mouseBtn, mode == CameraMode.MouseFocus);
            SetButtonActive(m_firstPersonBtn, mode == CameraMode.FirstPerson);
        }

        private void SetButtonActive(Button btn, bool isActive)
        {
            if (btn == null) return;
            btn.style.backgroundColor = isActive 
                ? new Color(0.2f, 0.6f, 0.9f, 1f) 
                : new Color(0.2f, 0.2f, 0.25f, 1f);
        }
    }
}
