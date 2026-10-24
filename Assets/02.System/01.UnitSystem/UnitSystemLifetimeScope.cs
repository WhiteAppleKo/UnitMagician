using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UnitSystem
{
    public class UnitSystemLifetimeScope : LifetimeScope
    {
        [Header("UI Toolkit Components")]
        [SerializeField] private UnitQuickSlotUIComponent quickSlotUI;
        [SerializeField] private UnitUnlockPopupUIComponent unlockPopupUI;
        [SerializeField] private MagicSelectionPopupUIComponent magicSelectionPopupUI;

        [Header("Gameplay Systems")]
        [SerializeField] private UnitCasterSystem unitCaster;
        [SerializeField] private MultiLockOnVisualizer multiLockOnVisualizer;
        [Tooltip("13-C 적용 전 변화 미리보기 비주얼. 비어 있으면 같은 이름의 오브젝트를 새로 만들어 등록합니다(고스트 머티리얼 미지정 시 미리보기가 보이지 않음).")]
        [SerializeField] private LockOnPreviewVisualizer lockOnPreviewVisualizer;

        [Header("Debug/Test Components")]
        [SerializeField] private UnitDebugAcquireTestComponent debugAcquireTest;

        [Header("Event System(09) Linkage - Optional")]
        [Tooltip("이벤트 투사체의 전환점에서 마법 큐를 발동하기 위한 이벤트 시스템 인스펙터 직접 참조입니다. 두 시스템은 서로 다른 " +
                 "VContainer 스코프 트리에 있어 생성자 주입이 불가능하므로, 09/10번과 동일하게 Start()에서 사후 배선합니다.")]
        [UnityEngine.Serialization.FormerlySerializedAs("eventSequencerScope")]
        [SerializeField] private GameEvent.EventSystemLifetimeScope eventSystemScope;

        protected override void Awake()
        {
            parentReference = ParentReference.Create<Movement.RefactoredLocomotion.LocomotionLifetimeScope>();
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            // Data & Service Register
            // 33번: 시작 보유 마법은 플레이어 PureData(PureStatData.defaultUnitMagics)에서 오며, 보유는 캐릭터 인벤토리(Character 스코프)가 소유한다.
            builder.Register<DefaultUnitEquipRequirementChecker>(Lifetime.Singleton).As<IUnitEquipRequirementChecker>();
            builder.Register<UnitCatalogService>(Lifetime.Singleton).As<IUnitCatalogService>();
            builder.Register<UnitChangeService>(Lifetime.Singleton).As<IUnitChangeService>().AsSelf();
            builder.Register<UnitCastRuleService>(Lifetime.Singleton).As<IUnitCastRuleService>();
            builder.Register<UnitBatchCastingService>(Lifetime.Singleton).As<IUnitBatchCastingService>();
            builder.Register<LockOnPreviewCalculator>(Lifetime.Singleton).As<ILockOnPreviewCalculator>();
            builder.Register<RuntimeDataMultiLockOn>(Lifetime.Singleton);
            builder.Register<RuntimeDataUnitQuickSlot>(Lifetime.Singleton);
            builder.Register<RuntimeDataMagicFavoriteSlots>(Lifetime.Singleton);

            // 28번: 마법 타입별 Applicator(순수 C#, 상태 없음) + 단일 적용 주체.
            // 30번: UnitApplicatorRegistry가 IReadOnlyList<IUnitApplicator>로 주입받아 PureData 서브클래스 타입(DataType, 인스턴스 생성)과
            // 대상 속성(UnitType, 목록 재계산) 두 키로 조회한다. 시전(UnitChangeService)·적용(UnitApplyLogicSystem)·미리보기(LockOnPreviewCalculator)가 같은 조회를 쓴다.
            builder.Register<MassUnitApplicator>(Lifetime.Singleton).As<IUnitApplicator>();
            builder.Register<TemperatureUnitApplicator>(Lifetime.Singleton).As<IUnitApplicator>();
            builder.Register<VectorUnitApplicator>(Lifetime.Singleton).As<IUnitApplicator>();
            builder.Register<UnitApplicatorRegistry>(Lifetime.Singleton).As<IUnitApplicatorRegistry>();
            // 31번: 마법 대상 상태 재계산(PureData 리셋 → 적용 목록 순회 → 확정). 실제 적용(UnitApplyLogicSystem)·미리보기(LockOnPreviewCalculator)·비용(UnitChangeService)이 같은 함수를 쓴다.
            builder.Register<UnitTargetStateCalculator>(Lifetime.Singleton).As<IUnitTargetStateCalculator>();
            builder.RegisterEntryPoint<UnitApplyLogicSystem>(Lifetime.Singleton);

            // Logic Systems Register
            builder.RegisterEntryPoint<UnitQuickSlotLogicSystem>(Lifetime.Singleton).AsSelf();
            builder.RegisterEntryPoint<MagicSelectionPopupLogicSystem>(Lifetime.Singleton).As<IMagicSelectionPopupLogic>();
            // 13-C: 락온 큐 적용 전 변화 미리보기(비주얼) + 예상 총비용(마나 UI)
            builder.RegisterEntryPoint<LockOnPreviewLogicSystem>(Lifetime.Singleton);
            builder.RegisterEntryPoint<LockOnManaPreviewLogicSystem>(Lifetime.Singleton);
            // 15-A: 락온 대상 소실(파괴/비활성/풀 반환) 시 지정 대기·큐 엔트리 정리
            builder.RegisterEntryPoint<LockOnTargetLossLogicSystem>(Lifetime.Singleton);
            // AsSelf(): Start()에서 이벤트 시스템의 비행 신호를 사후 배선(BindEventFlight)하기 위해 구체 타입으로 Resolve 가능하게 함.
            builder.Register<EventCastTriggerLogicSystem>(Lifetime.Singleton).AsSelf();

            // UI Toolkit Component Register
            if (quickSlotUI != null) builder.RegisterComponent(quickSlotUI);
            else builder.RegisterComponentInHierarchy<UnitQuickSlotUIComponent>();

            // 33번: 인벤토리 창 틀은 Character UI(InventoryUIComponent)이고, 마법 탭은 초기화 시 탭 레지스트리에 자신을 등록한다.
            builder.RegisterEntryPoint<MagicInventoryTabView>(Lifetime.Singleton);

            if (unlockPopupUI != null) builder.RegisterComponent(unlockPopupUI);
            else builder.RegisterComponentInHierarchy<UnitUnlockPopupUIComponent>();

            if (magicSelectionPopupUI != null) builder.RegisterComponent(magicSelectionPopupUI);
            else builder.RegisterComponentInHierarchy<MagicSelectionPopupUIComponent>();

            // Gameplay Component Register
            if (unitCaster != null) builder.RegisterComponent(unitCaster);
            else builder.RegisterComponentInHierarchy<UnitCasterSystem>();

            if (multiLockOnVisualizer != null) builder.RegisterComponent(multiLockOnVisualizer).As<IMultiLockOnVisualizer>();
            else builder.RegisterComponentInHierarchy<MultiLockOnVisualizer>().As<IMultiLockOnVisualizer>();

            if (lockOnPreviewVisualizer != null) builder.RegisterComponent(lockOnPreviewVisualizer).As<ILockOnPreviewVisualizer>();
            else builder.RegisterComponentOnNewGameObject<LockOnPreviewVisualizer>(Lifetime.Singleton, "LockOnPreviewVisualizer").As<ILockOnPreviewVisualizer>();

            // Debug/Test Component Register (테스트 전용 — 실제 게임 기능 아님)
            if (debugAcquireTest != null) builder.RegisterComponent(debugAcquireTest);
            else builder.RegisterComponentInHierarchy<UnitDebugAcquireTestComponent>();

            // Player Magic LockOn Component Register
            builder.RegisterComponentInHierarchy<MagicLockOnComponent>();

            // InputReader 등록
            builder.RegisterComponentInHierarchy<Synty.AnimationBaseLocomotion.Samples.InputSystem.InputReader>();
        }

        /// <summary>
        /// 이벤트 시스템(09)과의 연동 배선입니다. 두 스코프의 Container가 모두 준비된 뒤인 Start()에서 한 번만
        /// 연결합니다(CharacterLifetimeScope/AllySupportSystemLifetimeScope와 동일한 패턴).
        /// GameObject.Find/FindObjectOfType은 사용하지 않고 인스펙터에 직접 연결해 둔 eventSystemScope 참조만 씁니다.
        /// </summary>
        private void Start()
        {
            if (eventSystemScope == null)
            {
                Debug.LogWarning("[UnitSystemLifetimeScope] eventSystemScope가 연결되지 않아 이벤트 시스템 연동 없이 동작합니다. " +
                                  "이벤트 투사체 전환점 마법 발동은 트리거되지 않습니다(일반 시간정지 발동은 영향 없음).");
                return;
            }

            if (Container == null || eventSystemScope.Container == null)
            {
                Debug.LogError("[UnitSystemLifetimeScope] Container가 아직 준비되지 않아 이벤트 시스템 연동을 배선할 수 없습니다.");
                return;
            }

            var eventCastTrigger = Container.Resolve<EventCastTriggerLogicSystem>();
            eventCastTrigger.BindEventFlight(eventSystemScope.Container.Resolve<GameEvent.IEventFlightLogic>());
        }
    }
}
