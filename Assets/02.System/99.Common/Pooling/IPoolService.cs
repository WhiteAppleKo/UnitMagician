namespace Pooling
{
    public interface IPoolService<T, TData>
    {
        T Get(TData data);
        void Release(T item);
    }
}
