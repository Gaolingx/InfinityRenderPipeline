using System;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.VFX;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine.Rendering;
using InfinityTech.Component;
using System.Collections.Generic;
using InfinityTech.Rendering.RDG;
using InfinityTech.Rendering.Core;
using InfinityTech.Rendering.Feature;
using InfinityTech.Rendering.GPUResource;
using InfinityTech.Rendering.MeshPipeline;
using InfinityTech.Rendering.TerrainPipeline;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace InfinityTech.Rendering.Pipeline
{
    internal struct FViewUnifrom
    {
        private static readonly int ID_FrameIndex = Shader.PropertyToID("FrameIndex");
        private static readonly int ID_TAAJitter = Shader.PropertyToID("TAAJitter");
        private static readonly int ID_Matrix_WorldToView = Shader.PropertyToID("Matrix_WorldToView");
        private static readonly int ID_Matrix_ViewToWorld = Shader.PropertyToID("Matrix_ViewToWorld");
        private static readonly int ID_Matrix_Proj = Shader.PropertyToID("Matrix_Proj");
        private static readonly int ID_Matrix_InvProj = Shader.PropertyToID("Matrix_InvProj");
        private static readonly int ID_Matrix_JitterProj = Shader.PropertyToID("Matrix_JitterProj");
        private static readonly int ID_Matrix_InvJitterProj = Shader.PropertyToID("Matrix_InvJitterProj");
        private static readonly int ID_Matrix_FlipYProj = Shader.PropertyToID("Matrix_FlipYProj");
        private static readonly int ID_Matrix_InvFlipYProj = Shader.PropertyToID("Matrix_InvFlipYProj");
        private static readonly int ID_Matrix_FlipYJitterProj = Shader.PropertyToID("Matrix_FlipYJitterProj");
        private static readonly int ID_Matrix_InvFlipYJitterProj = Shader.PropertyToID("Matrix_InvFlipYJitterProj");
        private static readonly int ID_Matrix_ViewProj = Shader.PropertyToID("Matrix_ViewProj");
        private static readonly int ID_Matrix_InvViewProj = Shader.PropertyToID("Matrix_InvViewProj");
        private static readonly int ID_Matrix_ViewFlipYProj = Shader.PropertyToID("Matrix_ViewFlipYProj");
        private static readonly int ID_Matrix_InvViewFlipYProj = Shader.PropertyToID("Matrix_InvViewFlipYProj");
        private static readonly int ID_Matrix_ViewJitterProj = Shader.PropertyToID("Matrix_ViewJitterProj");
        private static readonly int ID_Matrix_InvViewJitterProj = Shader.PropertyToID("Matrix_InvViewJitterProj");
        private static readonly int ID_Matrix_ViewFlipYJitterProj = Shader.PropertyToID("Matrix_ViewFlipYJitterProj");
        private static readonly int ID_Matrix_InvViewFlipYJitterProj = Shader.PropertyToID("Matrix_InvViewFlipYJitterProj");
        private static readonly int ID_LastFrameIndex = Shader.PropertyToID("Prev_FrameIndex");
        private static readonly int ID_Matrix_LastViewProj = Shader.PropertyToID("Matrix_PrevViewProj");
        private static readonly int ID_Matrix_LastViewFlipYProj = Shader.PropertyToID("Matrix_PrevViewFlipYProj");


        public int frameIndex;
        public float2 tempJitter;
        public int lastFrameIndex;
        public float2 lastTempJitter;
        public Matrix4x4 matrix_WorldToView;
        public Matrix4x4 matrix_ViewToWorld;
        public Matrix4x4 matrix_Proj;
        public Matrix4x4 matrix_InvProj;
        public Matrix4x4 matrix_JitterProj;
        public Matrix4x4 matrix_InvJitterProj;
        public Matrix4x4 matrix_FlipYProj;
        public Matrix4x4 matrix_InvFlipYProj;
        public Matrix4x4 matrix_FlipYJitterProj;
        public Matrix4x4 matrix_InvFlipYJitterProj;
        public Matrix4x4 matrix_ViewProj;
        public Matrix4x4 matrix_InvViewProj;
        public Matrix4x4 matrix_ViewFlipYProj;
        public Matrix4x4 matrix_InvViewFlipYProj;
        public Matrix4x4 matrix_ViewJitterProj;
        public Matrix4x4 matrix_InvViewJitterProj;
        public Matrix4x4 matrix_ViewFlipYJitterProj;
        public Matrix4x4 matrix_InvViewFlipYJitterProj;
        public Matrix4x4 matrix_LastViewProj;
        public Matrix4x4 matrix_LastViewFlipYProj;

        private void UnpateCurrBufferData(Camera camera)
        {
            matrix_WorldToView = camera.worldToCameraMatrix;
            matrix_ViewToWorld = matrix_WorldToView.inverse;
            matrix_Proj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
            matrix_InvProj = matrix_Proj.inverse;
            matrix_JitterProj = FTemporalAntiAliasing.CaculateProjectionMatrix(camera, ref frameIndex, ref tempJitter, matrix_Proj);
            matrix_InvJitterProj = matrix_JitterProj.inverse;
            matrix_FlipYProj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
            matrix_InvFlipYProj = matrix_FlipYProj.inverse;
            matrix_FlipYJitterProj = FTemporalAntiAliasing.CaculateProjectionMatrix(camera, ref frameIndex, ref tempJitter, matrix_FlipYProj, false);
            matrix_InvFlipYJitterProj = matrix_FlipYJitterProj.inverse;
            matrix_ViewProj = matrix_Proj * matrix_WorldToView;
            matrix_InvViewProj = matrix_ViewProj.inverse;
            matrix_ViewFlipYProj = matrix_FlipYProj * matrix_WorldToView;
            matrix_InvViewFlipYProj = matrix_ViewFlipYProj.inverse;
            matrix_ViewJitterProj = matrix_JitterProj * matrix_WorldToView;
            matrix_InvViewJitterProj = matrix_ViewJitterProj.inverse;
            matrix_ViewFlipYJitterProj = matrix_FlipYJitterProj * matrix_WorldToView;
            matrix_InvViewFlipYJitterProj = matrix_ViewFlipYJitterProj.inverse;
        }

        private void UnpateLastBufferData()
        {
            lastFrameIndex = frameIndex;
            lastTempJitter = tempJitter;
            matrix_LastViewProj = matrix_ViewProj;
            matrix_LastViewFlipYProj = matrix_ViewFlipYProj;
        }

        public void UnpateViewUnifromData(Camera camera, in bool isLastData = false)
        {
            if(!isLastData) {
                UnpateCurrBufferData(camera);
            } else {
                UnpateLastBufferData();
            }
        }

        public void SetViewUnifromDataToGPU(CommandBuffer cmdBuffer, in float2 resolution)
        {
            cmdBuffer.SetGlobalInt(ID_FrameIndex, frameIndex);
            cmdBuffer.SetGlobalInt(ID_LastFrameIndex, lastFrameIndex);
            cmdBuffer.SetGlobalVector(ID_TAAJitter, new float4(tempJitter.x / resolution.x, tempJitter.y / resolution.y, lastTempJitter.x / resolution.x, lastTempJitter.y / resolution.y));
            cmdBuffer.SetGlobalMatrix(ID_Matrix_WorldToView, matrix_WorldToView);
            cmdBuffer.SetGlobalMatrix(ID_Matrix_ViewToWorld, matrix_ViewToWorld);
            cmdBuffer.SetGlobalMatrix(ID_Matrix_Proj, matrix_Proj);
            cmdBuffer.SetGlobalMatrix(ID_Matrix_InvProj, matrix_InvProj);
            cmdBuffer.SetGlobalMatrix(ID_Matrix_JitterProj, matrix_JitterProj);
            cmdBuffer.SetGlobalMatrix(ID_Matrix_InvJitterProj, matrix_InvJitterProj);
            cmdBuffer.SetGlobalMatrix(ID_Matrix_FlipYProj, matrix_FlipYProj);
            cmdBuffer.SetGlobalMatrix(ID_Matrix_InvFlipYProj, matrix_InvFlipYProj);
            cmdBuffer.SetGlobalMatrix(ID_Matrix_FlipYJitterProj, matrix_FlipYJitterProj);
            cmdBuffer.SetGlobalMatrix(ID_Matrix_InvFlipYJitterProj, matrix_InvFlipYJitterProj);
            cmdBuffer.SetGlobalMatrix(ID_Matrix_ViewProj, matrix_ViewProj);
            cmdBuffer.SetGlobalMatrix(ID_Matrix_InvViewProj, matrix_InvViewProj);
            cmdBuffer.SetGlobalMatrix(ID_Matrix_ViewFlipYProj, matrix_ViewFlipYProj);
            cmdBuffer.SetGlobalMatrix(ID_Matrix_InvViewFlipYProj, matrix_InvViewFlipYProj);
            cmdBuffer.SetGlobalMatrix(ID_Matrix_ViewJitterProj, matrix_ViewJitterProj);
            cmdBuffer.SetGlobalMatrix(ID_Matrix_InvViewJitterProj, matrix_InvViewJitterProj);
            cmdBuffer.SetGlobalMatrix(ID_Matrix_ViewFlipYJitterProj, matrix_ViewFlipYJitterProj);
            cmdBuffer.SetGlobalMatrix(ID_Matrix_InvViewFlipYJitterProj, matrix_InvViewFlipYJitterProj);
            cmdBuffer.SetGlobalMatrix(ID_Matrix_LastViewProj, matrix_LastViewProj);
            cmdBuffer.SetGlobalMatrix(ID_Matrix_LastViewFlipYProj, matrix_LastViewFlipYProj);
        }
    }

    public partial class InfinityRenderPipeline : RenderPipeline
    {
        private FGPUScene m_GPUScene;
        private RDGBuilder m_GraphBuilder;
        private FViewUnifrom m_ViewUnifrom;
        private FTemporalAntiAliasing m_TemporalAA;
        private NativeList<JobHandle> m_MeshPassJobRefs;
        private FMeshPassProcessor m_DepthMeshProcessor;
        private FMeshPassProcessor m_GBufferMeshProcessor;
        private FMeshPassProcessor m_ForwardMeshProcessor;

        internal InfinityRenderPipelineAsset renderPipelineAsset 
        { 
            get 
            { 
                return (InfinityRenderPipelineAsset)GraphicsSettings.currentRenderPipeline; 
            }
        }

        public InfinityRenderPipeline()
        {
            SetGraphicsSetting();
            m_GPUScene = new FGPUScene();
            m_ViewUnifrom = new FViewUnifrom();
            m_GraphBuilder = new RDGBuilder("RenderGraph");
            m_MeshPassJobRefs = new NativeList<JobHandle>(32, Allocator.Persistent);
            m_TemporalAA = new FTemporalAntiAliasing(renderPipelineAsset.temporalAAShader);
            m_DepthMeshProcessor = new FMeshPassProcessor(m_GPUScene, ref m_MeshPassJobRefs);
            m_GBufferMeshProcessor = new FMeshPassProcessor(m_GPUScene, ref m_MeshPassJobRefs);
            m_ForwardMeshProcessor = new FMeshPassProcessor(m_GPUScene, ref m_MeshPassJobRefs);
        }

        protected override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
        {
            //Setup FrameContext
            RTHandles.Initialize(Screen.width, Screen.height);
            CommandBuffer cmdBuffer = CommandBufferPool.Get("");
            FResourcePool resourcePool = GetWorld().resourcePool;
            m_GPUScene.Update(GetWorld().GetMeshBatchColloctor(), resourcePool, cmdBuffer, false);

            BeginFrameRendering(renderContext, cameras);
            for (int i = 0; i < cameras.Length; ++i)
            {
                Camera camera = cameras[i];
                CameraComponent cameraComponent = camera.GetComponent<CameraComponent>();

                //Camera Rendering
                using (new ProfilingScope(cmdBuffer, cameraComponent ? cameraComponent.viewProfiler : ProfilingSampler.Get(ERGProfileId.CameraRendering)))
                {
                    BeginCameraRendering(renderContext, camera);
                    {
                        FCullingData cullingData;
                        CullingResults cullingResult;
                        
                        #region InitViewContext
                        using (new ProfilingScope(cmdBuffer, ProfilingSampler.Get(ERGProfileId.ViewContext)))
                        {
                            bool isEditView = camera.cameraType == CameraType.SceneView;
                            bool isSceneView = camera.cameraType == CameraType.Game || camera.cameraType == CameraType.Reflection || camera.cameraType == CameraType.SceneView;

                            #if UNITY_EDITOR
                            if (isEditView) 
                            { 
                                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera); 
                            }
                            #endif

                            m_MeshPassJobRefs.Clear();
                            VFXManager.PrepareCamera(camera);
                            VFXManager.ProcessCameraCommand(camera, cmdBuffer);
                            renderContext.SetupCameraProperties(camera);
                            m_ViewUnifrom.UnpateViewUnifromData(camera);
                            m_ViewUnifrom.SetViewUnifromDataToGPU(cmdBuffer, new float2(camera.pixelWidth, camera.pixelHeight));

                            //Compute LOD
                            using (new ProfilingScope(cmdBuffer, ProfilingSampler.Get(ERGProfileId.ComputeLOD)))
                            {
                                List<TerrainComponent> terrains = GetWorld().GetWorldTerrains();
                                float4x4 matrix_Proj = TerrainUtility.GetProjectionMatrix(camera.fieldOfView + 30, camera.pixelWidth, camera.pixelHeight, camera.nearClipPlane, camera.farClipPlane);
                                for(int j = 0; j < terrains.Count; ++j)
                                {
                                    TerrainComponent terrain = terrains[j];
                                    terrain.ComputeLOD(camera.transform.position, matrix_Proj);
                                    
                                    #if UNITY_EDITOR
                                        if (Handles.ShouldRenderGizmos()) { terrain.DrawBounds(true); }
                                    #endif
                                }
                            }

                            //Scene Culling
                            camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters);
                            cullingParameters.cullingOptions = CullingOptions.ShadowCasters | CullingOptions.NeedsLighting | CullingOptions.DisablePerObjectCulling;

                            using (new ProfilingScope(cmdBuffer, ProfilingSampler.Get(ERGProfileId.CulllingScene)))
                            {
                                cullingResult = renderContext.Cull(ref cullingParameters);
                                cullingData = renderContext.DispatchCull(m_GPUScene, isSceneView, ref cullingParameters);
                            }
                        }
                        #endregion //InitViewContext

                        #region InitViewCommand
                        RenderDepth(camera, cullingData, cullingResult);
                        RenderGBuffer(camera, cullingData, cullingResult);
                        RenderMotion(camera, cullingData, cullingResult);
                        RenderForward(camera, cullingData, cullingResult);
                        RenderSkyBox(camera);
                        #if UNITY_EDITOR
                        RenderGizmos(camera, GizmoSubset.PostImageEffects);
#endif
                        RenderTemporalAA(camera, m_GraphBuilder.ScopeTexture(InfinityShaderIDs.DiffuseBuffer), cameraComponent.historyTexture);
                        RenderPresent(camera, m_GraphBuilder.ScopeTexture(InfinityShaderIDs.TemporalBuffer), camera.targetTexture);

                        JobHandle.CompleteAll(m_MeshPassJobRefs);
                        m_GraphBuilder.Execute(renderContext, GetWorld(), resourcePool, cmdBuffer);
                        #endregion //InitViewCommand

                        #region ReleaseViewContext
                        cullingData.Release();
                        m_ViewUnifrom.UnpateViewUnifromData(camera, true);
                        #endregion //ReleaseViewContext
                    }
                    EndCameraRendering(renderContext, camera);
                }
            }
            EndFrameRendering(renderContext, cameras);
            
            //Execute FrameContext
            renderContext.ExecuteCommandBuffer(cmdBuffer);
            renderContext.Submit();
            cmdBuffer.Clear();
            
            //Release FrameContext
            m_GPUScene.Release(resourcePool);
            CommandBufferPool.Release(cmdBuffer);
        }

        protected FRenderWorld GetWorld()
        {
            if (FRenderWorld.RenderWorld != null) 
            {
                return FRenderWorld.RenderWorld;
            }

            return null;
        }

        protected void SetGraphicsSetting()
        {
            Shader.globalRenderPipeline = "InfinityRenderPipeline";

            GraphicsSettings.lightsUseLinearIntensity = true;
            GraphicsSettings.lightsUseColorTemperature = true;
            InfinityRenderPipelineAsset PipelineAsset = (InfinityRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            GraphicsSettings.useScriptableRenderPipelineBatching = PipelineAsset.enableSRPBatch;

            SupportedRenderingFeatures.active = new SupportedRenderingFeatures()
            {
                reflectionProbeModes = SupportedRenderingFeatures.ReflectionProbeModes.Rotation,
                defaultMixedLightingModes = SupportedRenderingFeatures.LightmapMixedBakeModes.IndirectOnly,
                mixedLightingModes = SupportedRenderingFeatures.LightmapMixedBakeModes.IndirectOnly | SupportedRenderingFeatures.LightmapMixedBakeModes.Shadowmask,
                lightmapBakeTypes = LightmapBakeType.Baked | LightmapBakeType.Mixed | LightmapBakeType.Realtime,
                lightmapsModes = LightmapsMode.NonDirectional | LightmapsMode.CombinedDirectional,
                lightProbeProxyVolumes = true,
                motionVectors = true,
                receiveShadows = true,
                reflectionProbes = true,
                rendererPriority = true,
                overridesFog = true,
                overridesOtherLightingSettings = true,
                editableMaterialRenderQueue = true
                , enlighten = true
                , overridesLODBias = true
                , overridesMaximumLODLevel = true
            };
        }        
        
        protected override void Dispose(bool disposing)
        {
            if (!disposing) { return; }

            m_GraphBuilder.Dispose();
            m_MeshPassJobRefs.Dispose();
        }
    }
}