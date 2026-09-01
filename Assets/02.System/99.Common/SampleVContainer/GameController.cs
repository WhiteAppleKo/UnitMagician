using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace CodeLibrary.Scripts.VContainer
{
    public class GameController : MonoBehaviour
    {
        [Inject]
        private GameManager m_GameManager;

        private void Update()
        {
            // New Input System: Keyboard.current를 사용하여 스페이스바 입력 체크
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                if (m_GameManager != null)
                {
                    m_GameManager.CallGameManager();
                }
            }
        }
    }
}
