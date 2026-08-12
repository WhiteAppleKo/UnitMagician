using UnityEngine;
using VContainer;

namespace CharacterSystem
{
    /// <summary>
    /// GameObject 상에서 CharacterStatSystem 인스턴스 참조를 홀딩하는 컴포넌트입니다.
    /// </summary>
    public class CharacterStatComponent : MonoBehaviour
    {
        public CharacterStatSystem StatSystem { get; private set; }

        [Inject]
        public void Construct(CharacterStatSystem statSystem)
        {
            Initialize(statSystem);
        }

        public void Initialize(CharacterStatSystem statSystem)
        {
            StatSystem = statSystem;
        }
    }
}
