using UnityEngine;

namespace GameEvent
{
    /// <summary>
    /// 플레이어 입력 락(Event 입력 컨텍스트)을 걸거나 푸는 동작입니다. 걸려 있는 동안 카메라 회전/줌 같은 시선 조작이 차단됩니다.
    /// release = true이면 락을 풉니다. 이 동작은 값이 없습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "InputLockAction_", menuName = "GameEvent/Action/InputLock")]
    public class InputLockAction : EventActionSO
    {
        [Tooltip("켜면 이 동작은 입력 락을 풉니다.")]
        [SerializeField] private bool release;

        public bool Release => release;
    }
}
