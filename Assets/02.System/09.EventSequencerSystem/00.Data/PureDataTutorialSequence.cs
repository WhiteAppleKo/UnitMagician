using UnityEngine;

namespace EventSequencerSystem
{
    /// <summary>
    /// TutorialSequencerLogicSystem이 참조하는 불변 설정값([D] Pure Data)입니다.
    /// 03번(보스AI페이즈시퀀스시스템)의 PureDataBossPattern이 아직 존재하지 않아 값을 빌려올 수 없으므로,
    /// 이 시스템이 독립적으로 컴파일·동작할 수 있도록 자체 SO로 임계값/타임아웃을 보유합니다
    /// (06번 기획서 6.9절 정정 사항 및 Goal 프롬프트 "배경" 절 참고 — 03번 구현 시점에 값 소유권 재검토 예정).
    /// </summary>
    [CreateAssetMenu(fileName = "PureDataTutorialSequence_", menuName = "EventSequencerSystem/PureDataTutorialSequence")]
    public class PureDataTutorialSequence : ScriptableObject
    {
        [Header("Boss HP Threshold")]
        [SerializeField] private float bossHpThresholdRatio = 0.5f;

        [Header("Core Hit Timeout")]
        [SerializeField] private float coreHitTimeoutSeconds = 10f;

        /// <summary>보스 HP가 이 비율(현재/최대) 이하로 떨어지면 Stage3(회복/무적 페이즈)로 전환됩니다.</summary>
        public float BossHpThresholdRatio => bossHpThresholdRatio;

        /// <summary>Stage3 진입 후 이 시간(초) 동안 유효 피격이 없으면 타임아웃으로 OnCoreHitFail이 방송됩니다.</summary>
        public float CoreHitTimeoutSeconds => coreHitTimeoutSeconds;
    }
}