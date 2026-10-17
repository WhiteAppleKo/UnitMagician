using UnityEngine;
using PipeLine.Combat;
using CharacterSystem;
using GameEvent;
using PipeLine.Contexts;

namespace DestructibleSystem
{
    /// <summary>
    /// IDestructibleDoorVisualizer의 기본 구현체([V] World Visualizer)이자 IDamageable/IGateAwareDamageable/IHitStepFlagsProvider/IGateRequirementProvider
    /// 구현체입니다. 돌문 GameObject에 부착되어 실제 콜라이더 비활성화, 파편/복구 파티클 스폰, 사운드 재생을
    /// 전부 수행하는 "수동적 수행자"입니다. 언제 파괴할지/복구만 할지는 스스로 판단하지 않고
    /// DestructibleDoorLogicSystem(순수 C# 로직)이 결정합니다.
    ///
    /// NPC 전투 액터(NpcCombatActorVisualizer)와 동일하게, 돌문은 씬에 여러 개 존재할 수 있으므로 LogicSystem을
    /// VContainer 싱글턴으로 등록하지 않고 Awake()에서 자기 자신의 PureData로 인스턴스마다 직접 new로 생성해 보유합니다.
    ///
    /// [09.EventSystem 연동, 회귀 없는 최소 확장] IEventTarget을 추가로 구현해 이벤트 시스템이 돌문을 id로 가리키고
    /// (TargetId), 파괴 여부(IsDestroyed)와 파괴 통지(OnDestroyed)를 받을 수 있게 합니다. 이 인터페이스는 "명령"용
    /// IDestructibleDoorVisualizer와 분리된 별도의 "조회/통지" 계약이며(인터페이스 분리 원칙 - ISP), 09.EventSystem(소비자)이
    /// 정의를 소유합니다. 기존 PlayDestroy()/ApplyDamage()/PlayCrackAndRepair() 판정 로직은 변경하지 않고,
    /// PlayDestroy() 실행 시점에 플래그를 true로 세팅한 뒤 통지만 한 번 발행하는 최소 확장입니다(04번 시스템 회귀 없음).
    /// </summary>
    public class DestructibleDoorComponent : MonoBehaviour, IGateAwareDamageable, IHitStepFlagsProvider, IGateRequirementProvider, IFactionProvider, IDefeatedStateProvider, IDestructibleDoorVisualizer, IEventTarget
    {
        [SerializeField] private PureDataDestructibleDoor pureData;

        [Header("Destroy VFX/SFX (Placeholder)")]
        [SerializeField] private GameObject destroyVfxPrefab;
        [SerializeField] private AudioClip destroySfx;

        [Header("Crack & Repair VFX/SFX (Placeholder)")]
        [SerializeField] private GameObject crackVfxPrefab;
        [SerializeField] private AudioClip crackSfx;

        [Header("Event Target (09.EventSystem)")]
        [Tooltip("이벤트 SO(TargetDestroyedCompletion/NpcRepeatAttackAction 등)가 이 돌문을 가리킬 때 쓰는 id.")]
        [SerializeField] private string eventTargetId;

        [Header("Passage")]
        [SerializeField] private Collider[] passageColliders;

        public DestructibleDoorLogicSystem LogicSystem { get; private set; }

        /// <summary>IHitStepFlagsProvider 구현: 돌문 피격 스텝은 PureData(hitFlags, 기본 AttributeGate | SizeGate - 회피/방어 없이 속성·크기 게이트만)에서 읽습니다.</summary>
        public HitStepFlags CurrentHitFlags => pureData != null ? pureData.HitFlags : HitStepFlags.None;

        /// <summary>IGateRequirementProvider 구현: 속성 게이트 요구 최대 온도는 PureData에서 읽습니다(코드에 기준 숫자 없음).</summary>
        public float RequiredMaxTemperature => pureData != null ? pureData.RequiredMaxTemperature : 0f;

        /// <summary>IGateRequirementProvider 구현: 크기 게이트 요구 질량 비율은 PureData에서 읽습니다(코드에 기준 숫자 없음).</summary>
        public float RequiredMassRatio => pureData != null ? pureData.RequiredMassRatio : 0f;

        /// <summary>IFactionProvider 구현: 돌문 진영은 PureData(faction, 기본 Neutral - 누구에게나 피격 대상)에서 읽습니다.</summary>
        public FactionType Faction => pureData != null ? pureData.Faction : FactionType.Neutral;

        /// <summary>IEventTarget 구현: 이벤트 SO가 이 돌문을 가리키는 id(인스펙터 값).</summary>
        public string TargetId => eventTargetId;

        /// <summary>IEventTarget 구현: 공격 대상 등으로 쓰이는 돌문 GameObject.</summary>
        public GameObject TargetObject => gameObject;

        /// <summary>IEventTarget 구현: 돌문이 파괴된 시점(통로 콜라이더가 꺼진 직후)에 한 번 발행됩니다.</summary>
        public event System.Action OnDestroyed;

        /// <summary>
        /// IEventTarget 구현: PlayDestroy() 호출(파괴 판정) 여부를 기록해 노출하는 조회 전용 플래그입니다.
        /// 돌문 HP는 RuntimeData 없이 매 ApplyDamage 호출마다 독립 판정되지만(회귀 없음 - 기존 로직 변경 없음), 이
        /// 플래그 자체는 한 번 true가 되면 되돌아가지 않는 단방향 상태입니다(파괴 후 재생성되지 않는 씬 오브젝트 전제).
        /// </summary>
        public bool IsDestroyed { get; private set; }

        /// <summary>IDefeatedStateProvider 구현: 이미 파괴된 돌문은 쓰러진 대상으로 취급되어 투사체가 통과한다.</summary>
        public bool IsDefeated => IsDestroyed;

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
        /// IGateAwareDamageable 구현: 피격 파이프라인(ApplyDamageStep)이 산출한 FinalDamage와 게이트 결과(컨텍스트)를 LogicSystem에 위임합니다.
        /// LogicSystem이 매 호출 독립적으로(누적 없이) 속성/크기/데미지(HP 비교) 세 조건을 개별 확인해 판정합니다.
        /// </summary>
        public void ApplyDamage(int amount, in DamageContext context)
        {
            EnsureInitialized();
            LogicSystem?.ApplyDamage(amount, in context);
        }

        /// <summary>
        /// IDamageable 구현(컨텍스트 없는 기존 시그니처): 게이트 결과를 알 수 없는 호출이므로 게이트 통과가 확인되지 않은 것으로 취급해
        /// 파괴하지 않습니다(ApplyDamageStep은 IGateAwareDamageable 경로를 우선 사용하므로 정상 파이프라인에서는 호출되지 않음).
        /// </summary>
        public void ApplyDamage(int amount)
        {
            ApplyDamage(amount, default);
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
            OnDestroyed?.Invoke();
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
