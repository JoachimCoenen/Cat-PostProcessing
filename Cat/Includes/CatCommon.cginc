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



float1 Inv(float1 x) {
	return 1/x;
}
float2 Inv(float2 x) {
	return 1/x;
}
float3 Inv(float3 x) {
	return 1/x;
}
float4 Inv(float4 x) {
	return 1/x;
}
#if !defined(UNITY_COMPILER_HLSL2GLSL)
	half1 Inv(half1 x) {
		return 1/x;
	}
	half2 Inv(half2 x) {
		return 1/x;
	}
	half3 Inv(half3 x) {
		return 1/x;
	}
	half4 Inv(half4 x) {
		return 1/x;
	}
#endif


inline half1 DotPositive(half2 a, half2 b) {
	#if (SHADER_TARGET < 30 || defined(SHADER_API_PS3))
		return saturate(dot(a, b));
	#else
		return max(0, dot(a, b));
	#endif
}
inline half1 DotPositive(half3 a, half3 b) {
	#if (SHADER_TARGET < 30 || defined(SHADER_API_PS3))
		return saturate(dot(a, b));
	#else
		return max(0, dot(a, b));
	#endif
}

inline half1 Pow2 (half1 x) {
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

inline half1 Pow3(half1 x) {
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

inline half1 Pow8(half1 x) {
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

#if !defined(UNITY_STANDARD_BRDF_INCLUDED)
	inline half1 Pow4 (half1 x) {
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

	inline half1 Pow5 (half1 x) {
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

	inline half3 Unity_SafeNormalize(half3 inVec) {
	half1 dp3 = max(0.001f, dot(inVec, inVec));
	return inVec * rsqrt(dp3);
}
#endif

inline half2 Unity_SafeNormalize(half2 inVec) {
	half1 dp3 = max(0.001f, dot(inVec, inVec));
	return inVec * rsqrt(dp3);
}

inline float1 InvLerp(float1 a, float1 b, float1 value) {
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
#if !defined(UNITY_COMPILER_HLSL2GLSL)
	inline half1 InvLerp(half1 a, half1 b, half1 value) {
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

inline float1 InvLerpSat(float1 a, float1 b, float1 value) {
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
#if !defined(UNITY_COMPILER_HLSL2GLSL)
	inline half1 InvLerpSat(half1 a, half1 b, half1 value) {
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


inline float1 MaxC(float2 a) {
	return max(a.x, a.y);
}
inline float1 MaxC(float3 a) {
	return max(a.x,  max(a.y, a.z));
}
inline float1 MaxC(float4 a) {
	return max(MaxC(a.xy), MaxC(a.zw));
}
#if !defined(UNITY_COMPILER_HLSL2GLSL)
	inline half1 MaxC(half2 a) {
		return max(a.x, a.y);
	}
	inline half1 MaxC(half3 a) {
		return max(a.x,  max(a.y, a.z));
	}
	inline half1 MaxC(half4 a) {
		return max(MaxC(a.xy), MaxC(a.zw));
	}
#endif

inline float1 MinC(float2 a) {
	return min(a.x, a.y);
}
inline float1 MinC(float3 a) {
	return min(a.x,  min(a.y, a.z));
}
inline float1 MinC(float4 a) {
	return min(MinC(a.xy), MinC(a.zw));
}
#if !defined(UNITY_COMPILER_HLSL2GLSL)
	inline half1 MinC(half2 a) {
		return min(a.x, a.y);
	}
	inline half1 MinC(half3 a) {
		return min(a.x,  min(a.y, a.z));
	}
	inline half1 MinC(half4 a) {
		return min(MinC(a.xy), MinC(a.zw));
	}
#endif


inline half1 DisneyLuminance(half3 c) {
	return dot(half3(0.3, 0.5875, 0.1125), c); // Disneys luminance approx.
}

inline half1 UnityLuminance(half3 c) {
	return LinearRgbToLuminance(c);
}

inline float1 CompressBy(float1 value, float1 amplitude) {
	float1 invDenom = 1 / (1 + amplitude);
	return value * invDenom;
}
inline float2 CompressBy(float2 value, float2 amplitude) {
	float2 invDenom = 1 / (1 + amplitude);
	return value * invDenom;
}
inline float3 CompressBy(float3 value, float3 amplitude) {
	float3 invDenom = 1 / (1 + amplitude);
	return value * invDenom;
}
inline float4 CompressBy(float4 value, float4 amplitude) {
	float4 invDenom = 1 / (1 + amplitude);
	return value * invDenom;
}
#if !defined(UNITY_COMPILER_HLSL2GLSL)
	inline half1 CompressBy(half1 value, half1 amplitude) {
		half1 invDenom = 1 / (1 + amplitude);
		return value * invDenom;
	}
	inline half2 CompressBy(half2 value, half2 amplitude) {
		half2 invDenom = 1 / (1 + amplitude);
		return value * invDenom;
	}
	inline half3 CompressBy(half3 value, half3 amplitude) {
		half3 invDenom = 1 / (1 + amplitude);
		return value * invDenom;
	}
	inline half4 CompressBy(half4 value, half4 amplitude) {
		half4 invDenom = 1 / (1 + amplitude);
		return value * invDenom;
	}
#endif

inline float1 DeCompressBy(float1 value, float1 amplitude) {
	float1 invDenom = 1 / (1 - amplitude);
	return value * invDenom;
}
inline float2 DeCompressBy(float2 value, float2 amplitude) {
	float2 invDenom = 1 / (1 - amplitude);
	return value * invDenom;
}
inline float3 DeCompressBy(float3 value, float3 amplitude) {
	float3 invDenom = 1 / (1 - amplitude);
	return value * invDenom;
}
inline float4 DeCompressBy(float4 value, float4 amplitude) {
	float4 invDenom = 1 / (1 - amplitude);
	return value * invDenom;
}
#if !defined(UNITY_COMPILER_HLSL2GLSL)
	inline half1 DeCompressBy(half1 value, half1 amplitude) {
		half1 invDenom = 1 / (1 - amplitude);
		return value * invDenom;
	}
	inline half2 DeCompressBy(half2 value, half2 amplitude) {
		half2 invDenom = 1 / (1 - amplitude);
		return value * invDenom;
	}
	inline half3 DeCompressBy(half3 value, half3 amplitude) {
		half3 invDenom = 1 / (1 - amplitude);
		return value * invDenom;
	}
	inline half4 DeCompressBy(half4 value, half4 amplitude) {
		half4 invDenom = 1 / (1 - amplitude);
		return value * invDenom;
	}
#endif

inline float1 Compress(float1 value) {
	return CompressBy(value, value);
}
inline float2 Compress(float2 value) {
	return CompressBy(value, value);
}
inline float3 Compress(float3 value) {
	return CompressBy(value, value);
}
inline float4 Compress(float4 value) {
	return CompressBy(value, value);
}
#if !defined(UNITY_COMPILER_HLSL2GLSL)
	inline half1 Compress(half1 value) {
		return CompressBy(value, value);
	}
	inline half2 Compress(half2 value) {
		return CompressBy(value, value);
	}
	inline half3 Compress(half3 value) {
		return CompressBy(value, value);
	}
	inline half4 Compress(half4 value) {
		return CompressBy(value, value);
	}
#endif

inline float1 DeCompress(float1 value) {
	return DeCompressBy(value, value);
}
inline float2 DeCompress(float2 value) {
	return DeCompressBy(value, value);
}
inline float3 DeCompress(float3 value) {
	return DeCompressBy(value, value);
}
inline float4 DeCompress(float4 value) {
	return DeCompressBy(value, value);
}
#if !defined(UNITY_COMPILER_HLSL2GLSL)
	inline half1 DeCompress(half1 value) {
		return DeCompressBy(value, value);
	}
	inline half2 DeCompress(half2 value) {
		return DeCompressBy(value, value);
	}
	inline half3 DeCompress(half3 value) {
		return DeCompressBy(value, value);
	}
	inline half4 DeCompress(half4 value) {
		return DeCompressBy(value, value);
	}
#endif


inline float3 CompressLuminance(float3 value) {
	return CompressBy(value, DisneyLuminance(value));
}
#if !defined(UNITY_COMPILER_HLSL2GLSL)

	inline half3 CompressLuminance(half3 value) {
		return CompressBy(value, DisneyLuminance(value));
	}
#endif

inline float3 DeCompressLuminance(float3 value) {
	return DeCompressBy(value, DisneyLuminance(value));
}
#if !defined(UNITY_COMPILER_HLSL2GLSL)
	inline half3 DeCompressLuminance(half3 value) {
		return DeCompressBy(value, DisneyLuminance(value));
	}
#endif

float1 noiseSimple(float2 n) {
	float1 m = frac(dot(float2(17.9442719099991588, 76.0131556174964248), n));
	// Logistics Map equation:
	m = 4.00 * (m - m * m);
	m = 4.00 * (m - m * m);
	m = 4.00 * (m - m * m);
	float1 res = frac(sqrt(m) * 1363.1007331374358577975619300075) * 2 - 1;
	return res;//*abs(res);
	//return frac( (m) * 48) * 2 - 1;
}

float3 noiseSimple3(float2 n, float3 seed) {
	const float3 Q = { -0.06711056, 0.00583715, 0.19323805506419510 };
	float3 m;
	m.x = frac(dot(Q, float3(n, seed.x)));
	m.y = frac(dot(Q, float3(n, seed.y)));
	m.z = frac(dot(Q, float3(n, seed.z)));
	// Logistics Map equation:
	//m = 4.00 * (m - m * m);
	//m = 4.00 * (m - m * m);
	//m = 2.00 * (m - m * m);
	float3 res = frac(m * 52.9899189) * 2 - 1;
	return res;//*abs(res);
	//return frac( (m) * 48) * 2 - 1;
}


#define CAT_ARGS_TEX_INFO(_tex) sampler2D _tex, float4 _tex##_TexelSize
#define CAT_PASS_TEX_INFO(_tex) _tex, _tex##_TexelSize

#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
	#define CAT_ARGS_DEPTHT_EXTURE(tex) Texture2DArray tex, SamplerState sampler##tex
	#define CAT_PASS_DEPTHT_EXTURE(tex) tex, sampler##tex
#else
	#define CAT_ARGS_DEPTHT_EXTURE(tex) sampler2D_float tex
	#define CAT_PASS_DEPTHT_EXTURE(tex) tex
#endif

float1 sampleDepthFunc    (CAT_ARGS_DEPTHT_EXTURE(depthTexture), float2 uv) {
	return SAMPLE_DEPTH_TEXTURE(depthTexture, uv);
}
float1 sampleDepthProjFunc(CAT_ARGS_DEPTHT_EXTURE(depthTexture), float4 uv) {
	return SAMPLE_DEPTH_TEXTURE_PROJ(depthTexture, UNITY_PROJ_COORD(uv));
}
float1 sampleDepthLodFunc (CAT_ARGS_DEPTHT_EXTURE(depthTexture), float2 uv, float1 lod) {
	return SAMPLE_DEPTH_TEXTURE_LOD(depthTexture, float4(uv, 0, lod));
}

#define sampleDepth(depthTexture, uv) sampleDepthFunc(CAT_PASS_DEPTHT_EXTURE(depthTexture), uv)
#define sampleDepthProj(depthTexture, uv) sampleDepthProjFunc(CAT_PASS_DEPTHT_EXTURE(depthTexture), uv)
#define sampleDepthLod(depthTexture, uv, lod) sampleDepthLodFunc(CAT_PASS_DEPTHT_EXTURE(depthTexture), uv, lod)

float1 sampleEyeDepthFunc    (CAT_ARGS_DEPTHT_EXTURE(depthTexture), float2 uv) {
	return LinearEyeDepth(sampleDepth(depthTexture, uv));
}
float1 sampleEyeDepthProjFunc(CAT_ARGS_DEPTHT_EXTURE(depthTexture), float4 uv) {
	return LinearEyeDepth(sampleDepthProj(depthTexture, uv));
}
float1 sampleEyeDepthLodFunc (CAT_ARGS_DEPTHT_EXTURE(depthTexture), float2 uv, float1 lod) {
	return LinearEyeDepth(sampleDepthLod(depthTexture, uv, lod));
}

#define sampleEyeDepth(depthTexture, uv) sampleEyeDepthFunc(CAT_PASS_DEPTHT_EXTURE(depthTexture), uv)
#define sampleEyeDepthProj(depthTexture, uv) sampleEyeDepthProjFunc(CAT_PASS_DEPTHT_EXTURE(depthTexture), uv)
#define sampleEyeDepthLod(depthTexture, uv, lod) sampleEyeDepthLodFunc(CAT_PASS_DEPTHT_EXTURE(depthTexture), uv, lod)

float1 sample01DepthFunc    (CAT_ARGS_DEPTHT_EXTURE(depthTexture), float2 uv) {
	return Linear01Depth(sampleDepth(depthTexture, uv));
}
float1 sample01DepthProjFunc(CAT_ARGS_DEPTHT_EXTURE(depthTexture), float4 uv) {
	return Linear01Depth(sampleDepthProj(depthTexture, uv));
}
float1 sample01DepthLodFunc (CAT_ARGS_DEPTHT_EXTURE(depthTexture), float2 uv, float1 lod) {
	return Linear01Depth(sampleDepthLod(depthTexture, uv, lod));
}

#define sample01Depth(depthTexture, uv) sample01DepthFunc(CAT_PASS_DEPTHT_EXTURE(depthTexture), uv)
#define sample01DepthProj(depthTexture, uv) sample01DepthProjFunc(CAT_PASS_DEPTHT_EXTURE(depthTexture), uv)
#define sample01DepthLod(depthTexture, uv, lod) sample01DepthLodFunc(CAT_PASS_DEPTHT_EXTURE(depthTexture), uv, lod)

#if !defined(UNITY_DECLARE_DEPTH_TEXTURE)
	#define UNITY_DECLARE_DEPTH_TEXTURE(tex) sampler2D_float tex
#endif

float4 Tex2Dlod(sampler2D tex, float2 uv, float1 lod) {
	return tex2Dlod(tex, float4(uv, 0, lod));
}
float4 Tex2Dbias(sampler2D tex, float2 uv, float1 lod) {
	return tex2Dbias(tex, float4(uv, 0, lod));
}
float4 Tex2DPositive(sampler2D tex, float2 uv) {
	return max(0, tex2D(tex, uv));
}
float4 Tex2DlodPositive(sampler2D tex, float2 uv, float1 lod) {
	return max(0, tex2Dlod(tex, float4(uv, 0, lod)));
}
float4 Tex2DbiasPositive(sampler2D tex, float2 uv, float1 lod) {
	return max(0, tex2Dbias(tex, float4(uv, 0, lod)));
}
#if 0 && !(!defined(SHADER_TARGET_GLSL) && !defined(SHADER_API_PSSL) && !defined(SHADER_API_GLES3))
	float4 Tex2Dlod(sampler2D_float tex, float2 uv, float1 lod) {
		return tex2Dlod(tex, float4(uv, 0, lod));
	}
	float4 Tex2Dbias(sampler2D_float tex, float2 uv, float1 lod) {
		return tex2Dbias(tex, float4(uv, lod));
	}
	float4 Tex2DPositive(sampler2D_float tex, float2 uv) {
		return max(0, tex2D(tex, uv));
	}
	float4 Tex2DlodPositive(sampler2D_float tex, float2 uv, float1 lod) {
		return max(0, tex2Dlod(tex, uv, lod));
	}
	float4 Tex2DbiasPositive(sampler2D_float tex, float2 uv, float1 lod) {
		return max(0, tex2Dbias(tex, uv, lod));
	}
#endif // !(!defined(SHADER_TARGET_GLSL) && !defined(SHADER_API_PSSL) && !defined(SHADER_API_GLES3))
//----------------------------------------------------------------------------------------------------------------------


#endif // CAT_COMMON_INCLUDED
