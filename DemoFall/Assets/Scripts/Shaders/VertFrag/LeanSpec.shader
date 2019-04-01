// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/LEAN"
{
	Properties
	{
        _Albedo ("Albedo", Color) = (0,0,0,0)
        _Lean1 ("Lean 1", 2D) = "bump" {}
        _Lean2 ("Lean 2", 2D) = "bump" {}
        _LOD("Level of Detail", Float) = 0
        _Spec("Specular Exponent", Float) = 48
        [MaterialToggle] _AUTO_LOD("Automatic LOD", Float) = 0
		[MaterialToggle] _BECKMANN("Toggle use of Beckmann Distribution", Float) = 0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
        Tags{ "LightMode" = "ForwardBase" }
		LOD 100

		Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
            #pragma multi_compile  _AUTO_LOD_OFF _AUTO_LOD_ON 
			#pragma multi_compile _BECKMANN_OFF _BECKMANN_ON
            
			#include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc" // for _LightColor0
            
            inline float3 UnpackLeanNormal(float3 n){
                return float3(2 * n.xy - 1, n.z);
            }
    
			struct appdata
			{
				float4 vertex : POSITION;
                float4 normal : NORMAL;
                float4 tangent : TANGENT;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2   uv : TEXCOORD0;
				float4 pos : SV_POSITION;
                
                float3 tSpace0 : TEXCOORD1;
                float3 tSpace1 : TEXCOORD2;
                float3 tSpace2 : TEXCOORD3;
                
                float3 worldPos : TEXCOORD4;
                float3 normal : TEXCOORD5;
            };
           
			float3 _Albedo;
            
            sampler2D _Lean1;
            float4 _Lean1_ST;
            sampler2D _Lean2;
            float4 _Lean2_ST;
            
            float _LOD;
            float _Spec;
            float _Correction;
            
			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _Lean1);
				UNITY_TRANSFER_FOG(o,o.vertex);
                
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
               
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);
                fixed3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
                fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
                fixed3 worldBinormal = cross(worldNormal, worldTangent)* tangentSign;
                
                o.tSpace0 = worldTangent;
                o.tSpace1 = worldBinormal;
                o.tSpace2 = worldNormal;
                o.worldPos.xyz = worldPos;
                
                return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
                fixed3 lightDir = _WorldSpaceLightPos0.xyz;
                float3 viewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
                
                // Unpack Lean Maps
                
            #ifdef _AUTO_LOD_OFF
                float4 t1 = tex2Dlod (_Lean1, float4(i.uv,0, _LOD));
                float4 t2 = tex2Dlod (_Lean2, float4(i.uv,0, _LOD));
            #else
                float4 t1 = tex2D (_Lean1, i.uv.xy);
                float4 t2 = tex2D (_Lean2, i.uv.xy);
            #endif
                float3 normal = UnpackLeanNormal(t1.xyz);
                
                //Build B and M matrix
                float2 B = (2*t2.xy-1);
                float3 M =  float3( t2.zw, 2*t1.w - 1);
                
                //normal = fixed3(dot(i.tSpace0, normal), dot(i.tSpace1, normal), dot(i.tSpace2, normal));
                viewDir = float3(dot(i.tSpace0, viewDir), dot(i.tSpace1, viewDir), dot(i.tSpace2,viewDir));
                lightDir = float3(dot(i.tSpace0, lightDir), dot(i.tSpace1, lightDir), dot(i.tSpace2,lightDir));
                
                half3 h = normalize (lightDir + viewDir);
                    
                //Convert M to sigma by Equation 5
                float3 covMat =  M - float3(B.x * B.x, B.y * B.y, B.x * B.y);
				covMat.xy += 1.0f / _Spec;
                float det = covMat.x * covMat.y - covMat.z * covMat.z;
                
                //Calculate projection of halfVector onto the z = 1 plane, and shift
                float2 pH = h.xy/h.z - B;
                float e = pH.x * pH.x * covMat.y + pH.y*pH.y*covMat.x - 2*pH.x*pH.y*covMat.z;
        
                float spec = 0;
			#ifdef _BECKMANN_ON
                if(det > 0)
                    spec = exp(-0.5 * e/det)/(sqrt(det) * UNITY_TWO_PI);
			#else
				float nh = max(0, dot(normal, h));
				float norm = _Spec / (UNITY_TWO_PI);
				spec = pow(nh, _Spec) * norm;
			#endif
                 
                half4 c;
                c.rgb = (_Albedo*_LightColor0.rgb* saturate(dot(lightDir, normal)) + _LightColor0.rgb * spec);
                c.a = 1.0f;
                
                return c;
			}
			ENDCG
		}
	}
}
