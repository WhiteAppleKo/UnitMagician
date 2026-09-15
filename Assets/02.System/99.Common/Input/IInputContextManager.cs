using System;

namespace Common.InputSystem
{
    /// <summary>
    /// 입력 컨텍스트(플레이어, 전술 모드, UI 등) 전환 및 상태 관리를 추상화한 인터페이스입니다.
    /// </summary>
    public interface IInputContextManager
    {
        PlayerInputContext PlayerContext { get; }
        TacticalInputContext TacticalContext { get; }
        UIInputContext UIContext { get; }
        IInputContext CurrentContext { get; }

        void PushContext(IInputContext context);
        void PopContext(IInputContext context = null);

        event Action<IInputContext> OnContextChanged;
    }
}
