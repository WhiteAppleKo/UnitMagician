using UnityEngine;
using VContainer;

namespace CharacterSystem
{
    /// <summary>
    /// GameObject 상에서 CharacterStatSystem 인스턴스 참조 및 PureStatData 직렬화를 홀딩하는 컴포넌트입니다.
    /// </summary>
    public class CharacterStatComponent : MonoBehaviour
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
    }
}
