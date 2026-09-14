using System;
using System.Collections.Generic;
using UnityEngine;

namespace Common.InputSystem
{
    public class InputContextManager : MonoBehaviour
    {
        private static InputContextManager s_instance;
        public static InputContextManager Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = FindAnyObjectByType<InputContextManager>();
                    if (s_instance == null)
                    {
                        var go = new GameObject("[InputContextManager]");
                        s_instance = go.AddComponent<InputContextManager>();
                        if (Application.isPlaying)
                        {
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return s_instance;
            }
        }

        private readonly Stack<IInputContext> m_contextStack = new();

        public PlayerInputContext PlayerContext { get; } = new();
        public TacticalInputContext TacticalContext { get; } = new();
        public UIInputContext UIContext { get; } = new();

        public IInputContext CurrentContext => m_contextStack.Count > 0 ? m_contextStack.Peek() : null;

        public event Action<IInputContext> OnContextChanged;

        private void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }

            // 기본 컨텍스트: PlayerInputContext
            if (m_contextStack.Count == 0)
            {
                PushContext(PlayerContext);
            }
        }

        public void PushContext(IInputContext context)
        {
            if (context == null) return;

            // 이미 최상단에 동일한 컨텍스트가 있으면 중복 푸시 방지
            if (m_contextStack.Count > 0 && m_contextStack.Peek() == context)
            {
                return;
            }

            // 기존 최상단 일시정지
            if (m_contextStack.Count > 0)
            {
                m_contextStack.Peek().Pause();
            }

            m_contextStack.Push(context);
            context.Enter();

            ApplyCursorState(context);
            OnContextChanged?.Invoke(context);

            Debug.Log($"<color=cyan>[InputContextManager] Pushed Context:</color> {context.ContextType} (Stack Depth: {m_contextStack.Count})");
        }

        public void PopContext(IInputContext context = null)
        {
            if (m_contextStack.Count == 0) return;

            // 특정 context를 지정한 경우, 해당 context가 최상단이 아니면 스택에서 제거
            if (context != null && m_contextStack.Peek() != context)
            {
                var list = new List<IInputContext>(m_contextStack);
                if (list.Remove(context))
                {
                    context.Exit();
                    m_contextStack.Clear();
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        m_contextStack.Push(list[i]);
                    }
                }
                return;
            }

            // 최상단 Pop
            var popped = m_contextStack.Pop();
            popped.Exit();

            // 다음 최상단 Resume
            if (m_contextStack.Count > 0)
            {
                var current = m_contextStack.Peek();
                current.Resume();
                ApplyCursorState(current);
                OnContextChanged?.Invoke(current);
                Debug.Log($"<color=cyan>[InputContextManager] Popped Context:</color> {popped.ContextType}, Resumed: {current.ContextType} (Stack Depth: {m_contextStack.Count})");
            }
            else
            {
                // 스택이 비면 기본 PlayerContext로 복귀
                PushContext(PlayerContext);
            }
        }

        public void ApplyCursorState(IInputContext context)
        {
            if (context == null) return;

            Cursor.lockState = context.TargetCursorLockMode;
            Cursor.visible = context.TargetCursorVisible;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && CurrentContext != null)
            {
                ApplyCursorState(CurrentContext);
            }
        }
    }
}
