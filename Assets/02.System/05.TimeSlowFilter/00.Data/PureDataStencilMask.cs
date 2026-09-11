using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TimeSlowFilterSystem
{
    [CreateAssetMenu(fileName = "PureDataStencilMask", menuName = "UnitMagician/TimeSlowFilter/PureDataStencilMask")]
    public class PureDataStencilMask : ScriptableObject
    {
        [Header("Stencil Settings")]
        [Tooltip("스텐실 버퍼에 기록할 기준 번호")]
        [SerializeField] private int stencilRef = 1;

        [Header("Render Pass Settings")]
        [Tooltip("스텐실 패스 실행 렌더링 이벤트 시점")]
        [SerializeField] private RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

        [Header("Material Settings")]
        [Tooltip("스텐실 도장 전용 머티리얼")]
        [SerializeField] private Material maskMaterial;

        public int StencilRef => stencilRef;
        public RenderPassEvent PassEvent => renderPassEvent;
        public Material MaskMaterial => maskMaterial;
    }
}
