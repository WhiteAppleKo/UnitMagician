using System;

namespace BossPhaseSystem
{
    /// <summary>
    /// 보스 페이즈 진행 상태([D] Runtime Data)입니다. RuntimeDataTutorialSequence(09.EventSequencerSystem)와
    /// 동일한 결로, private set 필드 + 전용 변경 메서드(ChangePhase)로만 상태를 바꾸고 변경 시 이벤트만
    /// 발행합니다. 데이터 스스로 프레임을 갱신(Tick)하지 않습니다(코드_가이드라인 1-1절 Runtime Data 상세 규칙).
    /// </summary>
    public class RuntimeDataBossPhase
    {
        private BossPhaseType currentPhase;
        private BossPhaseType previousPhase;

        public BossPhaseType CurrentPhase => currentPhase;
        public BossPhaseType PreviousPhase => previousPhase;

        /// <summary>페이즈가 실제로 바뀔 때만 발행됩니다(같은 값으로의 재호출은 조기 반환되어 방송되지 않음).</summary>
        public event Action<BossPhaseType, BossPhaseType> OnPhaseChanged;

        public RuntimeDataBossPhase()
        {
            currentPhase = BossPhaseType.Idle;
            previousPhase = BossPhaseType.Idle;
        }

        /// <summary>
        /// 페이즈 전환 전용 메서드입니다. 같은 값이면 조기 반환하여 중복 방송을 막습니다.
        /// </summary>
        public void ChangePhase(BossPhaseType newPhase)
        {
            if (currentPhase == newPhase) return;

            var oldPhase = currentPhase;
            previousPhase = oldPhase;
            currentPhase = newPhase;

            OnPhaseChanged?.Invoke(oldPhase, newPhase);
        }
    }
}
