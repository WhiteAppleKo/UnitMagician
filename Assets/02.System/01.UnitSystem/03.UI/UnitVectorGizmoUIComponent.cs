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
        private bool isDraggingRing = false;

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
            }

            HideGizmoUI();
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

                // RuntimeData 수치 갱신 및 Applicator 전략 적용
                targetRuntimeData.SetVectorData(currentDirection, currentSpeed);
                targetRuntimeData.CurrentUnitData.Applicator.Apply(targetObject, targetRuntimeData);

                Debug.Log($"[UnitVectorGizmoUIComponent] Successfully applied vector strategy for {targetObject.name}. Direction: {currentDirection}, Speed: {currentSpeed}");
            }

            HideGizmoUI();
            isOpen = false;
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
        }
    }
}
