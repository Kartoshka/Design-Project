Shader "Hidden/SSAA/SRP_SSAA_UBER"
{
	HLSLINCLUDE
#include "Include/_SSAA_Utils.cginc"

	#include "Include/PostProcessingStack/StdLib.hlsl"
	#include "Include/PostProcessingStack/Colors.hlsl"
	#include "Include/PostProcessingStack/Dithering.hlsl"

	//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~DOWNSAMPLERS~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
	// Main Tex
	TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
	TEXTURE2D_SAMPLER2D(_SourceTex, sampler_SourceTex);

	// Properties
	float _Sharpness;
	float _SampleDistance;
	float _ResizeHeight;
	float _ResizeWidth;

	float4 FragSSAAFlip(VaryingsDefault i) : SV_Target
	{
		float2 uv = float2(i.texcoord.x, 1 - i.texcoord.y);
		float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

		return color;
	}

	// samplers
	float4 FragSSAADefault(VaryingsDefault i) : SV_Target
	{
		float4 color = SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, i.texcoordStereo);

		// fixed overlapping cameras
		
		float4 source = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoordStereo);
		//return color;
		return lerp(source, color, saturate(color.a));
	}

	float4 FragSSAANearest(VaryingsDefault i) : SV_Target
	{
		float2 uv = float2(i.texcoordStereo.x * _ResizeWidth, i.texcoordStereo.y * _ResizeHeight);
		float2 f = frac(uv);
		uv = float2(floor(uv.x) / _ResizeWidth, floor(uv.y) / _ResizeHeight);

		float4 color = lerp(
			SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, uv).rgba,
			SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, i.texcoordStereo).rgba,
			_Sharpness);

		// fixed overlapping cameras
		float4 source = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoordStereo);
		//return source;
		//return color;
		return lerp(source, color, saturate(color.a));
	}
	float4 FragSSAABilinear(VaryingsDefault i) : SV_Target
	{
		float squareW = (_SampleDistance / _ResizeWidth);
		float squareH = (_SampleDistance / _ResizeHeight);

		// neighbor pixels
		float4 top = SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, i.texcoordStereo + float2(0.0f, -squareH));
		float4 left = SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, i.texcoordStereo + float2(-squareW, 0.0f));
		float4 mid = SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, i.texcoordStereo + float2(0.0f, 0.0f));
		float4 right = SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, i.texcoordStereo + float2(squareW, 0.0f));
		float4 bot = SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, i.texcoordStereo + float2(0.0f, squareH));
		
		// avg
		float4 sampleaverage = (top + left + right + bot) / 4;

		// lerp based on sharpness
		float4 color = lerp(sampleaverage, mid, _Sharpness);

		// fixed overlapping cameras
		float4 source = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoordStereo);
		//return source;
		//return color;
		return lerp(source, color, saturate(mid.a));
	}
	float4 FragSSAABicubic(VaryingsDefault i) : SV_Target
	{
		float4 mid = SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, i.texcoordStereo);
		float4 tex = float4(1 / _ResizeWidth, 1 / _ResizeHeight, _ResizeWidth, _ResizeHeight);

		float2 uv = i.texcoordStereo * tex.zw + 0.5;

		float2 iuv = floor(uv);
		float2 fuv = frac(uv);

		float ampl0x = ampl0(fuv.x);
		float ampl1x = ampl1(fuv.x);
		float off0x = off0(fuv.x);
		float off1x = off1(fuv.x);
		float off0y = off0(fuv.y);
		float off1y = off1(fuv.y);

		float2 pixel0 = (float2(iuv.x + off0x, iuv.y + off0y) - 0.5) * tex.xy;
		float2 pixel1 = (float2(iuv.x + off1x, iuv.y + off0y) - 0.5) * tex.xy;
		float2 pixel2 = (float2(iuv.x + off0x, iuv.y + off1y) - 0.5) * tex.xy;
		float2 pixel3 = (float2(iuv.x + off1x, iuv.y + off1y) - 0.5) * tex.xy;

		float4 col = ampl0(fuv.y) * (
			ampl0x * SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, pixel0) +
			ampl1x * SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, pixel1)) +
			ampl1(fuv.y) * (ampl0x * SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, pixel2) +
			ampl1x * SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, pixel3));
		float4 color = lerp(col,mid,_Sharpness);

		// fixed overlapping cameras
		float4 source = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoordStereo);
		//return source;
		//return color;
		return lerp(source, color, saturate(color.a));
	}


	//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~FXAA~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
	// PS3 and XBOX360 aren't supported in Unity anymore, only use the PC variant
	#define FXAA_PC 1

	// Luma hasn't been encoded in alpha
	#define FXAA_GREEN_AS_LUMA 1
	#define FXAA_QUALITY__PRESET 28
	#define FXAA_QUALITY_SUBPIX 1.0
	#define FXAA_QUALITY_EDGE_THRESHOLD 0.063
	#define FXAA_QUALITY_EDGE_THRESHOLD_MIN 0.0312

	#include "Include/SRP_SSAA_FXAA.hlsl"

	float4 _MainTex_TexelSize;
	float _Intensity;
	float4 FragFXAA(VaryingsDefault i) : SV_Target
	{
		half4 color = 0.0;

		// Fast Approximate Anti-aliasing

		#if FXAA_HLSL_4 || FXAA_HLSL_5
			FxaaTex mainTex;
			mainTex.tex = _MainTex;
			mainTex.smpl = sampler_MainTex;
		#else
			FxaaTex mainTex = _MainTex;
		#endif

		color = FxaaPixelShader(
			i.texcoordStereo,                 // pos
			0.0,                        // fxaaConsolePosPos (unused)
			mainTex,                    // tex
			mainTex,                    // fxaaConsole360TexExpBiasNegOne (unused)
			mainTex,                    // fxaaConsole360TexExpBiasNegTwo (unused)
			_MainTex_TexelSize.xy,      // fxaaQualityRcpFrame
			0.0,                        // fxaaConsoleRcpFrameOpt (unused)
			0.0,                        // fxaaConsoleRcpFrameOpt2 (unused)
			0.0,                        // fxaaConsole360RcpFrameOpt2 (unused)
			FXAA_QUALITY_SUBPIX,
			FXAA_QUALITY_EDGE_THRESHOLD,
			FXAA_QUALITY_EDGE_THRESHOLD_MIN,
			0.0,                        // fxaaConsoleEdgeSharpness (unused)
			0.0,                        // fxaaConsoleEdgeThreshold (unused)
			0.0,                        // fxaaConsoleEdgeThresholdMin (unused)
			0.0                         // fxaaConsole360ConstDir (unused)
		);


		float4 main = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoordStereo);
		color.a = main.a;

		color.rgb = Dither(color.rgb, i.texcoordStereo);
		return lerp(main,color,_Intensity);
	}

	ENDHLSL

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass // Default pass
		{
			HLSLPROGRAM
			#if UNITY_2018_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2
				#pragma vertex VertDefault
				#pragma fragment FragSSAADefault
			#endif
			ENDHLSL
		}	
		Pass // Nearest neighbor
		{
			HLSLPROGRAM
			#if UNITY_2018_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2
				#pragma vertex VertDefault
				#pragma fragment FragSSAANearest
			#endif
			ENDHLSL
		}
		Pass // Bilinear
		{
			HLSLPROGRAM
			#if UNITY_2018_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2
				#pragma vertex VertDefault
				#pragma fragment FragSSAABilinear
			#endif
			ENDHLSL
		}
		Pass // Bicubic
		{
			HLSLPROGRAM
			#if UNITY_2018_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2
				#pragma vertex VertDefault
				#pragma fragment FragSSAABicubic
			#endif
			ENDHLSL
		}
		Pass // flip
		{
			HLSLPROGRAM
			#if UNITY_2018_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2
				#pragma vertex VertDefault
				#pragma fragment FragSSAAFlip
			#endif
			ENDHLSL
		}
		Pass // fxaa
		{
			HLSLPROGRAM
			#if UNITY_2018_1_OR_NEWER && UNITY_POST_PROCESSING_STACK_V2
				#pragma vertex VertDefault
				#pragma fragment FragFXAA
				#pragma target 3.0
			#endif
			ENDHLSL
		}
	}
}