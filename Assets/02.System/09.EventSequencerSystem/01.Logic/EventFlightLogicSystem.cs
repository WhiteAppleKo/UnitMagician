using System;
using UnityEngine;
using VContainer.Unity;

namespace EventSequencerSystem
{
    /// <summary>
    /// 3단계 이벤트(아군 투사체 비행)의 구간/진행도/전환점을 계산하는 순수 C# 로직입니다
    /// ([L] 유니티 생명주기 연동 로직 - ITickable, 13번 기획서 §3 "접근/이탈 곡선과 전환점").
    /// MonoBehaviour/Transform을 참조하지 않고 IEventFlightTracker가 주는 Vector3 값만 사용합니다.
    ///
    /// 이벤트 진입(Begin) 시: 플레이어 P와 보스 B를 스냅샷으로 고정하고 a = normalize(B - P)(뒤쪽 축)를 구한다.
    /// 발사 지점은 L = P - a * EventLaunchDistance + side * o 이다. side는 a에 수직인 수평 방향이고 o는 L->B 직선이
    /// P에서 정확히 EventLaunchSideClearance(부호 = 좌우)만큼 떨어져 지나가도록 푼 옆 이동량이다(Goal 45).
    /// 투사체는 L에서 B를 향해 직선으로 날아가므로 비행 방향은 d = normalize(B - L)이다(a가 아님 - 플레이어는 사선 밖에 있다).
    /// 매 프레임: s = dot(X - P, d). s &lt; 0 이면 접근(진행도 t = dot(X - L, d) / dot(P - L, d)),
    /// s &gt;= 0 이면 이탈(진행도 u = dot(X - P, d) / dot(B - P, d)). s = 0 인 지점은 직선 위에서 P에 가장 가까운 점이므로
    /// 접근 -> 이탈로 처음 넘어가는 프레임(전환점)이 투사체-플레이어 최근접 순간이다. 래치로 사이클당 한 번만 OnTurnaround를 발행한다.
    /// 모든 진행도는 0~1로 클램프한다. 0으로 나누는 경우(EventLaunchDistance &lt;= 0, P == B)만 방어한다.
    /// </summary>
    public class EventFlightLogicSystem : IEventFlightLogic, ITickable
    {
        private const float MinLength = 0.0001f;

        // 이격 거리는 L->B 직선이 P에서 떨어지는 수직 거리이므로 |B-P|보다 작아야 풀린다. |B-P|의 이 비율까지만 허용한다.
        private const float MaxClearanceRatio = 0.9f;

        private readonly PureDataTutorialSequence pureData;
        private readonly IEventFlightTracker tracker;

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

        public event Action<EventFlightPhase, float> OnFlightChanged;
        public event Action OnTurnaround;
        public event Action OnProjectileLost;

        public EventFlightLogicSystem(PureDataTutorialSequence pureData, IEventFlightTracker tracker)
        {
            this.pureData = pureData ?? throw new ArgumentNullException(nameof(pureData));
            this.tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        }

        public void Begin()
        {
            // 재도전 시 이전 사이클 상태/래치를 완전히 초기화한다.
            End();

            tracker.CaptureEndpoints();
            Vector3 p = tracker.PlayerPosition;
            Vector3 b = tracker.BossPosition;
            float launchDistance = pureData.EventLaunchDistance;

            Vector3 toBoss = b - p;
            if (launchDistance <= 0f || toBoss.sqrMagnitude < MinLength * MinLength)
            {
                Debug.LogWarning($"[EventFlightLogicSystem] 비행 구간을 시작할 수 없습니다(0으로 나누는 설정): " +
                                 $"EventLaunchDistance={launchDistance}, |B-P|={toBoss.magnitude}");
                return;
            }

            playerPosition = p;
            launchPosition = ResolveLaunchPosition(p, toBoss, launchDistance);

            // 투사체는 L에서 B로 직선 비행한다. 접근/이탈 판정과 진행도는 이 비행 방향 기준이다(L, P, B가 더 이상 한 직선이 아님).
            flightDirection = (b - launchPosition).normalized;
            approachLength = Vector3.Dot(p - launchPosition, flightDirection);
            exitLength = Vector3.Dot(b - p, flightDirection);

            tracker.BeginFlight(launchPosition);

            isActive = true;
            turnaroundLatched = false;
            phase = EventFlightPhase.Approach;
            progress = 0f;
        }

        /// <summary>
        /// L = P - a * launchDistance + side * o. 옆 방향 side는 뒤쪽 축 a에 수직인 수평 방향(cross(up, a))이고,
        /// o는 L-&gt;B 직선과 P 사이의 수직 거리가 정확히 clearance가 되도록 푼 값이다:
        /// o = clearance * (|B-P| + launchDistance) / sqrt(|B-P|^2 - clearance^2). 부호는 clearance의 부호(좌우)를 따른다.
        /// </summary>
        private Vector3 ResolveLaunchPosition(Vector3 p, Vector3 toBoss, float launchDistance)
        {
            float bossDistance = toBoss.magnitude;
            Vector3 axis = toBoss / bossDistance;
            Vector3 side = Vector3.Cross(Vector3.up, axis).normalized;

            float configured = pureData.EventLaunchSideClearance;
            float clearance = Mathf.Sign(configured) * Mathf.Min(Mathf.Abs(configured), bossDistance * MaxClearanceRatio);
            float sideOffset = clearance * (bossDistance + launchDistance) / Mathf.Sqrt(bossDistance * bossDistance - clearance * clearance);

            return p - axis * launchDistance + side * sideOffset;
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
            if (isActive)
            {
                tracker.EndFlight();
            }

            isActive = false;
            turnaroundLatched = false;
            projectileLostLatched = false;
            phase = EventFlightPhase.None;
            progress = 0f;
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
