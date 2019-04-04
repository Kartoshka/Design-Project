// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/Regular/SimpleSpecular"
{
	Properties
	{
        _Albedo ("Albedo", Color) = (0,0,0,0)
        _NormalMap ("Normal Map", 2D) = "white" {}
        _LOD("Level of Detail", Float) = 0
        _Spec("Specular Exponent", Float) = 48
        [MaterialToggle] _AUTO_LOD("Automatic LOD", Float) = 0
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

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc" // for _LightColor0

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
            };
           
			float3 _Albedo;
            sampler2D _NormalMap;
            float4 _NormalMap_ST;
            float _LOD;
            float _Spec;
            
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
                fixed3 worldBinormal = cross(worldNormal, worldTangent)* tangentSign;
                
                o.tSpace0 = worldTangent;
                o.tSpace1 = worldBinormal;
                o.tSpace2 = worldNormal;

                o.worldPos.xyz = worldPos;
                o.normal = v.normal;

                return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
                fixed3 lightDir = _WorldSpaceLightPos0.xyz;
                float3 viewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
                
            #ifdef _AUTO_LOD_OFF
                fixed3 normal = UnpackNormal (tex2Dlod (_NormalMap, float4(i.uv ,0, _LOD)));
            #else      
               fixed3 normal = UnpackNormal(tex2D(_NormalMap, i.uv));
            #endif
                
                //normal = fixed3(dot(i.tSpace0, normal), dot(i.tSpace1, normal), dot(i.tSpace2, normal));
                viewDir = float3(dot(i.tSpace0, viewDir),dot(i.tSpace1, viewDir),dot(i.tSpace2,viewDir));
                lightDir = float3(dot(i.tSpace0, lightDir),dot(i.tSpace1, lightDir),dot(i.tSpace2,lightDir));

                half3 h = normalize (lightDir + viewDir);

                half diff = max (0, dot (normal, lightDir));

                float nh = max (0, dot (normal, h));
                float spec = pow (nh, _Spec);
				float norm = _Spec / (UNITY_TWO_PI);
                
                half4 c;
				c.rgb = (_Albedo*_LightColor0.rgb * saturate(dot(normal, lightDir)) + _LightColor0.rgb * spec * norm);
                c.a = 1.0f;
                return c;
			}
			ENDCG
		}
	}
}
