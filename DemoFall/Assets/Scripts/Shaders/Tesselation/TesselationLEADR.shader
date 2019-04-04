Shader "Custom/LEADRTesselation" {

	Properties {
		_Albedo("Albedo", Color) = (0,0,0,0)
		_Lean1("Lean 1", 2D) = "bump" {}
		_Lean2("Lean 2", 2D) = "bump" {}

		_ParallaxMap("Displacement Map", 2D) = "black" {}
		_ParallaxStrength("Displacement Strength", Range(0, 1)) = 0

		_TessellationUniform("Tessellation Uniform", Range(1, 256)) = 1

		_LOD("Level of Detail", Float) = 0
		_Rough("Roughness", Float) = 0
		_Spec("Refractive Index", Range(0,1)) = 0.5
		[MaterialToggle] _AUTO_LOD("Automatic LOD", Float) = 0

		[HideInInspector] _SrcBlend ("_SrcBlend", Float) = 1
		[HideInInspector] _DstBlend ("_DstBlend", Float) = 0
		[HideInInspector] _ZWrite ("_ZWrite", Float) = 1
	}

		CGINCLUDE

			#define VERTEX_DISPLACEMENT_INSTEAD_OF_PARALLAX
			#define LEADR_MAPPING

			ENDCG

			SubShader{

				Pass {
					Tags {
						"LightMode" = "ForwardBase"
					}
					Blend[_SrcBlend][_DstBlend]
					ZWrite[_ZWrite]

					CGPROGRAM

					#pragma target 4.6

					#pragma shader_feature _ _RENDERING_CUTOUT _RENDERING_FADE _RENDERING_TRANSPARENT
					#pragma shader_feature _METALLIC_MAP
					#pragma shader_feature _ _SMOOTHNESS_ALBEDO _SMOOTHNESS_METALLIC
					#pragma shader_feature _NORMAL_MAP
					#pragma shader_feature _PARALLAX_MAP
					#pragma shader_feature _OCCLUSION_MAP
					#pragma shader_feature _EMISSION_MAP
					#pragma shader_feature _DETAIL_MASK
					#pragma shader_feature _DETAIL_ALBEDO_MAP
					#pragma shader_feature _DETAIL_NORMAL_MAP

					#pragma multi_compile _ LOD_FADE_CROSSFADE

					#pragma multi_compile_fwdbase
					#pragma multi_compile_fog

					#pragma vertex MyTessellationVertexProgram
					#pragma fragment LEADRFrag
					#pragma hull MyHullProgram
					#pragma domain LEADRDomain

					#define FORWARD_BASE_PASS

					#include "My Lighting.cginc"
					#include "MyLEADR.cginc"
					#include "MyTessellation.cginc"

			ENDCG
			}
		}

	//CustomEditor "MyLightingShaderGUI"
}