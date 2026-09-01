using System.Collections.Generic;
using UnityEngine;

namespace Pooling
{
    public struct PoolSettingStruct
    {
        
    }
    // 추상 클래스로 베이스를 잡습니다.
    public abstract class PoolSettingData<TData> : ScriptableObject
    {
        public TData pureData; // 원본 데이터
        protected TData runtimeData; // 계산된 결과값

        // 말씀하신 재계산 로직
        public abstract TData ReCalculateRuntimeData(); 
    }

    // 실제 파일로 만들 수 있는 구체적인 클래스
    // 반드시 사용할 풀 시스템에 맞는 세팅 클래스를 만들어야함
    [CreateAssetMenu(fileName = "PoolSettingData", menuName = "Pooling/PoolSettingData")]
    public class PoolSetting : PoolSettingData<PoolSettingStruct>
    {
        public override PoolSettingStruct ReCalculateRuntimeData()
        {
            // 여기서 Pure를 가공해 Runtime을 만듭니다.
            return runtimeData;
        }
    }
}