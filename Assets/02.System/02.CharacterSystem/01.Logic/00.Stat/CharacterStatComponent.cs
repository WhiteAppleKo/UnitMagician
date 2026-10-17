    using UnityEngine;
using VContainer;
using Synty.AnimationBaseLocomotion.Samples;
using PipeLine.Combat;

namespace CharacterSystem
{
    /// <summary>
    /// GameObject 상에서 CharacterStatSystem 인스턴스 참조 및 PureStatData 직렬화를 홀딩하며,
    /// SampleObjectLockOn을 상속하여 적대 진영(Enemy) 대상에 대해서만 락온 하이라이트 마커를 활성화합니다.
    /// 또한 IDamageable을 구현/위임하여, 피격 파이프라인(ApplyDamageStep)이 구체적인 스탯 시스템 구현을 몰라도
    /// GetComponent&lt;IDamageable&gt;()만으로 데미지를 적용할 수 있게 합니다.
    /// </summary>
    public class CharacterStatComponent : SampleObjectLockOn, IDamageable, IDefeatedStateProvider,
        IHitStepFlagsProvider, IAttackStepFlagsProvider, IFactionProvider, IFactionSettable, IHostilityRule
    {
        [SerializeField] private PureStatData pureStatData;

        public static CharacterStatComponent PlayerStat { get; private set; }

        public PureStatData PureStatData => pureStatData;
        public CharacterStatSystem StatSystem { get; private set; }
        public ICharacterStatService StatService => StatSystem;

        private void Awake()
        {
            EnsureInitialized();

            // "적이 아니면 플레이어"가 아니라 진영이 Player일 때만 플레이어 스탯이다
            // (아군/중립 NPC에 스탯 컴포넌트를 붙여도 PlayerStat이 덮어써지지 않게 한다).
            if (Faction == FactionType.Player)
            {
                PlayerStat = this;
            }
        }

        /// <summary>
        /// 모든 Awake/주입이 끝난 뒤(Start) 최종 StatSystem의 런타임 진영을 몸체 레이어로 반영하고, 진영 변경 이벤트를 구독한다(16-B).
        /// </summary>
        protected override void Start()
        {
            base.Start();
            started = true;
            BindFactionLayer();
        }

        private void OnDestroy()
        {
            UnbindFactionLayer();

            if (PlayerStat == this)
            {
                PlayerStat = null;
            }
        }

        [Inject]
        public void Construct(CharacterStatSystem statSystem)
        {
            Initialize(statSystem);
        }

        public void Initialize(CharacterStatSystem statSystem)
        {
            StatSystem = statSystem;
            if (started) BindFactionLayer();
        }

        private bool started;
        private RuntimeStatData boundRuntimeData;

        private void BindFactionLayer()
        {
            UnbindFactionLayer();

            var runtimeData = StatSystem?.RuntimeData;
            if (runtimeData == null) return;

            boundRuntimeData = runtimeData;
            boundRuntimeData.OnFactionChanged += HandleFactionChanged;
            ApplyBodyLayer(runtimeData.CurrentFaction);
        }

        private void UnbindFactionLayer()
        {
            if (boundRuntimeData == null) return;

            boundRuntimeData.OnFactionChanged -= HandleFactionChanged;
            boundRuntimeData = null;
        }

        private void HandleFactionChanged(FactionType faction)
        {
            ApplyBodyLayer(faction);
        }

        /// <summary>진영에 맞는 몸체 레이어(Player/Ally -> BodyAlly, Enemy -> BodyEnemy, Neutral -> BodyNeutral)를 루트와 모든 자식 콜라이더에 적용한다.</summary>
        private void ApplyBodyLayer(FactionType faction)
        {
            // 구르기 무적(19-B)으로 IgnoreAttack 레이어가 적용된 동안에는 진영이 바뀌어도 몸체 레이어로 덮어쓰지 않는다.
            // 오버라이드가 풀릴 때 그 시점의 CurrentFaction으로 복원된다(SetAttackIgnored).
            if (attackIgnored) return;

            FactionLayers.ApplyLayerRecursively(gameObject, FactionLayers.GetBodyLayer(faction));
        }

        private bool attackIgnored;

        /// <summary>
        /// 몸체 레이어 오버라이드(19-B 구르기 무적). true이면 루트와 모든 자식 콜라이더를 IgnoreAttack 레이어로 옮기고
        /// (이후 OnFactionChanged는 레이어를 바꾸지 않는다), false이면 저장해 둔 레이어가 아니라 그 시점의 현재 진영(Faction)에
        /// 맞는 몸체 레이어로 복원한다.
        /// </summary>
        public void SetAttackIgnored(bool ignored)
        {
            if (attackIgnored == ignored) return;

            attackIgnored = ignored;
            if (ignored)
            {
                FactionLayers.ApplyLayerRecursively(gameObject, FactionLayers.GetIgnoreAttackLayer());
            }
            else
            {
                ApplyBodyLayer(Faction);
            }
        }

        /// <summary>컴포넌트가 꺼지거나 파괴될 때 오버라이드가 남아 IgnoreAttack 레이어에 갇히지 않도록 몸체 레이어로 복원한다.</summary>
        private void OnDisable()
        {
            SetAttackIgnored(false);
        }

        /// <summary>
        /// IDamageable 구현: 피격 파이프라인(ApplyDamageStep)이 산출한 최종 데미지를 StatService.TakeDamage로 위임합니다.
        /// </summary>
        public void ApplyDamage(int amount)
        {
            EnsureInitialized();
            StatService?.TakeDamage(amount);
        }

        /// <summary>
        /// IDefeatedStateProvider 구현: HP가 최솟값(0) 이하이면 쓰러진 것으로 본다. 스탯을 아직 만들 수 없으면(데이터 없음) 살아 있는 것으로 취급.
        /// </summary>
        public bool IsDefeated
        {
            get
            {
                EnsureInitialized();
                var hp = StatSystem?.RuntimeData?.HP;
                return hp != null && hp.CurrentValue <= hp.MinValue;
            }
        }

        private void EnsureInitialized()
        {
            if (StatSystem == null && pureStatData != null)
            {
                StatSystem = new CharacterStatSystem(pureStatData);
            }
        }

        /// <summary>
        /// IFactionProvider 구현: 런타임 진영(RuntimeStatData.CurrentFaction)을 돌려준다. 런타임 데이터가 아직 없으면
        /// PureStatData의 기본 진영을 쓰고, 데이터가 전혀 없으면 Neutral이다.
        /// </summary>
        public FactionType Faction
        {
            get
            {
                if (StatSystem?.RuntimeData != null)
                {
                    return StatSystem.RuntimeData.CurrentFaction;
                }
                if (pureStatData != null)
                {
                    return pureStatData.DefaultFaction;
                }
                return FactionType.Neutral;
            }
        }

        /// <summary>
        /// IFactionSettable 구현: 런타임 진영(RuntimeStatData.CurrentFaction)을 설정한다. 몸체 레이어 갱신과 OnFactionChanged 발행은
        /// 기존 경로(HandleFactionChanged)가 처리한다. 런타임 데이터가 아직 없으면 먼저 초기화한다.
        /// </summary>
        public void SetFaction(FactionType faction)
        {
            EnsureInitialized();
            var runtimeData = StatSystem?.RuntimeData;
            if (runtimeData == null) return;

            runtimeData.CurrentFaction = faction;
        }

        /// <summary>IHostilityRule 구현: 적대화 임계 연속 피격 횟수(PureStatData). 데이터가 없으면 0(적대화 안 함).</summary>
        public int HostileHitThreshold => pureStatData != null ? pureStatData.HostileHitThreshold : 0;

        /// <summary>IHostilityRule 구현: 연속 피격 유효 시간(초, PureStatData).</summary>
        public float HostileHitWindowSeconds => pureStatData != null ? pureStatData.HostileHitWindowSeconds : 0f;

        /// <summary>IHitStepFlagsProvider 구현: 이 캐릭터의 현재 피격 플래그(PureStatData 기본값 + 버프/페이즈의 Add/Suppress 반영).</summary>
        public HitStepFlags CurrentHitFlags
        {
            get
            {
                EnsureInitialized();
                var runtimeData = StatSystem?.RuntimeData;
                return runtimeData != null ? runtimeData.CombatFlags.CurrentHitFlags : HitStepFlags.None;
            }
        }

        /// <summary>IAttackStepFlagsProvider 구현: 이 캐릭터가 공격할 때(투사체를 발사할 때) 쓰는 공격 플래그.</summary>
        public AttackStepFlags CurrentAttackFlags
        {
            get
            {
                EnsureInitialized();
                var runtimeData = StatSystem?.RuntimeData;
                return runtimeData != null ? runtimeData.CombatFlags.CurrentAttackFlags : AttackStepFlags.None;
            }
        }

        public bool IsEnemy()
        {
            return Faction == FactionType.Enemy;
        }

        public override void Highlight(bool enable, bool targetLock)
        {
            // 적대 진영(Enemy)일 때만 락온 하이라이트 마커 활성화 허용 (아군/비적대는 비활성화)
            if (!IsEnemy())
            {
                base.Highlight(false, false);
                return;
            }

            base.Highlight(enable, targetLock);
        }
    }
}
