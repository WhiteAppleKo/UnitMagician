using UnityEngine;
using VContainer;
using VContainer.Unity;
using CharacterSystem;

namespace EventSequencerSystem
{
    /// <summary>
    /// EventSequencerSystem(09.EventSequencerSystem)의 VContainer LifetimeScope입니다.
    /// bossStatComponent는 인스펙터 고정 참조입니다(06번 기획서 6.9절 "보스 참조 방식" — 씬에 보스가 유일하게
    /// 하나만 존재하므로 별도 태그/식별 인터페이스 없이 고정 참조로 충분).
    /// 03번(보스AI페이즈시퀀스시스템)이 아직 구현되지 않아 실제 보스가 씬에 없으므로, 이 필드에는 임시로
    /// 씬에 이미 배치된 임의의 캐릭터(예: EnemyTest)를 연결해 배선/검증합니다 — 실제 보스로 교체하는 것은
    /// 03번 구현 시점의 작업입니다.
    ///
    /// IInteractionService는 기존 InteractionSystemLifetimeScope를 부모 스코프로 연결해 주입받습니다
    /// (NpcSystemLifetimeScope와 동일한 parentReference 패턴).
    ///
    /// [DefaultExecutionOrder] 필요 이유: bossStatComponent.StatService는 그 GameObject의 자체 Awake()
    /// (CharacterStatComponent.EnsureInitialized())가 먼저 실행되어야 준비됩니다. Unity는 서로 다른
    /// GameObject 간 Awake() 순서를 보장하지 않으므로, 이 스코프의 Awake()(Configure 포함)가 항상
    /// 나중에 실행되도록 명시적으로 순서를 뒤로 미룹니다(기본 순서 0보다 큰 값).
    /// </summary>
    [DefaultExecutionOrder(500)]
    public class EventSequencerSystemLifetimeScope : LifetimeScope
    {
        [Header("Boss Reference (Temporary substitute until 03.BossAI exists)")]
        [SerializeField] private CharacterStatComponent bossStatComponent;

        [Header("Pure Data (Optional - auto-created with defaults if empty)")]
        [SerializeField] private PureDataTutorialSequence pureDataTutorialSequence;

        protected override void Awake()
        {
            parentReference = ParentReference.Create<InteractionSystem.InteractionSystemLifetimeScope>();
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            if (bossStatComponent == null)
            {
                Debug.LogError("[EventSequencerSystemLifetimeScope] bossStatComponent가 연결되지 않았습니다. " +
                                "씬의 임의 캐릭터(예: EnemyTest)를 인스펙터에 임시로 연결하세요.");
                return;
            }

            if (bossStatComponent.StatService == null)
            {
                Debug.LogError("[EventSequencerSystemLifetimeScope] bossStatComponent.StatService가 아직 " +
                                "초기화되지 않았습니다(CharacterStatComponent.Awake() 미실행 또는 PureStatData 미지정).");
                return;
            }

            // 1. 보스(대체 참조) ICharacterStatService + 식별용 GameObject 등록
            builder.RegisterInstance<ICharacterStatService>(bossStatComponent.StatService);
            builder.RegisterInstance(bossStatComponent.gameObject);

            // 2. Pure Data 등록 (인스펙터 미지정 시 기본값으로 생성)
            PureDataTutorialSequence pureData = pureDataTutorialSequence != null
                ? pureDataTutorialSequence
                : ScriptableObject.CreateInstance<PureDataTutorialSequence>();
            builder.RegisterInstance(pureData);

            // 3. Runtime Data 등록
            builder.Register<RuntimeDataTutorialSequence>(Lifetime.Scoped);

            // 4. Logic System 등록 (IInteractionService는 부모 스코프인 InteractionSystemLifetimeScope에서 주입됨)
            builder.Register<TutorialSequencerLogicSystem>(Lifetime.Scoped)
                .As<ITickable>()
                .As<System.IDisposable>()
                .As<IEventSequencer>();
        }
    }
}