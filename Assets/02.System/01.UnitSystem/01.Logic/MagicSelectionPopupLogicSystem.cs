using System;
using System.Collections.Generic;
using VContainer;
using VContainer.Unity;

namespace UnitSystem
{
    /// <summary>
    /// 시간정지(완전정지) 중 마법 락온 후 "어떤 마법을 쓸지"를 고르는 33원정대 스타일 Q/W/E/F 선택지 팝업의
    /// 열림/닫힘 상태와 선택 확정 로직을 담당하는 순수 C# 로직 시스템입니다(12번 기획서 §3/§4).
    ///
    /// 다중 락온 누적(RuntimeDataMultiLockOn), 퀵슬롯 선택(RuntimeDataUnitQuickSlot),
    /// 시간정지 해제(RuntimeDataTimeSlow) 체인은 이미 완성돼 있으므로 이 클래스는 그 체인을 "트리거"만 합니다.
    /// MagicLockOnComponent.OnDisable() → UnitCasterSystem.ExecuteBatchCast() 로 이어지는 기존 로직은 수정하지 않습니다.
    /// </summary>
    public class MagicSelectionPopupLogicSystem : IMagicSelectionPopupLogic, IInitializable, IDisposable
    {
        private readonly RuntimeDataMultiLockOn multiLockOnData;
        private readonly RuntimeDataUnitQuickSlot quickSlotData;
        private readonly CharacterSystem.RuntimeDataTimeSlow timeSlowData;
        private readonly IUnitCatalogService catalogService;
        private readonly RuntimeDataMagicFavoriteSlots favoriteSlots;

        private bool isPopupOpen;
        private bool isFullListOpen;

        public bool IsPopupOpen => isPopupOpen;
        public bool IsFullListOpen => isFullListOpen;

        public event Action OnPopupOpened;
        public event Action OnPopupClosed;
        public event Action<bool> OnFullListToggled;

        [Inject]
        public MagicSelectionPopupLogicSystem(
            RuntimeDataMultiLockOn multiLockOnData,
            RuntimeDataUnitQuickSlot quickSlotData,
            CharacterSystem.RuntimeDataTimeSlow timeSlowData,
            IUnitCatalogService catalogService,
            RuntimeDataMagicFavoriteSlots favoriteSlots)
        {
            this.multiLockOnData = multiLockOnData;
            this.quickSlotData = quickSlotData;
            this.timeSlowData = timeSlowData;
            this.catalogService = catalogService;
            this.favoriteSlots = favoriteSlots;
        }

        public void Initialize()
        {
            if (multiLockOnData == null) return;

            multiLockOnData.OnTargetAdded -= HandleTargetAdded;
            multiLockOnData.OnTargetAdded += HandleTargetAdded;
            multiLockOnData.OnTargetRemoved -= HandleTargetRemoved;
            multiLockOnData.OnTargetRemoved += HandleTargetRemoved;
            multiLockOnData.OnTargetsCleared -= HandleTargetsCleared;
            multiLockOnData.OnTargetsCleared += HandleTargetsCleared;
        }

        public void Dispose()
        {
            if (multiLockOnData == null) return;

            multiLockOnData.OnTargetAdded -= HandleTargetAdded;
            multiLockOnData.OnTargetRemoved -= HandleTargetRemoved;
            multiLockOnData.OnTargetsCleared -= HandleTargetsCleared;
        }

        private void HandleTargetAdded(RuntimeDataUnitGroup target)
        {
            // 최초 1개(0->1)가 되는 시점만 오픈. 이미 열려 있으면(추가 락온) 아무 것도 하지 않는다 — 팝업 유지.
            if (isPopupOpen) return;
            if (multiLockOnData.TargetCount != 1) return;

            OpenPopup();
        }

        private void HandleTargetRemoved(RuntimeDataUnitGroup target)
        {
            if (isPopupOpen && multiLockOnData.TargetCount == 0)
            {
                ClosePopup();
            }
        }

        private void HandleTargetsCleared()
        {
            if (isPopupOpen)
            {
                ClosePopup();
            }
        }

        private void OpenPopup()
        {
            if (isPopupOpen) return;

            isPopupOpen = true;
            OnPopupOpened?.Invoke();
        }

        private void ClosePopup()
        {
            if (!isPopupOpen) return;

            isPopupOpen = false;
            if (isFullListOpen)
            {
                isFullListOpen = false;
                OnFullListToggled?.Invoke(false);
            }

            OnPopupClosed?.Invoke();
        }

        public void OpenFullList()
        {
            if (!isPopupOpen || isFullListOpen) return;

            isFullListOpen = true;
            OnFullListToggled?.Invoke(true);
        }

        public void CloseFullList()
        {
            if (!isFullListOpen) return;

            isFullListOpen = false;
            OnFullListToggled?.Invoke(false);
        }

        public IReadOnlyList<PureDataUnit> GetEquippedUnitsForFullList()
        {
            return catalogService != null ? catalogService.GetEquippedUnits() : Array.Empty<PureDataUnit>();
        }

        /// <summary>
        /// W/E/F 입력에 대응하는 즐겨찾기 슬롯 확정. 슬롯이 비어 있으면 아무 효과가 없다(기획서 §5 체크리스트).
        /// </summary>
        public void ConfirmFavoriteSlot(int index)
        {
            if (!isPopupOpen || favoriteSlots == null) return;

            var unit = favoriteSlots.GetSlot(index);
            if (unit == null) return;

            ConfirmUnit(unit);
        }

        /// <summary>
        /// Q 전체 목록에서 유닛을 직접 클릭했을 때 호출되는 공통 확정 처리.
        /// SelectUnit + SetSlowActive(false) 한 줄이 기존에 완성된 시간정지 해제->일괄 시전 체인을 그대로 재사용한다.
        /// </summary>
        public void ConfirmUnit(PureDataUnit unit)
        {
            if (!isPopupOpen || unit == null) return;

            quickSlotData?.SelectUnit(unit);
            timeSlowData?.SetSlowActive(false);

            // SetSlowActive(false) 호출 체인(OnSlowStateChanged -> PlayerLockOnController.SetSlowMode(false) ->
            // MagicLockOnComponent.OnDisable() -> UnitCasterSystem.ExecuteBatchCast() -> multiLockOnData.ClearTargets())이
            // 동기적으로 HandleTargetsCleared()를 먼저 호출해 ClosePopup()을 끝냈을 수 있으므로, 아래 호출은
            // 가드(!isPopupOpen return)에 의해 중복 발행 없이 안전하게 마무리 역할만 한다.
            ClosePopup();
        }
    }
}
