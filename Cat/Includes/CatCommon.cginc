#if !defined(CAT_COMMON_INCLUDED)
#define CAT_COMMON_INCLUDED
#include "UnityCG.cginc"

// does literally nothing (NOP = Null OPeration ):
#define NOP 1==2

#define CAT_DECLARE_OUTPUT(type,name) 	\
	type name;							\
	UNITY_INITIALIZE_OUTPUT(type,name);	\

// HLSL attributes
#if defined(UNITY_COMPILER_HLSL)
	#define UNITY_UNROLL_N(n)	[unroll(n)]
#else
	#define UNITY_UNROLL_N(n)
#endif

inline  float2 transformTex(float2 uv, float4 mapST) {
	return uv * mapST.xy + mapST.zw;
}

inline half DotPositive(half2 a, half2 b) {
	#if (SHADER_TARGET < 30 || defined(SHADER_API_PS3))
		return saturate(dot(a, b));
	#else
		return max(0, dot(a, b));
	#endif
}
inline half DotPositive(half3 a, half3 b) {
	#if (SHADER_TARGET < 30 || defined(SHADER_API_PS3))
		return saturate(dot(a, b));
	#else
		return max(0, dot(a, b));
	#endif
}


inline half Pow2 (half x) {
	return x*x;
}
inline half2 Pow2 (half2 x) {
	return x*x;
}
inline half3 Pow2 (half3 x) {
	return x*x;
}
inline half4 Pow2 (half4 x) {
	return x*x;
}
//----------------------------------------------------------------------------------------------------------------------
inline half Pow3(half x) {
	return x*x*x;
}
inline half2 Pow3(half2 x) {
	return x*x*x;
}
inline half3 Pow3(half3 x) {
	return x*x*x;
}
inline half4 Pow3(half4 x) {
	return x*x*x;
}
//----------------------------------------------------------------------------------------------------------------------
inline half Pow8(half x) {
	x = x*x;	x = x*x;	return x*x;
}
inline half2 Pow8(half2 x) {
	x = x*x;	x = x*x;	return x*x;
}
inline half3 Pow8(half3 x) {
	x = x*x;	x = x*x;	return x*x;
}
inline half4 Pow8(half4 x) {
	x = x*x;	x = x*x;	return x*x;
}
//----------------------------------------------------------------------------------------------------------------------
#if !defined(UNITY_STANDARD_BRDF_INCLUDED)
inline half Pow4 (half x) {
	return x*x * x*x;
}
inline half2 Pow4 (half2 x) {
	return x*x * x*x;
}
inline half3 Pow4 (half3 x) {
	return x*x * x*x;
}
inline half4 Pow4 (half4 x) {
	return x*x * x*x;
}
//----------------------------------------------------------------------------------------------------------------------
inline half Pow5 (half x) {
	return x*x * x*x * x;
}
inline half2 Pow5 (half2 x) {
	return x*x * x*x * x;
}
inline half3 Pow5 (half3 x) {
	return x*x * x*x * x;
}
inline half4 Pow5 (half4 x) {
	return x*x * x*x * x;
}
//----------------------------------------------------------------------------------------------------------------------
inline half3 Unity_SafeNormalize(half3 inVec)
{
	half dp3 = max(0.001f, dot(inVec, inVec));
	return inVec * rsqrt(dp3);
}
#endif

inline half2 Unity_SafeNormalize(half2 inVec)
{
	half dp3 = max(0.001f, dot(inVec, inVec));
	return inVec * rsqrt(dp3);
}
//----------------------------------------------------------------------------------------------------------------------
// float InverseLerp(float a, float b, float value)
// t = (a-value)/(a-b)
// return t
#if !defined(UNITY_COMPILER_HLSL2GLSL)
inline half InvLerp(half a, half b, half value) {
	return (a-value)/(a-b);
}
inline half2 InvLerp(half2 a, half2 b, half2 value) {
	return (a-value)/(a-b);
}
inline half3 InvLerp(half3 a, half3 b, half3 value) {
	return (a-value)/(a-b);
}
inline half4 InvLerp(half4 a, half4 b, half4 value) {
	return (a-value)/(a-b);
}
#endif

inline float InvLerp(float a, float b, float value) {
	return (a-value)/(a-b);
}
inline float2 InvLerp(float2 a, float2 b, float2 value) {
	return (a-value)/(a-b);
}
inline float3 InvLerp(float3 a, float3 b, float3 value) {
	return (a-value)/(a-b);
}
inline float4 InvLerp(float4 a, float4 b, float4 value) {
	return (a-value)/(a-b);
}
//----------------------------------------------------------------------------------------------------------------------
#if !defined(UNITY_COMPILER_HLSL2GLSL)
inline half InvLerpSat(half a, half b, half value) {
	return saturate(InvLerp(a, b, value));
}
inline half2 InvLerpSat(half2 a, half2 b, half2 value) {
	return saturate(InvLerp(a, b, value));
}
inline half3 InvLerpSat(half3 a, half3 b, half3 value) {
	return saturate(InvLerp(a, b, value));
}
inline half4 InvLerpSat(half4 a, half4 b, half4 value) {
	return saturate(InvLerp(a, b, value));
}
#endif

