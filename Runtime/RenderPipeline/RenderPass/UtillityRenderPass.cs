﻿using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Rendering;
using InfinityTech.Rendering.RDG;
using UnityEngine.Experimental.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace InfinityTech.Rendering.Pipeline
{
    public partial class InfinityRenderPipeline
    {

        ///////////Gizmos Graph
        struct GizmosPassData
        {
        #if UNITY_EDITOR
            public Camera camera;
            public GizmoSubset gizmoSubset;
        #endif
        }

        void RenderGizmos(Camera camera, GizmoSubset gizmoSubset)
        {
#if UNITY_EDITOR
            if (Handles.ShouldRenderGizmos())
            {
                // Add GizmosPass
                m_GraphBuilder.AddPass<GizmosPassData>("Gizmos", ProfilingSampler.Get(CustomSamplerId.Gizmos),
                (ref GizmosPassData passData, ref RDGPassBuilder PassBuilder) =>
                {
                    passData.camera = camera;
                    passData.gizmoSubset = gizmoSubset;
                },
                (ref GizmosPassData passData, ref RDGContext graphContext) =>
                {
                    graphContext.renderContext.DrawGizmos(passData.camera, passData.gizmoSubset);
                });
            }
#endif
        }

        ///////////SkyBox Graph
        struct SkyBoxData
        {
            public Camera camera;
        }

        void RenderSkyBox(Camera camera)
        {
            // Add SkyAtmospherePass
            m_GraphBuilder.AddPass<SkyBoxData>("SkyBox", ProfilingSampler.Get(CustomSamplerId.SkyBox),
            (ref SkyBoxData passData, ref RDGPassBuilder passBuilder) =>
            {
                passData.camera = camera;
            },
            (ref SkyBoxData passData, ref RDGContext graphContext) =>
            {
                graphContext.renderContext.DrawSkybox(passData.camera);
            });
        }

        ///////////Present Graph
        struct PresentViewData
        {
            public RDGTextureRef srcBuffer;
            public RenderTargetIdentifier dscBuffer;
        }

        void RenderPresentView(Camera camera, RDGTextureRef srcTexture, RenderTexture dscTexture)
        {
            // Add PresentPass
            m_GraphBuilder.AddPass<PresentViewData>("Present", ProfilingSampler.Get(CustomSamplerId.Present),
            (ref PresentViewData passData, ref RDGPassBuilder passBuilder) =>
            {
                passData.srcBuffer = passBuilder.ReadTexture(srcTexture);
                passData.dscBuffer = new RenderTargetIdentifier(dscTexture);
            },
            (ref PresentViewData passData, ref RDGContext graphContext) =>
            {
                RenderTexture SrcBuffer = passData.srcBuffer;
                float4 ScaleBias = new float4((float)camera.pixelWidth / (float)SrcBuffer.width, (float)camera.pixelHeight / (float)SrcBuffer.height, 0.0f, 0.0f);
                if (!dscTexture) { ScaleBias.w = ScaleBias.y; ScaleBias.y *= -1; }

                graphContext.cmdBuffer.SetGlobalVector(InfinityShaderIDs.ScaleBias, ScaleBias);
                graphContext.cmdBuffer.DrawFullScreen(GraphicsUtility.GetViewport(camera), passData.srcBuffer, passData.dscBuffer, 1);
            });
        }

        public static RendererListDesc CreateRendererListDesc(CullingResults CullingData, Camera RenderCamera, ShaderTagId PassName, RenderQueueRange? renderQueueRange = null, PerObjectData rendererConfiguration = 0, bool excludeObjectMotionVectors = false, Material overrideMaterial = null, RenderStateBlock ? stateBlock = null)
        {
            RendererListDesc result = new RendererListDesc(PassName, CullingData, RenderCamera)
            {
                rendererConfiguration = rendererConfiguration,
                renderQueueRange = RenderQueueRange.opaque,
                sortingCriteria = SortingCriteria.CommonOpaque,
                stateBlock = stateBlock,
                overrideMaterial = overrideMaterial,
                excludeObjectMotionVectors = excludeObjectMotionVectors
            };
            return result;
        }

        public static void DrawRendererList(ScriptableRenderContext RenderContext, RendererList RendererList)
        {
            if (RendererList.stateBlock == null) {
                RenderContext.DrawRenderers(RendererList.cullingResult, ref RendererList.drawSettings, ref RendererList.filteringSettings);
            } else {
                var RenderStateBlock = RendererList.stateBlock.Value;
                RenderContext.DrawRenderers(RendererList.cullingResult, ref RendererList.drawSettings, ref RendererList.filteringSettings, ref RenderStateBlock);
            }
        }

    }
}
