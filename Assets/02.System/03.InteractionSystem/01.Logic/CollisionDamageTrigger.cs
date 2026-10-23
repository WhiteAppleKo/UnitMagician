using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using PipeLine.Contexts;
using PipeLine.Combat;
using CharacterSystem;
using Pooling;
using InteractionSystem.Data;

namespace InteractionSystem.Logic
{
    /// <summary>
    /// 오브젝트 충돌 시 대상에게 데미지 상호작용을 전달하는 컴포넌트입니다.
    /// 같은 편 투사체/몸체는 진영 레이어(FactionLayers, 충돌 매트릭스)에서 충돌 자체가 일어나지 않으므로 이 컴포넌트에는
    /// 같은 편 판정이 없습니다(16-B). 중복 피격 방지 예외 처리가 포함되어 있습니다.
    /// </summary>
    public class CollisionDamageTrigger : MonoBehaviour, IPoolReusable, IAttackStepFlagsProvider, IFactionProvider, IFactionSettable, IAttackDamageProvider
    {
        [Header("Damage Settings")]
        [Tooltip("기본 데미지 원본(PureData, 28번 §6-10). Awake·풀 재사용 시 이 값으로, 발사 시 발사 값으로 RuntimeDataAttackDamage를 초기화한다.")]
        [SerializeField] private PureDataAttackDamage attackDamageData;
        [SerializeField] private float accuracyRate = 1f;
        [SerializeField] private float critRate = 0.2f;
        [SerializeField] private float critMultiplier = 1.5f;

        [Header("Exception Settings")]
        [SerializeField] private LayerMask targetLayer = ~0;

        [Header("Hit Cooldown Settings")]
        [SerializeField] private float hitCooldown = 0.5f;
        [SerializeField] private bool destroyOnImpact = false;
        [Tooltip("true이면 첫 번째로 인정된 피격 이후에는 더 이상 충돌을 받지 않는다(풀링되는 투사체용 - 판정이 진행되는 동안 추가 피격으로 " +
                 "판정이 겹치거나, 반환 후 재사용된 오브젝트가 이전 판정에 쓰이지 않게 한다). 기본 false는 기존 동작(쿨타임마다 재피격)을 유지한다.")]
        [SerializeField] private bool singleImpact = false;

        private IInteractionService interactionService;
        private readonly Dictionary<GameObject, float> hitHistory = new();

        private bool hasImpacted;

        // 16-A: 투사체 독자 공격 상태. 발사 시 발사 설정 값으로 확정되고(ApplyLaunchAttackState), 풀 재사용(ResetForReuse)에서 초기화된다.
        // attacker는 발사자이며 마법이 바꾸지 않는다(DamageContext.Attacker의 출처). 진영(faction)은 마법으로 바뀔 수 있다(SetFaction).
        private GameObject attacker;
        private FactionType faction = FactionType.Neutral;
        private readonly CombatFlagState attackFlags = new CombatFlagState(AttackStepFlags.None, HitStepFlags.None);

        // 28번 §6-9/§6-10: 이 오브젝트의 공격 데미지 런타임 데이터(BaseDamage·CurrentDamage). 같은 오브젝트의 RuntimeDataUnitGroup이
        // Awake에서 IAttackDamageProvider로 참조하므로 필드 초기화로 인스턴스를 먼저 만들어 두고, 값은 Awake/재사용/발사 시 Initialize한다.
        private readonly RuntimeDataAttackDamage attackDamage = new RuntimeDataAttackDamage();

        /// <summary>IAttackDamageProvider 구현: 마법(Mass 등)이 CurrentDamage를 저장하고, 충돌 시 RawDamage로 쓰이는 공격 데미지 데이터.</summary>
        public RuntimeDataAttackDamage AttackDamage => attackDamage;

        /// <summary>
        /// 피격 판정(ProcessDamageAsync)이 끝난 직후, destroyOnImpact 처리 전에 발행됩니다. 풀링되는 투사체는 이 이벤트로 "판정을 마친 뒤" 풀에 반환됩니다.
        /// 판정 중 예외가 나도 발행됩니다(풀 누수 방지).
        /// </summary>
        public event Action OnImpactResolved;

        /// <summary>singleImpact가 켜진 상태에서 인정된 피격이 이미 있었는지(판정 진행 중 포함). ResetForReuse로 초기화됩니다.</summary>
        public bool HasImpacted => hasImpacted;

        /// <summary>발사자(발사 시 1회 확정). 없으면 null이며 DamageContext.Attacker는 투사체 자신이 된다.</summary>
        public GameObject Attacker => attacker;