inline float InvLerpSat(float a, float b, float value) {
	return saturate(InvLerp(a, b, value));
}
inline float2 InvLerpSat(float2 a, float2 b, float2 value) {
	return saturate(InvLerp(a, b, value));
}
inline float3 InvLerpSat(float3 a, float3 b, float3 value) {
	return saturate(InvLerp(a, b, value));
}
inline float4 InvLerpSat(float4 a, float4 b, float4 value) {
	return saturate(InvLerp(a, b, value));
}
//----------------------------------------------------------------------------------------------------------------------
#if !defined(UNITY_COMPILER_HLSL2GLSL)
inline half MaxC(half2 a) {
	return max(a.x, a.y);
}
inline half MaxC(half3 a) {
	return max(a.x,  max(a.y, a.z));
}
inline half MaxC(half4 a) {
	return max(MaxC(a.xy), MaxC(a.zw));
}
#endif
inline float MaxC(float2 a) {
	return max(a.x, a.y);
}
inline float MaxC(float3 a) {
	return max(a.x,  max(a.y, a.z));
}
inline float MaxC(float4 a) {
	return max(MaxC(a.xy), MaxC(a.zw));
}
//----------------------------------------------------------------------------------------------------------------------
#if !defined(UNITY_COMPILER_HLSL2GLSL)
inline half MinC(half2 a) {
	return min(a.x, a.y);
}
inline half MinC(half3 a) {
	return min(a.x,  min(a.y, a.z));
}
inline half MinC(half4 a) {
	return min(MinC(a.xy), MinC(a.zw));
}
#endif
inline float MinC(float2 a) {
	return min(a.x, a.y);
}
inline float MinC(float3 a) {
	return min(a.x,  min(a.y, a.z));
}
inline float MinC(float4 a) {
	return min(MinC(a.xy), MinC(a.zw));
}
//----------------------------------------------------------------------------------------------------------------------
float noiseSimple(float2 n) {
	float m = frac(dot(float2(12.9898, 78.233), n));
	// Logistics Map equation:
	m = 4.00 * (m - m * m);
	m = 4.00 * (m - m * m);
	m = 4.00 * (m - m * m);
	float res = frac(m * 127.65321) * 2 - 1;
	return res;//*abs(res);
	//return frac( (m) * 48) * 2 - 1;
}
//----------------------------------------------------------------------------------------------------------------------

// Tranforms direction from object to camera space
inline float3 Cat_ObjectToViewDir(float3 dir) {
	return UnityObjectToViewPos(dir);
}
//----------------------------------------------------------------------------------------------------------------------
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
	#define CAT_ARGS_DEPTHT_EXTURE(tex) Texture2DArray tex, SamplerState sampler##tex
	#define CAT_PASS_DEPTHT_EXTURE(tex) tex, sampler##tex
#else
	#define CAT_ARGS_DEPTHT_EXTURE(tex) sampler2D_float tex
	#define CAT_PASS_DEPTHT_EXTURE(tex) tex
