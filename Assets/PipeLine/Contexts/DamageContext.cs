using UnityEngine;

namespace PipeLine.Contexts
{
    /// <summary>
    /// 캐릭터 공격/피격 파이프라인 연산에 활용되는 Pure Data Context 구조체입니다.
    /// 매 타격마다 생성되는 핫 패스이므로 GC 할당을 피하기 위해 struct로 선언되어 있습니다.
    /// (값 타입이므로 메서드 인자로 전달/재대입 시 복사가 발생합니다. 파이프라인 내부에서는
    /// `context = await step.Execute(context)` 처럼 반환값으로 갱신된 복사본을 계속 이어받는 방식으로 사용합니다.)
    /// </summary>
    public struct DamageContext
    {
        public GameObject Attacker { get; set; }

        /// <summary>
        /// 실제로 피격 대상과 충돌한 물리적 가해 오브젝트(예: 투사체)입니다. Attacker는 소유자(owner - 투사체를 쏜 NPC/마법 시전자)일 수
        /// 있어 투사체 자신이 아니므로, 투사체에 부착된 속성 데이터(RuntimeDataUnitGroup의 온도/질량 등)를 읽어야 하는 스텝
        /// (AttributeGateStep)은 이 값을 우선 사용합니다. 지정하지 않으면(null) Attacker를 가해 오브젝트로 간주합니다.
        /// </summary>
        public GameObject Source { get; set; }

        public GameObject Victim { get; set; }
        public int RawDamage { get; set; }
        public float AttackerAccRate { get; set; }
        public float VictimEvaRate { get; set; }
        public float CritRate { get; set; }
        public float CritMultiplier { get; set; }
        public int Defense { get; set; }

        public bool IsEvaded { get; set; }
        public bool IsCritical { get; set; }
        public int FinalDamage { get; set; }

        /// <summary>
        /// 공격 파이프라인 실행이 끝나고 피격 파이프라인으로 넘어갔음을 나타내는 플래그입니다.
        /// "피격 판정"과 "데미지 판정"을 분리해서 기록하기 위해 추가되었습니다.
        /// </summary>
        public bool HitRegistered { get; set; }

        /// <summary>
        /// 어떤 스텝이든 "이 이후 스텝은 실행할 필요가 없다"고 판단하면 true로 설정합니다.
        /// <see cref="PipeLine.Combat.CombatPipelineManager"/>가 공격/피격 루프에서 각 스텝 실행 직후
        /// 이 플래그를 확인해 즉시 중단합니다 (예: EvasionStep이 회피를 판정하면 이후 Defense 등은 불필요).
        /// 회피 전용이 아니라 범용 중단 신호이므로, 향후 다른 게이트 스텝(예: 속성 게이트)도 동일하게 재사용할 수 있습니다.
        /// 단, ApplyDamageStep은 플래그 게이트/중단 대상이 아닌 고정 마지막 스텝이라 이 값과 무관하게 항상 실행됩니다.
        /// </summary>
        public bool Aborted { get; set; }

        /// <summary>
        /// 속성(온도) 게이트 결과(20번 기획서). AttributeGateStep이 실행된 경우에만 true가 될 수 있으며(기본 false),
        /// 실패하면 Aborted도 함께 세워져 이후 스텝과 데미지 적용이 중단됩니다. 소비자(돌문/보스 판정)가 최종 판정 때 개별 확인합니다.
        /// </summary>
        public bool AttributeGatePassed { get; set; }

        /// <summary>
        /// 크기(질량 비율) 게이트 결과(20번 기획서). SizeGateStep이 실행된 경우에만 true가 될 수 있으며(기본 false),
        /// 실패해도 Aborted는 세우지 않습니다 - 속성은 통과했으므로 "금 갔다 복구" 같은 피드백이 나가야 하기 때문입니다.
        /// </summary>
        public bool SizeGatePassed { get; set; }
    }
}
