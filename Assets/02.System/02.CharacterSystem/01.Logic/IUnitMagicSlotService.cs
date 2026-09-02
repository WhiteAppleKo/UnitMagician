using System;
using System.Collections.Generic;
using UnitSystem;

namespace CharacterSystem
{
    /// <summary>
    /// 마법 슬롯 관리 및 현재 마법 발동 기능을 추상화한 DIP 인터페이스입니다.
    /// </summary>
    public interface IUnitMagicSlotService
    {
        PureDataUnit CurrentMagic { get; }
        IReadOnlyList<PureDataUnit> MagicSlots { get; }
        event Action<PureDataUnit> OnMagicChanged;
        event Action<bool, string> OnFireResult;
        void SelectSlot(int index);
        void AddMagic(PureDataUnit magic);
        bool FireCurrentMagic();
    }
}
