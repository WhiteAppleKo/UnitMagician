using UnityEngine;

namespace NpcSystem
{
    /// <summary>
    /// 아군 투사체 풀링(15-B)의 불변 설정값([D] Pure Data)입니다. 풀 크기와 "빗나감 반환" 기준(최대 거리/최대 수명)을 조정 가능한 값으로 둡니다.
    /// 최종 수치/밸런스는 범위 밖이며 기본값은 동작 확인용입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "PureDataNpcProjectile_", menuName = "NpcSystem/PureDataNpcProjectile")]
    public class PureDataNpcProjectile : ScriptableObject
    {
        [Header("Pool Size")]
        [Tooltip("풀 초기 용량(UnityEngine.Pool.ObjectPool defaultCapacity).")]
        [SerializeField] private int defaultPoolSize = 8;
        [Tooltip("풀에 보관되는 비활성 항목의 상한. 이를 넘겨 반환된 투사체는 파괴된다.")]
        [SerializeField] private int maxPoolSize = 16;

        [Header("Miss Return (충돌하지 못한 투사체의 반환 기준)")]
        [Tooltip("발사 지점으로부터 이 거리(미터)를 넘으면 빗나간 것으로 보고 풀로 반환한다. 화면 밖은 카메라마다 달라 거리 기준을 쓴다.")]
        [SerializeField] private float maxDistanceFromLaunch = 80f;
        [Tooltip("발사 후 이 시간(초, 스케일된 게임 시간)이 지나도 충돌하지 못하면 풀로 반환한다.")]
        [SerializeField] private float maxLifetimeSeconds = 15f;

        public int DefaultPoolSize => defaultPoolSize;
        public int MaxPoolSize => maxPoolSize;
        public float MaxDistanceFromLaunch => maxDistanceFromLaunch;
        public float MaxLifetimeSeconds => maxLifetimeSeconds;
    }
}
