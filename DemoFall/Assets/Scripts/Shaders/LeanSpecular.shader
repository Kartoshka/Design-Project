Shader "Custom/LeanSpecular" {
    Properties {
      _MainTex ("Albedo", Color) = (0,0,0,0)
      _Lean1 ("Lean 1", 2D) = "bump" {}
      _Lean2 ("Lean 2", 2D) = "bump" {}
      _LOD("Level of Detail", Float) = 0
      _SC("Scale",Float) = 1
      _SpecExp("Specular Exponent", Float) = 1
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
    
    struct Input {
        float2 uv_MainTex;
        float2 uv_Lean1;
        float2 uv_Lean2;
    };
    
    struct Output{
        fixed3 Albedo;  // diffuse color
        fixed3 Normal;  // tangent space normal, if written
        fixed3 Emission;
        half Specular;  // specular power in 0..1 range
        fixed Gloss;    // specular intensity
        fixed Alpha;    // alpha for transparencies
        float3 M;
        float2 B;
    };
    
    half4 LightingLeanSpecular (Output s, half3 lightDir, half3 viewDir, half atten) {
        half3 h = normalize (lightDir + viewDir);
        half diff = max (0, dot (s.Normal, lightDir));
        
        //Convert M to sigma by Equation 5
        float3 covMat =  s.M - float3(s.B * s.B, s.B.x * s.B.y);
        float detCovMat = covMat.x * covMat.y - covMat.z * covMat.z;
        
        //Calculate projection of halfVector onto the z = 1 plane, and shift
        float2 pH = h.xy/h.z - s.B;
        float e = pH.x * pH.x * covMat.y + pH.y*pH.y*covMat.x - 2*pH.x*pH.y*covMat.z;
        
        
        float nh = max (0, dot (s.Normal, h));
        float spec = 0;
        if(detCovMat > 0)
            spec = exp(-0.5 * e/detCovMat)/sqrt(detCovMat);//s.Specular; //pow (nh, 48.0);

        half4 c;
        c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * spec) * atten;
        c.a = s.Alpha;
        return c;
    }
    
    float4 _MainTex;
    sampler2D _Lean1;
    sampler2D _Lean2;
    
    float _LOD;
    float _SC;
    
    void surf (Input IN, inout Output o) {
        o.Albedo = _MainTex; 
        
        float4 t1;
    #ifdef _AUTO_LOD_OFF
        t1 = tex2Dlod (_Lean1, float4(IN.uv_Lean1.xy,0, _LOD));
    #else
        t1 = tex2D (_Lean1, IN.uv_Lean1.xy);
    #endif
        o.Normal = unpackNormal(t1.xyz);
        
        float4 t2 =  tex2D(_Lean2, IN.uv_Lean2.xy);
        
        float2 B = (2*t2.xy-1) * _SC;
        float3 M =  float3( t2.zw, 2*t1.w - 1) * _SC * _SC;
        
        o.B = B;
        o.M = M;
    }
    
    ENDCG
    }
    
    FallBack "Diffuse"
}
