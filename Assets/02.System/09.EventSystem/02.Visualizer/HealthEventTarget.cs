using System;
using UnityEngine;
using CharacterSystem;

namespace GameEvent
{
    /// <summary>
    /// 체력을 가진 캐릭터(예: 보스)를 이벤트 대상(IEventHealthTarget)으로 노출하는 얇은 [V] 컴포넌트입니다. 같은 GameObject의 CharacterStatComponent가 가진
    /// HP/무적 상태를 이벤트 시스템이 id(eventTargetId)로 가리켜 읽을 수 있게 하는 어댑터이며 판단 로직은 없습니다.
    /// 대상 목록은 EventSystemLifetimeScope 인스펙터에 직접 등록합니다(Find* 금지). HP 통지는 첫 구독 시점에 CharacterStatComponent.StatService가 준비된 뒤 연결합니다.
    /// </summary>
    public class HealthEventTarget : MonoBehaviour, IEventHealthTarget
    {
        [Tooltip("이벤트 SO(BossHpRatioTrigger/CoreHitCompletion)가 이 대상을 가리킬 때 쓰는 id.")]
        [SerializeField] private string eventTargetId;
        [Tooltip("HP/무적 상태를 읽을 같은 GameObject의 CharacterStatComponent.")]
        [SerializeField] private CharacterStatComponent statComponent;

        private Action<float> hpRatioChanged;
        private Action destroyed;
        private IReadOnlyClampValueInt boundHp;

        public string TargetId => eventTargetId;
        public GameObject TargetObject => gameObject;

        public bool IsDestroyed => statComponent != null && statComponent.StatService != null && statComponent.StatService.RuntimeData.HP.CurrentValue <= 0;

        public bool IsInvincible => statComponent != null && statComponent.StatService != null && statComponent.StatService.RuntimeData.IsInvincible;

        public float HpRatio
        {
            get
            {
                var hp = statComponent != null ? statComponent.StatService?.RuntimeData.HP : null;
                return hp != null && hp.MaxValue > 0 ? hp.CurrentValue / (float)hp.MaxValue : 1f;
            }
        }

        public event Action<float> OnHpRatioChanged
        {
            add { hpRatioChanged += value; BindHp(); }
            remove { hpRatioChanged -= value; }
        }

        public event Action OnDestroyed
        {
            add { destroyed += value; BindHp(); }
            remove { destroyed -= value; }
        }

        private void BindHp()
        {
            if (boundHp != null || statComponent == null || statComponent.StatService == null) return;

            boundHp = statComponent.StatService.RuntimeData.HP;
            boundHp.OnValueChanged += HandleHpChanged;
        }

        private void HandleHpChanged(int current, int max)
        {
            hpRatioChanged?.Invoke(max > 0 ? current / (float)max : 1f);

            if (current <= 0) destroyed?.Invoke();
        }

        private void OnDestroy()
        {
            if (boundHp != null) boundHp.OnValueChanged -= HandleHpChanged;
            boundHp = null;
        }
    }
}
