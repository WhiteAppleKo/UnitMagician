using System.Collections.Generic;
using VContainer;
using VContainer.Unity;

namespace CharacterSystem
{
    /// <summary>
    /// [System] 캐릭터 인벤토리 로직(33번 기획서 §3-1, 순수 C#, Character 스코프).
    /// PureData 습득(<see cref="TryAcquire"/>)·보유 조회를 <see cref="RuntimeDataInventory"/>에 위임하고,
    /// 초기화 시 플레이어 PureData(<see cref="PureStatData.DefaultUnitMagics"/>)의 시작 마법을 순서대로 지급한다.
    /// RuntimeData를 만들지 않는다(RuntimeDataUnit은 장착할 때 생성 — ③).
    /// </summary>
    public class InventoryLogicSystem : IInventoryService, IInitializable
    {
        private readonly RuntimeDataInventory inventory;
        private readonly PureStatData playerStatData;

        [Inject]
        public InventoryLogicSystem(RuntimeDataInventory inventory, PureStatData playerStatData = null)
        {
            this.inventory = inventory;
            this.playerStatData = playerStatData;
        }

        public void Initialize()
        {
            if (playerStatData == null) return;

            // 시작 보유 마법의 원천은 플레이어 PureData 에셋 값뿐이다(코드·씬에 시작 목록 없음).
            IReadOnlyList<UnitSystem.PureDataUnit> defaultMagics = playerStatData.DefaultUnitMagics;
            if (defaultMagics == null) return;

            for (int i = 0; i < defaultMagics.Count; i++)
            {
                TryAcquire(defaultMagics[i]);
            }
        }

        public bool TryAcquire(IInventoryItem item)
        {
            return inventory.TryAdd(item);
        }

        public bool IsPossessed(IInventoryItem item)
        {
            return inventory.Contains(item);
        }

        public IReadOnlyList<IInventoryItem> GetItems(ItemType itemType)
        {
            return inventory.GetItems(itemType);
        }
    }
}