        /// <summary>IFactionProvider 구현: 투사체의 현재 진영(발사 시 발사자 진영으로 초기화).</summary>
        public FactionType Faction => faction;

        /// <summary>IAttackStepFlagsProvider 구현: 투사체가 들고 있는 현재 공격 플래그(발사 시 발사자 값으로 초기화).</summary>
        public AttackStepFlags CurrentAttackFlags => attackFlags.CurrentAttackFlags;

        /// <summary>투사체의 공격 플래그 상태. 마법이 비트를 Add/Suppress할 수 있는 경로(이번 작업에서 쓰는 마법은 없다).</summary>
        public CombatFlagState AttackFlagState => attackFlags;

        /// <summary>
        /// 발사 설정 값(발사자/진영/공격 플래그)으로 투사체의 공격 상태를 확정한다. 이전 사용에서 쌓인 Add/Suppress 카운터는
        /// 모두 비워진다. attacker는 이 호출로만 정해지며 마법이 바꾸지 않는다. 투사체 레이어도 발사 진영으로 맞춘다.
        /// </summary>
        public void ApplyLaunchAttackState(GameObject launchAttacker, FactionType launchFaction, AttackStepFlags launchAttackFlags)
        {
            attacker = launchAttacker;
            SetFaction(launchFaction);
            attackFlags.Reset(launchAttackFlags, HitStepFlags.None);
        }

        /// <summary>
        /// 투사체의 진영을 바꾼다(마법으로 적 투사체를 빼앗는 경로). 진영에 맞는 투사체 레이어를 루트와 모든 자식 콜라이더에
        /// 재귀 적용하므로, 따로 풀어 줄 충돌 쌍 없이 같은 편 몸체와는 충돌하지 않고 반대 편(원래 발사자 등)과는 충돌한다.
        /// attacker(발사자)는 바꾸지 않는다.
        /// </summary>
        public void SetFaction(FactionType newFaction)
        {
            faction = newFaction;
            FactionLayers.ApplyLayerRecursively(gameObject, FactionLayers.GetAttackLayer(newFaction));
        }

        [Inject]
        public void Construct(IInteractionService interactionService)
        {
            this.interactionService = interactionService;
        }

        public void Initialize(IInteractionService interactionService)
        {
            this.interactionService = interactionService;
        }

        private void Awake()
        {
            InitializeAttackDamageFromData();
        }

