namespace Pooling
{
    public interface IPoolable<TData>
    {
        void Setting(TData data);
        void SetActive();
        void ReturnToPool();
    }
}