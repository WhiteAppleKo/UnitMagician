using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using CharacterSystem;

namespace GameEvent
{
    /// <summary>
    /// 이벤트 시스템(09.EventSystem)의 VContainer LifetimeScope입니다. 범용 이벤트 실행기(EventLogicSystem, IEventSystem)와
    /// 씬 시작 시 등록되는 이벤트 SO 목록(PureDataEventSet), 이벤트 대상(IEventTarget) 목록, 트리거/완료 평가기를 등록하고,
    /// 이벤트 투사체 비행 계산(EventFlightLogicSystem) 및 그 비주얼 추적기(EventFlightTrackerComponent)를 등록합니다.
    /// 이벤트 동작 핸들러(IEventActionHandler)는 이 스코프가 아니라 각 소유 시스템의 LifetimeScope가 Start()에서 IEventSystem.RegisterActionHandler로 사후 등록합니다
    /// (서로 다른 VContainer 스코프 트리라 생성자 주입이 불가능).
    ///
    /// 보스 50% 이벤트(강 이벤트)는 이벤트 SO(PureDataEvent)로 정의되며, 예전의 Tutorial 시퀀서(고정 신호 5개)는 99.Legacy로 격리되었습니다.
    /// bossStatComponent는 비행 추적기에 보스 위치(Transform)를 내려주기 위한 인스펙터 고정 참조입니다(06번 기획서 6.9절 "보스 참조 방식" - 씬에 보스가 유일하게
    /// 하나만 존재하므로 고정 참조로 충분). 보스의 체력/무적 상태는 이벤트 대상(HealthEventTarget, IEventHealthTarget)이 id로 제공합니다.
    ///
    /// IInteractionService는 기존 InteractionSystemLifetimeScope를 부모 스코프로 연결해 주입받습니다
    /// (NpcSystemLifetimeScope와 동일한 parentReference 패턴).
    ///
    /// [DefaultExecutionOrder] 필요 이유: bossStatComponent의 자체 Awake()가 먼저 실행되어야 하고, 서로 다른 GameObject 간 Awake() 순서를
    /// Unity가 보장하지 않으므로, 이 스코프의 Awake()(Configure 포함)가 항상 나중에 실행되도록 명시적으로 순서를 뒤로 미룹니다(기본 순서 0보다 큰 값).
    /// </summary>
    [DefaultExecutionOrder(500)]
    public class EventSystemLifetimeScope : LifetimeScope
    {
        [Header("Event SO 등록 (씬 시작 시 이벤트 시스템에 등록되는 이벤트 SO 목록)")]
        [SerializeField] private PureDataEventSet eventSet;

        [Header("Event Targets (이벤트 SO가 id로 가리키는 월드 대상 - IEventTarget을 구현한 컴포넌트를 직접 등록, Find* 금지)")]
        [SerializeField] private List<MonoBehaviour> eventTargets = new List<MonoBehaviour>();

        [Header("Boss Reference (비행 추적기에 보스 위치를 내려주는 고정 참조 - 03.BossAI 전까지 임시 대체 참조)")]
        [SerializeField] private CharacterStatComponent bossStatComponent;

        [Header("Event Flight Tracker (13번 - 이벤트 투사체 비행 추적기)")]
        [Tooltip("투사체/플레이어/보스 위치를 값으로 제공하는 비주얼 구현체(IEventFlightTracker). 인스펙터 고정 참조입니다.")]
        [SerializeField] private EventFlightTrackerComponent flightTracker;

        protected override void Awake()
        {
            parentReference = ParentReference.Create<InteractionSystem.InteractionSystemLifetimeScope>();
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            ConfigureEventSystem(builder);
            ConfigureEventProjectileFlight(builder);
        }

        /// <summary>
        /// 범용 이벤트 시스템 등록입니다. 평가기/대상 레지스트리/실행기는 모두 인터페이스로 등록하며, 동작 핸들러와 소유 시스템 평가기는
        /// 각 소유 시스템의 LifetimeScope가 Start()에서 IEventSystem.Register*로 사후 등록합니다(서로 다른 스코프 트리라 생성자 주입 불가).
        /// </summary>
        private void ConfigureEventSystem(IContainerBuilder builder)
        {
            if (eventSet == null)
            {
                Debug.LogError("[EventSystemLifetimeScope] eventSet(PureDataEventSet)이 연결되지 않았습니다. 등록된 이벤트 없이 동작합니다.");
            }

            var targets = new List<IEventTarget>();
            foreach (var targetBehaviour in eventTargets)
            {
                if (targetBehaviour is IEventTarget target)
                {
                    targets.Add(target);
                }
                else if (targetBehaviour != null)
                {
                    Debug.LogError($"[EventSystemLifetimeScope] eventTargets의 '{targetBehaviour.name}'({targetBehaviour.GetType().Name})은 IEventTarget을 구현하지 않아 등록하지 않았습니다.");
                }
            }

            builder.RegisterInstance(eventSet != null ? eventSet : ScriptableObject.CreateInstance<PureDataEventSet>());
            builder.Register<RuntimeDataEvent>(Lifetime.Scoped);
            builder.Register<EventTargetRegistry>(Lifetime.Scoped)
                .WithParameter<IEventTarget[]>(targets.ToArray())
                .As<IEventTargetProvider>();
            builder.Register<SceneStartTriggerEvaluator>(Lifetime.Scoped).As<IEventTriggerEvaluator>();
            builder.Register<BossHpRatioTriggerEvaluator>(Lifetime.Scoped).As<IEventTriggerEvaluator>();
            builder.Register<TargetDestroyedCompletionEvaluator>(Lifetime.Scoped).As<IEventCompletionEvaluator>();
            builder.Register<CoreHitCompletionEvaluator>(Lifetime.Scoped).As<IEventCompletionEvaluator>();
            builder.Register<EventLogicSystem>(Lifetime.Scoped)
                .As<IEventSystem>()
                .As<ITickable>()
                .As<System.IDisposable>();
        }

        /// <summary>
        /// 이벤트 투사체 비행 등록입니다. 비행 추적기(비주얼)에 보스 Transform을 내려주고, 비행 계산 로직(IEventFlightLogic)을 등록합니다.
        /// 발사 수치(각도/거리/속도/데미지)는 이벤트 SO(EventProjectileLaunchAction)가 비행 시작 때 넘기므로 이 스코프에는 PureData 등록이 없습니다.
        /// </summary>
        private void ConfigureEventProjectileFlight(IContainerBuilder builder)
        {
            if (flightTracker == null)
            {
                Debug.LogError("[EventSystemLifetimeScope] flightTracker(EventFlightTrackerComponent)가 연결되지 않았습니다. " +
                                "이벤트 투사체 비행 구간 계산을 할 수 없어 비행 로직 등록을 중단합니다.");
                return;
            }

            if (bossStatComponent == null)
            {
                Debug.LogError("[EventSystemLifetimeScope] bossStatComponent가 연결되지 않았습니다. " +
                                "씬의 보스(또는 임시 대체 참조, 예: EnemyTest)를 인스펙터에 연결하세요.");
                return;
            }

            flightTracker.SetBossTransform(bossStatComponent.transform);
            builder.RegisterComponent(flightTracker).As<IEventFlightTracker>();
            builder.Register<EventFlightLogicSystem>(Lifetime.Scoped)
                .As<IEventFlightLogic>()
                .As<ITickable>();
        }
    }
}
