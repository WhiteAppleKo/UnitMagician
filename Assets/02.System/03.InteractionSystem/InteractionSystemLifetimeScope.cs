using UnityEngine;
using VContainer;
using VContainer.Unity;
using InteractionSystem.Logic;
using PipeLine.Combat;
using PipeLine.UnitMagic;

namespace InteractionSystem
{
    public class InteractionSystemLifetimeScope : LifetimeScope
    {
        [Header("PipeLine Assets")]
        [SerializeField] private UnitMagicPipeLine unitMagicPipeLine;

        protected override void Configure(IContainerBuilder builder)
        {
            if (unitMagicPipeLine != null) builder.RegisterInstance(unitMagicPipeLine);

            // 진영 레이어 충돌 매트릭스(같은 편 투사체-몸체 충돌 제거)를 시작 시 한 번 설정한다(16-B). 규칙의 출처는 FactionRelation이며
            // 프로젝트 설정의 충돌 매트릭스는 손으로 바꾸지 않는다. 전투 상호작용(투사체/피격)의 시작 지점이 이 스코프라 여기서 한 줄로 호출한다.
            FactionLayers.ConfigureCollisionMatrix();

            // CombatPipelineManager는 플래그->스텝 배열 캐시를 세션 내내 유지해야 하므로 Singleton으로 등록합니다.
            builder.Register<CombatPipelineManager>(Lifetime.Singleton).As<ICombatPipelineManager>().AsSelf();

            // [13-B 선행 수정] Scoped로 등록하면 이 스코프를 부모로 둔 자식 스코프(시퀀서/Npc/...)마다 별도 인스턴스가
            // 생성되어, 투사체(CollisionDamageTrigger)가 발행하는 OnDamageProcessed가 시퀀서가 구독 중인 인스턴스에
            // 전달되지 않는 결함이 있었다. InteractionSystem은 ICombatPipelineManager(Singleton)/UnitMagicPipeLine(인스턴스)만
            // 보유하는 상태 없는 서비스이고 이벤트는 "누구에게나 브로드캐스트"가 의도이므로 Singleton으로 단일화한다.
            builder.Register<Logic.InteractionSystem>(Lifetime.Singleton).As<IInteractionService>().AsSelf();

            // [21] 중립 NPC 적대화 규칙: OnDamageProcessed를 구독해 중립 피격자의 연속 피격을 센다. 진입점(IInitializable/IDisposable)으로
            // 등록해 스코프 시작 시 구독하고 스코프 해제 시 구독을 끊는다.
            builder.RegisterEntryPoint<NeutralHostilityLogicSystem>(Lifetime.Singleton);
        }
    }
}
