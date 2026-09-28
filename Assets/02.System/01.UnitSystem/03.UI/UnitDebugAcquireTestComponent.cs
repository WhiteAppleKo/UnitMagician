using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace UnitSystem
{
    /// <summary>
    /// [테스트 전용] 실제 게임 기능이 아닙니다.
    /// 필드 아이템 픽업 등 정식 습득 트리거가 아직 구현되지 않아 인벤토리/장착 시스템을
    /// 눈으로 확인할 수 없는 문제를 임시로 해결하기 위한 디버그 컴포넌트입니다.
    /// 숫자키 1~4를 누르면 인스펙터에 지정된 <see cref="PureDataUnit"/>을
    /// <see cref="IUnitCatalogService.AcquireUnit"/>으로 즉시 습득시킵니다.
    /// </summary>
    public class UnitDebugAcquireTestComponent : MonoBehaviour
    {
        [SerializeField] private PureDataUnit[] testUnits = new PureDataUnit[4];

        private IUnitCatalogService catalogService;

        [Inject]
        public void Construct(IUnitCatalogService catalogService)
        {
            this.catalogService = catalogService;
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            TryAcquireByIndex(0, Keyboard.current.digit1Key.wasPressedThisFrame);
            TryAcquireByIndex(1, Keyboard.current.digit2Key.wasPressedThisFrame);
            TryAcquireByIndex(2, Keyboard.current.digit3Key.wasPressedThisFrame);
            TryAcquireByIndex(3, Keyboard.current.digit4Key.wasPressedThisFrame);
        }

        private void TryAcquireByIndex(int index, bool wasPressedThisFrame)
        {
            if (!wasPressedThisFrame) return;

            AcquireByIndex(index);
        }

        /// <summary>
        /// index(0~3)에 해당하는 testUnits 항목을 습득시킵니다. 키 입력 폴백뿐 아니라
        /// eval/테스트 코드에서 직접 호출해 검증할 수 있도록 public으로 노출합니다.
        /// </summary>
        public void AcquireByIndex(int index)
        {
            if (catalogService == null)
            {
                Debug.LogWarning("[UnitDebugAcquireTestComponent] IUnitCatalogService가 아직 주입되지 않았습니다.");
                return;
            }

            if (index < 0 || index >= testUnits.Length || testUnits[index] == null)
            {
                Debug.LogWarning($"[UnitDebugAcquireTestComponent] testUnits[{index}]가 비어 있습니다.");
                return;
            }

            var unit = testUnits[index];
            catalogService.AcquireUnit(unit);
            Debug.Log($"[UnitDebugAcquireTestComponent] Acquired unit: {unit.name} (index {index})");
        }
    }
}
