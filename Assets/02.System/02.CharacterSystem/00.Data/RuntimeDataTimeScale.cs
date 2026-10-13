using System;
using System.Collections.Generic;

namespace CharacterSystem
{
    /// <summary>
    /// Time.timeScale 합성 데이터(17번 기획서). 시간정지/이벤트 감속이 요청하는 배율(<see cref="RequestedScale"/>)과
    /// UI들의 일시정지 요청자 집합을 보관하고, 둘을 합성한 유효 배율(<see cref="EffectiveScale"/>)을 계산합니다.
    /// 일시정지 요청자는 집합(멱등)으로 관리하므로 같은 요청자가 여러 번 요청/해제해도 카운트가 어긋나지 않습니다.
    /// 실제 Time.timeScale 쓰기는 이 데이터를 구독하는 TimeScaleLogicSystem → TimeSlowVisualizer 경로에서만 일어납니다.
    /// </summary>
    public class RuntimeDataTimeScale
    {
        private readonly HashSet<object> pauseRequesters = new HashSet<object>();
        private float requestedScale = 1.0f;

        /// <summary>시간정지/이벤트 감속이 요청한 배율(기본 1).</summary>
        public float RequestedScale => requestedScale;
        public int PauseCount => pauseRequesters.Count;
        public bool IsPaused => pauseRequesters.Count > 0;
        /// <summary>일시정지 중이면 0, 아니면 요청 배율.</summary>
        public float EffectiveScale => IsPaused ? 0.0f : requestedScale;

        /// <summary>유효 배율이 실제로 바뀔 때만 발행됩니다.</summary>
        public event Action<float> OnEffectiveScaleChanged;
        /// <summary>일시정지 여부(IsPaused)가 바뀔 때만 발행됩니다.</summary>
        public event Action<bool> OnPauseStateChanged;

        public void SetRequestedScale(float scale)
        {
            if (scale < 0.0f) scale = 0.0f;
            if (requestedScale == scale) return;

            float previousEffective = EffectiveScale;
            requestedScale = scale;
            NotifyEffectiveScaleIfChanged(previousEffective);
        }

        /// <summary>요청자별 일시정지 요청을 올리거나 내립니다(멱등).</summary>
        public void SetPaused(object requester, bool paused)
        {
            if (requester == null) return;

            bool wasPaused = IsPaused;
            float previousEffective = EffectiveScale;

            bool changed = paused ? pauseRequesters.Add(requester) : pauseRequesters.Remove(requester);
            if (!changed) return;

            if (wasPaused != IsPaused)
            {
                OnPauseStateChanged?.Invoke(IsPaused);
            }

            NotifyEffectiveScaleIfChanged(previousEffective);
        }

        private void NotifyEffectiveScaleIfChanged(float previousEffective)
        {
            float current = EffectiveScale;
            if (previousEffective == current) return;
            OnEffectiveScaleChanged?.Invoke(current);
        }
    }
}
