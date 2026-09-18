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
    }
}
