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

            // 5. 비동기 데미지 연산 및 완료 후 소멸 처리
            ProcessDamageAndDestroyAsync(victim).Forget();
        }

        private async UniTaskVoid ProcessDamageAndDestroyAsync(GameObject victim)
        {
            EnsureInteractionService();

            var context = new DamageContext
            {
                Attacker = owner != null ? owner : gameObject,
                Victim = victim,
                RawDamage = baseDamage,
                CritRate = critRate,
                CritMultiplier = critMultiplier,
                IsCritical = Random.value < critRate
            };

            if (interactionService != null)
            {
                await interactionService.ProcessDamageAsync(context);
            }
            else
            {
                Debug.LogWarning("[CollisionDamageTrigger] IInteractionService not found in scene!");
            }

            // 데미지 적용 완료 후 오브젝트 소멸
            if (destroyOnImpact)
            {
                Destroy(gameObject);
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

        private void EnsureInteractionService()
        {
            if (interactionService == null)
            {
                var scope = FindFirstObjectByType<InteractionSystemLifetimeScope>();
                if (scope != null && scope.Container != null)
                {
                    interactionService = scope.Container.Resolve<IInteractionService>();
                }
            }
        }
    }
}
