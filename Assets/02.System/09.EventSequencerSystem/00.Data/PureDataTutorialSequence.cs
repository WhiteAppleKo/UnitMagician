using UnityEngine;

namespace EventSequencerSystem
{
    /// <summary>
    /// TutorialSequencerLogicSystem이 참조하는 불변 설정값([D] Pure Data)입니다.
    /// 03번(보스AI페이즈시퀀스시스템)의 PureDataBossPattern이 아직 존재하지 않아 값을 빌려올 수 없으므로,
    /// 이 시스템이 독립적으로 컴파일·동작할 수 있도록 자체 SO로 임계값/성공 기준 데미지를 보유합니다
    /// (06번 기획서 6.9절 정정 사항 및 Goal 프롬프트 "배경" 절 참고 — 03번 구현 시점에 값 소유권 재검토 예정).
    /// 13번 기획서로 시간 기반 타임아웃(과거 coreHitTimeoutSeconds)은 제거되었다 - 3단계 판정은 명중 결과로만 한다.
    /// </summary>
    [CreateAssetMenu(fileName = "PureDataTutorialSequence_", menuName = "EventSequencerSystem/PureDataTutorialSequence")]
    public class PureDataTutorialSequence : ScriptableObject
    {
        [Header("Boss HP Threshold")]
        [SerializeField] private float bossHpThresholdRatio = 0.5f;

        [Header("Core Hit Success Condition (13번 기획서 §3 - 3단계 판정은 명중 결과로만)")]
        [Tooltip("3단계 성공에 필요한 최소 FinalDamage. 성공 = 속성 게이트 통과 AND 크기 게이트 통과 AND FinalDamage >= 이 값(세 조건을 개별 확인, 20번 기획서). " +
                 "얼음만 쓰고 크기를 키우지 않은 경우는 크기 게이트에서 실패, 얼음+크기 확대를 쓴 경우는 충족하도록 정한다 " +
                 "(FinalDamage = CollisionDamageTrigger.baseDamage x 질량 비율, AttributeGateStep).")]
        [SerializeField] private int coreHitRequiredDamage = 20;

        [Header("Gate Requirements (20번 기획서 - 보스 회복 페이즈의 게이트 요구값, 판정 코드에는 숫자가 없다)")]
        [Tooltip("속성 게이트: 투사체 온도가 이 값 미만이어야 통과(기본 0 = 얼음 계열).")]
        [SerializeField] private float requiredMaxTemperature = 0f;
        [Tooltip("크기 게이트: 투사체 질량 비율(현재/원본)이 이 값 이상이어야 통과.")]
        [SerializeField] private float requiredMassRatio = 1.5f;

        [Header("Event Flight (3-Stage Approach/Exit)")]
        [Tooltip("이벤트 진입 시 아군 투사체의 발사 지점 L이 플레이어 뒤쪽 축(보스->플레이어 방향)에서 플레이어로부터 떨어지는 거리(미터). " +
                 "여기에 아래 이격 거리만큼 옆으로 옮긴 위치가 최종 L입니다. 0 이하이면 비행 구간 계산을 시작하지 않습니다.")]
        [SerializeField] private float eventLaunchDistance = 12f;
        [Tooltip("투사체 비행 직선(L->B)이 플레이어 P에서 떨어져 지나가는 최소 거리(미터, 수평 방향). 플레이어가 사선 위에 놓이지 않도록 L을 옆으로 옮겨 이 거리를 만듭니다. " +
                 "부호가 좌우입니다: 양수 = 플레이어가 보스를 바라볼 때 L이 오른쪽, 음수 = 왼쪽(투사체도 플레이어의 그쪽 옆으로 스칩니다). 0이면 L, P, B가 한 직선입니다(이격 없음). " +
                 "플레이어-보스 거리의 90%를 넘으면 그 값으로 제한됩니다(보스에 맞는 직선이 이보다 멀리 플레이어를 비켜갈 수 없음).")]
        [SerializeField] private float eventLaunchSideClearance = 3f;

        /// <summary>보스 HP가 이 비율(현재/최대) 이하로 떨어지면 Stage3(회복/무적 페이즈)로 전환됩니다.</summary>
        public float BossHpThresholdRatio => bossHpThresholdRatio;

        /// <summary>3단계 성공에 필요한 최소 FinalDamage(속성·크기 게이트 통과가 전제). 13번/20번 기획서.</summary>
        public int CoreHitRequiredDamage => coreHitRequiredDamage;

        /// <summary>속성 게이트 통과 조건: 투사체 온도 &lt; 이 값. 20번 기획서.</summary>
        public float RequiredMaxTemperature => requiredMaxTemperature;

        /// <summary>크기 게이트 통과 조건: 투사체 질량 비율 &gt;= 이 값. 20번 기획서.</summary>
        public float RequiredMassRatio => requiredMassRatio;

        /// <summary>발사 지점 L = P - d * EventLaunchDistance + (옆 방향 이동)의 뒤쪽 축 거리(d = normalize(B - P)). 13번 기획서 §3.</summary>
        public float EventLaunchDistance => eventLaunchDistance;

        /// <summary>투사체 비행 직선(L-&gt;B)이 플레이어 P에서 떨어져 지나가는 최소 거리. 부호 = 좌우. 13번 기획서 §3, Goal 45.</summary>
        public float EventLaunchSideClearance => eventLaunchSideClearance;
    }
}