using System;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_2018_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2 && SSAA_HDRP
using UnityEngine.Rendering.PostProcessing;
namespace MadGoat_SSAA
{
    [Serializable]
    [PostProcess(typeof(SsaaAlphaGrabHandlerRenderer), PostProcessEvent.BeforeStack, "SSAA/SSAA_Alpha_Copy")]
    public sealed class SsaaAlphaGrabHandler : PostProcessEffectSettings
    {
        public UnityEngine.Rendering.PostProcessing.TextureParameter destTex = new UnityEngine.Rendering.PostProcessing.TextureParameter { value = null, overrideState = true };

    }

    public sealed class SsaaAlphaGrabHandlerRenderer : PostProcessEffectRenderer<SsaaAlphaGrabHandler>
    {
        public override void Render(PostProcessRenderContext context)
        {
            var destId = new RenderTargetIdentifier(settings.destTex);
            var sheet = context.propertySheets.Get(Shader.Find("Hidden/SSAA/SRP_SSAA_UBER"));
            
            context.command.BlitFullscreenTriangle(context.source, destId,sheet,6);
        }
    }



    [Serializable]
    [PostProcess(typeof(SsaaAlphaPasteHandlerRenderer), PostProcessEvent.BeforeStack, "SSAA/SSAA_Alpha_Paste")]
    public sealed class SsaaAlphaPasteHandler : PostProcessEffectSettings
    {
        public UnityEngine.Rendering.PostProcessing.TextureParameter alphaTex = new UnityEngine.Rendering.PostProcessing.TextureParameter { value = null, overrideState = true };

    }

    public sealed class SsaaAlphaPasteHandlerRenderer : PostProcessEffectRenderer<SsaaAlphaPasteHandler>
    {
        public override void Render(PostProcessRenderContext context)
        {
            var alphaId = new RenderTargetIdentifier(settings.alphaTex);
            var sheet = context.propertySheets.Get(Shader.Find("Hidden/SSAA/SRP_SSAA_UBER"));
            context.command.SetGlobalTexture("_MainTexA", alphaId);
            context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 6);
        }
    }
}
#endif