using UnityEngine;
using VContainer;
using Synty.AnimationBaseLocomotion.Samples;

namespace CharacterSystem
{
    /// <summary>
    /// GameObject 상에서 CharacterStatSystem 인스턴스 참조 및 PureStatData 직렬화를 홀딩하며,
    /// SampleObjectLockOn을 상속하여 적대 진영(Enemy) 대상에 대해서만 락온 하이라이트 마커를 활성화합니다.
    /// </summary>
    public class CharacterStatComponent : SampleObjectLockOn
    {
        [SerializeField] private PureStatData pureStatData;

        public PureStatData PureStatData => pureStatData;
        public CharacterStatSystem StatSystem { get; private set; }

        private void Awake()
        {
            EnsureInitialized();
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
