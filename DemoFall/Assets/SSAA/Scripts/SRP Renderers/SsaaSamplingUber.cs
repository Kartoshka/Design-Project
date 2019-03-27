using System;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_2018_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2 && (SSAA_HDRP || SSAA_LWRP)
using UnityEngine.Rendering.PostProcessing;
namespace MadGoat_SSAA
{
    [Serializable]
    [PostProcess(typeof(SsaaSamplingUberRenderer), PostProcessEvent.BeforeStack, "SSAA/SSAA_Apply")]
    public sealed class SsaaSamplingUber : PostProcessEffectSettings
    {
        public UnityEngine.Rendering.PostProcessing.TextureParameter sourceTex = new UnityEngine.Rendering.PostProcessing.TextureParameter { value = null, overrideState = true };

        public UnityEngine.Rendering.PostProcessing.BoolParameter useFXAA = new UnityEngine.Rendering.PostProcessing.BoolParameter { value = false, overrideState = true };
        public UnityEngine.Rendering.PostProcessing.FloatParameter intensityFXAA = new UnityEngine.Rendering.PostProcessing.FloatParameter { value = 1f, overrideState = true };

        public UnityEngine.Rendering.PostProcessing.IntParameter shaderPass = new UnityEngine.Rendering.PostProcessing.IntParameter { value = 0, overrideState = true };

        public UnityEngine.Rendering.PostProcessing.FloatParameter resizeWidth = new UnityEngine.Rendering.PostProcessing.FloatParameter { value = 0.5f, overrideState = true };
        public UnityEngine.Rendering.PostProcessing.FloatParameter resizeHeight = new UnityEngine.Rendering.PostProcessing.FloatParameter { value = 0.5f, overrideState = true };
        public UnityEngine.Rendering.PostProcessing.FloatParameter sharpness = new UnityEngine.Rendering.PostProcessing.FloatParameter { value = 0.5f, overrideState = true };
        public UnityEngine.Rendering.PostProcessing.FloatParameter sampleDistance = new UnityEngine.Rendering.PostProcessing.FloatParameter { value = 0.5f, overrideState = true };

        public UnityEngine.Rendering.PostProcessing.BoolParameter flip = new UnityEngine.Rendering.PostProcessing.BoolParameter { value = false, overrideState = true };
    }

    public sealed class SsaaSamplingUberRenderer : PostProcessEffectRenderer<SsaaSamplingUber>
    {
        public override void Render(PostProcessRenderContext context)
        {
            var sourceTexId = new RenderTargetIdentifier(settings.sourceTex);
            var sheet = context.propertySheets.Get(Shader.Find("Hidden/SSAA/SRP_SSAA_UBER"));

            //RenderTexture rt_buff = RenderTexture.GetTemporary(settings.sourceTex.value.width, settings.sourceTex.value.height, 24, RenderTextureFormat.ARGBHalf);
            RenderTexture rt_buff = RenderTexture.GetTemporary(settings.sourceTex.value.width, settings.sourceTex.value.height, 24, RenderTextureFormat.ARGBHalf);
            sheet.properties.SetFloat("_Sharpness", settings.sharpness);
            sheet.properties.SetFloat("_SampleDistance", settings.sampleDistance);
            sheet.properties.SetFloat("_ResizeHeight", settings.resizeHeight);
            sheet.properties.SetFloat("_ResizeWidth", settings.resizeWidth);

            //// flip
            //context.command.BlitFullscreenTriangle(sourceTexId, rt_buff, sheet, settings.flip ? 4 : 0);

            context.command.BlitFullscreenTriangle(sourceTexId, rt_buff);
            if (settings.useFXAA)
            {
                // fxaa
                sheet.properties.SetFloat("_Intensity", settings.intensityFXAA);
                RenderTexture rt_buff2 = RenderTexture.GetTemporary(settings.sourceTex.value.width, settings.sourceTex.value.height, 24, RenderTextureFormat.ARGBHalf);
                context.command.BlitFullscreenTriangle(rt_buff, rt_buff2, sheet, 5);

                // final
                sheet.properties.SetTexture("_SourceTex", rt_buff2);
                context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, settings.shaderPass);

                RenderTexture.ReleaseTemporary(rt_buff2);
            }
            else
            {
                // final
                context.command.BlitFullscreenTriangle(sourceTexId, rt_buff);
                sheet.properties.SetTexture("_SourceTex", settings.sourceTex);
                context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, settings.shaderPass);
            }
            RenderTexture.ReleaseTemporary(rt_buff);
        }
    }
}
#endif