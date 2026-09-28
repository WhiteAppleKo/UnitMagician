using UnityEngine;
using PipeLine.Combat;
using AllySupportSystem;

namespace DestructibleSystem
{
    /// <summary>
    /// IDestructibleDoorVisualizer의 기본 구현체([V] World Visualizer)이자 IDamageable/IHitStepFlagsProvider
    /// 구현체입니다. 돌문 GameObject에 부착되어 실제 콜라이더 비활성화, 파편/복구 파티클 스폰, 사운드 재생을
    /// 전부 수행하는 "수동적 수행자"입니다. 언제 파괴할지/복구만 할지는 스스로 판단하지 않고
    /// DestructibleDoorLogicSystem(순수 C# 로직)이 결정합니다.
    ///
    /// NPC 전투 액터(NpcCombatActorVisualizer)와 동일하게, 돌문은 씬에 여러 개 존재할 수 있으므로 LogicSystem을
    /// VContainer 싱글턴으로 등록하지 않고 Awake()에서 자기 자신의 PureData로 인스턴스마다 직접 new로 생성해 보유합니다.
    ///
    /// [10.AllySupportSystem 연동, 회귀 없는 최소 확장] IDestructibleTargetStatus를 추가로 구현해 IsDestroyed를
    /// 외부에 조회 전용으로 노출합니다(02번 기획서 2.8절 - 기존에 파괴 여부를 조회할 공개 프로퍼티가 없어 확정된
    /// 확장). 이 인터페이스는 "명령"용 IDestructibleDoorVisualizer와 분리된 별도의 읽기 전용 "조회" 계약이며
    /// (인터페이스 분리 원칙 - ISP), 10.AllySupportSystem(소비자)이 정의를 소유합니다. 기존 PlayDestroy()/
    /// ApplyDamage()/PlayCrackAndRepair() 로직은 전혀 변경하지 않고, PlayDestroy() 실행 시점에 플래그만 한 번
    /// true로 세팅하는 최소 확장입니다(04번 시스템 회귀 없음).
    /// </summary>
    public class DestructibleDoorComponent : MonoBehaviour, IDamageable, IHitStepFlagsProvider, IDestructibleDoorVisualizer, IDestructibleTargetStatus
    {
        [SerializeField] private PureDataDestructibleDoor pureData;

        [Header("Destroy VFX/SFX (Placeholder)")]
        [SerializeField] private GameObject destroyVfxPrefab;
        [SerializeField] private AudioClip destroySfx;

        [Header("Crack & Repair VFX/SFX (Placeholder)")]
        [SerializeField] private GameObject crackVfxPrefab;
        [SerializeField] private AudioClip crackSfx;

        [Header("Passage")]
        [SerializeField] private Collider[] passageColliders;

        public DestructibleDoorLogicSystem LogicSystem { get; private set; }

        /// <summary>IHitStepFlagsProvider 구현: 돌문은 회피/방어 판정이 필요 없고 속성 게이트만 필요합니다.</summary>
        public HitStepFlags CurrentHitFlags => HitStepFlags.AttributeGate;

        /// <summary>
        /// IDestructibleTargetStatus 구현: PlayDestroy() 호출(파괴 판정) 여부를 기록해 노출하는 조회 전용 플래그입니다.
        /// 돌문 HP는 RuntimeData 없이 매 ApplyDamage 호출마다 독립 판정되지만(회귀 없음 - 기존 로직 변경 없음), 이
        /// 플래그 자체는 한 번 true가 되면 되돌아가지 않는 단방향 상태입니다(파괴 후 재생성되지 않는 씬 오브젝트 전제).
        /// </summary>
        public bool IsDestroyed { get; private set; }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (LogicSystem == null && pureData != null)
            {
                LogicSystem = new DestructibleDoorLogicSystem(pureData, this);
            }

            if (passageColliders == null || passageColliders.Length == 0)
            {
                passageColliders = GetComponentsInChildren<Collider>(true);
            }
        }

        /// <summary>
        /// IDamageable 구현: 피격 파이프라인(ApplyDamageStep)이 산출한 FinalDamage를 LogicSystem에 위임합니다.
        /// LogicSystem이 매 호출 독립적으로(누적 없이) 2차 게이트(HP 비교)를 판정합니다.
        /// </summary>
        public void ApplyDamage(int amount)
        {
            EnsureInitialized();
            LogicSystem?.ApplyDamage(amount);
        }

        public void PlayDestroy()
        {
            IsDestroyed = true;

            if (destroyVfxPrefab != null)
            {
                Object.Instantiate(destroyVfxPrefab, transform.position, transform.rotation);
            }

            if (destroySfx != null)
            {
                AudioSource.PlayClipAtPoint(destroySfx, transform.position);
            }

            for (int i = 0; i < passageColliders.Length; i++)
            {
                if (passageColliders[i] != null)
                {
                    passageColliders[i].enabled = false;
                }
            }

            Debug.Log($"[DestructibleDoorComponent] '{name}' DESTROYED - passage opened.");
        }

        public void PlayCrackAndRepair()
        {
            if (crackVfxPrefab != null)
            {
                Object.Instantiate(crackVfxPrefab, transform.position, transform.rotation);
            }

            if (crackSfx != null)
            {
                AudioSource.PlayClipAtPoint(crackSfx, transform.position);
            }

            Debug.Log($"[DestructibleDoorComponent] '{name}' cracked but survived (not destroyed).");
        }
    }
}
