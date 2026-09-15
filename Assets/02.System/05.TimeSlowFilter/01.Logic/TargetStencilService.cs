using System;
using System.Collections.Generic;
using CharacterSystem;
using UnityEngine;
using VContainer;

namespace TimeSlowFilterSystem
{
    public class TargetStencilService : ITargetStencilService, IDisposable
    {
        private static TargetStencilService s_CurrentInstance;
        public static TargetStencilService CurrentInstance => s_CurrentInstance;
        private static readonly HashSet<Renderer> s_PendingRegistrations = new();

        private readonly HashSet<Renderer> targetRenderers = new();
        private readonly RuntimeDataTimeSlow runtimeDataTimeSlow;
        private readonly ITimeSlowVisualizer timeSlowVisualizer;
        private bool isActive;

        public IReadOnlyCollection<Renderer> TargetRenderers => targetRenderers;
        public bool IsActive => isActive;

        public static IReadOnlyCollection<Renderer> AllTargetRenderers
        {
            get
            {
                if (s_CurrentInstance != null && s_CurrentInstance.targetRenderers.Count > 0)
                {
                    return s_CurrentInstance.targetRenderers;
                }
                return s_PendingRegistrations;
            }
        }

        public event Action OnTargetsChanged;
        public event Action<bool> OnActiveStateChanged;

        [Inject]
        public TargetStencilService(
            RuntimeDataTimeSlow runtimeDataTimeSlow = null,
            ITimeSlowVisualizer timeSlowVisualizer = null)
        {
            this.runtimeDataTimeSlow = runtimeDataTimeSlow;
            this.timeSlowVisualizer = timeSlowVisualizer;

            s_CurrentInstance = this;
            TargetStencilRendererFeature.BindService(this);

            if (this.runtimeDataTimeSlow != null)
            {
                this.runtimeDataTimeSlow.OnSlowStateChanged += SetActive;
                SetActive(this.runtimeDataTimeSlow.IsSlowActive);
            }
            else if (this.timeSlowVisualizer != null)
            {
                this.timeSlowVisualizer.OnSlowStateChanged += SetActive;
            }

            if (s_PendingRegistrations.Count > 0)
            {
                foreach (var renderer in s_PendingRegistrations)
                {
                    if (renderer != null)
                    {
                        targetRenderers.Add(renderer);
                    }
                }
                s_PendingRegistrations.Clear();
                OnTargetsChanged?.Invoke();
            }
        }

        public void SetActive(bool active)
        {
            if (isActive == active) return;
            isActive = active;
            OnActiveStateChanged?.Invoke(isActive);
        }

        public void RegisterRenderer(Renderer renderer)
        {
            if (renderer == null) return;
            if (targetRenderers.Add(renderer))
            {
                OnTargetsChanged?.Invoke();
            }
        }

        public void UnregisterRenderer(Renderer renderer)
        {
            if (renderer == null) return;
            if (targetRenderers.Remove(renderer))
            {
                OnTargetsChanged?.Invoke();
            }
        }

        public void RegisterRenderers(IEnumerable<Renderer> renderers)
        {
            if (renderers == null) return;
            bool changed = false;
            foreach (var renderer in renderers)
            {
                if (renderer != null && targetRenderers.Add(renderer))
                {
                    changed = true;
                }
            }
            if (changed)
            {
                OnTargetsChanged?.Invoke();
            }
        }

        public void UnregisterRenderers(IEnumerable<Renderer> renderers)
        {
            if (renderers == null) return;
            bool changed = false;
            foreach (var renderer in renderers)
            {
                if (renderer != null && targetRenderers.Remove(renderer))
                {
                    changed = true;
                }
            }
            if (changed)
            {
                OnTargetsChanged?.Invoke();
            }
        }

        public void RegisterFromComponent(Component component)
        {
            if (component == null) return;
            var renderers = component.GetComponentsInChildren<Renderer>(true);
            RegisterRenderers(renderers);
        }

        public void UnregisterFromComponent(Component component)
        {
            if (component == null) return;
            var renderers = component.GetComponentsInChildren<Renderer>(true);
            UnregisterRenderers(renderers);
        }

        public void Clear()
        {
            if (targetRenderers.Count > 0)
            {
                targetRenderers.Clear();
                OnTargetsChanged?.Invoke();
            }
        }

        public void Dispose()
        {
            if (runtimeDataTimeSlow != null)
            {
                runtimeDataTimeSlow.OnSlowStateChanged -= SetActive;
            }
            if (timeSlowVisualizer != null)
            {
                timeSlowVisualizer.OnSlowStateChanged -= SetActive;
            }
            Clear();
            TargetStencilRendererFeature.UnbindService(this);
            if (s_CurrentInstance == this)
            {
                s_CurrentInstance = null;
            }
        }

        public static void RegisterTargetStatic(Component component)
        {
            if (component == null) return;
            var renderers = component.GetComponentsInChildren<Renderer>(true);
            RegisterRenderersStatic(renderers);
        }

        public static void UnregisterTargetStatic(Component component)
        {
            if (component == null) return;
            var renderers = component.GetComponentsInChildren<Renderer>(true);
            UnregisterRenderersStatic(renderers);
        }

        public static void RegisterRendererStatic(Renderer renderer)
        {
            if (renderer == null) return;
            if (s_CurrentInstance != null)
            {
                s_CurrentInstance.RegisterRenderer(renderer);
            }
            else
            {
                s_PendingRegistrations.Add(renderer);
            }
        }

        public static void UnregisterRendererStatic(Renderer renderer)
        {
            if (renderer == null) return;
            if (s_CurrentInstance != null)
            {
                s_CurrentInstance.UnregisterRenderer(renderer);
            }
            else
            {
                s_PendingRegistrations.Remove(renderer);
            }
        }

        public static void RegisterRenderersStatic(IEnumerable<Renderer> renderers)
        {
            if (renderers == null) return;
            if (s_CurrentInstance != null)
            {
                s_CurrentInstance.RegisterRenderers(renderers);
            }
            else
            {
                foreach (var r in renderers)
                {
                    if (r != null) s_PendingRegistrations.Add(r);
                }
            }
        }

        public static void UnregisterRenderersStatic(IEnumerable<Renderer> renderers)
        {
            if (renderers == null) return;
            if (s_CurrentInstance != null)
            {
                s_CurrentInstance.UnregisterRenderers(renderers);
            }
            else
            {
                foreach (var r in renderers)
                {
                    if (r != null) s_PendingRegistrations.Remove(r);
                }
            }
        }
    }
}
