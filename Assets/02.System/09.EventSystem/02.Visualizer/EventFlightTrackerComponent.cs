using System;
using UnityEngine;
using NpcSystem;

namespace GameEvent
{
    /// <summary>
    /// IEventFlightTracker의 구현체([V] World Visualizer)입니다. Transform 접근은 이 클래스 안에서만 일어나며,
    /// Logic(EventFlightLogicSystem)에는 Vector3 값만 제공합니다(13번 기획서 §3, DLV Rule 2).
    ///
    /// - CaptureEndpoints: 이벤트 진입 시점의 플레이어/보스 위치를 P/B로 고정(스냅샷)합니다.
    ///   플레이어 위치는 발밑이 아니라 투사체가 지나가는 높이를 맞추기 위해 이벤트 SO의 추적 높이 오프셋(playerAnchorOffset)을 더합니다.
    /// - BeginFlight: 아군(INpcProjectileLaunchHook)이 다음에 쏘는 투사체 1개를 발사 지점 L에서, 이벤트 SO의 속도/데미지로 쏘도록 예약하고,
    ///   그 투사체가 스폰되면 참조를 받아 매 프레임 위치(ProjectilePosition)를 제공합니다.
    /// - 아군 NPC 참조는 AllySupportSystemLifetimeScope와 같은 인스펙터 고정 참조(allyActor)이며,
    ///   GameObject.Find/FindObjectOfType은 쓰지 않습니다. 보스 Transform은 EventSystemLifetimeScope가
    ///   bossStatComponent의 Transform을 SetBossTransform으로 내려줍니다(중복 참조 방지).
    /// </summary>
    public class EventFlightTrackerComponent : MonoBehaviour, IEventFlightTracker
    {
        [Header("Fixed References (Inspector)")]
        [SerializeField] private Transform playerTransform;
        [Tooltip("이벤트 지원 사격을 하는 특수 아군 NPC. AllySupportSystemLifetimeScope의 allyVisualizer와 같은 대상을 연결합니다.")]
        [SerializeField] private NpcCombatActorVisualizer allyActor;

        private Transform bossTransform;
        private INpcProjectileLaunchHook launchHook;

        private Transform projectile;

        // 풀링(15-B): 이벤트 투사체로 인정한 시점의 세대 값. 반환 후 재사용된 같은 인스턴스는 세대가 달라 "같은 이벤트 투사체"로 오인되지 않는다.
        private NpcProjectile projectilePooled;
        private int acquiredGeneration;

        private bool isTracking;
        private bool hasAcquiredProjectile;

        private Vector3 launchPosition;
        private Vector3 playerPosition;
        private Vector3 bossPosition;
        private Vector3 lastProjectilePosition;

        /// <summary>
        /// Unity 파괴 null과 비활성화까지 포함해 추적 중인 투사체가 살아 있는지 판정합니다. 풀링된 투사체는 반환(비활성화) 뒤 같은 프레임에
        /// 재사용돼 다시 활성이 될 수 있으므로 세대 값도 비교해 "다른 발사로 재사용된 인스턴스"는 소실된 것으로 본다.
        /// </summary>
        public bool HasProjectile => projectile != null && projectile.gameObject.activeInHierarchy && IsSameGeneration();

        private bool IsSameGeneration()
        {
            return projectilePooled == null || projectilePooled.Generation == acquiredGeneration;
        }
        public bool HasAcquiredProjectile => hasAcquiredProjectile;
        public Vector3 LaunchPosition => launchPosition;
        public Vector3 PlayerPosition => playerPosition;
        public Vector3 BossPosition => bossPosition;

        public Vector3 ProjectilePosition
        {
            get
            {
                if (projectile != null)
                {
                    lastProjectilePosition = projectile.position;
                }

                return lastProjectilePosition;
            }
        }

        private void Awake()
        {
            launchHook = allyActor;
        }

        /// <summary>EventSystemLifetimeScope가 보스 대체 참조의 Transform을 내려주는 진입점입니다.</summary>
        public void SetBossTransform(Transform boss)
        {
            bossTransform = boss;
        }

        public void CaptureEndpoints(Vector3 playerAnchorOffset)
        {
            playerPosition = playerTransform != null ? playerTransform.position + playerAnchorOffset : Vector3.zero;
            bossPosition = bossTransform != null ? bossTransform.position : Vector3.zero;
        }

        public bool IsEventProjectile(GameObject source)
        {
            // 소실(파괴/비활성화)된 투사체는 이벤트 투사체로 취급하지 않는다. Unity 파괴 null 비교(==)를 사용한다.
            if (source == null || !HasProjectile) return false;

            return source.transform.IsChildOf(projectile);
        }

        public void BeginFlight(Vector3 launch, float projectileSpeed, int projectileDamage)
        {
            EndFlight();

            launchPosition = launch;
            lastProjectilePosition = launch;
            isTracking = true;

            if (launchHook == null)
            {
                Debug.LogWarning("[EventFlightTrackerComponent] allyActor가 연결되지 않아 투사체 발사 지점/참조를 받을 수 없습니다.");
                return;
            }

            launchHook.ArmLaunchOrigin(launch, projectileSpeed, projectileDamage);
            launchHook.OnProjectileSpawned += HandleProjectileSpawned;
        }

        public void CloseLaunchWindow()
        {
            if (launchHook == null) return;

            launchHook.OnProjectileSpawned -= HandleProjectileSpawned;
            launchHook.DisarmLaunchOrigin();
        }

        public void EndFlight()
        {
            CloseLaunchWindow();

            isTracking = false;
            hasAcquiredProjectile = false;
            projectile = null;
            projectilePooled = null;
        }

        /// <summary>이벤트 시작 방송 중 아군이 처음 스폰한 투사체 1개만 추적 대상으로 삼는다.</summary>
        private void HandleProjectileSpawned(GameObject spawned)
        {
            if (!isTracking || projectile != null || spawned == null) return;

            projectile = spawned.transform;
            projectilePooled = spawned.GetComponent<NpcProjectile>();
            acquiredGeneration = projectilePooled != null ? projectilePooled.Generation : 0;
            hasAcquiredProjectile = true;
            lastProjectilePosition = projectile.position;

            // 이벤트 투사체가 플레이어(P)를 지나쳐 날아가는 것은 같은 편(아군 투사체-플레이어) 레이어 충돌 매트릭스가 보장한다(FactionLayers).
        }

        private void OnDestroy()
        {
            EndFlight();
        }
    }
}
