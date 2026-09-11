using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimeSlowFilterSystem
{
    public interface ITargetStencilService
    {
        IReadOnlyCollection<Renderer> TargetRenderers { get; }
        bool IsActive { get; }

        event Action OnTargetsChanged;
        event Action<bool> OnActiveStateChanged;

        void SetActive(bool isActive);
        void RegisterRenderer(Renderer renderer);
        void UnregisterRenderer(Renderer renderer);
        void RegisterRenderers(IEnumerable<Renderer> renderers);
        void UnregisterRenderers(IEnumerable<Renderer> renderers);
        void RegisterFromComponent(Component component);
        void UnregisterFromComponent(Component component);
        void Clear();
    }
}
