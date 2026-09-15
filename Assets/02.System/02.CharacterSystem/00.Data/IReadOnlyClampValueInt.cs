using System;

namespace CharacterSystem
{
    /// <summary>
    /// ClampValueInt의 읽기 전용 뷰입니다. Increase/Reduce/SetCurrent/SetRange 등 상태 변경 메서드를 노출하지 않습니다.
    /// </summary>
    public interface IReadOnlyClampValueInt
    {
        int MinValue { get; }
        int MaxValue { get; }
        int CurrentValue { get; }
        event Action<int, int> OnValueChanged;
    }
}
