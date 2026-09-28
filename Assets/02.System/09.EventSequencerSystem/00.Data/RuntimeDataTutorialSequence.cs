using System;

namespace EventSequencerSystem
{
    /// <summary>
    /// 튜토리얼 진행 상태([D] Runtime Data)입니다. RuntimeDataPlayerState(02.CharacterSystem)와 동일한 결로,
    /// private set 필드 + 전용 변경 메서드(ChangeStage)로만 상태를 바꾸고, 변경 시 이벤트만 발행합니다.
    /// 데이터 스스로 프레임 갱신을 돌지 않습니다(코드_가이드라인 1-1절 Runtime Data 상세 규칙 준수).
    /// </summary>
    public class RuntimeDataTutorialSequence
    {
        private TutorialStage currentStage;
        private TutorialStage previousStage;

        public TutorialStage CurrentStage => currentStage;
        public TutorialStage PreviousStage => previousStage;

        /// <summary>단계가 실제로 바뀔 때만 발행됩니다(같은 값으로의 재호출은 조기 반환되어 방송되지 않음).</summary>
        public event Action<TutorialStage, TutorialStage> OnStageChanged;

        public RuntimeDataTutorialSequence()
        {
            currentStage = TutorialStage.Stage1;
            previousStage = TutorialStage.Stage1;
        }

        /// <summary>
        /// 튜토리얼 단계 전환 전용 메서드입니다. 같은 값이면 조기 반환하여 중복 방송을 막습니다
        /// (06번 기획서 6.5절 "이미 처리된 단계의 신호 무시" 요구사항).
        /// </summary>
        public void ChangeStage(TutorialStage newStage)
        {
            if (currentStage == newStage) return;

            var oldStage = currentStage;
            previousStage = oldStage;
            currentStage = newStage;

            OnStageChanged?.Invoke(oldStage, newStage);
        }
    }
}