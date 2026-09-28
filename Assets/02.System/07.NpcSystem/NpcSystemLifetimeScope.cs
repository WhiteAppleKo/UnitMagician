using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace NpcSystem
{
    /// <summary>
    /// NpcSystem(07.NpcSystem)의 VContainer LifetimeScope입니다.
    /// NpcCombatActorLogicSystem(순수 C# 로직)은 NPC마다 NpcCombatActorVisualizer.Awake()가 직접 new로 생성해
    /// 보유하므로(RuntimeDataUnitGroup.Awake() 패턴 - NPC는 다중 인스턴스이므로 VContainer 싱글턴 등록 대상이 아니다)
    /// 여기서 등록하지 않는다. 다만 Attack 실행 시 런타임 스폰 오브젝트(투사체)에
    /// IObjectResolver.InjectGameObject를 명시적으로 호출해야 하므로, NpcCombatActorVisualizer(MonoBehaviour)가
    /// [Inject] Construct(IObjectResolver)를 통해 컨테이너의 리졸버를 주입받아야 한다.
    ///
    /// [버그 정정 - 튜토리얼 씬 통합 조립 중 발견] 이 주입은 과거 builder.RegisterComponentInHierarchy&lt;NpcCombatActorVisualizer&gt;()로
    /// 처리했으나, VContainer 소스(FindComponentProvider.cs)를 확인한 결과 이 메서드는 "씬 하이어라키의 모든 인스턴스를
    /// 등록한다"는 게 아니라 씬에서 가장 먼저 찾은 인스턴스 단 하나만 등록/주입한다. 그 결과 씬에 NpcCombatActorVisualizer가
    /// 둘 이상(보스용 EnemyTest, 아군용 NpcCombatActor_Dummy 등) 있으면 하이어라키 순서상 먼저인 쪽만 주입받고 나머지는
    /// resolver가 null로 남아 SpawnAndInject -> resolver.InjectGameObject가 실패한다.
    ///
    /// [추가 발견 - Play Mode 실검증 중 확인] 처음에는 LifetimeScope.autoInjectGameObjects(인스펙터 직렬화 GameObject
    /// 리스트)로 교체를 시도했으나, 이 메커니즘은 IObjectResolver.InjectGameObject(GameObject)를 호출해 대상
    /// GameObject에 붙은 "모든" MonoBehaviour를 재귀적으로 주입한다. 그런데 EnemyTest/NpcCombatActor_Dummy에는
    /// NpcCombatActorVisualizer 외에 CharacterStatComponent(02.CharacterSystem)도 함께 붙어 있고, 이 컴포넌트는
    /// [Inject] Construct(CharacterStatSystem)을 요구한다. CharacterStatSystem은 이 스코프(NpcSystemLifetimeScope,
    /// 부모=InteractionSystemLifetimeScope)의 컨테이너가 아니라 보스/캐릭터별로 별도 관리되는(BossPhaseSystemLifetimeScope가
    /// bossStatComponent.StatService를 인스펙터 고정 참조로 직접 읽는 방식) 값이라 이 스코프 트리에서는 전혀 등록되지 않는다.
    /// 그 결과 autoInjectGameObjects를 쓰면 Awake() 시점에 실제로
    /// "VContainerException: Failed to resolve CharacterSystem.CharacterStatComponent :
    /// No such registration of type: CharacterSystem.CharacterStatSystem" 예외가 발생해 AutoInjectAll() 전체가
    /// 중단되고, 그 결과 두 NpcCombatActorVisualizer 모두 resolver를 주입받지 못하는(=버그가 전혀 해결되지 않는)
    /// 것을 Play Mode 실행으로 직접 확인했다. 따라서 GameObject 전체를 재귀 주입하는 autoInjectGameObjects 대신,
    /// VContainer_가이드라인 2-1/3-3절이 이미 쓰는 "컴포넌트 단위 인스펙터 등록"(builder.RegisterComponent(instance))
    /// 패턴을 NpcCombatActorVisualizer 각 인스턴스에 그대로 적용한다 - 이 방식은 등록된 그 컴포넌트 인스턴스만
    /// 주입하고 같은 GameObject의 다른 컴포넌트(CharacterStatComponent 등)는 건드리지 않는다.
    /// (GameObject.Find/FindObjectOfType는 여전히 금지 - 인스펙터에 직접 등록한 컴포넌트 리스트만 사용)
    ///
    /// ProjectileAttackBehaviorSO.Execute -> visualizer.SpawnAndInject -> resolver.InjectGameObject(projectile) 경로에서
    /// 투사체의 CollisionDamageTrigger.Construct(IInteractionService)가 실제로 해석되려면, 이 스코프의 컨테이너가
    /// IInteractionService/ICombatPipelineManager를 등록한 InteractionSystemLifetimeScope의 자식 컨테이너여야 한다
    /// (VContainer는 자식 스코프가 부모 스코프의 등록을 상속하지만, 형제 스코프끼리는 서로의 등록을 보지 못한다).
    /// 그래서 부모 스코프를 InteractionSystemLifetimeScope로 명시적으로 지정한다.
    /// </summary>
    public class NpcSystemLifetimeScope : LifetimeScope
    {
        [Header("NpcCombatActorVisualizer 인스턴스 (씬에 배치된 모든 인스턴스를 직접 등록 - 인스펙터 전용)")]
        [SerializeField] private List<NpcCombatActorVisualizer> npcCombatActorVisualizers = new List<NpcCombatActorVisualizer>();

        protected override void Awake()
        {
            parentReference = ParentReference.Create<InteractionSystem.InteractionSystemLifetimeScope>();
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            // 같은 구체 타입(NpcCombatActorVisualizer)을 키 없이 둘 이상 RegisterComponent하면 VContainer가
            // "동일 구현 타입 + Singleton" 조합을 컬렉션(IEnumerable<T>) 등록의 중복으로 간주해
            // VContainerException("Conflict implementation type")을 던진다(Play Mode 실검증 중 확인).
            // 각 인스턴스를 자기 자신으로 Keyed()해 서로 다른 키를 부여함으로써 이 충돌을 피한다 - 어차피 이
            // 스코프에서는 아무도 NpcCombatActorVisualizer를 타입으로 Resolve하지 않고, 강제 주입 실행만 필요하다.
            foreach (var visualizer in npcCombatActorVisualizers)
            {
                if (visualizer != null)
                {
                    builder.RegisterComponent(visualizer).Keyed(visualizer);
                }
            }
        }
    }
}
