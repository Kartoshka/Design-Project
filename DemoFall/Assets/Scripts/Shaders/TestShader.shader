Shader "Unlit/TestShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
// Upgrade NOTE: excluded shader from DX11; has structs without semantics (struct v2f members tSpace0,tSpace1,tSpace2)
#pragma exclude_renderers d3d11
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"
            #include "UnityShaderVariables.cginc"
            #include "UnityShaderUtilities.cginc"
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            #include "HLSLSupport.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
                float4 normal : NORMAL;
                float4 tangent : TANGENT;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 pos : SV_POSITION;
                float3 normal: TEXCOORD5;
                float3 tSpace0 : TEXCOORD1;
                float3 tSpace1 : TEXCOORD2;
                float3 tSpace2 :TEXCOORD3;
                float3 worldPos : TEXCOORD4;
                UNITY_LIGHTING_COORDS(5,6)
            };

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
            sampler2D _NormalMap;
            float4 _NormalMap_ST;
            
			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _NormalMap);
				UNITY_TRANSFER_FOG(o,o.vertex);
                
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);
                fixed3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
                fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
                fixed3 worldBinormal = cross(worldNormal, worldTangent) * tangentSign;
                
                o.tSpace0 = worldTangent;//float3(worldTangent.x, worldBinormal.x, worldNormal.x);
                o.tSpace1 = worldBinormal;//float3(worldTangent.y, worldBinormal.y, worldNormal.y);
                o.tSpace2 = worldNormal;//float3(worldTangent.z, worldBinormal.z, worldNormal.z);
                o.worldPos.xyz = worldPos;
                o.normal = worldNormal;
                UNITY_TRANSFER_LIGHTING(o,v.texcoord1.xy); // pass shadow and, possibly, light cookie coordinates to pixel shader

                return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
                fixed3 lightDir = _WorldSpaceLightPos0.xyz;
                
                fixed3 normal = UnpackNormal(tex2D(_NormalMap, i.uv));
                float3 worldN;
                worldN = normalize(worldN);
                float3 transform  = lightDir;
				// sample the texture
				fixed4 col = float4(dot(i.tSpace0, transform),dot(i.tSpace1, transform),dot(i.tSpace2,transform),1.0);//tex2D(_MainTex, i.uv);

				return col;
			}
			ENDCG
		}
	}
}
