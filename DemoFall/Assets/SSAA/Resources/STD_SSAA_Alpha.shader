Shader "Hidden/SSAA_Alpha" 
{
	Properties 
	{
		_MainTex ("Texture", 2D) = "" {} 
	}
	SubShader {
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" }
		Blend SrcAlpha OneMinusSrcAlpha
		Pass {
 			ZTest Always Cull Off ZWrite On

			CGPROGRAM
			// include the unityCG.cginc
			#include "UnityCG.cginc"
			#include "Include/_SSAA_Utils.cginc"

			#pragma vertex vert//_img
			#pragma fragment frag

			sampler2D _MainTex;
			float4 _MainTex_ST;

			struct Input
			{
				float4 position : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varying
			{
				float4 position : SV_POSITION;
				float2 texcoord : TEXCOORD0;
				float2 uvSPR : TEXCOORD1; // Single Pass Stereo UVs
			};

			Varying vert(Input input)
			{
				Varying o;
				o.position = UnityObjectToClipPos(input.position);
				o.texcoord = input.uv.xy;
				o.uvSPR = UnityStereoScreenSpaceUVAdjust(input.position.xy, _MainTex_ST);
				return o;
			}

			fixed4 frag(v2f i) : COLOR
			{
				float4 col = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.texcoord, _MainTex_ST)).rgba;
				return float4(col.a,0,0,1);
			}
			ENDCG 

		}Pass {
			ZTest Always Cull Off ZWrite On

			CGPROGRAM
				// include the unityCG.cginc
				#include "UnityCG.cginc"
				#include "Include/_SSAA_Utils.cginc"

				#pragma vertex vert//_img
				#pragma fragment frag

				sampler2D _MainTex;
				float4 _MainTex_ST;

				// Texture from behind
				sampler2D _MainTexA;
				float4 _MainTexA_ST;

				struct Input
				{
					float4 position : POSITION;
					float2 uv : TEXCOORD0;
				};

				struct Varying
				{
					float4 position : SV_POSITION;
					float2 texcoord : TEXCOORD0;
					float2 uvSPR : TEXCOORD1; // Single Pass Stereo UVs
				};

				Varying vert(Input input)
				{
					Varying o;
					o.position = UnityObjectToClipPos(input.position);
					o.texcoord = input.uv.xy;
					o.uvSPR = UnityStereoScreenSpaceUVAdjust(input.position.xy, _MainTex_ST);
					return o;
				}

				fixed4 frag(v2f i) : COLOR
				{
					float4 col = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.texcoord, _MainTex_ST)).rgba;
					col.a = tex2D(_MainTexA, UnityStereoScreenSpaceUVAdjust(i.texcoord, _MainTexA_ST)).r;

					return col;
				}
				ENDCG

			}
	}
	Fallback Off 
}
