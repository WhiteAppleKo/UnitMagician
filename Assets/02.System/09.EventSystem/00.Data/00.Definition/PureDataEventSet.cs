using System.Collections.Generic;
using UnityEngine;

namespace GameEvent
{
    /// <summary>
    /// 씬 시작 시 이벤트 시스템에 등록되는 이벤트 SO 목록입니다([D] Pure Data). 25번 기획서 §7-4: "이벤트 SO 등록은 씬 시작 시에 한다".
    /// 새 이벤트는 이 목록에 PureDataEvent 에셋을 추가하는 것만으로 동작하며 실행기 코드는 바뀌지 않습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "PureDataEventSet_", menuName = "GameEvent/PureDataEventSet")]
    public class PureDataEventSet : ScriptableObject
    {
        [SerializeField] private PureDataEvent[] events = new PureDataEvent[0];

        public IReadOnlyList<PureDataEvent> Events => events;
    }
}
