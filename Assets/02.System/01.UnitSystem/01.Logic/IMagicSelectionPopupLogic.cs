using System;
using System.Collections.Generic;

namespace UnitSystem
{
    /// <summary>
    /// MagicSelectionPopupUIComponent(View)가 실제로 사용하는 MagicSelectionPopupLogicSystem의
    /// 공개 표면만 노출하는 인터페이스입니다(VContainer 가이드라인 — 구체 클래스 직접 주입 금지).
    /// </summary>
    public interface IMagicSelectionPopupLogic
    {
        bool IsPopupOpen { get; }
        bool IsFullListOpen { get; }

        event Action OnPopupOpened;
        event Action OnPopupClosed;
        event Action<bool> OnFullListToggled;

        void ConfirmFavoriteSlot(int index);
        void ConfirmUnit(PureDataUnit unit);
        void OpenFullList();
        void CloseFullList();
        IReadOnlyList<PureDataUnit> GetEquippedUnitsForFullList();
    }
}
