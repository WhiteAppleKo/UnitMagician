using UnityEngine;

namespace GameEvent
{
    /// <summary>
    /// [Legacy] Goal 51에서 이 수치들은 이벤트 SO(BossHpRatioTrigger/CoreHitCompletion/EventProjectileLaunchAction/TimeSlowOverrideAction/CameraOverrideAction/BossRecoverAction)로
    /// 이전되었고 이 SO는 어디서도 읽지 않습니다. 이력 보존용 격리 파일입니다.
    /// 이벤트 시퀀서(3단계 이벤트)의 연출/판정 수치를 한 곳에 모은 불변 설정값([D] Pure Data, "이벤트 SO")입니다.
    /// 사용자 확정(22번 기획서 §11): "모든 값은 이벤트SO를 통해 값을 조정할 수 있어야한다." 그래서 이벤트 연출에만 쓰는 수치
    /// (HP 임계값, 발사 지점 각도/거리, 감속 최저 timeScale/곡선, 이벤트 카메라 블렌드/FOV/눈 높이)는 전부 이 SO 하나의 인스펙터에서 조정한다.
    /// 감속(CharacterSystem)과 카메라(CameraMovement)는 다른 VContainer 스코프 트리에 있으므로, 이 SO가 구현하는 읽기 전용 인터페이스
    /// (IEventTimeSlowSettings / IEventCameraSettings)로만 값을 읽는다(DIP).
    /// 03번(보스AI페이즈시퀀스시스템)의 PureDataBossPattern이 아직 존재하지 않아 값을 빌려올 수 없으므로,
    /// 이 시스템이 독립적으로 컴파일·동작할 수 있도록 자체 SO로 임계값/성공 기준 데미지를 보유합니다
    /// (06번 기획서 6.9절 정정 사항 및 Goal 프롬프트 "배경" 절 참고 — 03번 구현 시점에 값 소유권 재검토 예정).
    /// 13번 기획서로 시간 기반 타임아웃(과거 coreHitTimeoutSeconds)은 제거되었다 - 3단계 판정은 명중 결과로만 한다.
    /// </summary>
    [CreateAssetMenu(fileName = "PureDataTutorialSequence_", menuName = "GameEvent/PureDataTutorialSequence")]
    public class PureDataTutorialSequence : ScriptableObject, IEventTimeSlowSettings, IEventCameraSettings, IEventLaunchSettings
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

        [Header("Event Launch Point (22번 기획서 §11 - 발사 지점은 플레이어 우측 N도)")]
        [Tooltip("발사 지점 L의 방향 각도(도). 이벤트 시작 시점의 플레이어 P에서 보스 B로 향하는 수평 방향을 0도로 보고, 위에서 내려다볼 때 " +
                 "오른쪽(시계 방향)으로 이 각도만큼 회전한 방향에 L을 둡니다(사용자 명세 '플레이어 우측 120도 정도, 시야각에서 벗어난 지점'). " +
                 "음수면 왼쪽입니다. 90도 안팎 미만이면 L이 플레이어 시야각 안에 들어오고, 보스 쪽(0도 부근)에 가까울수록 투사체가 플레이어와 겹쳐 스칩니다.")]
        [SerializeField] private float eventLaunchAngleDegrees = 120f;
        [Tooltip("이벤트 진입 시 발사 지점 L이 플레이어 P에서 떨어지는 수평 거리(미터). 0 이하이면 비행 구간 계산을 시작하지 않습니다.")]
        [SerializeField] private float eventLaunchDistance = 12f;

