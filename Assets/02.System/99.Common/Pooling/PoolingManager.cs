using UnityEngine;
using UnityEngine.Pool; // 유니티 내장 풀 사용을 위해 필수

namespace Pooling
{
    public class PoolingManager<T, TData> : MonoBehaviour, IPoolService<T, TData> where T : MonoBehaviour, IPoolable<TData>
    {
        [SerializeField] private T m_PoolPrefab;
        [SerializeField] private int m_DefaultSize = 10;
        [SerializeField] private int m_MaxSize = 20;

        // 유니티 내장 풀 (CountAll/CountActive/CountInactive 통계 조회를 위해 구체 타입으로 보관)
        private ObjectPool<T> m_Pool;
        private TData m_Data;

        // 반드시 실제 세팅 데이터를 적절하게 만들어서 넣어야함. PoolSettingStruct 참조
        public PoolSettingData<TData> poolSettingData;

        /// <summary>풀 초기 용량. 파생 클래스가 PureData 등 다른 출처의 값을 쓰고 싶으면 재정의한다.</summary>
        protected virtual int DefaultSize => m_DefaultSize;

        /// <summary>풀에 보관되는 비활성 항목의 상한. 이를 넘겨 반환된 항목은 파괴된다. 파생 클래스가 재정의할 수 있다.</summary>
        protected virtual int MaxSize => m_MaxSize;

        // --- 풀 통계 (검증/디버그용) ---

        /// <summary>지금까지 생성된 항목 수(파괴된 것 제외 여부는 UnityEngine.Pool.ObjectPool 정의를 따른다).</summary>
        public int CountAll => m_Pool != null ? m_Pool.CountAll : 0;

        /// <summary>현재 풀 밖(사용 중)에 있는 항목 수.</summary>
        public int CountActive => m_Pool != null ? m_Pool.CountActive : 0;

        /// <summary>현재 풀 안(비활성 대기)에 있는 항목 수.</summary>
        public int CountInactive => m_Pool != null ? m_Pool.CountInactive : 0;

        // 풀 생성은 첫 사용 시점까지 미룬다. 파생 클래스가 VContainer 주입으로 받은 설정(DefaultSize/MaxSize)을 쓸 수 있도록,
        // Awake 순서(주입 전/후 불확실)에 의존하지 않기 위해서다.
        private ObjectPool<T> EnsurePool()
        {
            return m_Pool ??= new ObjectPool<T>(
                createFunc: CreatePoolItem,       // (1) 부족할 때 생성하는 로직
                actionOnGet: GetPooledItem,      // (2) 풀에서 꺼낼 때 로직
                actionOnRelease: ReturnedToPool,// (3) 풀에 반납할 때 로직
                actionOnDestroy: DestroyPoolItem, // (4) maxSize 초과 시 파괴 로직
                collectionCheck: true,            // 중복 반납 방지 체크
                defaultCapacity: DefaultSize,
                maxSize: MaxSize
            );
        }

        // (1) 생성 로직 Get했을 때 부족해도 생성됨. 새 항목은 비활성 상태로 시작한다(활성화는 Get의 Setting -> SetActive 단계에서만).
        // 파생 클래스는 base 호출 뒤 DI 주입 등 생성 직후 처리를 덧붙일 수 있다.
        protected virtual T CreatePoolItem()
        {
            T obj = Instantiate(m_PoolPrefab, transform);
            obj.gameObject.SetActive(false);
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
            return EnsurePool().Get();
        }

        public T Get()
        {
            if (poolSettingData != null)
            {
                m_Data = poolSettingData.ReCalculateRuntimeData();
            }
            return EnsurePool().Get();
        }

        public void Release(T obj)
        {
            EnsurePool().Release(obj);
        }
    }
}
