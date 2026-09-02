using System;
using System.Collections.Generic;
using UnitSystem;

namespace CharacterSystem
{
    /// <summary>
    /// 마법 슬롯 관리 및 현재 선택된 마법 발동 로직을 담당하는 시스템입니다.
    /// </summary>
    public class UnitMagicSlotSystem : IUnitMagicSlotService
    {
        private readonly ICharacterStatService statSystem;
        private readonly List<PureDataUnit> magicSlots = new List<PureDataUnit>();
        private int currentSlotIndex = 0;

        public IReadOnlyList<PureDataUnit> MagicSlots => magicSlots;
        public PureDataUnit CurrentMagic => (magicSlots.Count > 0 && currentSlotIndex >= 0 && currentSlotIndex < magicSlots.Count) ? magicSlots[currentSlotIndex] : null;

        public event Action<PureDataUnit> OnMagicChanged;
        public event Action<bool, string> OnFireResult;

        public UnitMagicSlotSystem(ICharacterStatService statSystem, IEnumerable<PureDataUnit> initialMagics = null)
        {
            this.statSystem = statSystem ?? throw new ArgumentNullException(nameof(statSystem));
            if (initialMagics != null)
            {
                magicSlots.AddRange(initialMagics);
            }
        }

        public void SelectSlot(int index)
        {
            if (index < 0 || index >= magicSlots.Count) return;
            currentSlotIndex = index;
            OnMagicChanged?.Invoke(CurrentMagic);
        }

        public void AddMagic(PureDataUnit magic)
        {
            if (magic == null) return;
            magicSlots.Add(magic);
            if (magicSlots.Count == 1)
            {
                SelectSlot(0);
            }
        }

        public bool FireCurrentMagic()
        {
            var magic = CurrentMagic;
            if (magic == null)
            {
                OnFireResult?.Invoke(false, "No Magic Equipped");
                return false;
            }

            if (!statSystem.UseMP(magic.BaseCost))
            {
                OnFireResult?.Invoke(false, "Insufficient Mana");
                return false;
            }

            OnFireResult?.Invoke(true, "Casted " + magic.UnitName);
            return true;
        }
    }
}
