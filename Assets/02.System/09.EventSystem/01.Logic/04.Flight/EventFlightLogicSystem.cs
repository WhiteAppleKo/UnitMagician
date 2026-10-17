using System;
using UnityEngine;
using VContainer.Unity;

namespace GameEvent
{
    /// <summary>
    /// 3단계 이벤트(아군 투사체 비행)의 구간/진행도/전환점을 계산하는 순수 C# 로직입니다
    /// ([L] 유니티 생명주기 연동 로직 - ITickable, 13번 기획서 §3 "접근/이탈 곡선과 전환점").
    /// MonoBehaviour/Transform을 참조하지 않고 IEventFlightTracker가 주는 Vector3 값만 사용합니다.
    ///
    /// 이벤트 진입(Begin) 시: 플레이어 P와 보스 B를 스냅샷으로 고정하고 e = normalize(수평(B - P))를 구한다.
    /// 발사 지점은 L = P + R(+LaunchAngleDegrees, 위축) * e * LaunchDistance 이다(22번 기획서 §11 - 사용자 명세
    /// "보스-플레이어 직선 기준 플레이어 우측으로 120도 정도, 시야각에서 벗어난 지점"; 위에서 볼 때 시계 방향 회전, 높이는 P와 같다).
    /// 투사체는 L에서 B를 향해 직선으로 날아가므로 비행 방향은 d = normalize(B - L)이다(e가 아님 - 플레이어는 사선 밖에 있다).
    /// 매 프레임: s = dot(X - P, d). s &lt; 0 이면 접근(진행도 t = dot(X - L, d) / dot(P - L, d)),
    /// s &gt;= 0 이면 이탈(진행도 u = dot(X - P, d) / dot(B - P, d)). s = 0 인 지점은 직선 위에서 P에 가장 가까운 점이므로
    /// 접근 -> 이탈로 처음 넘어가는 프레임(전환점)이 투사체-플레이어 최근접 순간이다. 래치로 사이클당 한 번만 OnTurnaround를 발행한다.
    /// 모든 진행도는 0~1로 클램프한다. 0으로 나누는 경우(LaunchDistance &lt;= 0, 수평 P == B)만 방어한다.
    /// 발사 수치(각도/거리/투사체 속도/데미지/추적 높이 오프셋)는 이벤트 SO(EventProjectileLaunchAction)가 IEventLaunchSettings로 Begin에 넘기며 이 클래스에는 숫자가 없다.
    /// </summary>
    public class EventFlightLogicSystem : IEventFlightLogic, ITickable
    {
        private const float MinLength = 0.0001f;

        private readonly IEventFlightTracker tracker;

        private IEventLaunchSettings settings;
        private bool isActive;
        private bool turnaroundLatched;
        private bool projectileLostLatched;
        private Vector3 launchPosition;
        private Vector3 playerPosition;
        private Vector3 flightDirection;
        private float approachLength;
        private float exitLength;

        private EventFlightPhase phase;
        private float progress;

        public bool IsActive => isActive;
        public EventFlightPhase Phase => phase;
        public float Progress => progress;
        public Vector3 LaunchPosition => launchPosition;

        public event Action<EventFlightPhase, float> OnFlightChanged;
        public event Action OnTurnaround;
        public event Action OnFlightStarted;
        public event Action OnFlightEnded;
        public event Action OnProjectileLost;

        public EventFlightLogicSystem(IEventFlightTracker tracker)
        {
            this.tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        }

        public void Begin(IEventLaunchSettings launchSettings)
        {
            // 재도전 시 이전 사이클 상태/래치를 완전히 초기화한다.
            End();

            settings = launchSettings ?? throw new ArgumentNullException(nameof(launchSettings));
            tracker.CaptureEndpoints(settings.TrackingHeightOffset);
            Vector3 p = tracker.PlayerPosition;
            Vector3 b = tracker.BossPosition;
            float launchDistance = settings.LaunchDistance;

            Vector3 horizontalToBoss = Vector3.ProjectOnPlane(b - p, Vector3.up);
            if (launchDistance <= 0f || horizontalToBoss.sqrMagnitude < MinLength * MinLength)
            {
                Debug.LogWarning($"[EventFlightLogicSystem] 비행 구간을 시작할 수 없습니다(0으로 나누는 설정): " +
                                 $"LaunchDistance={launchDistance}, 수평 |B-P|={horizontalToBoss.magnitude}");
                return;
            }

            playerPosition = p;
            launchPosition = ResolveLaunchPosition(p, horizontalToBoss.normalized, launchDistance);

            // 투사체는 L에서 B로 직선 비행한다. 접근/이탈 판정과 진행도는 이 비행 방향 기준이다(L, P, B가 더 이상 한 직선이 아님).
            flightDirection = (b - launchPosition).normalized;
            approachLength = Vector3.Dot(p - launchPosition, flightDirection);
            exitLength = Vector3.Dot(b - p, flightDirection);

            tracker.BeginFlight(launchPosition, settings.ProjectileSpeed, settings.ProjectileDamage);

            isActive = true;
            turnaroundLatched = false;
            phase = EventFlightPhase.Approach;
            progress = 0f;
            OnFlightStarted?.Invoke();
        }

