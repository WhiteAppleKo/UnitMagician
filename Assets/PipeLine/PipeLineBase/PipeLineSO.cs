using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace PipeLine.PipeLineBase
{
    public interface IPipeLine<T>
    {
        void Enqueue(IPipeLineStep<T> step, int priority);
        UniTask<T> Run(T context);
    }

    /// <summary>
    /// 모든 파이프라인 에셋의 최상위 비제네릭 베이스 클래스입니다. (에디터 타겟용)
    /// </summary>
    public abstract class PipeLineSoBase : ScriptableObject { }

    public abstract class PipeLineSo<T> : PipeLineSoBase, IPipeLine<T>
    {
        [SerializeReference]
        protected List<IPipeLineStep<T>> steps = new List<IPipeLineStep<T>>();

        public void Enqueue(IPipeLineStep<T> step, int priority)
        {
            throw new System.NotImplementedException();
        }

        public async UniTask<T> Run(T context)
        {
            if (steps == null) return context;

            foreach (var step in steps)
            {
                context = await step.Execute(context);

                // 각 단계 실행 후 중단 조건 체크
                if (ShouldBreak(context))
                {
                    Debug.Log($"[{name}] Pipeline broken at step: {step.GetType().Name}");
                    break;
                }
            }

            return context;
        }

        /// <summary>
        /// 파이프라인을 중단할지 여부를 결정합니다. 하위 클래스에서 오버라이드하여 조건을 정의합니다.
        /// </summary>
        protected virtual bool ShouldBreak(T context) => false;
    }
}