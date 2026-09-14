using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace TimeSlowFilterSystem
{
    public class TargetStencilRendererFeature : ScriptableRendererFeature
    {
        private static ITargetStencilService s_CurrentService;

        [Header("Data Settings")]
        [SerializeField] private PureDataStencilMask pureData;

        [Header("Material Settings")]
        [SerializeField] private Material maskMaterial;
        [SerializeField] private Shader maskShader;

        [Header("Kurosawa Fullscreen Settings")]
        [SerializeField] private Material kurosawaMaterial;

        private TargetStencilPass stencilPass;
        private KurosawaFullscreenPass kurosawaPass;
        private Material runtimeMaterial;

        public static void BindService(ITargetStencilService service)
        {
            s_CurrentService = service;
        }

        public static void UnbindService(ITargetStencilService service)
        {
            if (s_CurrentService == service)
            {
                s_CurrentService = null;
            }
        }

        public static ITargetStencilService CurrentService => s_CurrentService;

        public override void Create()
        {
            Material targetMat = maskMaterial;
            if (targetMat == null && pureData != null)
            {
                targetMat = pureData.MaskMaterial;
            }

            if (targetMat == null && maskShader != null)
            {
                runtimeMaterial = new Material(maskShader);
                targetMat = runtimeMaterial;
            }

            RenderPassEvent passEvent = pureData != null ? pureData.PassEvent : RenderPassEvent.AfterRenderingOpaques;
            stencilPass = new TargetStencilPass(targetMat, passEvent);

            if (kurosawaMaterial != null)
            {
                kurosawaPass = new KurosawaFullscreenPass(kurosawaMaterial);
            }
        }

        private float currentIntensity = 0f;

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Preview)
                return;

            // 시간 정지 상태 감지 (순수 데이터 서비스 플래그 연동)
            bool isSlow = s_CurrentService != null && s_CurrentService.IsActive;
            float targetIntensity = isSlow ? 1.0f : 0.0f;
            float transitionSpeed = 1.0f / 0.3f;

            if (Application.isPlaying)
            {
                currentIntensity = Mathf.MoveTowards(currentIntensity, targetIntensity, Time.unscaledDeltaTime * transitionSpeed);
            }
            else
            {
                currentIntensity = targetIntensity;
            }

            // 완전히 꺼져 있을 때는 패스를 전혀 등록하지 않음 (평소 100% 정상 화면)
            if (currentIntensity <= 0.001f && !isSlow)
            {
                Shader.SetGlobalFloat("_DesaturateAmount", 0f);
                if (kurosawaMaterial != null) kurosawaMaterial.SetFloat("_DesaturateAmount", 0f);
                return;
            }

            // 흑백 강도를 셰이더 및 머티리얼에 실시간 전달
            Shader.SetGlobalFloat("_DesaturateAmount", currentIntensity);
            if (kurosawaMaterial != null)
            {
                kurosawaMaterial.SetFloat("_DesaturateAmount", currentIntensity);
            }

            var targets = TargetStencilService.AllTargetRenderers;
            if (targets != null && targets.Count > 0)
            {
                renderer.EnqueuePass(stencilPass);
            }

            if (kurosawaPass != null)
            {
                renderer.EnqueuePass(kurosawaPass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (runtimeMaterial != null)
            {
                DestroyImmediate(runtimeMaterial);
                runtimeMaterial = null;
            }
        }

        private class TargetStencilPass : ScriptableRenderPass
        {
            private const string PassTag = "TargetStencilPass";
            private readonly Material material;
            private readonly List<Renderer> activeRenderers = new();

            public TargetStencilPass(Material material, RenderPassEvent passEvent)
            {
                this.material = material;
                this.renderPassEvent = passEvent;
                this.profilingSampler = new ProfilingSampler(PassTag);
            }

            private class PassData
            {
                internal Material passMaterial;
                internal readonly List<Renderer> renderersToDraw = new();
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var targets = TargetStencilService.AllTargetRenderers;
                if (targets == null || targets.Count == 0 || material == null)
                    return;

                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                if (cameraData.cameraType == CameraType.Preview)
                    return;

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                activeRenderers.Clear();
                foreach (var renderer in targets)
                {
                    if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy)
                    {
                        activeRenderers.Add(renderer);
                    }
                }

                if (activeRenderers.Count == 0)
                    return;

                using (var builder = renderGraph.AddRasterRenderPass<PassData>(PassTag, out var passData, profilingSampler))
                {
                    passData.passMaterial = material;
                    passData.renderersToDraw.Clear();
                    passData.renderersToDraw.AddRange(activeRenderers);

                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Write);
                    builder.AllowGlobalStateModification(true);

                    builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
                    {
                        for (int i = 0; i < data.renderersToDraw.Count; i++)
                        {
                            var r = data.renderersToDraw[i];
                            if (r == null) continue;

                            int submeshCount = r.sharedMaterials != null ? r.sharedMaterials.Length : 1;
                            for (int sub = 0; sub < submeshCount; sub++)
                            {
                                rgContext.cmd.DrawRenderer(r, data.passMaterial, sub, 0);
                            }
                        }
                    });
                }
            }

            [Obsolete("호환성 모드를 위한 렌더링 경로입니다.")]
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var targets = TargetStencilService.AllTargetRenderers;
                if (targets == null || targets.Count == 0 || material == null)
                    return;

                if (renderingData.cameraData.cameraType == CameraType.Preview)
                    return;

                activeRenderers.Clear();
                foreach (var renderer in targets)
                {
                    if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy)
                    {
                        activeRenderers.Add(renderer);
                    }
                }

                if (activeRenderers.Count == 0)
                    return;

                CommandBuffer cmd = CommandBufferPool.Get(PassTag);
                using (new ProfilingScope(cmd, profilingSampler))
                {
                    for (int i = 0; i < activeRenderers.Count; i++)
                    {
                        var r = activeRenderers[i];
                        if (r == null) continue;

                        int submeshCount = r.sharedMaterials != null ? r.sharedMaterials.Length : 1;
                        for (int sub = 0; sub < submeshCount; sub++)
                        {
                            cmd.DrawRenderer(r, material, sub, 0);
                        }
                    }
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }

        private class KurosawaFullscreenPass : ScriptableRenderPass
        {
            private const string PassTag = "KurosawaFullscreenPass";
            private readonly Material material;

            public KurosawaFullscreenPass(Material material)
            {
                this.material = material;
                this.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
                this.profilingSampler = new ProfilingSampler(PassTag);
            }

            private class PassData
            {
                internal Material passMaterial;
                internal TextureHandle sourceTexture;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (material == null) return;

                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                if (cameraData.cameraType == CameraType.Preview) return;

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                if (!resourceData.activeColorTexture.IsValid()) return;

                RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;
                TextureHandle tempColor = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "KurosawaTempColor", false);

                // 원본 화면 복사 패스
                using (var copyBuilder = renderGraph.AddRasterRenderPass<PassData>("KurosawaCopyPass", out var copyData, profilingSampler))
                {
                    copyData.sourceTexture = resourceData.activeColorTexture;
                    copyBuilder.UseTexture(copyData.sourceTexture, AccessFlags.Read);
                    copyBuilder.SetRenderAttachment(tempColor, 0, AccessFlags.Write);
                    copyBuilder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
                    {
                        Blitter.BlitTexture(rgContext.cmd, data.sourceTexture, new Vector4(1, 1, 0, 0), 0.0f, false);
                    });
                }

                // 흑백 필터 적용 및 스텐실 판정 패스
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(PassTag, out var passData, profilingSampler))
                {
                    passData.passMaterial = material;
                    passData.sourceTexture = tempColor;

                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                    if (resourceData.activeDepthTexture.IsValid())
                    {
                        builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);
                    }
                    builder.UseTexture(tempColor, AccessFlags.Read);
                    builder.AllowGlobalStateModification(true);

                    builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
                    {
                        Blitter.BlitTexture(rgContext.cmd, data.sourceTexture, new Vector4(1, 1, 0, 0), data.passMaterial, 0);
                    });
                }
            }

            [Obsolete("호환성 모드를 위한 렌더링 경로입니다.")]
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (material == null) return;
                CommandBuffer cmd = CommandBufferPool.Get(PassTag);
                using (new ProfilingScope(cmd, profilingSampler))
                {
                    RTHandle cameraTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
                    Blitter.BlitCameraTexture(cmd, cameraTarget, cameraTarget, material, 0);
                }
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }
    }
}
