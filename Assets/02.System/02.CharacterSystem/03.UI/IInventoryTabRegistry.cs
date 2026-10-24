using System.Collections.Generic;

namespace CharacterSystem
{
    /// <summary>
    /// 인벤토리 탭 레지스트리(33번 기획서 §3-2). 등록된 순서대로 탭 버튼이 생기며, 같은 ItemType 탭은 하나만 등록된다.
    /// 새 ItemType 탭은 그 탭 뷰가 여기에 자신을 등록하기만 하면 되고, 인벤토리 창 코드는 고치지 않는다.
    /// </summary>
    public interface IInventoryTabRegistry
    {
        /// <summary>등록된 탭(등록 순서).</summary>
        IReadOnlyList<IInventoryTabView> Tabs { get; }

        /// <summary>탭을 끝에 등록한다. 같은 ItemType 탭이 이미 있으면 false.</summary>
        bool RegisterTab(IInventoryTabView tab);

        /// <summary>탭을 등록 해제한다. 선택 중이던 탭이면 남은 첫 탭이 선택된다.</summary>
        void UnregisterTab(IInventoryTabView tab);
    }
}
