using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

namespace GameEvent
{
    /// <summary>
    /// 범용 이벤트 실행기([L] 유니티 생명주기 연동 로직 - ITickable/IDisposable)입니다. 어떤 이벤트 SO(PureDataEvent)가 들어와도 같은 방식으로
    /// "트리거 감시 -> onStart 동작 -> 완료 판정 -> onSuccess/onFail 동작 -> 활성 이벤트 목록 갱신" 흐름을 실행하며, 특정 이벤트/트리거/완료/동작을 알지 못합니다.
    /// 트리거/완료/동작의 구체 처리는 SO 타입별로 등록된 평가기/핸들러(IEventTriggerEvaluator/IEventCompletionEvaluator/IEventActionHandler)가 하므로
    /// 이벤트 종류가 늘어도 이 클래스는 바뀌지 않습니다(25번 기획서).
    ///
    /// 등록/시작 시점: 이벤트 SO 목록(PureDataEventSet)은 생성 시점(씬 시작)에 RuntimeDataEvent에 등록(Locked)됩니다. 평가기/핸들러는 소유 시스템의
    /// LifetimeScope가 Start()에서 사후 등록하므로, 이벤트 시작(선행 조건이 없는 이벤트를 Armed로 전환)은 모든 MonoBehaviour.Start()가 끝난 뒤인
    /// 첫 Tick()에서 한 번만 합니다(Tick은 그 한 번 외에는 아무 일도 하지 않습니다).
    /// 선행 이벤트(requiresCompleted)가 모두 Completed가 되면 Locked 이벤트가 Armed로 갱신되고, rearmOnFail인 이벤트는 실패 후 다시 Armed가 됩니다
    /// (조건 분기 이벤트가 마무리되면 활성 이벤트 목록을 갱신 - 사용자 확정 25번 §7-4).
    /// </summary>
    public class EventLogicSystem : IEventSystem, ITickable, IDisposable
    {
        private readonly RuntimeDataEvent runtimeData;
        private readonly IEventTargetProvider targets;

        private readonly List<PureDataEvent> events = new List<PureDataEvent>();
        private readonly Dictionary<string, EventContext> contexts = new Dictionary<string, EventContext>();
        private readonly Dictionary<Type, IEventTriggerEvaluator> triggerEvaluators = new Dictionary<Type, IEventTriggerEvaluator>();
        private readonly Dictionary<Type, IEventCompletionEvaluator> completionEvaluators = new Dictionary<Type, IEventCompletionEvaluator>();
        private readonly Dictionary<Type, IEventActionHandler> actionHandlers = new Dictionary<Type, IEventActionHandler>();
        private readonly Dictionary<string, IDisposable> triggerHandles = new Dictionary<string, IDisposable>();
        private readonly Dictionary<string, IDisposable> completionHandles = new Dictionary<string, IDisposable>();

        private bool started;

        public event Action<string> OnEventStarted;
        public event Action<string, EventResult> OnEventCompleted;

        public EventLogicSystem(
            PureDataEventSet eventSet,
            RuntimeDataEvent runtimeData,
            IEventTargetProvider targets,
            IEnumerable<IEventTriggerEvaluator> triggerEvaluators,
            IEnumerable<IEventCompletionEvaluator> completionEvaluators)
        {
            this.runtimeData = runtimeData ?? throw new ArgumentNullException(nameof(runtimeData));
            this.targets = targets ?? throw new ArgumentNullException(nameof(targets));

            foreach (var evaluator in triggerEvaluators) RegisterTriggerEvaluator(evaluator);
            foreach (var evaluator in completionEvaluators) RegisterCompletionEvaluator(evaluator);

            if (eventSet == null) return;
            foreach (var definition in eventSet.Events) RegisterEvent(definition);
        }

        public EventState GetState(string eventId) => runtimeData.GetState(eventId);

        public EventResult GetResult(string eventId) => runtimeData.GetResult(eventId);

        public void RegisterActionHandler(IEventActionHandler handler)
        {
            if (handler == null) return;

            if (!actionHandlers.TryAdd(handler.ActionType, handler))
            {
                Debug.LogWarning($"[EventLogicSystem] 동작 '{handler.ActionType.Name}'의 핸들러가 이미 있어 새 핸들러로 교체합니다.");
                actionHandlers[handler.ActionType] = handler;
            }
        }

        public void RegisterTriggerEvaluator(IEventTriggerEvaluator evaluator)
        {
            if (evaluator != null) triggerEvaluators[evaluator.TriggerType] = evaluator;
        }

        public void RegisterCompletionEvaluator(IEventCompletionEvaluator evaluator)
        {
            if (evaluator != null) completionEvaluators[evaluator.CompletionType] = evaluator;
        }

        /// <summary>첫 Tick에서만 이벤트를 시작합니다(평가기/핸들러 사후 등록이 끝난 뒤). 이후 Tick은 아무 일도 하지 않습니다.</summary>
        public void Tick()
        {
            if (started) return;

            started = true;
            RefreshActiveEvents();
        }

        public void Dispose()
        {
            foreach (var handle in triggerHandles.Values) handle?.Dispose();
            foreach (var handle in completionHandles.Values) handle?.Dispose();
            triggerHandles.Clear();
            completionHandles.Clear();
        }

