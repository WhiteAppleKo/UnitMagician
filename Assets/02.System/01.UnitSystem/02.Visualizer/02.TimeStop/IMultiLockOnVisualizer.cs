using System.Collections.Generic;
using UnityEngine;

namespace UnitSystem
{
    /// <summary>
    /// 1인칭 및 3인칭 숄더뷰 다중 락온 조준선 및 락온 카운트 UI/비주얼 추상 인터페이스입니다.
    /// </summary>
    public interface IMultiLockOnVisualizer
    {
        void SetCrosshairVisible(bool visible);
        void UpdateLockOnCount(int count);
        void PlayBatchCastEffect();
        IReadOnlyList<Collider> DetectAimTargets(float radius, float maxDistance);
        IReadOnlyList<Collider> DetectAimTargets(float radius, float maxDistance, LayerMask mask);
    }
}

