using System;

namespace CharacterSystem
{
    /// <summary>
    /// Time.timeScale 일시정지 요청 서비스(17번 기획서). UI(Option, 인벤토리 등)는 Time.timeScale을 직접 쓰지 않고
    /// 열릴 때 SetPaused(this, true), 닫힐 때 SetPaused(this, false)만 호출합니다.
    /// </summary>
    public interface ITimeScaleService
    {
        bool IsPaused { get; }
        float EffectiveScale { get; }
        event Action<bool> OnPauseStateChanged;

        /// <summary>요청자별 일시정지 요청을 올리거나(true) 내립니다(false). 같은 요청자의 중복 호출은 멱등입니다.</summary>
        void SetPaused(object requester, bool paused);
    }
}
