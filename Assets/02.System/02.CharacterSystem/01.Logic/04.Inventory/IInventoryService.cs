using System.Collections.Generic;

namespace CharacterSystem
{
    /// <summary>
    /// 캐릭터 인벤토리 로직 계약(33번 기획서 §3-1). 보유 확인·습득은 모두 이 인터페이스로 묻는다
    /// (필드 습득·디버그 습득·마법 탭·장착 전 보유 검사).
    /// </summary>
    public interface IInventoryService
    {
        /// <summary>PureData 항목을 인벤토리에 넣는다. 같은 에셋이 이미 있으면 넣지 않고 false.</summary>
        bool TryAcquire(IInventoryItem item);

        /// <summary>같은 PureData 에셋을 보유 중인지.</summary>
        bool IsPossessed(IInventoryItem item);

        /// <summary>그 종류 항목(PureData)을 넣은 순서대로.</summary>
        IReadOnlyList<IInventoryItem> GetItems(ItemType itemType);
    }
}