        [Header("Event Slow (3-Stage Approach/Exit Curve)")]
        [Tooltip("접근->이탈 전환점(투사체가 플레이어를 스치는 순간)의 최저 TimeScale. 0보다 큰 '거의 정지' 값(예: 0.02~0.05). " +
                 "0이면 투사체가 전환점에 영영 닿지 못하므로 허용하지 않습니다.")]
        [Range(0.001f, 1f)]
        [SerializeField] private float eventSlowMinTimeScale = 0.03f;
        [Tooltip("접근 구간 완급 곡선: x=접근 진행도 t(0~1), y=감속 비율(0=정상 속도, 1=최저 TimeScale). timeScale = lerp(1, min, curve(t)).")]
        [SerializeField] private AnimationCurve eventApproachCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [Tooltip("이탈 구간 완급 곡선: x=이탈 진행도 u(0~1), y=회복 비율(0=최저 TimeScale, 1=정상 속도). timeScale = lerp(min, 1, curve(u)).")]
        [SerializeField] private AnimationCurve eventExitCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Event Camera (First Person)")]
        [Tooltip("평상시 숄더 가상 카메라 <-> 이벤트 1인칭 가상 카메라 전환(Cinemachine 블렌드) 시간(초). 이벤트 시작 때 카메라가 '천천히' 투사체 쪽으로 돌아가는 시간입니다. 0 이하면 즉시 컷")]
        [SerializeField] private float eventCameraBlendTime = 0.5f;
        [Tooltip("이벤트 1인칭 가상 카메라의 세로 시야각(FOV)")]
        [SerializeField] private float eventCameraFieldOfView = 70f;
        [Tooltip("이벤트 1인칭 카메라(캐릭터 눈) 높이. 플레이어 루트 위치 기준 위쪽 오프셋(m)")]
        [SerializeField] private float eventCameraEyeHeight = 1.68f;

        /// <summary>보스 HP가 이 비율(현재/최대) 이하로 떨어지면 Stage3(회복/무적 페이즈)로 전환됩니다.</summary>
        public float BossHpThresholdRatio => bossHpThresholdRatio;

        /// <summary>3단계 성공에 필요한 최소 FinalDamage(속성·크기 게이트 통과가 전제). 13번/20번 기획서.</summary>
        public int CoreHitRequiredDamage => coreHitRequiredDamage;

        /// <summary>속성 게이트 통과 조건: 투사체 온도 &lt; 이 값. 20번 기획서.</summary>
        public float RequiredMaxTemperature => requiredMaxTemperature;

        /// <summary>크기 게이트 통과 조건: 투사체 질량 비율 &gt;= 이 값. 20번 기획서.</summary>
        public float RequiredMassRatio => requiredMassRatio;

        /// <summary>발사 지점 L의 방향 각도(도). P-&gt;B 수평 방향 기준 위에서 볼 때 오른쪽(시계 방향) 회전. 22번 기획서 §11.</summary>
        public float EventLaunchAngleDegrees => eventLaunchAngleDegrees;

        /// <summary>발사 지점 L이 플레이어 P에서 떨어지는 수평 거리. 13번 기획서 §3, 22번 기획서 §11.</summary>
        public float EventLaunchDistance => eventLaunchDistance;

        public float EventSlowMinTimeScale => Mathf.Max(0.001f, eventSlowMinTimeScale);
        public AnimationCurve EventApproachCurve => eventApproachCurve;
        public AnimationCurve EventExitCurve => eventExitCurve;

        public float EventCameraBlendTime => eventCameraBlendTime;
        public EventCameraBlendStyle EventCameraBlendStyle => EventCameraBlendStyle.EaseInOut;
        public float EventCameraFieldOfView => eventCameraFieldOfView;
        public float EventCameraEyeHeight => eventCameraEyeHeight;

        // IEventLaunchSettings(레거시 호환): 발사 각도/거리는 이 SO의 값, 투사체 속도/데미지 지정은 없음(0 = NPC 데이터 값), 추적 높이 오프셋은 기존 기본값.
        public float LaunchAngleDegrees => eventLaunchAngleDegrees;
        public float LaunchDistance => eventLaunchDistance;
        public float ProjectileSpeed => 0f;
        public int ProjectileDamage => 0;
        public Vector3 TrackingHeightOffset => Vector3.up;
    }
}
