using UnityEngine;

namespace AreaFreezeSystem
{
    /// <summary>
    /// AreaFreezeVisualizer의 불변 설정값([D] Pure Data)입니다. 05번 기획서 5.8/5.9절 - freezeRadius(보스 위치를
    /// 중심으로 IFreezable 대상을 수집하는 반경)와 freezeSequenceDelay(수집된 대상 사이의 순차 Freeze() 호출
    /// 간격, 5.4절 "순차적으로 덮이는" 프리징 웨이브 연출의 근거값)만 노출합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "PureDataAreaFreeze_", menuName = "AreaFreezeSystem/PureDataAreaFreeze")]
    public class PureDataAreaFreeze : ScriptableObject
    {
        [Header("Area Freeze")]
        [Tooltip("보스 위치를 중심으로 IFreezable 대상을 수집하는 반경(미터). Physics.OverlapSphere 반경으로 " +
                 "사용됩니다(5.8절).")]
        [SerializeField] private float freezeRadius = 8.0f;

        [Tooltip("수집된 대상들 사이의 순차 Freeze() 호출 간격(초). 5.4절 '순차적으로 덮이는' 프리징 웨이브 " +
                 "연출의 근거값 - 0 이하이면 지연 없이 즉시 다음 대상으로 넘어갑니다.")]
        [SerializeField] private float freezeSequenceDelay = 0.3f;

        public float FreezeRadius => freezeRadius;
        public float FreezeSequenceDelay => freezeSequenceDelay;
    }
}
