using System;
using System.Collections.Generic;

namespace GameEvent
{
    /// <summary>
    /// 이벤트별 진행 상태([D] Runtime Data)입니다. 이벤트 id별로 상태(EventState)와 마지막 결과(EventResult)를 보관하고,
    /// private set + 전용 변경 메서드로만 바꾸며 값이 실제로 바뀔 때만 이벤트를 발행합니다(스스로 Tick을 돌지 않음).
    /// </summary>
    public class RuntimeDataEvent
    {
        private readonly Dictionary<string, EventState> states = new Dictionary<string, EventState>();
        private readonly Dictionary<string, EventResult> results = new Dictionary<string, EventResult>();

        /// <summary>이벤트의 상태가 실제로 바뀔 때만 발행됩니다. (eventId, 새 상태)</summary>
        public event Action<string, EventState> OnStateChanged;

        /// <summary>이벤트를 Locked 상태로 등록합니다. 이미 등록된 id이거나 비어 있으면 false(등록하지 않음).</summary>
        public bool Register(string eventId)
        {
            if (string.IsNullOrEmpty(eventId) || states.ContainsKey(eventId)) return false;

            states.Add(eventId, EventState.Locked);
            results.Add(eventId, EventResult.None);
            return true;
        }

        public bool IsRegistered(string eventId) => !string.IsNullOrEmpty(eventId) && states.ContainsKey(eventId);

        public EventState GetState(string eventId) => states[eventId];

        public EventResult GetResult(string eventId) => results[eventId];

        /// <summary>상태 전환 전용 메서드입니다. 같은 값이면 조기 반환하여 중복 발행을 막습니다.</summary>
        public void ChangeState(string eventId, EventState newState)
        {
            if (states[eventId] == newState) return;

            states[eventId] = newState;
            OnStateChanged?.Invoke(eventId, newState);
        }

        public void SetResult(string eventId, EventResult result)
        {
            results[eventId] = result;
        }
    }
}
