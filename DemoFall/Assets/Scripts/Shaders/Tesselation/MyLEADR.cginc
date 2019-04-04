#if !defined(MY_LEADR_INCLUDED)
#define MY_LEADR_INCLUDED

#include "My Lighting Input.cginc"

#if !defined(ALBEDO_FUNCTION)
	#define ALBEDO_FUNCTION GetAlbedo
#endif

float3 _Albedo;

sampler2D _Lean1;
float4 _Lean1_ST;
sampler2D _Lean2;
float4 _Lean2_ST;

float _LOD;
float _Rough;
float _Spec;

inline float3 UnpackLeanNormal(float3 n) {
	return float3(2 * n.xy - 1, n.z);
}

// Masking and shadowing term
inline float Lambda(float3 w, float2 B, float3 covMat) {
	// Angles
	float theta = acos(w.z);
	float phi = acos(w.x / sin(theta));
	// Eq 14
	float mu = cos(phi) * B.x + sin(phi) * B.y;
	// Eq 15
	float sigma_sq = cos(phi) * cos(phi) * covMat.x +
					 sin(phi) * sin(phi) * covMat.y +
					 2 * cos(phi) * sin(phi) * covMat.z;
	// extend to Gaussian with approximation Walter et al. (2007)
	float v = (w.z / sin(theta) - mu) / (sqrt(sigma_sq * 2));
	if (v < 1.6)
		return (1 - 1.259 * v + 0.396 * v * v) / (3.535 * v + 2.181 * v * v);
	return 0;
}

//schlick functions
float SchlickFresnel(float LdotH) {
	float x = clamp(1.0 - LdotH, 0.0, 1.0);
	float x2 = x * x;
	return x2 * x2*x;
}

float SchlickIORFresnelFunction(float ior,float LdotH) {
	float f0 = pow((ior - 1) / (ior + 1),2);
	return f0 + (1 - f0) * SchlickFresnel(LdotH);
}



InterpolatorsVertex LEADRVert(VertexData v) {
#ifdef LEADR_MAPPING
	InterpolatorsVertex i;
	UNITY_INITIALIZE_OUTPUT(InterpolatorsVertex, i);

	i.uv.xy = TRANSFORM_TEX(v.uv, _Lean1);

#if VERTEX_DISPLACEMENT
	float displacement = tex2Dlod(_DisplacementMap, float4(i.uv.xy, 0, 0)).g;
	//Allows for negative displacement
	displacement *= _DisplacementStrength;
	v.normal = normalize(v.normal);
	v.vertex.xyz += v.normal * displacement;
#endif

	i.pos = UnityObjectToClipPos(v.vertex);
	i.worldPos.xyz = mul(unity_ObjectToWorld, v.vertex);
	i.normal = UnityObjectToWorldNormal(v.normal);
	i.tangent = UnityObjectToWorldDir(v.tangent.xyz);
	i.binormal = CreateBinormal(i.normal, i.tangent, v.tangent.w);

	UNITY_TRANSFER_SHADOW(i, v.uv1);

	return i;

	//NOT LEADR
#else
	InterpolatorsVertex i;
	UNITY_INITIALIZE_OUTPUT(InterpolatorsVertex, i);
	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_TRANSFER_INSTANCE_ID(v, i);

	i.uv.xy = TRANSFORM_TEX(v.uv, _MainTex);
	i.uv.zw = TRANSFORM_TEX(v.uv, _DetailTex);

#if VERTEX_DISPLACEMENT
	float displacement = tex2Dlod(_DisplacementMap, float4(i.uv.xy, 0, 0)).g;
	//Allows for negative displacement
	displacement = (displacement - 0.5) * _DisplacementStrength;
	v.normal = normalize(v.normal);
	v.vertex.xyz += v.normal * displacement;
#endif

	i.pos = UnityObjectToClipPos(v.vertex);
	i.worldPos.xyz = mul(unity_ObjectToWorld, v.vertex);
#if FOG_DEPTH
	i.worldPos.w = i.pos.z;
#endif
	i.normal = UnityObjectToWorldNormal(v.normal);

#if defined(BINORMAL_PER_FRAGMENT)
	i.tangent = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);
#else
	i.tangent = UnityObjectToWorldDir(v.tangent.xyz);
	i.binormal = CreateBinormal(i.normal, i.tangent, v.tangent.w);
