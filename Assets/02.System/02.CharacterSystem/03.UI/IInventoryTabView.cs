using UnityEngine.UIElements;

namespace CharacterSystem
{
    /// <summary>
    /// 인벤토리 창의 탭 하나(33번 기획서 §3-2). 각 탭 뷰는 자기 ItemType, 탭 이름, 내용 그리기 함수를 가진다.
    /// 탭 뷰는 <see cref="IInventoryTabRegistry"/>에 스스로 등록하며, 인벤토리 창 코드는 탭 종류를 알지 못한다.
    /// </summary>
    public interface IInventoryTabView
    {
        /// <summary>이 탭이 보여 주는 항목 종류(레지스트리 키).</summary>
        ItemType ItemType { get; }

        /// <summary>탭 버튼에 표시할 이름.</summary>
        string TabName { get; }

        /// <summary>탭이 선택돼 내용 영역이 이 탭에 맡겨질 때 호출된다. 내용 영역은 비어 있는 상태로 넘어온다.</summary>
        void Show(VisualElement contentRoot);

        /// <summary>다른 탭이 선택되거나 창이 닫힐 때 호출된다. 이후로는 내용 영역을 건드리지 않는다.</summary>
        void Hide();
    }
}
