using UnityEngine;
using UnitSystem;

namespace PickupSystem
{
    /// <summary>
    /// 씬에 배치되는 바닥 아이템입니다. 모든 아이템이 같은 외형(프리팹 하나)이고, 인스펙터의 GrantedUnit만 다릅니다.
    /// PickupDetectorComponent의 Physics.OverlapSphere가 찾을 수 있도록 같은 오브젝트에 Collider(Trigger)가 있어야 합니다.
    /// Consume()은 단순 비활성화입니다(리스폰/저장은 범위 밖).
    /// </summary>
    public class PickupItemComponent : MonoBehaviour, IPickupTarget
    {
        [Tooltip("이 아이템을 주우면 습득하는 마법(PureDataUnit). 줍기만으로는 자동 장착되지 않습니다.")]
        [SerializeField] private PureDataUnit grantedUnit;

        public PureDataUnit GrantedUnit => grantedUnit;

        public Vector3 Position => transform.position;

        public void Consume()
        {
            gameObject.SetActive(false);
        }
    }
}
