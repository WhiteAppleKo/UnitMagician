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
        public float AttackerAccRate { get; set; }
        public float VictimEvaRate { get; set; }
        public float CritRate { get; set; }
        public float CritMultiplier { get; set; }
        public int Defense { get; set; }

        public bool IsEvaded { get; set; }
        public bool IsCritical { get; set; }
        public int FinalDamage { get; set; }
    }
}