        /// <summary>
        /// L = P + R * e * launchDistance. e는 P에서 B로 향하는 수평 단위 방향이고 R은 위 축 기준 +LaunchAngleDegrees 회전
        /// (Unity는 왼손 좌표계라 +Y 축 양의 회전이 위에서 볼 때 시계 방향 = 플레이어가 보스를 볼 때의 오른쪽).
        /// </summary>
        private Vector3 ResolveLaunchPosition(Vector3 p, Vector3 horizontalToBoss, float launchDistance)
        {
            Vector3 launchDirection = Quaternion.AngleAxis(settings.LaunchAngleDegrees, Vector3.up) * horizontalToBoss;
            return p + launchDirection * launchDistance;
        }

        public void CloseLaunchWindow()
        {
            if (!isActive) return;

            tracker.CloseLaunchWindow();

            if (!tracker.HasProjectile)
            {
                Debug.LogWarning("[EventFlightLogicSystem] 이벤트 시작 방송 중 아군 투사체가 발사되지 않았습니다 " +
                                 "(아군 AttackBehavior/타겟 누락 등). 비행 구간은 투사체가 없어 진행되지 않습니다.");
            }
        }

        public void End()
        {
            bool wasActive = isActive;
            if (wasActive)
            {
                tracker.EndFlight();
            }

            isActive = false;
            turnaroundLatched = false;
            projectileLostLatched = false;
            phase = EventFlightPhase.None;
            progress = 0f;

            if (wasActive) OnFlightEnded?.Invoke();
        }

        public bool IsEventProjectile(GameObject source)
        {
            return isActive && tracker.IsEventProjectile(source);
        }

        public void Tick()
        {
            if (!isActive) return;

            if (!tracker.HasProjectile)
            {
                // 발사 후(참조를 한 번이라도 받은 뒤) 투사체가 사라졌는데 아직 판정이 없다면 소프트락 방지를 위해 소실을 알린다.
                // 발사 전(아직 참조 없음)은 소실이 아니다. 정상 명중 소멸은 판정(End 호출)이 먼저 처리되어 isActive가 이미 false이므로 여기 오지 않는다.
                // 구독자가 End()를 호출해 isActive가 꺼지므로 사이클당 한 번만 발행되고, 래치로 이중 발행도 막는다.
                if (tracker.HasAcquiredProjectile && !projectileLostLatched)
                {
                    projectileLostLatched = true;
                    OnProjectileLost?.Invoke();
                }

                return;
            }

            Vector3 x = tracker.ProjectilePosition;
            float s = Vector3.Dot(x - playerPosition, flightDirection);

            if (phase == EventFlightPhase.Approach)
            {
                if (s >= 0f && !turnaroundLatched)
                {
                    // 전환점: 음 -> 양으로 처음 바뀌는 프레임(래치로 한 번만). 신호를 먼저 방송해 구독자(마법 발동)가
                    // 접근 구간 최저 timeScale 상태에서 실행되게 한 뒤, 구간을 이탈로 전환한다.
                    turnaroundLatched = true;
                    OnTurnaround?.Invoke();
                    SetFlight(EventFlightPhase.Exit, ExitProgress(s));
                    return;
                }

                float t = approachLength > MinLength ? Vector3.Dot(x - launchPosition, flightDirection) / approachLength : 0f;
                SetFlight(EventFlightPhase.Approach, Mathf.Clamp01(t));
            }
            else if (phase == EventFlightPhase.Exit)
            {
                SetFlight(EventFlightPhase.Exit, ExitProgress(s));
            }
        }

        private float ExitProgress(float s)
        {
            if (exitLength <= MinLength) return 1f;
            return Mathf.Clamp01(s / exitLength);
        }

        private void SetFlight(EventFlightPhase newPhase, float newProgress)
        {
            if (newPhase == phase && Mathf.Approximately(newProgress, progress)) return;

            phase = newPhase;
            progress = newProgress;
            OnFlightChanged?.Invoke(phase, progress);
        }
    }
}
