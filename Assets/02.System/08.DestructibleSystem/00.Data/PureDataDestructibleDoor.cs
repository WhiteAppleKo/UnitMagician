using UnityEngine;
using CharacterSystem;
using PipeLine.Combat;

namespace DestructibleSystem
{
    /// <summary>
    /// DestructibleDoorLogicSystem이 참조하는 불변 설정값([D] Pure Data)입니다.
    /// 공격자 쪽 기본 데미지(baseDamage)는 이 SO의 책임이 아니므로(공격자 PureData/CollisionDamageTrigger 쪽에 있음),
    /// 여기서는 파괴 판정에 필요한 임계값(Hp)과 게이트 요구값(속성 최대 온도/크기 질량 비율), 전투 파이프라인에서의 진영/피격 플래그만 보관합니다.
    /// 판정 코드(AttributeGateStep/SizeGateStep/DestructibleDoorLogicSystem)에는 기준 숫자가 없으며 모두 여기서 읽습니다(20번 기획서).
    /// </summary>
    [CreateAssetMenu(fileName = "PureDataDestructibleDoor_", menuName = "DestructibleSystem/PureDataDestructibleDoor")]
    public class PureDataDestructibleDoor : ScriptableObject
    {
        [Header("Destruction Threshold")]
        [SerializeField] private int hp = 100;

        [Header("Gate Requirements (20번 기획서 - 판정 요구값은 전부 여기서 읽는다)")]
        [Tooltip("속성 게이트: 투사체 온도가 이 값 미만이어야 통과(기본 0 = 얼음 계열). 통과 못 하면 스파크(데미지 미적용).")]
        [SerializeField] private float requiredMaxTemperature = 0f;
        [Tooltip("크기 게이트: 투사체 질량 비율(현재/원본)이 이 값 이상이어야 통과. 속성은 통과했는데 못 넘으면 금 갔다 복구.")]
        [SerializeField] private float requiredMassRatio = 1.5f;

        [Header("Combat Pipeline (16-A: 엔티티별 진영/피격 플래그)")]
        [Tooltip("돌문 진영. Neutral = 어떤 투사체에게나 피격 대상.")]
        [SerializeField] private FactionType faction = FactionType.Neutral;
        [Tooltip("돌문 피격 스텝. 회피/방어 판정은 필요 없고 속성(온도) 게이트와 크기(질량 비율) 게이트만 쓴다.")]
        [SerializeField] private HitStepFlags hitFlags = HitStepFlags.AttributeGate | HitStepFlags.SizeGate;

        /// <summary>파괴 판정 임계값입니다. 매 충돌마다 FinalDamage가 이 값 이상이면(게이트 통과 전제) 파괴됩니다(누적 없음).</summary>
        public int Hp => hp;

        /// <summary>속성 게이트 통과 조건: 투사체 온도 &lt; 이 값.</summary>
        public float RequiredMaxTemperature => requiredMaxTemperature;

        /// <summary>크기 게이트 통과 조건: 투사체 질량 비율 &gt;= 이 값.</summary>
        public float RequiredMassRatio => requiredMassRatio;

        public FactionType Faction => faction;
        public HitStepFlags HitFlags => hitFlags;
    }
}
