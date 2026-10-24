using System;
using System.Collections.Generic;

namespace CharacterSystem
{
    /// <summary>
    /// [RuntimeData] 캐릭터 인벤토리(33번 기획서 §3-1). 캐릭터 스코프(CharacterLifetimeScope)에 하나 있고,
    /// 항목을 PureData 에셋 참조(<see cref="IInventoryItem"/>) 그대로 ItemType별로 넣은 순서대로 보관한다.
    /// 습득할 때 RuntimeData(예: RuntimeDataUnit)를 만들지 않는다 — RuntimeData는 장착할 때 생긴다(③).
    /// 같은 PureData 에셋(참조 동일)은 중복 보관하지 않는다. 같은 클래스라도 별개 에셋이면 각각 보관한다.
    /// 상태 변경은 <see cref="TryAdd"/>/<see cref="Remove"/>로만 하고, 바뀔 때 이벤트만 발행한다.
    /// </summary>
    public class RuntimeDataInventory
    {
        private static readonly IReadOnlyList<IInventoryItem> EmptyItems = Array.Empty<IInventoryItem>();

        private readonly Dictionary<ItemType, List<IInventoryItem>> itemsByType = new Dictionary<ItemType, List<IInventoryItem>>();
        private readonly HashSet<IInventoryItem> allItems = new HashSet<IInventoryItem>(ReferenceComparer.Instance);
        private readonly List<ItemType> itemTypes = new List<ItemType>();

        /// <summary>항목이 추가됐을 때(추가된 PureData 항목).</summary>
        public event Action<IInventoryItem> OnItemAdded;
        /// <summary>항목이 제거됐을 때(제거된 PureData 항목).</summary>
        public event Action<IInventoryItem> OnItemRemoved;

        /// <summary>항목이 하나 이상 있는 종류 목록(처음 들어온 순서).</summary>
        public IReadOnlyList<ItemType> ItemTypes => itemTypes;

        /// <summary>그 종류의 항목(PureData)을 넣은 순서대로 반환한다. 없으면 빈 목록.</summary>
        public IReadOnlyList<IInventoryItem> GetItems(ItemType itemType)
        {
            return itemsByType.TryGetValue(itemType, out var list) ? list : EmptyItems;
        }

        /// <summary>같은 PureData 에셋(참조 동일)이 있는지.</summary>
        public bool Contains(IInventoryItem item)
        {
            if (IsNullItem(item)) return false;
            return allItems.Contains(item);
        }

        /// <summary>같은 에셋이 없으면 그 종류 목록 끝에 추가하고 <see cref="OnItemAdded"/>를 발행한다. 이미 있으면 false.</summary>
        public bool TryAdd(IInventoryItem item)
        {
            if (IsNullItem(item)) return false;
            if (!allItems.Add(item)) return false;

            if (!itemsByType.TryGetValue(item.ItemType, out var list))
            {
                list = new List<IInventoryItem>();
                itemsByType.Add(item.ItemType, list);
            }

            list.Add(item);
            if (list.Count == 1) itemTypes.Add(item.ItemType);

            OnItemAdded?.Invoke(item);
            return true;
        }

        /// <summary>항목을 제거하고 <see cref="OnItemRemoved"/>를 발행한다(지금 호출처는 없고, 확장용). 없으면 false.</summary>
        public bool Remove(IInventoryItem item)
        {
            if (IsNullItem(item)) return false;
            if (!allItems.Remove(item)) return false;

            if (itemsByType.TryGetValue(item.ItemType, out var list))
            {
                list.Remove(item);
                if (list.Count == 0)
                {
                    itemsByType.Remove(item.ItemType);
                    itemTypes.Remove(item.ItemType);
                }
            }

            OnItemRemoved?.Invoke(item);
            return true;
        }

        // 항목은 ScriptableObject(PureData)이므로 파괴된 에셋도 null로 본다(UnityEngine.Object의 == 재정의).
        private static bool IsNullItem(IInventoryItem item)
        {
            if (item == null) return true;
            return item is UnityEngine.Object unityObject && unityObject == null;
        }

        /// <summary>항목 동일성은 에셋 참조 동일로만 판단한다.</summary>
        private sealed class ReferenceComparer : IEqualityComparer<IInventoryItem>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();

            public bool Equals(IInventoryItem x, IInventoryItem y) => ReferenceEquals(x, y);

            public int GetHashCode(IInventoryItem obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
