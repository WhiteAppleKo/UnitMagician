using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using PipeLine.Contexts;
using CharacterSystem;

namespace InteractionSystem.Logic
{
    /// <summary>
    /// 오브젝트 충돌 시 대상에게 데미지 상호작용을 전달하는 컴포넌트입니다.
    /// 시전자(Owner) 및 중복 피격 방지 예외 처리가 포함되어 있습니다.
    /// </summary>
    public class CollisionDamageTrigger : MonoBehaviour
    {
        [Header("Damage Settings")]
        [SerializeField] private int baseDamage = 10;
        [SerializeField] private float accuracyRate = 1f;
        [SerializeField] private float critRate = 0.2f;
        [SerializeField] private float critMultiplier = 1.5f;

        [Header("Owner & Exception Settings")]
        [SerializeField] private GameObject owner;
        [SerializeField] private bool ignoreOwnerParentChild = true;
        [SerializeField] private LayerMask targetLayer = ~0;

        [Header("Hit Cooldown Settings")]
        [SerializeField] private float hitCooldown = 0.5f;
        [SerializeField] private bool destroyOnImpact = false;

        private IInteractionService interactionService;
        private readonly Dictionary<GameObject, float> hitHistory = new();

        [Inject]
        public void Construct(IInteractionService interactionService)
        {
            this.interactionService = interactionService;
        }

        public void Initialize(IInteractionService interactionService)
        {
            this.interactionService = interactionService;
        }

        private void Start()
        {
            IgnoreOwnerCollisions();
        }

        public void SetOwner(GameObject newOwner)
        {
            owner = newOwner;
            IgnoreOwnerCollisions();
        }

        public void SetBaseDamage(int damage)
        {
            baseDamage = damage;
        }

        public void IgnoreOwnerCollisions()
        {
            if (owner == null) return;

            var myColliders = GetComponentsInChildren<Collider>(true);
            var ownerColliders = owner.GetComponentsInChildren<Collider>(true);

            foreach (var myCol in myColliders)
            {
                foreach (var ownerCol in ownerColliders)
                {
                    if (myCol != null && ownerCol != null && myCol != ownerCol)
                    {
                        Physics.IgnoreCollision(myCol, ownerCol, true);
                    }
                }
            }
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

            // 1. 레이어 예외 처리
            if (((1 << victim.layer) & targetLayer) == 0) return;

            // 2. 시전자(Owner) 피격 예외 처리
            if (IsOwnerOrRelative(victim)) return;

            // 3. 피격 대상 CharacterStatComponent 미부착 예외 처리
            if (!victim.TryGetComponent<CharacterStatComponent>(out _)) return;

            // 4. 중복 피격 쿨타임 예외 처리
            if (hitHistory.TryGetValue(victim, out float lastHitTime))
            {
                if (Time.time - lastHitTime < hitCooldown) return;
            }

            hitHistory[victim] = Time.time;
            PruneExpiredHitHistory();

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
                Attacker = owner != null ? owner : gameObject,
                Victim = victim,
                RawDamage = baseDamage,
                AttackerAccRate = accuracyRate,
                CritRate = critRate,
                CritMultiplier = critMultiplier
            };

            if (interactionService != null)
            {
                await interactionService.ProcessDamageAsync(context);
            }

            // 데미지 적용 완료 후 오브젝트 소멸
            if (destroyOnImpact)
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

        private bool IsOwnerOrRelative(GameObject target)
        {
            if (owner == null) return false;
            if (target == owner) return true;

            if (ignoreOwnerParentChild)
            {
                if (target.transform.IsChildOf(owner.transform)) return true;
                if (owner.transform.IsChildOf(target.transform)) return true;
            }

            return false;
        }
    }
}
