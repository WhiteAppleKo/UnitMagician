using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization; // 유니티 내장 풀 사용을 위해 필수

namespace Pooling
{
    public class PoolingManager<T, TData> : MonoBehaviour, IPoolService<T, TData> where T : MonoBehaviour, IPoolable<TData>
    {
        [SerializeField] private T m_PoolPrefab;
        [SerializeField] private int m_DefaultSize = 10;
        [SerializeField] private int m_MaxSize = 20;
        
        // 유니티 내장 풀 인터페이스
        private IObjectPool<T> m_Pool;
        private TData m_Data;
        
        // 반드시 실제 세팅 데이터를 적절하게 만들어서 넣어야함. PoolSettingStruct 참조
        public PoolSettingData<TData> poolSettingData;
        private void Awake()
        {
            // ObjectPool 초기화
            m_Pool = new ObjectPool<T>(
                createFunc: CreatePoolItem,       // (1) 부족할 때 생성하는 로직
                actionOnGet: GetPooledItem,      // (2) 풀에서 꺼낼 때 로직
                actionOnRelease: ReturnedToPool,// (3) 풀에 반납할 때 로직
                actionOnDestroy: DestroyPoolItem, // (4) maxSize 초과 시 파괴 로직
                collectionCheck: true,            // 중복 반납 방지 체크
                defaultCapacity: m_DefaultSize,
                maxSize: m_MaxSize
            );
        }

        // (1) 생성 로직 Get했을 때 부족해도 생성됨
        private T CreatePoolItem()
        {
            T obj = Instantiate(m_PoolPrefab, transform);
            return obj;
        }

        // (2) 꺼낼 때 (Get)
        private void GetPooledItem(T obj)
        {
            obj.Setting(m_Data);
            obj.SetActive();
        }

        // (3) 반납할 때 (Release)
        private void ReturnedToPool(T obj)
        {
            obj.ReturnToPool();
        }

        // (4) 파괴 (풀이 꽉 찼는데 반납될 경우 메모리 해제)
        private void DestroyPoolItem(T obj)
        {
            Destroy(obj.gameObject);
        }

        // --- 외부 접근용 메서드 ---

        public T Get(TData data)
        {
            m_Data = data;
            return m_Pool.Get();
        }

        public T Get()
        {
            if (poolSettingData != null)
            {
                m_Data = poolSettingData.ReCalculateRuntimeData();
            }
            return m_Pool.Get();
        }

        public void Release(T obj)
        {
            m_Pool.Release(obj);
        }
    }
}