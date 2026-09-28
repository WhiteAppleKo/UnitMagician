using System;
using UnityEngine;
using VContainer.Unity;
using NpcSystem;
using EventSequencerSystem;

namespace AllySupportSystem
{
    /// <summary>
    /// 아군 NPC(INpcCombatActor)에게 "언제 무엇을 공격할지"만 지시하는 얇은 배선 로직입니다
    /// ([L] 유니티 생명주기 연동 로직 - ITickable/IDisposable). 실제 이동/발사/모션 재생은 전부 01번 프레임워크
    /// (INpcCombatActor 구현체)가 수행하며, 이 클래스는 그 실행 시점만 판단합니다(02번 기획서 2.1/2.8절,
    /// 코드_가이드라인_v1.0.md DLV Rule 2 - Blind Logic). MonoBehaviour/Transform/Collider 등 구체 타입을
    /// 직접 참조하지 않습니다 - 돌문 파괴 여부 조회도 DestructibleDoorComponent(MonoBehaviour)가 아니라
    /// IDestructibleTargetStatus 인터페이스로만 받습니다.
    ///
    /// 1단계: elapsedSinceLastFire가 attackIntervalSeconds(3~4초)에 도달할 때마다 stoneDoorTarget의 파괴 여부
    /// (IDestructibleTargetStatus.IsDestroyed)를 확인합니다 - false면 Attack 후 타이머 리셋, true면
    /// PlayMotion("Idle") 호출 후 타이머를 영구 정지합니다(02번 기획서 2.5/2.8절 "돌문 파괴 후 대기 모션 전환"이
    /// 최종 상태 - 이후 재개하지 않음).
    ///
    /// 3단계/재도전: 06(이벤트 시퀀서, IEventSequencer)이 서로 다른 VContainer 스코프 트리에 있어 생성자 주입이
    /// 불가능한 경우가 09/10번 작업에서 반복 확인되었으므로, CharacterLifetimeScope/LocomotionLifetimeScope와
    /// 동일한 패턴 - LifetimeScope가 두 스코프 Container가 모두 준비된 뒤(Start()) BindEventSequencer()를
    /// 사후 호출하는 방식을 그대로 재사용합니다(AllySupportSystemLifetimeScope.cs 참고).
    /// </summary>
    public class AllySupportTriggerLogicSystem : ITickable, IDisposable
    {
        private readonly INpcCombatActor ally;
        private readonly GameObject stoneDoorTarget;
        private readonly GameObject bossTarget;
        private readonly IDestructibleTargetStatus doorStatus;
        private readonly float attackIntervalSeconds;

        private float elapsedSinceLastFire;
        private bool doorTimerStopped;

        private IEventSequencer eventSequencer;

        public AllySupportTriggerLogicSystem(
            INpcCombatActor ally,
            GameObject stoneDoorTarget,
            GameObject bossTarget,
            IDestructibleTargetStatus doorStatus,
            float attackIntervalSeconds)
        {
            this.ally = ally ?? throw new ArgumentNullException(nameof(ally));
            this.stoneDoorTarget = stoneDoorTarget;
            this.bossTarget = bossTarget;
            this.doorStatus = doorStatus ?? throw new ArgumentNullException(nameof(doorStatus));
            this.attackIntervalSeconds = attackIntervalSeconds;
        }

        /// <summary>
        /// 06(이벤트 시퀀서) 연동 사후 배선입니다. AllySupportSystemLifetimeScope가 자신과
        /// EventSequencerSystemLifetimeScope의 Container가 모두 빌드된 뒤(Start()) 한 번 호출합니다.
        /// 재호출 시 기존 구독을 먼저 해제해 중복 구독을 막습니다(TimeSlowLogicSystem.BindEventSequencer와 동일 패턴).
        /// </summary>
        public void BindEventSequencer(IEventSequencer sequencer)
        {
            UnbindEventSequencer();

            eventSequencer = sequencer;
            if (eventSequencer != null)
            {
                eventSequencer.OnBoss50Percent += HandleBoss50Percent;
                eventSequencer.OnCoreHitFail += HandleCoreHitFail;
            }
        }

        private void UnbindEventSequencer()
        {
            if (eventSequencer == null) return;

            eventSequencer.OnBoss50Percent -= HandleBoss50Percent;
            eventSequencer.OnCoreHitFail -= HandleCoreHitFail;
            eventSequencer = null;
        }

        public void Dispose()
        {
            UnbindEventSequencer();
        }

        /// <summary>
        /// 1단계 반복 타이머 판정입니다. 돌문이 파괴되지 않은 동안 attackIntervalSeconds 간격으로 Attack을
        /// 반복 호출하고, 파괴된 순간을 감지하면 PlayMotion("Idle") 호출 후 타이머를 영구 정지합니다
        /// (재개 없음 - 02번 기획서 2.5/2.8절).
        /// </summary>
        public void Tick()
        {
            if (doorTimerStopped) return;
            if (stoneDoorTarget == null) return;

            if (doorStatus.IsDestroyed)
            {
                ally.PlayMotion("Idle");
                doorTimerStopped = true;
                return;
            }

            elapsedSinceLastFire += Time.deltaTime;
            if (elapsedSinceLastFire >= attackIntervalSeconds)
            {
                ally.Attack(stoneDoorTarget);
                elapsedSinceLastFire = 0f;
            }
        }

        /// <summary>3단계: 06의 보스 HP 50% 도달 신호 수신 시 지원 사격 1회.</summary>
        private void HandleBoss50Percent()
        {
            ally.Attack(bossTarget);
        }

        /// <summary>4단계 실패(재도전) 신호 수신 시 즉시 재발사 - 재도전마다 반복될 수 있습니다.</summary>
        private void HandleCoreHitFail()
        {
            ally.Attack(bossTarget);
        }
    }
}