#endif
	float sampleDepthFunc    (CAT_ARGS_DEPTHT_EXTURE(depthTexture), float2 uv) {
		return SAMPLE_DEPTH_TEXTURE(depthTexture, uv);
	}
	float sampleDepthProjFunc(CAT_ARGS_DEPTHT_EXTURE(depthTexture), float4 uv) {
		return SAMPLE_DEPTH_TEXTURE_PROJ(depthTexture, UNITY_PROJ_COORD(uv));
	}
	float sampleDepthLodFunc (CAT_ARGS_DEPTHT_EXTURE(depthTexture), float2 uv, float lod) {
		return SAMPLE_DEPTH_TEXTURE_LOD(depthTexture, float4(uv, 0, lod));
	}
	
	#define sampleDepth(depthTexture, uv) sampleDepthFunc(CAT_PASS_DEPTHT_EXTURE(depthTexture), uv)
	#define sampleDepthProj(depthTexture, uv) sampleDepthProjFunc(CAT_PASS_DEPTHT_EXTURE(depthTexture), uv)
	#define sampleDepthLod(depthTexture, uv, lod) sampleDepthLodFunc(CAT_PASS_DEPTHT_EXTURE(depthTexture), uv, lod)
	
	float sampleEyeDepthFunc    (CAT_ARGS_DEPTHT_EXTURE(depthTexture), float2 uv) {
		return LinearEyeDepth(sampleDepth(depthTexture, uv));
	}
	float sampleEyeDepthProjFunc(CAT_ARGS_DEPTHT_EXTURE(depthTexture), float4 uv) {
		return LinearEyeDepth(sampleDepthProj(depthTexture, uv));
	}
	float sampleEyeDepthLodFunc (CAT_ARGS_DEPTHT_EXTURE(depthTexture), float2 uv, float lod) {
		return LinearEyeDepth(sampleDepthLod(depthTexture, uv, lod));
	}
	
	#define sampleEyeDepth(depthTexture, uv) sampleEyeDepthFunc(CAT_PASS_DEPTHT_EXTURE(depthTexture), uv)
	#define sampleEyeDepthProj(depthTexture, uv) sampleEyeDepthProjFunc(CAT_PASS_DEPTHT_EXTURE(depthTexture), uv)
	#define sampleEyeDepthLod(depthTexture, uv, lod) sampleEyeDepthLodFunc(CAT_PASS_DEPTHT_EXTURE(depthTexture), uv, lod)
	
	float sample01DepthFunc    (CAT_ARGS_DEPTHT_EXTURE(depthTexture), float2 uv) {
		return Linear01Depth(sampleDepth(depthTexture, uv));
	}
	float sample01DepthProjFunc(CAT_ARGS_DEPTHT_EXTURE(depthTexture), float4 uv) {
		return Linear01Depth(sampleDepthProj(depthTexture, uv));
	}
	float sample01DepthLodFunc (CAT_ARGS_DEPTHT_EXTURE(depthTexture), float2 uv, float lod) {
		return Linear01Depth(sampleDepthLod(depthTexture, uv, lod));
	}
	
	#define sample01Depth(depthTexture, uv) sample01DepthFunc(CAT_PASS_DEPTHT_EXTURE(depthTexture), uv)
	#define sample01DepthProj(depthTexture, uv) sample01DepthProjFunc(CAT_PASS_DEPTHT_EXTURE(depthTexture), uv)
	#define sample01DepthLod(depthTexture, uv, lod) sample01DepthLodFunc(CAT_PASS_DEPTHT_EXTURE(depthTexture), uv, lod)
	
#if !defined(UNITY_DECLARE_DEPTH_TEXTURE)
	#define UNITY_DECLARE_DEPTH_TEXTURE(tex) sampler2D_float tex
#endif
	//----------------------------------------------------------------------------------------------------------------------

	//#define tex2Dlod(tex, uv, lod) tex2Dlod(tex, float4(uv, 0, lod))

	float4 Tex2Dlod(sampler2D tex, float2 uv, float lod) {
		return tex2Dlod(tex, float4(uv, 0, lod));
	}

	float4 Tex2Dbias(sampler2D tex, float2 uv, float lod) {
		return tex2Dbias(tex, float4(uv, 0, lod));
	}

	float4 Tex2DPositive(sampler2D tex, float2 uv) {
		return max(0, tex2D(tex, uv));
	}
	float4 Tex2DlodPositive(sampler2D tex, float2 uv, float lod) {
		return max(0, tex2Dlod(tex, float4(uv, 0, lod)));
	}
	float4 Tex2DbiasPositive(sampler2D tex, float2 uv, float lod) {
		return max(0, tex2Dbias(tex, float4(uv, 0, lod)));
	}


#if 0 && !(!defined(SHADER_TARGET_GLSL) && !defined(SHADER_API_PSSL) && !defined(SHADER_API_GLES3))
float4 Tex2Dlod(sampler2D_float tex, float2 uv, float lod) {
	return tex2Dlod(tex, float4(uv, 0, lod));
}
float4 Tex2Dbias(sampler2D_float tex, float2 uv, float lod) {
	return tex2Dbias(tex, float4(uv, lod));
}

float4 Tex2DPositive(sampler2D_float tex, float2 uv) {
	return max(0, tex2D(tex, uv));
}
float4 Tex2DlodPositive(sampler2D_float tex, float2 uv, float lod) {
	return max(0, tex2Dlod(tex, uv, lod));
}
float4 Tex2DbiasPositive(sampler2D_float tex, float2 uv, float lod) {
	return max(0, tex2Dbias(tex, uv, lod));
}
#endif // !(!defined(SHADER_TARGET_GLSL) && !defined(SHADER_API_PSSL) && !defined(SHADER_API_GLES3))
//----------------------------------------------------------------------------------------------------------------------


inline half DisneyLuminance(half3 c) {
	return dot(half3(0.3, 0.5875, 0.1125), c); // Disneys luminance approx.
}


#endif // CAT_COMMON_INCLUDED
