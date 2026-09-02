using System;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace UnitSystem
{
    /// <summary>
    /// 탑뷰/쿼터뷰(HybridFocus, PlayerOnly, MouseFocus) 시점 전용 마법 조작 전략입니다.
    /// 마우스 커서 위치의 대상을 실시간 호버 감지하여 정보를 표출하고,
    /// 마우스 클릭 시 즉시 핀포인트 단위 마법을 발동합니다.
    /// </summary>
    public class TopViewMouseCastingStrategy : IUnitCastingStrategy
    {
        private Camera mainCamera;
        private LayerMask targetLayer;
        private UIDocument uiDocument;
        private UnitChangeService changeService;
        private UnitQuickSlotUIComponent quickSlotUI;
        private GameObject ownerObject;

        private VisualElement targetInfoContainer;
        private Label targetNameText;
        private Label targetUnitsText;

        public TopViewMouseCastingStrategy(
            Camera mainCamera,
            LayerMask targetLayer,
            UIDocument uiDocument,
            UnitChangeService changeService,
            UnitQuickSlotUIComponent quickSlotUI,
            GameObject ownerObject)
        {
            this.mainCamera = mainCamera;
            this.targetLayer = targetLayer;
            this.uiDocument = uiDocument;
            this.changeService = changeService;
            this.quickSlotUI = quickSlotUI;
            this.ownerObject = ownerObject;

            InitializeUI();
        }

        private void InitializeUI()
        {
            if (uiDocument != null && uiDocument.rootVisualElement != null)
            {
                var root = uiDocument.rootVisualElement;
                targetInfoContainer = root.Q<VisualElement>("TargetInfoContainer");
                targetNameText = root.Q<Label>("TargetNameText");
                targetUnitsText = root.Q<Label>("TargetUnitsText");
            }
        }

        public void Enter()
        {
            InitializeUI();
            HideTargetHoverUI();
        }

        public void Exit()
        {
            HideTargetHoverUI();
        }

        public void Dispose()
        {
            Exit();
        }

        public void Update()
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

            // UI Toolkit 엘리먼트 피킹 검사 및 인게임 레이캐스트 차단
            if (IsPointerOverUIToolkit(mousePos))
            {
                HideTargetHoverUI();
                return;
            }

            Ray ray = mainCamera.ScreenPointToRay(mousePos);
            bool isMouseClicked = mouse.rightButton.wasPressedThisFrame || mouse.leftButton.wasPressedThisFrame;

            if (isMouseClicked)
            {
                Debug.Log($"[TopViewMouseCastingStrategy] Mouse Clicked at Pos: {mousePos}");
            }

            float castRadius = 0.3f;
            RaycastHit[] hits = Physics.SphereCastAll(ray, castRadius, 100f, targetLayer);

            if (hits != null && hits.Length > 0)
            {
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                foreach (var hit in hits)
                {
                    var targetDataGroup = hit.collider.GetComponentInParent<RuntimeDataUnitGroup>();
                    if (targetDataGroup != null && targetDataGroup.IsTargetable)
                    {
                        var targetObj = targetDataGroup.gameObject;
                        if (isMouseClicked)
                        {
                            Debug.Log($"[TopViewMouseCastingStrategy] SphereCast Hit Success! Target: <color=yellow>{targetObj.name}</color> (Collider: {hit.collider.name})");
                        }

                        // 1. 마우스 조준(Hover) 시각 UI 피드백 표출
                        ShowTargetHoverUI(targetObj.name, targetDataGroup);

                        // 2. 마우스 클릭 시 선택 단위 매칭 핀포인트 변환 수행
                        if (isMouseClicked)
                        {
                            PureDataUnit selectedUnitData = quickSlotUI != null ? quickSlotUI.CurrentSelectedUnit : null;
                            if (selectedUnitData == null)
                            {
                                Debug.LogWarning("[TopViewMouseCastingStrategy] No Unit is currently selected in Quick Slots.");
                                return;
                            }

                            if (!targetDataGroup.HasUnit(selectedUnitData.UnitType))
                            {
                                Debug.LogWarning($"[TopViewMouseCastingStrategy] Target {targetObj.name} does not support unit category for {selectedUnitData.UnitType}");
                                return;
                            }

                            var matchingUnitData = targetDataGroup.GetMatchingUnitData(selectedUnitData.UnitType);

                            if (matchingUnitData != null)
                            {
                                if (selectedUnitData.UnitType == UnitType.Vector)
                                {
                                    matchingUnitData.SetPureDataUnit(selectedUnitData);

                                    var gizmoUI = ownerObject != null ? ownerObject.GetComponent<UnitVectorGizmoUIComponent>() : null;
                                    if (gizmoUI == null) gizmoUI = UnityEngine.Object.FindFirstObjectByType<UnitVectorGizmoUIComponent>();

                                    if (gizmoUI != null)
                                    {
                                        gizmoUI.OpenVectorGizmo(targetObj, matchingUnitData);
                                        Debug.Log($"[TopViewMouseCastingStrategy] Opened Vector Gizmo UI for {targetObj.name}");
                                    }
                                    else
                                    {
                                        Debug.LogWarning("[TopViewMouseCastingStrategy] UnitVectorGizmoUIComponent not found in scene.");
                                    }
                                }
                                else
                                {
                                    float newValue = CalculateNewValueForUnit(matchingUnitData, selectedUnitData);
                                    Debug.Log($"[TopViewMouseCastingStrategy] Pinpoint Unit Change for {targetObj.name}: {matchingUnitData.CurrentUnit} -> {selectedUnitData.UnitType}");

                                    if (changeService != null)
                                    {
                                        var playerStatComp = UnityEngine.Object.FindFirstObjectByType<CharacterSystem.CharacterStatComponent>();
                                        var casterStatData = playerStatComp?.StatSystem?.RuntimeData;
                                        changeService.ChangeUnit(targetObj, matchingUnitData, selectedUnitData, newValue, casterStatData, null, ownerObject);
                                    }
                                }
                            }
                        }
                        return;
                    }
                }

                if (isMouseClicked)
                {
                    Debug.LogWarning($"[TopViewMouseCastingStrategy] SphereCast Hit Colliders ({hits.Length}), but NONE have RuntimeDataUnitGroup attached!");
                }
            }
            else if (isMouseClicked)
            {
                Debug.LogWarning("[TopViewMouseCastingStrategy] SphereCast Hit Failed. No Collider in ray path.");
            }

            // 조준 대상 없을 시 UI 숨김
            HideTargetHoverUI();
        }

        private void ShowTargetHoverUI(string objectName, RuntimeDataUnitGroup targetDataGroup)
        {
            if (targetInfoContainer == null) InitializeUI();
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
            if (targetInfoContainer == null) InitializeUI();
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
            var documents = UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var doc in documents)
            {
                if (doc == null || doc.rootVisualElement == null || doc.rootVisualElement.panel == null) continue;

                var panel = doc.rootVisualElement.panel;
                Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(mouseScreenPos.x, Screen.height - mouseScreenPos.y));
                VisualElement picked = panel.Pick(panelPos);

                if (picked != null && picked != doc.rootVisualElement)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
