using System;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_2018_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2 && (SSAA_HDRP || SSAA_LWRP)
using UnityEngine.Rendering.PostProcessing;
namespace MadGoat_SSAA
{
    [Serializable]
    [PostProcess(typeof(SsaaVRSamplingUberRenderer), PostProcessEvent.BeforeStack, "SSAA/SSAA_Apply_VR")]
    public sealed class SsaaVRSamplingUber : PostProcessEffectSettings
    {
        public UnityEngine.Rendering.PostProcessing.BoolParameter useFXAA = new UnityEngine.Rendering.PostProcessing.BoolParameter { value = false, overrideState = true };
        public UnityEngine.Rendering.PostProcessing.FloatParameter intensityFXAA = new UnityEngine.Rendering.PostProcessing.FloatParameter { value = 1f, overrideState = true };

        public UnityEngine.Rendering.PostProcessing.IntParameter shaderPass = new UnityEngine.Rendering.PostProcessing.IntParameter { value = 0, overrideState = true };

        public UnityEngine.Rendering.PostProcessing.FloatParameter resizeWidth = new UnityEngine.Rendering.PostProcessing.FloatParameter { value = 0.5f, overrideState = true };
        public UnityEngine.Rendering.PostProcessing.FloatParameter resizeHeight = new UnityEngine.Rendering.PostProcessing.FloatParameter { value = 0.5f, overrideState = true };
        public UnityEngine.Rendering.PostProcessing.FloatParameter sharpness = new UnityEngine.Rendering.PostProcessing.FloatParameter { value = 0.5f, overrideState = true };
        public UnityEngine.Rendering.PostProcessing.FloatParameter sampleDistance = new UnityEngine.Rendering.PostProcessing.FloatParameter { value = 0.5f, overrideState = true };

        public UnityEngine.Rendering.PostProcessing.BoolParameter flip = new UnityEngine.Rendering.PostProcessing.BoolParameter { value = false, overrideState = true };
    }

    public sealed class SsaaVRSamplingUberRenderer : PostProcessEffectRenderer<SsaaVRSamplingUber>
    {
        public override void Render(PostProcessRenderContext context)
        {
            var sheet = context.propertySheets.Get(Shader.Find("Hidden/SSAA/SRP_SSAA_UBER"));

            RenderTexture rt_buff = RenderTexture.GetTemporary(context.width, context.height, 24, RenderTextureFormat.ARGBHalf);
            // fix for singlepass issue in u2018

            rt_buff.vrUsage = VRTextureUsage.TwoEyes;

            sheet.properties.SetFloat("_Sharpness", settings.sharpness);
            sheet.properties.SetFloat("_SampleDistance", settings.sampleDistance);
            sheet.properties.SetFloat("_ResizeHeight", settings.resizeHeight);
            sheet.properties.SetFloat("_ResizeWidth", settings.resizeWidth);

            // flip
            context.command.BlitFullscreenTriangle(context.source, rt_buff, sheet, settings.flip ? 4 : 0);
            if (settings.useFXAA)
            {
                // fxaa
                sheet.properties.SetFloat("_Intensity", settings.intensityFXAA);

                RenderTexture rt_buff2 = RenderTexture.GetTemporary(context.width, context.height, 24, RenderTextureFormat.ARGBHalf);

                rt_buff2.vrUsage = VRTextureUsage.TwoEyes;

                context.command.BlitFullscreenTriangle(rt_buff, rt_buff2, sheet, 5);
                context.command.BlitFullscreenTriangle(rt_buff2, context.destination, sheet, settings.shaderPass);
                RenderTexture.ReleaseTemporary(rt_buff2);
            }
            else
            {
                // final
                context.command.BlitFullscreenTriangle(rt_buff, context.destination, sheet, settings.shaderPass);
            }

            RenderTexture.ReleaseTemporary(rt_buff);
        
        }
    }
}
#endif