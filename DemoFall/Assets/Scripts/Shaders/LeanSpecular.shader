Shader "Custom/LeanSpecular" {
    Properties {
      _MainTex ("Albedo", Color) = (0,0,0,0)
      _Lean1 ("Lean 1", 2D) = "bump" {}
      _Lean2 ("Lean 2", 2D) = "bump" {}
      _LOD("Level of Detail", Float) = 0
      [MaterialToggle] _AUTO_LOD("Automatic LOD", Float) = 0
    }
 
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200

    CGPROGRAM
    #pragma surface surf LeanSpecular
    #pragma multi_compile  _AUTO_LOD_OFF _AUTO_LOD_ON
    
        
    inline float3 unpackNormal(float3 n){
        return float3(2 * n.xy - 1, n.z);
    }
    
    half4 LightingLeanSpecular (SurfaceOutput s, half3 lightDir, half3 viewDir, half atten) {
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
        float2 uv_Lean1;
        float2 uv_Lean2;
    };
    
    float4 _MainTex;
    sampler2D _Lean1;
    sampler2D _Lean2;
    
    float _LOD;
    
    void surf (Input IN, inout SurfaceOutput o) {
    o.Albedo = _MainTex; 
    float4 t1;
    #ifdef _AUTO_LOD_OFF
        t1 = tex2Dlod (_Lean1, float4(IN.uv_Lean1.xy,0, _LOD));
    #else
        t1 = tex2D (_Lean1, IN.uv_Lean1.xy);
    #endif
        o.Normal =unpackNormal(t1.xyz);//float4(2 * t1.xy -1, t1.z, 1.0f);
    }
    
    ENDCG
    }
    
    FallBack "Diffuse"
}
