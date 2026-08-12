using UnityEngine;

namespace PipeLine.Contexts
{
    /// <summary>
    /// 캐릭터 피격 파이프라인 연산에 활용되는 Pure Data Context 객체입니다.
    /// </summary>
    public class DamageContext
    {
        public GameObject Attacker { get; set; }
        public GameObject Victim { get; set; }
        public int RawDamage { get; set; }
        public float AttackerAccRate { get; set; } = 0.9f;
        public float VictimEvaRate { get; set; } = 0.1f;
        public float CritRate { get; set; } = 0.2f;
        public float CritMultiplier { get; set; } = 1.5f;
        public int Defense { get; set; } = 10;

        public bool IsEvaded { get; set; }
        public bool IsCritical { get; set; }
        public int FinalDamage { get; set; }
    }
}
