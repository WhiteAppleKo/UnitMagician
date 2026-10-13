using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace CodeLibrary.Scripts.VContainer
{
    public class Tester : ITickable
    {
        [Inject]
        private GameManager m_GameManager;

        public void Tick()
        {
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