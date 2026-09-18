using VContainer;
using VContainer.Unity;

namespace NpcSystem
{
    /// <summary>
    /// NpcSystem(07.NpcSystem)의 VContainer LifetimeScope입니다.
    /// NpcCombatActorComponent 자체는 다른 서비스에 의존하지 않지만, Attack 실행 시
    /// 런타임 스폰 오브젝트(투사체)에 IObjectResolver.InjectGameObject를 명시적으로 호출해야 하므로
    /// (autoInjectGameObjects는 씬에 미리 배치된 오브젝트에만 적용되고 Instantiate된 오브젝트에는 적용되지 않는다)
    /// NpcCombatActorComponent가 [Inject] Construct(IObjectResolver)를 통해 컨테이너의 리졸버를 주입받도록
    /// 씬 하이어라키의 모든 인스턴스를 여기서 등록한다.
    /// ProjectileAttackBehaviorSO.Execute -> actor.SpawnAndInject -> resolver.InjectGameObject(projectile) 경로에서
    /// 투사체의 CollisionDamageTrigger.Construct(IInteractionService)가 실제로 해석되려면, 이 스코프의 컨테이너가
    /// IInteractionService/ICombatPipelineManager를 등록한 InteractionSystemLifetimeScope의 자식 컨테이너여야 한다
    /// (VContainer는 자식 스코프가 부모 스코프의 등록을 상속하지만, 형제 스코프끼리는 서로의 등록을 보지 못한다).
    /// 그래서 부모 스코프를 InteractionSystemLifetimeScope로 명시적으로 지정한다.
    /// </summary>
    public class NpcSystemLifetimeScope : LifetimeScope
    {
        protected override void Awake()
        {
            parentReference = ParentReference.Create<InteractionSystem.InteractionSystemLifetimeScope>();
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<NpcCombatActorComponent>();
        }
    }
}
