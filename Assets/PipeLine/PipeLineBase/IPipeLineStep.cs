using Cysharp.Threading.Tasks;

namespace PipeLine.PipeLineBase
{
    public interface IPipeLineStep<T>
    {
        UniTask<T> Execute(T context);
    }
}

namespace PipeLine
{
    public interface IPipeLineStep<T> : PipeLineBase.IPipeLineStep<T> { }
}

