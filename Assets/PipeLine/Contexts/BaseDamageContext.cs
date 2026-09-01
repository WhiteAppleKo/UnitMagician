using UnityEngine;

namespace PipeLine
{
    public class BaseDamageContext
    {
        public GameObject Attacker { get; set; }
        public GameObject Victim { get; set; }
        
        public int BaseDamage { get; set; }
        public float VictimEvadeRate { get; set; }
        public float VictimBlockChance { get; set; }
        public float AttackerCritChance { get; set; }
        public float AttackerCritDamage { get; set; }
        public float AttackerAccuracy { get; set; }
        
        public bool IsEvaded { get; set; }
        public bool IsBlocked { get; set; }
        
        public bool AttackFailed  { get; set; }
    }
}
