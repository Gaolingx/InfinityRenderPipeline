using UnityEngine;
using Unity.Collections;
using UnityEngine.Rendering;
using InfinityTech.Rendering.RenderGraph;
using UnityEngine.Experimental.Rendering;
using InfinityTech.Rendering.MeshPipeline;

namespace InfinityTech.Rendering.Pipeline
{
    public partial class InfinityRenderPipeline
    {
        struct AtmospherePassData
        {
            public RGTextureRef skyTarget;
            public RGTextureRef volumeLUT;
            public RGTextureRef scatteringLUT;
            public RGTextureRef transmittionLUT;
        }

        void RenderSkyAtmosphere(Camera RenderCamera)
        {
            //Add SkyAtmospherePass
            using (RGComputePassRef passRef = m_RGBuilder.AddComputePass<AtmospherePassData>(ProfilingSampler.Get(CustomSamplerId.RenderAtmosphere)))
            {
                //Setup Phase
                ref AtmospherePassData passData = ref passRef.GetPassData<AtmospherePassData>();

                //Execute Phase
                passRef.SetExecuteFunc((in AtmospherePassData passData, in RGComputeEncoder cmdEncoder, RGObjectPool objectPool) =>
                {

                });
            }
        }
    }
}
