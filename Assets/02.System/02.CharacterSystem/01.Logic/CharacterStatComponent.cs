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
    public class CharacterStatComponent : SampleObjectLockOn, IDamageable
    {
        [SerializeField] private PureStatData pureStatData;

        public static CharacterStatComponent PlayerStat { get; private set; }

        public PureStatData PureStatData => pureStatData;
        public CharacterStatSystem StatSystem { get; private set; }
        public ICharacterStatService StatService => StatSystem;

        private void Awake()
        {
            EnsureInitialized();
            if (!IsEnemy())
            {
                PlayerStat = this;
            }
        }

        private void OnDestroy()
        {
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
        }

        /// <summary>
        /// IDamageable 구현: 피격 파이프라인(ApplyDamageStep)이 산출한 최종 데미지를 StatService.TakeDamage로 위임합니다.
        /// </summary>
        public void ApplyDamage(int amount)
        {
            EnsureInitialized();
            StatService?.TakeDamage(amount);
        }

        private void EnsureInitialized()
        {
            if (StatSystem == null && pureStatData != null)
            {
                StatSystem = new CharacterStatSystem(pureStatData);
            }
        }

        public bool IsEnemy()
        {
            if (StatSystem?.RuntimeData != null)
            {
                return StatSystem.RuntimeData.CurrentFaction == FactionType.Enemy;
            }
            if (pureStatData != null)
            {
                return pureStatData.DefaultFaction == FactionType.Enemy;
            }
            return false;
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
