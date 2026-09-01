using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using VContainer;

namespace UnitSystem
{
    public class UnitCasterSystem : MonoBehaviour
    {
        [Header("Targeting Settings")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private LayerMask targetLayer = -1;
        [SerializeField] private UIDocument uiDocument;

        private VisualElement targetInfoContainer;
        private Label targetNameText;
        private Label targetUnitsText;

        private UnitChangeService changeService;
        private UnitQuickSlotUIComponent quickSlotUI;

        [Inject]
        public void Construct(UnitChangeService changeService, UnitQuickSlotUIComponent quickSlotUI)
        {
            this.changeService = changeService;
            this.quickSlotUI = quickSlotUI;
        }

        private void OnEnable()
        {
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            if (uiDocument != null && uiDocument.rootVisualElement != null)
            {
                var root = uiDocument.rootVisualElement;
                targetInfoContainer = root.Q<VisualElement>("TargetInfoContainer");
                targetNameText = root.Q<Label>("TargetNameText");
                targetUnitsText = root.Q<Label>("TargetUnitsText");
            }
        }

        private void Update()
        {
            HandleMouseHoverAndCast();
        }

        private void HandleMouseHoverAndCast()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            Vector2 mousePos = mouse.position.ReadValue();

            // UI Toolkit 가이드라인 준수: 마우스 포인터 위치 UI Toolkit 엘리먼트 실시간 피킹(Pick) 검사 및 인게임 레이캐스트 차단
            if (IsPointerOverUIToolkit(mousePos))
            {
                HideTargetHoverUI();
                return;
            }
            Ray ray = mainCamera.ScreenPointToRay(mousePos);

            bool isMouseClicked = mouse.rightButton.wasPressedThisFrame || mouse.leftButton.wasPressedThisFrame;

            if (isMouseClicked)
            {
                Debug.Log($"[UnitCasterSystem] Mouse Clicked at Pos: {mousePos}");
            }

            float castRadius = 0.3f;
            RaycastHit[] hits = Physics.SphereCastAll(ray, castRadius, 100f, targetLayer);

            if (hits != null && hits.Length > 0)
            {
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                foreach (var hit in hits)
                {
                    var targetDataGroup = hit.collider.GetComponentInParent<RuntimeDataUnitGroup>();
                    if (targetDataGroup != null)
                    {
                        var targetObj = targetDataGroup.gameObject;
                        if (isMouseClicked)
                        {
                            Debug.Log($"[UnitCasterSystem] SphereCast Hit Success! Target: <color=yellow>{targetObj.name}</color> (Collider: {hit.collider.name})");
                        }

                        // 1. 마우스 조준(Hover) 시각 UI 피드백 표출
                        ShowTargetHoverUI(targetObj.name, targetDataGroup);

                        // 2. 마우스 클릭 시 선택 단위 매칭 핀포인트 변환 수행
                        if (isMouseClicked)
                        {
                            PureDataUnit selectedUnitData = quickSlotUI != null ? quickSlotUI.CurrentSelectedUnit : null;
                            if (selectedUnitData == null)
                            {
                                Debug.LogWarning("[UnitCasterSystem] No Unit is currently selected in Quick Slots.");
                                return;
                            }

                            if (!targetDataGroup.HasUnit(selectedUnitData.UnitType))
                            {
                                Debug.LogWarning($"[UnitCasterSystem] Target {targetObj.name} does not support unit category for {selectedUnitData.UnitType}");
                                return;
                            }

                            var matchingUnitData = targetDataGroup.GetMatchingUnitData(selectedUnitData.UnitType);

                            if (matchingUnitData != null)
                            {
                                if (selectedUnitData.UnitType == UnitType.Vector)
                                {
                                    // 선택된 PureDataUnit (Applicator 포함) 타깃 런타임 데이터에 이벤트를 발행하지 않고 바인딩만 진행
                                    matchingUnitData.SetPureDataUnit(selectedUnitData);

                                    var gizmoUI = GetComponent<UnitVectorGizmoUIComponent>();
                                    if (gizmoUI == null) gizmoUI = FindFirstObjectByType<UnitVectorGizmoUIComponent>();

                                    if (gizmoUI != null)
                                    {
                                        gizmoUI.OpenVectorGizmo(targetObj, matchingUnitData);
                                        Debug.Log($"[UnitCasterSystem] Opened Vector Gizmo UI for {targetObj.name}");
                                    }
                                    else
                                    {
                                        Debug.LogWarning("[UnitCasterSystem] UnitVectorGizmoUIComponent not found in scene.");
                                    }
                                }
                                else
                                {
                                    float newValue = CalculateNewValueForUnit(matchingUnitData, selectedUnitData);
                                    Debug.Log($"[UnitCasterSystem] Pinpoint Unit Change for {targetObj.name}: {matchingUnitData.CurrentUnit} -> {selectedUnitData.UnitType}");

                                    if (changeService != null)
                                    {
                                        var playerStatComp = FindFirstObjectByType<CharacterSystem.CharacterStatComponent>();
                                        var casterStatData = playerStatComp?.StatSystem?.RuntimeData;
                                        changeService.ChangeUnit(targetObj, matchingUnitData, selectedUnitData, newValue, casterStatData, null, gameObject);
                                    }
                                }
                            }
                        }
                        return;
                    }
                }

                if (isMouseClicked)
                {
                    Debug.LogWarning($"[UnitCasterSystem] SphereCast Hit Colliders ({hits.Length}), but NONE have RuntimeDataUnitGroup attached!");
                }
            }
            else if (isMouseClicked)
            {
                Debug.LogWarning("[UnitCasterSystem] SphereCast Hit Failed. No Collider in ray path.");
            }

            // 조준 대상 없을 시 UI 숨김
            HideTargetHoverUI();
        }