#endif



#if defined(LIGHTMAP_ON) || ADDITIONAL_MASKED_DIRECTIONAL_SHADOWS
	i.lightmapUV = v.uv1 * unity_LightmapST.xy + unity_LightmapST.zw;
#endif

#if defined(DYNAMICLIGHTMAP_ON)
	i.dynamicLightmapUV =
		v.uv2 * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif

	UNITY_TRANSFER_SHADOW(i, v.uv1);

	ComputeVertexLightColor(i);

#if defined (_PARALLAX_MAP)
#if defined(PARALLAX_SUPPORT_SCALED_DYNAMIC_BATCHING)
	v.tangent.xyz = normalize(v.tangent.xyz);
	v.normal = normalize(v.normal);
#endif
	float3x3 objectToTangent = float3x3(
		v.tangent.xyz,
		cross(v.normal, v.tangent.xyz) * v.tangent.w,
		v.normal
		);
	i.tangentViewDir = mul(objectToTangent, ObjSpaceViewDir(v.vertex));
#endif

	return i;
#endif
}

FragmentOutput LEADRFrag(Interpolators i) 
{
#ifdef LEADR_MAPPING
	fixed3 lightDir = _WorldSpaceLightPos0.xyz;
	float3 viewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));

	// Unpack Lean Maps

#ifdef _AUTO_LOD_OFF
	float4 t1 = tex2Dlod(_Lean1, float4(i.uv.xy,0, _LOD));
	float4 t2 = tex2Dlod(_Lean2, float4(i.uv.xy,0, _LOD));
#else
	float4 t1 = tex2D(_Lean1, i.uv.xy);
	float4 t2 = tex2D(_Lean2, i.uv.xy);
#endif
	float3 normal = UnpackLeanNormal(t1.xyz);

	//Build B and M matrix
	float2 B = (2 * t2.xy - 1);
	float3 M = float3(t2.zw, 2 * t1.w - 1);

	//normal = fixed3(dot(i.tSpace0, normal), dot(i.tSpace1, normal), dot(i.tSpace2, normal));
	viewDir = normalize(float3(dot(i.tangent, viewDir), dot(i.binormal, viewDir), dot(i.normal,viewDir)));
	lightDir = normalize(float3(dot(i.tangent, lightDir), dot(i.binormal, lightDir), dot(i.normal,lightDir)));

	half3 h = normalize(lightDir + viewDir);

	//Convert M to sigma by Equation 5
	float3 covMat = M - float3(B.x * B.x, B.y * B.y, B.x * B.y);
	covMat.xy += 0.5f * _Rough * _Rough;
	float det = covMat.x * covMat.y - covMat.z * covMat.z;

	//Calculate projection of halfVector onto the z = 1 plane, and shift
	float2 pH = h.xy / h.z - B;
	float e = pH.x * pH.x * covMat.y + pH.y*pH.y*covMat.x - 2 * pH.x*pH.y*covMat.z;

	float P22 = 0;
	if (det > 0)
		P22 = exp(-0.5 * e / det) / (sqrt(det) * UNITY_TWO_PI);

	// Calculate normal distribution function D following anisotropic Beckmann formulation  
	// Eq (10)
	float D = P22 / pow(h.z, 4);

	// Microfacet theory, micronormals average to mesonormal 
	// Eq (11) TODO: NEGATIVE????
	float3 mesonormal = normalize(float3(B.x, B.y, 1));

	// Use Shlick approximation for Fresnel term
	float F = SchlickIORFresnelFunction(_Spec, dot(lightDir, h));

	// Specular surface shading formulation for LEADR
	// Eq (17)
	float spec = (mesonormal.z / dot(mesonormal, viewDir))* (_LightColor0.rgb * F * D) / (4 * (1 + Lambda(viewDir, B, covMat) + Lambda(lightDir, B, covMat)));

	half4 c;
	c.rgb = (_Albedo * _LightColor0.rgb * saturate(dot(lightDir, normal)) + spec);
	c.a = 1.0f;

	FragmentOutput output;
	output.color = c;
	return output;
#else

	FragmentOutput output;
	output.color = float4(1.0,0,0,1.0);
	return output;
#endif
}

#endif