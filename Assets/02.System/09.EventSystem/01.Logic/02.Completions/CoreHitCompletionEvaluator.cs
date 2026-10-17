using System;
using UnityEngine;
using InteractionSystem.Logic;
using PipeLine.Contexts;

namespace GameEvent
{
    /// <summary>
    /// CoreHitCompletion 평가기입니다. 대상(무적 상태의 IEventHealthTarget)이 맞은 피격(IInteractionService.OnDamageProcessed) 중 이벤트 투사체의 피격만 판정합니다.
    /// 성공 = (속성 게이트 요구 시 통과) AND (크기 게이트 요구 시 통과) AND FinalDamage &gt;= requiredDamage, 그 외(속성 실패 / 크기 부족 / 데미지 미달 / 마법 없음)는 모두 실패이며
    /// 실패한 게이트는 한 줄 로그로 남깁니다. 투사체가 명중 없이 소실되면(IEventFlightLogic.OnProjectileLost) 소프트락 방지 안전망으로 실패 처리합니다.
    /// 판정이 나면 비행 추적을 끝(IEventFlightLogic.End)낸 뒤 결과를 통지합니다. 판정 수치는 전부 CoreHitCompletion SO에 있고 이 클래스에는 숫자가 없습니다.
    /// </summary>
    public class CoreHitCompletionEvaluator : IEventCompletionEvaluator
    {
        private readonly IInteractionService interactionService;
        private readonly IEventFlightLogic flightLogic;

        public CoreHitCompletionEvaluator(IInteractionService interactionService, IEventFlightLogic flightLogic)
        {
            this.interactionService = interactionService ?? throw new ArgumentNullException(nameof(interactionService));
            this.flightLogic = flightLogic ?? throw new ArgumentNullException(nameof(flightLogic));
        }

        public Type CompletionType => typeof(CoreHitCompletion);

        public IDisposable Watch(EventCompletionSO completion, IEventContext context, Action<EventResult> onCompleted)
        {
            var spec = (CoreHitCompletion)completion;
            if (!context.TryGetTarget(spec.TargetId, out var target) || !(target is IEventHealthTarget healthTarget))
            {
                Debug.LogError($"[CoreHitCompletionEvaluator] 이벤트 '{context.EventId}': 체력 대상 '{spec.TargetId}'를 찾을 수 없어 완료 판정을 할 수 없습니다.");
                return null;
            }

            return new HitSubscription(spec, healthTarget, interactionService, flightLogic, onCompleted);
        }

        private sealed class HitSubscription : IDisposable
        {
            private readonly CoreHitCompletion spec;
            private readonly IEventHealthTarget target;
            private readonly IInteractionService interactionService;
            private readonly IEventFlightLogic flightLogic;
            private readonly Action<EventResult> onCompleted;
            private bool finished;

            public HitSubscription(CoreHitCompletion spec, IEventHealthTarget target, IInteractionService interactionService, IEventFlightLogic flightLogic, Action<EventResult> onCompleted)
            {
                this.spec = spec;
                this.target = target;
                this.interactionService = interactionService;
                this.flightLogic = flightLogic;
                this.onCompleted = onCompleted;

                interactionService.OnDamageProcessed += HandleDamageProcessed;
                flightLogic.OnProjectileLost += HandleProjectileLost;
            }

            public void Dispose()
            {
                interactionService.OnDamageProcessed -= HandleDamageProcessed;
                flightLogic.OnProjectileLost -= HandleProjectileLost;
            }

            /// <summary>
            /// 판정 대상은 이벤트 투사체가 대상을 맞힌 피격으로 한정합니다(13번 기획서). 1단계 타이머 샷, 직전 사이클의 재도전 샷, 플레이어 일반 공격 등
            /// 이벤트 투사체가 아닌 피격은(대상 HP는 정상 감소하지만) 판정하지 않고 무시합니다. 대상이 무적(회복 페이즈) 상태인 동안만 판정합니다.
            /// </summary>
            private void HandleDamageProcessed(DamageContext context)
            {
                if (finished) return;
                if (target.TargetObject == null || context.Victim != target.TargetObject) return;
                if (!target.IsInvincible) return;
                if (!flightLogic.IsEventProjectile(context.Source)) return;

                bool attributePassed = !spec.RequireAttributeGate || (context.AttributeGatePassed && !context.Aborted);
                bool sizePassed = !spec.RequireSizeGate || context.SizeGatePassed;
                bool damagePassed = context.FinalDamage >= spec.RequiredDamage;

                if (attributePassed && sizePassed && damagePassed)
                {
                    Debug.Log($"[CoreHitCompletionEvaluator] Core hit judgement SUCCESS - attribute/size/damage gates all passed (FinalDamage: {context.FinalDamage})");
                    Finish(EventResult.Success);
                }
                else
                {
                    Debug.Log($"[CoreHitCompletionEvaluator] Core hit judgement FAIL - failed gate(s): {DescribeFailedGates(attributePassed, sizePassed, damagePassed)} (FinalDamage: {context.FinalDamage})");
                    Finish(EventResult.Fail);
                }
            }

            /// <summary>이벤트 투사체가 명중 판정 없이 소실된 경우의 소프트락 안전망(시간 기반 타임아웃이 아니라 투사체 소실 감지)입니다.</summary>
            private void HandleProjectileLost()
            {
                if (finished) return;

                Debug.Log("[CoreHitCompletionEvaluator] Core hit judgement FAIL - 이벤트 투사체가 명중 없이 소실됨");
                Finish(EventResult.Fail);
            }

            private void Finish(EventResult result)
            {
                finished = true;
                flightLogic.End();
                onCompleted(result);
            }

            private static string DescribeFailedGates(bool attributePassed, bool sizePassed, bool damagePassed)
            {
                // 속성 게이트 실패(Aborted)면 이후 크기 게이트는 실행되지 않으므로 속성 게이트만 실패 사유로 남긴다.
                if (!attributePassed) return "속성게이트(attribute)";

                string failed = string.Empty;
                if (!sizePassed) failed += "크기게이트(size) ";
                if (!damagePassed) failed += "데미지(damage) ";
                return failed.TrimEnd();
            }
        }
    }
}
