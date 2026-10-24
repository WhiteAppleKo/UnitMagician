using System;
using UnityEngine;
using Synty.AnimationBaseLocomotion.Samples;
using TimeSlowFilterSystem;
using Pooling;
using PipeLine.Combat;

namespace UnitSystem
{
    /// <summary>
    /// 오브젝트에 부착되는 단위 데이터 그룹 및 락온 타겟 컴포넌트입니다.
    /// 마법 대상 PureData(PureDataUnitTarget - 지원 속성, 원본 온도, 31번)를 참조하고, 그 PureData로 초기화한
    /// 마법 대상 RuntimeData(RuntimeDataUnitTarget: 현재 상태 + 적용 마법 목록)를 하나 보유하며, 목록이 바뀌면 마법 적용 요청(OnAnyUnitApplyRequested)을,
    /// 즉시 마법(Vector)이 적용되면 즉시 마법 적용 요청(OnAnyInstantUnitApplyRequested, 32번)을 발행합니다.
    /// 원본 값은 이 컴포넌트에 두지 않습니다(PureData에만 있음).
    /// 풀링되는 오브젝트(15-B 아군 투사체)에 붙어 있으면 반환 시 "대상 소실"을 알리고(OnAnyTargetRetired),
    /// 재사용 직전에 적용 목록/상태(PureData 값)/락온 마커를 초기값으로 되돌립니다(IPoolReusable).
    /// </summary>
    public class RuntimeDataUnitGroup : SampleObjectLockOn, IPoolReusable
    {
        [Header("Unit Group Setup")]
        [Tooltip("이 마법 대상의 원본 PureData(지원 속성, 원본 온도). 비어 있으면 지원 속성 없음으로 동작합니다.")]
        [SerializeField] private PureDataUnitTarget targetData;
        [SerializeField] private bool isTargetable = true;

        public bool IsTargetable => isTargetable;

        public event Action<bool> OnTargetableChanged;

        /// <summary>
        /// 풀로 반환되는 대상(IPoolReusable.OnReturnedToPool)이 발행하는 소실 통지(15-B). 락온 큐/지정 대기를 관리하는 쪽
        /// (LockOnTargetLossLogicSystem)이 구독해, 반환과 재사용이 같은 프레임에 일어나도 주기 점검을 기다리지 않고
        /// 해당 대상의 엔트리를 즉시 제거한다. 투사체(07.NpcSystem)가 UnitSystem의 락온 데이터를 직접 알지 않도록 하는 연결 고리다.
        /// </summary>
        public static event Action<RuntimeDataUnitGroup> OnAnyTargetRetired;

        /// <summary>
        /// 어떤 대상이든 적용 마법 목록이 바뀌면(RuntimeDataUnitTarget.OnAppliedUnitsChanged - 추가/해제) 발행되는 마법 적용 요청(28번 §6-6 (b), 31번 §3-2 4).
        /// 마법 적용의 단일 호출 주체인 UnitApplyLogicSystem(순수 C#, VContainer 등록)이 구독해, 대상 상태를 "PureData 값으로 리셋 → 목록 순회 적용 → 확정"으로
        /// 다시 만든 뒤(IUnitTargetStateCalculator.Recalculate) 바뀐 속성의 시각과 데미지를 적용한다.
        /// 인자: (대상 묶음, 대상 RuntimeData, 목록이 바뀐 속성, 추가된 인스턴스 - 해제면 null).
        /// 대상 묶음(UnitApplyTarget)은 Awake에서 같은 오브젝트의 IUnitTargetVisualizer/IAttackDamageProvider로 한 번 구성한다(Logic이 GetComponent하지 않음).
        /// </summary>
        public static event Action<UnitApplyTarget, RuntimeDataUnitTarget, UnitType, RuntimeDataUnit> OnAnyUnitApplyRequested;

        /// <summary>
        /// 어떤 대상이든 즉시 마법(Vector)이 적용되면(RuntimeDataUnitTarget.OnInstantUnitApplied) 발행되는 즉시 마법 적용 요청(32번 §3-2 3).
        /// UnitApplyLogicSystem이 구독해 재계산 없이 그 인스턴스의 Applicator로 시각만 한 번 적용한다(목록에 남지 않으므로 해제·재계산·풀 재사용 때 되돌리지 않음).
        /// 인자: (대상 묶음, 대상 RuntimeData, 적용된 즉시 마법 인스턴스).
        /// </summary>
        public static event Action<UnitApplyTarget, RuntimeDataUnitTarget, RuntimeDataUnit> OnAnyInstantUnitApplyRequested;

