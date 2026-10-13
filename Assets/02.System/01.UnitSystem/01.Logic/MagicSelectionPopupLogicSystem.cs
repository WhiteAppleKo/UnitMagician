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
    /// 13번 기획서(12번 정정): 락온할 때마다(RuntimeDataMultiLockOn.PendingTarget 설정) 팝업이 열리고,
    /// 선택은 그 락온에 쓸 마법을 "지정"(CommitPending)할 뿐 시간정지는 해제하지 않습니다.
    /// 쌓인 (대상, 마법) 쌍은 시간정지 해제(MagicLockOnComponent.OnDisable → UnitCasterSystem.ExecuteBatchCast)
    /// 또는 이벤트 전환점에서 한꺼번에 발동합니다.
    /// </summary>
    public class MagicSelectionPopupLogicSystem : IMagicSelectionPopupLogic, IInitializable, IDisposable
    {
        private readonly RuntimeDataMultiLockOn multiLockOnData;
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
            IUnitCatalogService catalogService,
            RuntimeDataMagicFavoriteSlots favoriteSlots)
        {
            this.multiLockOnData = multiLockOnData;
            this.catalogService = catalogService;
            this.favoriteSlots = favoriteSlots;
        }

        public void Initialize()
        {
            if (multiLockOnData == null) return;

            multiLockOnData.OnPendingChanged -= HandlePendingChanged;
            multiLockOnData.OnPendingChanged += HandlePendingChanged;
        }

        public void Dispose()
        {
            if (multiLockOnData == null) return;

            multiLockOnData.OnPendingChanged -= HandlePendingChanged;
        }

        /// <summary>
        /// 락온(지정 시작)할 때마다 팝업을 연다 — 같은 대상을 다시 락온해도 마찬가지.
        /// 지정 확정/취소/큐 비움 등으로 PendingTarget이 비면 팝업을 닫는다.
        /// </summary>
        private void HandlePendingChanged()
        {
            if (multiLockOnData.PendingTarget != null)
            {
                OpenPopup();
            }
            else
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
        /// 락온 대기 중인 대상에 이 마법을 "지정"(CommitPending)할 뿐, 시간정지는 해제하지 않는다(13번 기획서).
        /// </summary>
        public void ConfirmUnit(PureDataUnit unit)
        {
            if (!isPopupOpen || unit == null) return;

            // CommitPending → OnPendingChanged(PendingTarget == null) → HandlePendingChanged → ClosePopup 이 동기적으로 일어난다.
            multiLockOnData?.CommitPending(unit);

            // 대기 대상이 없는 비정상 상태에서도 팝업이 남지 않도록 마무리(이미 닫혔다면 가드로 무시).
            ClosePopup();
        }
    }
}