        /// <summary>PureData 기본 데미지로 BaseDamage = CurrentDamage를 초기화한다(Awake·풀 재사용). 발사 값은 발사 시 AttackDamage.Initialize로 덮어쓴다.</summary>
        private void InitializeAttackDamageFromData()
        {
            if (attackDamageData == null)
            {
                Debug.LogWarning($"[CollisionDamageTrigger] PureDataAttackDamage is not assigned on {gameObject.name}. BaseDamage = 0.");
                attackDamage.Initialize(0);
                return;
            }

            attackDamage.Initialize(attackDamageData.BaseDamage);
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleCollision(other.gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            HandleCollision(collision.gameObject);
        }

        private void HandleCollision(GameObject victim)
        {
            if (victim == null) return;

            // 0. 단일 피격 모드: 이미 인정된 피격이 있으면(판정 진행 중 포함) 이후 충돌은 무시
            if (singleImpact && hasImpacted) return;

            // 1. 레이어 예외 처리
            if (((1 << victim.layer) & targetLayer) == 0) return;

            // 2. 같은 편 통과는 여기서 판정하지 않는다 - 진영 레이어 충돌 매트릭스에서 충돌 이벤트 자체가 오지 않는다(FactionLayers).

            // 3. 피격 대상 IDamageable 미부착 예외 처리
            // (과거에는 CharacterStatComponent 부착 여부로 필터링했으나, 그러면 캐릭터가 아닌 IDamageable
            // 구현체(예: 파괴 가능 환경 오브젝트인 돌문)가 애초에 파이프라인에 진입하지 못해 항상 무반응이었다.
            // IDamageable은 캐릭터/오브젝트 공용 최소 인터페이스이므로 이 체크로 대체한다 - 04번 문서 참고.)
            if (!victim.TryGetComponent<IDamageable>(out _)) return;

            // 3-1. 이미 쓰러진(HP 0 이하/파괴된) 대상은 피격 판정 대상이 아니다 — singleImpact를 소비하지 않고 그대로 통과시켜
            // 사선 위의 시체가 풀링 투사체를 소진시키지 않게 한다. IDefeatedStateProvider를 구현하지 않은 IDamageable은 항상 살아 있는 것으로 본다.
            if (victim.TryGetComponent<IDefeatedStateProvider>(out var defeatedState) && defeatedState.IsDefeated) return;

            // 4. 중복 피격 쿨타임 예외 처리
            if (hitHistory.TryGetValue(victim, out float lastHitTime))
            {
                if (Time.time - lastHitTime < hitCooldown) return;
            }

            hitHistory[victim] = Time.time;
            PruneExpiredHitHistory();
            if (singleImpact) hasImpacted = true;

            // 5. 비동기 데미지 연산 및 완료 후 소멸 처리
            ProcessDamageAndDestroyAsync(victim).Forget();
        }

        private async UniTaskVoid ProcessDamageAndDestroyAsync(GameObject victim)
        {
            if (interactionService == null)
            {
                Debug.LogWarning($"[CollisionDamageTrigger] IInteractionService is not injected/initialized on {gameObject.name}!");
            }

            // IsCritical/IsEvaded는 이제 CombatPipelineManager 경유로 실행되는 CriticalStep/EvasionStep이 실제로 판정하므로
            // 여기서 미리 굴리지 않습니다. (AttackerAccRate/VictimEvaRate에 대응하는 실제 회피/명중 스탯 시스템은 아직 없어서
            // VictimEvaRate는 0으로 유지 - 기본적으로는 항상 명중하도록 accuracyRate를 1(100%)로 둡니다.)
            var context = new DamageContext
            {
                // 발사자(attacker)가 공격 주체다. 마법으로 진영이 바뀌어도 Attacker는 바뀌지 않는다.
                Attacker = attacker != null ? attacker : gameObject,
                // 속성 게이트(AttributeGateStep)가 발사자가 아니라 이 투사체 자신의 유닛 데이터(온도/질량)를 읽을 수 있게 한다.
                Source = gameObject,
                Victim = victim,
                // 마법 적용 시 저장된 현재 데미지(28번 §6-9) - 충돌 시점에 질량 비율을 곱하지 않는다.
                RawDamage = attackDamage.CurrentDamage,
                AttackerAccRate = accuracyRate,
                CritRate = critRate,
                CritMultiplier = critMultiplier
            };

            try
            {
                if (interactionService != null)
                {
                    await interactionService.ProcessDamageAsync(context);
                }
            }
            finally
            {
                // 판정(OnDamageProcessed 등)이 모두 끝난 뒤에만 구독자(풀 반환 등)에게 알린다.
                OnImpactResolved?.Invoke();
            }

            // 데미지 적용 완료 후 오브젝트 소멸 (풀링되는 투사체는 destroyOnImpact를 끄고 OnImpactResolved로 풀에 반환한다)
            if (destroyOnImpact && this != null)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 파괴된 피격 대상이나 쿨타임이 지난 항목이 hitHistory에 무한히 누적되는 것을 방지합니다.
        /// </summary>
        private readonly List<GameObject> expiredHitCache = new();

        private void PruneExpiredHitHistory()
        {
            expiredHitCache.Clear();
            foreach (var entry in hitHistory)
            {
                if (entry.Key == null || Time.time - entry.Value >= hitCooldown)
                {
                    expiredHitCache.Add(entry.Key);
                }
            }

            foreach (var key in expiredHitCache)
            {
                hitHistory.Remove(key);
            }
        }

        // --- IPoolReusable (15-B: 풀링 재사용 시 상태 초기화) ---

        /// <summary>풀에 반환되는 순간: 걸어 둔 충돌 쌍이 없으므로(진영 레이어로 대체) 되돌릴 것이 없다.</summary>
        public void OnReturnedToPool()
        {
        }

        /// <summary>
        /// 재사용 직전(Setting 단계): 발사자/진영(마법으로 빼앗긴 값)/공격 플래그/피격 이력/단일 피격 잠금을 초기값으로 되돌린다.
        /// 레이어도 초기 진영(Neutral = AttackNeutral)으로 돌려놓으며, 곧이어 SetActive에서 발사 진영 레이어로 확정된다.
        /// </summary>
        public void ResetForReuse()
        {
            attacker = null;
            SetFaction(FactionType.Neutral);
            attackFlags.Reset(AttackStepFlags.None, HitStepFlags.None);
            hitHistory.Clear();
            expiredHitCache.Clear();
            hasImpacted = false;
            // 이전 사용의 마법 데미지 결과(CurrentDamage)를 지운다. 풀링 투사체는 곧이어 SetActive에서 발사 값으로 다시 Initialize된다.
            InitializeAttackDamageFromData();
        }
    }
}