        private bool initialIsTargetable;
        private UnitApplyTarget applyTarget;

        /// <summary>마법 대상 RuntimeData(PureData 기반 현재 상태 + 적용 마법 목록). Awake(InitUnits) 전에는 null.</summary>
        public RuntimeDataUnitTarget UnitTarget { get; private set; }

        /// <summary>
        /// 마법 대상으로 유효한지의 단일 판정 기준(15-A): 파괴되지 않았고(Unity null 아님) 계층에서 활성 상태일 것.
        /// 파괴됨과 비활성/풀 반환됨을 모두 "소실"로 취급한다.
        /// </summary>
        public static bool IsValidTarget(RuntimeDataUnitGroup group)
        {
            return group != null && group.gameObject.activeInHierarchy;
        }

        private void Awake()
        {
            initialIsTargetable = isTargetable;

            // 같은 프리팹 내부 구성요소 참조(VContainer_가이드라인 §4)로 마법 적용 대상 묶음을 한 번 구성한다.
            // 공격 데미지 데이터는 투사체 등 IAttackDamageProvider를 가진 대상에만 있다(없으면 null).
            var attackDamageProvider = GetComponent<IAttackDamageProvider>();
            applyTarget = new UnitApplyTarget(GetComponent<IUnitTargetVisualizer>(), attackDamageProvider?.AttackDamage);

            if (UnitTarget == null)
            {
                InitUnits();
            }
        }

        /// <summary>풀 반환 순간(아직 활성): 락온 마커를 끄고 락온 큐/지정 대기에 소실을 즉시 통지한다.</summary>
        public void OnReturnedToPool()
        {
            Highlight(false, false);
            OnAnyTargetRetired?.Invoke(this);
        }

        /// <summary>재사용 직전: 적용 마법 목록을 비우고 상태(질량 배율 1, 온도 = PureData 값)를 되돌리며(이벤트 없음), 타게팅 가능 여부/락온 마커도 초기화한다.</summary>
        public void ResetForReuse()
        {
            if (UnitTarget != null) UnitTarget.Reset();
            else InitUnits();
            isTargetable = initialIsTargetable;
            Highlight(false, false);
        }

        private void OnEnable()
        {
            TargetStencilService.RegisterTargetStatic(this);
        }

        private void OnDisable()
        {
            TargetStencilService.UnregisterTargetStatic(this);
        }

        /// <summary>대상 PureData로 초기화한(질량 배율 1, 온도 = PureData 값) 빈 적용 목록의 마법 대상 RuntimeData를 새로 구성한다.</summary>
        public void InitUnits()
        {
            UnbindUnitEvents();

            if (targetData == null)
            {
                Debug.LogWarning($"[RuntimeDataUnitGroup] '{name}' has no PureDataUnitTarget (targetData). It will not accept any magic.", this);
            }

            UnitTarget = new RuntimeDataUnitTarget(targetData);
            UnitTarget.OnAppliedUnitsChanged += HandleAppliedUnitsChanged;
            UnitTarget.OnInstantUnitApplied += HandleInstantUnitApplied;
        }

        private void OnDestroy()
        {
            UnbindUnitEvents();
        }

        private void UnbindUnitEvents()
        {
            if (UnitTarget == null) return;

            UnitTarget.OnAppliedUnitsChanged -= HandleAppliedUnitsChanged;
            UnitTarget.OnInstantUnitApplied -= HandleInstantUnitApplied;
        }

        private void HandleAppliedUnitsChanged(UnitType unitType, RuntimeDataUnit added)
        {
            if (UnitTarget == null || applyTarget == null) return;

            OnAnyUnitApplyRequested?.Invoke(applyTarget, UnitTarget, unitType, added);
        }

        private void HandleInstantUnitApplied(RuntimeDataUnit instance)
        {
            if (UnitTarget == null || applyTarget == null) return;

            OnAnyInstantUnitApplyRequested?.Invoke(applyTarget, UnitTarget, instance);
        }

        public void SetTargetable(bool targetable)
        {
            if (isTargetable == targetable) return;

            isTargetable = targetable;
            if (!isTargetable)
            {
                Highlight(false, false);
            }

            OnTargetableChanged?.Invoke(isTargetable);
        }

        public override void Highlight(bool enable, bool targetLock)
        {
            if (!isTargetable)
            {
                base.Highlight(false, false);
                return;
            }

            base.Highlight(enable, targetLock);
        }
    }
}
