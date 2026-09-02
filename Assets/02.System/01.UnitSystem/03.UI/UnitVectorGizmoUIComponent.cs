using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnitSystem
{
    public class UnitVectorGizmoUIComponent : MonoBehaviour
    {
        [Header("UI & Visual Settings")]
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private LineRenderer trajectoryLineRenderer;
        [SerializeField] private LineRenderer rotationRingRenderer;
        [SerializeField] private float maxSpeedRange = 50f;
        [SerializeField] private UnitGhostPreviewComponent ghostPreview;
        [SerializeField] private PipeLine.UnitMagic.UnitMagicPipeLine unitMagicPipeLine;

        private GameObject targetObject;
        private RuntimeDataUnit targetRuntimeData;
        private Vector3 currentDirection = Vector3.forward;
        private float currentSpeed = 0f;
        private bool isOpen = false;

        private VisualElement rootVisualElement;
        private VisualElement vectorGizmoContainer;
        private Slider speedSlider;
        private Label speedValueText;
        private Button applyButton;

        private Camera mainCamera;

        [SerializeField] private CharacterSystem.CharacterStatComponent playerStatComponent;

        [VContainer.Inject]
        public void Construct(UnitGhostPreviewComponent ghostPreview)
        {
            this.ghostPreview = ghostPreview;
        }

        private void OnEnable()
        {
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            InitUI();
        }

        private void InitUI()
        {
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();

            if (uiDocument != null && uiDocument.rootVisualElement != null)
            {
                rootVisualElement = uiDocument.rootVisualElement;
                rootVisualElement.pickingMode = PickingMode.Ignore; // 투명 배경 영역 포인터 감지 차단

                vectorGizmoContainer = rootVisualElement.Q<VisualElement>("VectorGizmoContainer");
                speedSlider = rootVisualElement.Q<Slider>("SpeedSlider");
                speedValueText = rootVisualElement.Q<Label>("SpeedValueText");
                applyButton = rootVisualElement.Q<Button>("ApplyVectorButton");

                // UXML 바인딩 미존재 시 동적 UI Toolkit 컨트롤 자동 생성
                if (vectorGizmoContainer == null)
                {
                    vectorGizmoContainer = new VisualElement();
                    vectorGizmoContainer.name = "VectorGizmoContainer";
                    vectorGizmoContainer.style.position = Position.Absolute;
                    vectorGizmoContainer.style.top = 20;
                    vectorGizmoContainer.style.right = 20;
                    vectorGizmoContainer.style.width = 260;
                    vectorGizmoContainer.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
                    vectorGizmoContainer.style.paddingTop = 15;
                    vectorGizmoContainer.style.paddingBottom = 15;
                    vectorGizmoContainer.style.paddingLeft = 15;
                    vectorGizmoContainer.style.paddingRight = 15;
                    vectorGizmoContainer.style.borderTopLeftRadius = 10;
                    vectorGizmoContainer.style.borderTopRightRadius = 10;
                    vectorGizmoContainer.style.borderBottomLeftRadius = 10;
                    vectorGizmoContainer.style.borderBottomRightRadius = 10;

                    speedValueText = new Label("Speed: 0.0 m/s");
                    speedValueText.style.color = Color.yellow;
                    speedValueText.style.fontSize = 16;
                    speedValueText.style.unityFontStyleAndWeight = FontStyle.Bold;
                    speedValueText.style.marginBottom = 10;

                    speedSlider = new Slider(0f, maxSpeedRange);
                    speedSlider.style.marginBottom = 15;

                    applyButton = new Button();
                    applyButton.text = "APPLY VECTOR";
                    applyButton.style.backgroundColor = new Color(0.2f, 0.6f, 1.0f);
                    applyButton.style.color = Color.white;
                    applyButton.style.height = 35;
                    applyButton.style.unityFontStyleAndWeight = FontStyle.Bold;

                    vectorGizmoContainer.Add(speedValueText);
                    vectorGizmoContainer.Add(speedSlider);
                    vectorGizmoContainer.Add(applyButton);

                    rootVisualElement.Add(vectorGizmoContainer);
                }

                if (speedSlider != null)
                {
                    speedSlider.lowValue = 0f;
                    speedSlider.highValue = maxSpeedRange;
                    speedSlider.RegisterValueChangedCallback(OnSpeedSliderChanged);
                }

                if (applyButton != null)
                {
                    applyButton.clicked += ApplyVectorAndClose;
                }

                // 가이드라인 3항: 가변형 UI 드래그 포인터 이벤트 등록
                SetupDraggableWindow();
            }

            HideGizmoUI();
        }

        private bool isDraggingWindow = false;
        private Vector3 startPointerPos;
        private Vector3 startTranslatePos;

        private void SetupDraggableWindow()
        {
            if (vectorGizmoContainer == null) return;

            vectorGizmoContainer.RegisterCallback<PointerDownEvent>(OnWindowPointerDown);
            vectorGizmoContainer.RegisterCallback<PointerMoveEvent>(OnWindowPointerMove);
            vectorGizmoContainer.RegisterCallback<PointerUpEvent>(OnWindowPointerUp);
        }

        private void OnWindowPointerDown(PointerDownEvent evt)
        {
            if (evt.target is Slider || evt.target is Button) return;

            isDraggingWindow = true;
            startPointerPos = evt.position;
            startTranslatePos = new Vector3(vectorGizmoContainer.resolvedStyle.translate.x, vectorGizmoContainer.resolvedStyle.translate.y, 0f);

            // 가이드라인 3항 규칙 2: BringToFront() 부모 계층 최상단 렌더링/입력 반영
            vectorGizmoContainer.BringToFront();
            vectorGizmoContainer.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnWindowPointerMove(PointerMoveEvent evt)
        {
            if (!isDraggingWindow || !vectorGizmoContainer.HasPointerCapture(evt.pointerId)) return;

            Vector3 delta = evt.position - startPointerPos;
            float targetX = startTranslatePos.x + delta.x;
            float targetY = startTranslatePos.y + delta.y;

            // 가이드라인 3항 규칙 3: UI 창 패널 영역 밖 이탈 방지 좌표 제한 처리 (Clamp)
            float minX = -Screen.width + vectorGizmoContainer.layout.width + 40f;
            float maxX = 0f;
            float minY = 0f;
            float maxY = Screen.height - vectorGizmoContainer.layout.height - 40f;

            float clampedX = Mathf.Clamp(targetX, minX, maxX);
            float clampedY = Mathf.Clamp(targetY, minY, maxY);

            // 가이드라인 3항 규칙 1: USS translate 속성 업데이트 위치 이동 구현
            vectorGizmoContainer.style.translate = new Translate(clampedX, clampedY, 0f);
            evt.StopPropagation();
        }

        private void OnWindowPointerUp(PointerUpEvent evt)
        {
            if (isDraggingWindow && vectorGizmoContainer.HasPointerCapture(evt.pointerId))
            {
                vectorGizmoContainer.ReleasePointer(evt.pointerId);
                isDraggingWindow = false;
                evt.StopPropagation();
            }
        }

        public void OpenVectorGizmo(GameObject target, RuntimeDataUnit runtimeData)
        {
            if (target == null || runtimeData == null) return;

            targetObject = target;
            targetRuntimeData = runtimeData;
            mainCamera = Camera.main;

            var rb = targetObject.GetComponent<Rigidbody>();
            if (rb != null && rb.linearVelocity.sqrMagnitude > 0.01f)
            {
                currentSpeed = rb.linearVelocity.magnitude;
                currentDirection = rb.linearVelocity.normalized;
            }
            else
            {
                currentDirection = targetObject.transform.forward;
                currentSpeed = runtimeData.VectorSpeed > 0f ? runtimeData.VectorSpeed : 10f;
            }

            if (rootVisualElement == null) InitUI();

            isOpen = true;
            ShowGizmoUI();

            if (ghostPreview != null)
            {
                ghostPreview.ShowPreview(targetObject);
            }

            UpdateVisuals();
        }

        private void Update()
        {
            if (!isOpen || targetObject == null) return;

            HandleMouseAimInput();
            UpdateVisuals();
        }

        private void HandleMouseAimInput()
        {
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null || mainCamera == null) return;

            Vector2 mousePos = mouse.position.ReadValue();

            // UI Toolkit 작업 가이드라인 준수: UI 엘리먼트 실시간 피킹(Pick) 검사 및 인게임 레이캐스트 차단
            if (uiDocument != null && uiDocument.rootVisualElement != null && uiDocument.rootVisualElement.panel != null)
            {
                var panel = uiDocument.rootVisualElement.panel;
                Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(mousePos.x, Screen.height - mousePos.y));
                VisualElement picked = panel.Pick(panelPos);
                if (picked != null && picked != rootVisualElement)
                {
                    return; // UI 조작 중 레이캐스트 차단
                }
            }

            // 마우스 클릭 시 대상 오브젝트 위치 -> 마우스 클릭 지점 사이 정규화(Normalize) 방향 즉시 설정
            if (mouse.leftButton.isPressed)
            {
                Ray ray = mainCamera.ScreenPointToRay(mousePos);
                Plane plane = new Plane(Vector3.up, targetObject.transform.position);
                if (plane.Raycast(ray, out float enter))
                {
                    Vector3 hitPoint = ray.GetPoint(enter);
                    Vector3 dir = (hitPoint - targetObject.transform.position).normalized;
                    dir.y = 0f;
                    if (dir != Vector3.zero)
                    {
                        currentDirection = dir.normalized;
                    }
                }
            }
        }

        private void UpdateVisuals()
        {
            if (targetObject == null) return;

            Vector3 targetPos = targetObject.transform.position;

            // 1. 회전 링 (Dial UI) 렌더링
            if (rotationRingRenderer != null)
            {
                rotationRingRenderer.enabled = true;
                int segments = 36;
                rotationRingRenderer.positionCount = segments + 1;
                float radius = 2.0f;
                for (int i = 0; i <= segments; i++)
                {
                    float angle = i * (2 * Mathf.PI / segments);
                    Vector3 pos = targetPos + new Vector3(Mathf.Cos(angle) * radius, 0.1f, Mathf.Sin(angle) * radius);
                    rotationRingRenderer.SetPosition(i, pos);
                }
            }

            // 2. 점선 예상 이동 경로 화살표 렌더링
            if (trajectoryLineRenderer != null)
            {
                trajectoryLineRenderer.enabled = true;
                trajectoryLineRenderer.positionCount = 2;
                trajectoryLineRenderer.SetPosition(0, targetPos + Vector3.up * 0.2f);
                
                float arrowLength = Mathf.Clamp(currentSpeed * 0.3f, 1.5f, 15f);
                Vector3 endPos = targetPos + Vector3.up * 0.2f + currentDirection * arrowLength;
                trajectoryLineRenderer.SetPosition(1, endPos);
            }

            // 3. UI 슬라이더 및 텍스트 갱신
            if (speedSlider != null)
            {
                speedSlider.SetValueWithoutNotify(currentSpeed);
            }
            if (speedValueText != null)
            {
                speedValueText.text = $"Speed: {currentSpeed:F1} m/s";
            }

            // 4. Ghost Preview 실시간 Transform 갱신
            if (ghostPreview != null && ghostPreview.IsShowing)
            {
                ghostPreview.UpdatePreview(targetObject.transform.localScale, currentDirection);
            }
        }

        private void OnSpeedSliderChanged(ChangeEvent<float> evt)
        {
            currentSpeed = evt.newValue;
            UpdateVisuals();
        }

        public void ApplyVectorAndClose()
        {
            if (targetObject != null && targetRuntimeData != null)
            {
                // Applicator 엄격 검증: Applicator 미연결(null) 시 물리 적용 연산 스킵
                if (targetRuntimeData.CurrentUnitData == null || targetRuntimeData.CurrentUnitData.Applicator == null)
                {
                    Debug.LogWarning($"[UnitVectorGizmoUIComponent] Cannot apply vector! Applicator is missing (null) in PureDataUnit for target {targetObject.name}. Assignment on PureDataUnit asset is required.");
                    HideGizmoUI();
                    isOpen = false;
                    return;
                }

                var playerComp = playerStatComponent 
                              ?? GetComponentInParent<CharacterSystem.CharacterStatComponent>() 
                              ?? CharacterSystem.CharacterStatComponent.PlayerStat;
                var statData = playerComp?.StatSystem?.RuntimeData;

                var context = new PipeLine.Contexts.UnitMagicContext
                {
                    Caster = playerComp != null ? playerComp.gameObject : null,
                    CasterStatData = statData,
                    TargetObject = targetObject,
                    TargetRuntimeData = targetRuntimeData,
                    SelectedPureData = targetRuntimeData.CurrentUnitData,
                    VectorDirection = currentDirection,
                    VectorSpeed = currentSpeed,
                    RequiredMana = targetRuntimeData.CurrentUnitData != null ? targetRuntimeData.CurrentUnitData.BaseCost : 0
                };

                if (ghostPreview != null && ghostPreview.IsShowing)
                {
                    ghostPreview.ApplyAndDestroy(() => ApplyVectorMagic(context));
                }
                else
                {
                    ApplyVectorMagic(context);
                }
            }

            HideGizmoUI();
            isOpen = false;
        }

        private void ApplyVectorMagic(PipeLine.Contexts.UnitMagicContext context)
        {
            if (unitMagicPipeLine == null)
            {
                unitMagicPipeLine = ScriptableObject.CreateInstance<PipeLine.UnitMagic.UnitMagicPipeLine>();
            }

            if (unitMagicPipeLine != null)
            {
                unitMagicPipeLine.Run(context).Forget();
            }
            else
            {
                Debug.LogError("[UnitVectorGizmoUIComponent] UnitMagicPipeLine asset missing! Execution blocked.");
            }
        }

        private void ShowGizmoUI()
        {
            if (vectorGizmoContainer != null) vectorGizmoContainer.style.display = DisplayStyle.Flex;
        }

        private void HideGizmoUI()
        {
            if (vectorGizmoContainer != null) vectorGizmoContainer.style.display = DisplayStyle.None;
            if (rotationRingRenderer != null) rotationRingRenderer.enabled = false;
            if (trajectoryLineRenderer != null) trajectoryLineRenderer.enabled = false;
            if (ghostPreview != null && ghostPreview.IsShowing) ghostPreview.HidePreview();
        }
    }
}
