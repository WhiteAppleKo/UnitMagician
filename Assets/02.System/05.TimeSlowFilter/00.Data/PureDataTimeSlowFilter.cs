using UnityEngine;

namespace TimeSlowFilterSystem
{
    [CreateAssetMenu(fileName = "PureDataTimeSlowFilter", menuName = "UnitMagician/TimeSlowFilter/PureDataTimeSlowFilter")]
    public class PureDataTimeSlowFilter : ScriptableObject
    {
        [Header("Filter Transition Settings")]
        [Tooltip("흑백 필터 진입 및 해제 전환 시간 (초)")]
        [SerializeField] private float transitionDuration = 0.3f;

        [Tooltip("흑백 변환 시 적용할 대비 수치")]
        [SerializeField] private float contrast = 1.15f;

        [Header("Shader Property Settings")]
        [Tooltip("흑백 채도 감소 프로퍼티 이름")]
        [SerializeField] private string shaderPropertyName = "_DesaturateAmount";

        [Tooltip("대비 조정 프로퍼티 이름")]
        [SerializeField] private string contrastPropertyName = "_Contrast";

        public float TransitionDuration => transitionDuration;
        public float Contrast => contrast;
        public string ShaderPropertyName => shaderPropertyName;
        public string ContrastPropertyName => contrastPropertyName;
    }
}