        private void ShowTargetHoverUI(string objectName, RuntimeDataUnitGroup targetDataGroup)
        {
            if (targetInfoContainer == null) return;

            targetInfoContainer.style.display = DisplayStyle.Flex;
            if (targetNameText != null) targetNameText.text = $"Target: {objectName}";

            if (targetUnitsText != null)
            {
                StringBuilder sb = new StringBuilder("Units: ");
                foreach (var unitData in targetDataGroup.UnitRuntimeDataList)
                {
                    sb.Append($"[{unitData.CurrentUnit}: {unitData.CurrentValue}] ");
                }
                targetUnitsText.text = sb.ToString();
            }
        }

        private void HideTargetHoverUI()
        {
            if (targetInfoContainer != null)
            {
                targetInfoContainer.style.display = DisplayStyle.None;
            }
        }

        private float CalculateNewValueForUnit(RuntimeDataUnit targetUnit, PureDataUnit spellUnit)
        {
            if (targetUnit == null || spellUnit == null) return 0f;
            
            float originalVal = targetUnit.OriginalValue > 0f ? targetUnit.OriginalValue : 1.0f;

            switch (spellUnit.UnitType)
            {
                case UnitType.Mass:
                    // 대상 구체의 오리지널 질량값 * 캐스터가 발사하는 단위 에셋의 고유 멀티플라이어
                    return originalVal * spellUnit.MassScaleMultiplier;
                case UnitType.Volume:
                    return originalVal;
                case UnitType.Vector:
                    return -targetUnit.CurrentValue;
                default:
                    return targetUnit.CurrentValue;
            }
        }

        private bool IsPointerOverUIToolkit(Vector2 mouseScreenPos)
        {
            var documents = FindObjectsByType<UnityEngine.UIElements.UIDocument>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var doc in documents)
            {
                if (doc == null || doc.rootVisualElement == null || doc.rootVisualElement.panel == null) continue;

                var panel = doc.rootVisualElement.panel;
                Vector2 panelPos = UnityEngine.UIElements.RuntimePanelUtils.ScreenToPanel(panel, new Vector2(mouseScreenPos.x, Screen.height - mouseScreenPos.y));
                UnityEngine.UIElements.VisualElement picked = panel.Pick(panelPos);

                if (picked != null && picked != doc.rootVisualElement)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
