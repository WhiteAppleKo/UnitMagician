using UnityEngine;

namespace AreaFreezeSystem
{
    /// <summary>
    /// 지형 오브젝트용 IFreezable 구현체([V] World Visualizer)입니다. 사화산 지형 메시(또는 테스트용 임의
    /// 오브젝트)에 부착해 광역 빙결 대상 목록에 포함시킵니다. Physics.OverlapSphere가 이 컴포넌트를 찾으려면
    /// 같은 GameObject에 Collider가 있어야 합니다(씬 배치 시 주의).
    ///
    /// 실제 얼음 텍스처/셰이더 아트 리소스는 이번 범위 밖이므로(Goal 프롬프트 "제외" 절) 로그 기반
    /// 플레이스홀더로 대체합니다(5.4절 "프리징 웨이브" 확산 연출 위치).
    /// </summary>
    public class FreezableTerrainComponent : MonoBehaviour, IFreezable
    {
        public void Freeze()
        {
            Debug.Log($"<color=lightblue>[FreezableTerrainComponent] '{name}' 얼음 텍스처 확산 (플레이스홀더)</color>");
        }
    }
}
