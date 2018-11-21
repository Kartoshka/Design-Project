Shader "Custom/SpecularSimple" {
    Properties {
      _MainTex ("Albedo", Color) = (0,0,0,0)
      _BumpMap ("Bumpmap", 2D) = "bump" {}
      _LOD("Level of Detail", Float) = 0
      [MaterialToggle] _AUTO_LOD("Automatic LOD", Float) = 0
    }
 
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

    CGPROGRAM
    #pragma surface surf SimpleSpecular
    #pragma multi_compile  _AUTO_LOD_OFF _AUTO_LOD_ON

    half4 LightingSimpleSpecular (SurfaceOutput s, half3 lightDir, half3 viewDir, half atten) {
        half3 h = normalize (lightDir + viewDir);

        half diff = max (0, dot (s.Normal, lightDir));

        float nh = max (0, dot (s.Normal, h));
        float spec = pow (nh, 48.0);

        half4 c;
        c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * spec) * atten;
        c.a = s.Alpha;
        return c;
    }

    struct Input {
        float2 uv_MainTex;
        float2 uv_BumpMap;
    };
    
    float4 _MainTex;
    sampler2D _BumpMap;
    float _LOD;
    
    void surf (Input IN, inout SurfaceOutput o) {
        o.Albedo = _MainTex; 
    #ifdef _AUTO_LOD_OFF
        o.Normal = UnpackNormal (tex2Dlod (_BumpMap, float4(IN.uv_BumpMap.xy,0, _LOD)));
    #else
        o.Normal = UnpackNormal (tex2D (_BumpMap, IN.uv_BumpMap.xy));
    #endif

    }
    ENDCG
	}
    
	FallBack "Diffuse"
}