        private void RegisterEvent(PureDataEvent definition)
        {
            if (definition == null) return;

            if (!runtimeData.Register(definition.EventId))
            {
                Debug.LogError($"[EventLogicSystem] 이벤트 id '{definition.EventId}'가 비어 있거나 중복되어 등록하지 않았습니다: {definition.name}");
                return;
            }

            if (definition.Trigger == null)
            {
                Debug.LogError($"[EventLogicSystem] 이벤트 '{definition.EventId}'에 시작 조건(trigger)이 없어 시작되지 않습니다.");
            }

            events.Add(definition);
            contexts.Add(definition.EventId, new EventContext(definition, targets));
        }

        /// <summary>Locked 이벤트 중 선행 이벤트가 모두 완료된 것을 Armed로 갱신합니다(활성 이벤트 목록 갱신).</summary>
        private void RefreshActiveEvents()
        {
            for (int i = 0; i < events.Count; i++)
            {
                var definition = events[i];
                if (runtimeData.GetState(definition.EventId) != EventState.Locked) continue;
                if (definition.Trigger == null || !PrerequisitesCompleted(definition)) continue;

                Arm(definition);
            }
        }

        private bool PrerequisitesCompleted(PureDataEvent definition)
        {
            foreach (var requiredId in definition.RequiresCompleted)
            {
                if (!runtimeData.IsRegistered(requiredId) || runtimeData.GetState(requiredId) != EventState.Completed) return false;
            }

            return true;
        }

        private void Arm(PureDataEvent definition)
        {
            string id = definition.EventId;
            runtimeData.ChangeState(id, EventState.Armed);

            if (!triggerEvaluators.TryGetValue(definition.Trigger.GetType(), out var evaluator))
            {
                Debug.LogError($"[EventLogicSystem] 이벤트 '{id}': 시작 조건 '{definition.Trigger.GetType().Name}'의 평가기가 등록되어 있지 않습니다.");
                return;
            }

            var handle = evaluator.Watch(definition.Trigger, contexts[id], () => HandleTriggered(definition));
            // Watch 안에서 곧바로 충족되어 이미 시작된 경우에는 감시 핸들을 보관하지 않고 해제한다.
            if (runtimeData.GetState(id) == EventState.Armed) triggerHandles[id] = handle;
            else handle?.Dispose();
        }

        private void HandleTriggered(PureDataEvent definition)
        {
            string id = definition.EventId;
            if (runtimeData.GetState(id) != EventState.Armed) return;

            ReleaseHandle(triggerHandles, id);
            StartEvent(definition);
        }

        private void StartEvent(PureDataEvent definition)
        {
            string id = definition.EventId;
            runtimeData.ChangeState(id, EventState.Running);
            Debug.Log($"[EventLogicSystem] 이벤트 '{id}' 시작 ({definition.Intensity})");

            ExecuteActions(definition.OnStart, contexts[id]);
            OnEventStarted?.Invoke(id);

            if (definition.Completion == null)
            {
                Complete(definition, EventResult.Success);
                return;
            }

            if (!completionEvaluators.TryGetValue(definition.Completion.GetType(), out var evaluator))
            {
                Debug.LogError($"[EventLogicSystem] 이벤트 '{id}': 완료 조건 '{definition.Completion.GetType().Name}'의 평가기가 등록되어 있지 않습니다.");
                return;
            }

            var handle = evaluator.Watch(definition.Completion, contexts[id], result => Complete(definition, result));
            // Watch 안에서 곧바로 판정이 나 이미 완료된 경우에는 감시 핸들을 보관하지 않고 해제한다.
            if (runtimeData.GetState(id) == EventState.Running) completionHandles[id] = handle;
            else handle?.Dispose();
        }

        private void Complete(PureDataEvent definition, EventResult result)
        {
            string id = definition.EventId;
            if (runtimeData.GetState(id) != EventState.Running) return;

            ReleaseHandle(completionHandles, id);
            runtimeData.SetResult(id, result);
            runtimeData.ChangeState(id, EventState.Completed);
            Debug.Log($"[EventLogicSystem] 이벤트 '{id}' 완료: {result}");

            ExecuteActions(result == EventResult.Success ? definition.OnSuccess : definition.OnFail, contexts[id]);
            OnEventCompleted?.Invoke(id, result);

            if (result == EventResult.Fail && definition.RearmOnFail && definition.Trigger != null) Arm(definition);

            RefreshActiveEvents();
        }

        private void ExecuteActions(EventActionSO[] actions, IEventContext context)
        {
            foreach (var action in actions)
            {
                if (action == null) continue;

                if (!actionHandlers.TryGetValue(action.GetType(), out var handler))
                {
                    Debug.LogError($"[EventLogicSystem] 이벤트 '{context.EventId}': 동작 '{action.GetType().Name}'의 핸들러가 등록되어 있지 않습니다({action.name}).");
                    continue;
                }

                handler.Execute(action, context);
            }
        }

        private static void ReleaseHandle(Dictionary<string, IDisposable> handles, string id)
        {
            if (!handles.Remove(id, out var handle)) return;

            handle?.Dispose();
        }

        private sealed class EventContext : IEventContext
        {
            private readonly IEventTargetProvider targets;

            public EventContext(PureDataEvent definition, IEventTargetProvider targets)
            {
                EventId = definition.EventId;
                Intensity = definition.Intensity;
                this.targets = targets;
            }

            public string EventId { get; }
            public EventIntensity Intensity { get; }

            public bool TryGetTarget(string targetId, out IEventTarget target) => targets.TryGetTarget(targetId, out target);
        }
    }
}
