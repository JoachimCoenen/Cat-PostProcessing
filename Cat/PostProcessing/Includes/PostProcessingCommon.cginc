#if !defined(POST_PROCESSING_COMMON_INCLUDED)
#define POST_PROCESSING_COMMON_INCLUDED

#include "UnityCG.cginc"
#include "../../Includes/LightingInterface.cginc"
#include "../../Includes/CatCommon.cginc"

// Consts:

static const half EPSILON = 1e-4;



	//	sampler2D		_BlueNoise;						float4 _BlueNoise_TexelSize;
		sampler2D		_MainTex;						float4	_MainTex_TexelSize;

		bool			_IsVelocityPredictionEnabled;
uniform	float2			_TAAJitterVelocity;

uniform sampler2D		_CameraGBufferTexture0;	uniform float4	_CameraGBufferTexture0_TexelSize;
uniform sampler2D		_CameraGBufferTexture1;
uniform sampler2D		_CameraGBufferTexture2;
uniform sampler2D		_CameraGBufferTexture3;
uniform sampler2D		_CameraReflectionsTexture;
uniform sampler2D_half	_CameraMotionVectorsTexture;
uniform UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture); 	uniform float4	_CameraDepthTexture_TexelSize;
uniform sampler2D_half _DepthTexture;	// accessing a custom Texture is Faster than sampling the camera depth buffer directly.

#define _GBuffer_TexelSize _CameraGBufferTexture0_TexelSize

#define CAT_GET_GBUFFER(s, uv)													\
	half4 gbuffer0##s = Tex2Dlod(_CameraGBufferTexture0,    uv, 0);	\
	half4 gbuffer1##s = Tex2Dlod(_CameraGBufferTexture1,    uv, 0);	\
	half4 gbuffer2##s = Tex2Dlod(_CameraGBufferTexture2,    uv, 0);	\
	half4 gbuffer3##s = Tex2Dlod(_CameraGBufferTexture3, uv, 0);	\
	GBufferData s = unpackGBuffer(gbuffer0##s, gbuffer1##s, gbuffer2##s, gbuffer3##s);

float2 SnapToPixel(float2 uv, float4 texelSize) {
	float2 pos = uv * texelSize.zw;
	// snap to pixel center:
	return ( floor(pos - 0) + 0.5 ) * texelSize.xy;
}

float2 SnapBetweenPixel(float2 uv, float4 texelSize) {
	float2 pos = uv * texelSize.zw;
	// snap to pixel center:
	return ( floor(pos - 0.5) + 1.0 ) * texelSize.xy;
}

float3 GetScreenPos(float2 uv, float depth) {
	return float3(uv * 2 - 1, depth);
}

float3 ViewToScreenPos(float3 vsPos) {
	float3 ssPos;
	ssPos.x = vsPos.x / vsPos.z * unity_CameraProjection[0][0] - unity_CameraProjection[0][2];
	ssPos.y = vsPos.y / vsPos.z * unity_CameraProjection[1][1] + unity_CameraProjection[1][2];
	ssPos.z = (1 / vsPos.z - _ZBufferParams.w) / _ZBufferParams.z;
	return ssPos;
}

float3 ScreenToViewPos(float3 ssPos) {
	float linEyeZ = -LinearEyeDepth(ssPos.z);
	float3 vsPos;
	vsPos.x = (-ssPos.x - unity_CameraProjection[0][2]) / unity_CameraProjection[0][0] * linEyeZ;
	vsPos.y = (-ssPos.y + unity_CameraProjection[1][2]) / unity_CameraProjection[1][1] * linEyeZ;
	vsPos.z = -linEyeZ;
	return vsPos;
}

float4 WorldToView(float4 wsVector) {
	return mul(unity_WorldToCamera, wsVector);
}

float4 ViewToWorld(float4 vsVector) {
	return mul(unity_CameraToWorld, vsVector);
}
 
float3 WorldToViewDir(float3 wsDir) {
	return WorldToView(float4(wsDir, 0)).xyz;
}

float3 ViewToWorldDir(float3 vsDir) {
	return ViewToWorld(float4(vsDir, 0)).xyz;
}

float3 WorldToViewPos(float3 wsPos) {
	return WorldToView(float4(wsPos, 1)).xyz;
}

float3 ViewToWorldPos(float3 vsPos) {
	return ViewToWorld(float4(vsPos, 1));
}

float3 WorldToScreenPos(float3 wsPos) {
	return ViewToScreenPos(WorldToViewPos(wsPos));
}

float3 ScreenToWorldPos(float3 ssPos) {
	float3 vsPos = ScreenToViewPos(ssPos);
	return ViewToWorldPos(vsPos);
}

float2 FlipUVs(in float2 uv) {
	#if UNITY_UV_STARTS_AT_TOP
		if (true || _MainTex_TexelSize.y < 0) {
			uv.y = 1-uv.y;
		}
	#endif
	return uv;
}

struct VertexInput {
	float4 vertex : POSITION;
	float2 texcoord : TEXCOORD;
};

struct VertexOutput {
	float4 pos : POSITION;
	float2 uv : TEXCOORD0;
};
VertexOutput vert(VertexInput v) {
	VertexOutput o;
	o.pos = float4(v.vertex.xy, 0, 1);
	o.uv = FlipUVs(v.texcoord.xy);
	return o;
}

struct VertexOutputVS {
	float4 pos : POSITION;
	float2 uv : TEXCOORD0;
	float3 vsRay : TEXCOORD1;
};
VertexOutputVS vertVS(VertexInput v) {
	VertexOutputVS o;
	o.pos = float4(v.vertex.xy, 0, 1);
	o.uv = FlipUVs(v.texcoord.xy);
	o.vsRay = ScreenToViewPos(GetScreenPos(o.uv, 0.5));
	o.vsRay /= o.vsRay.z;
	return o;
}

struct VertexOutputWS {
	float4 pos : POSITION;
	float2 uv : TEXCOORD0;
	float3 wsRay : TEXCOORD2; // TEXCOORD*2* is intentional!
};
VertexOutputWS vertWS(VertexInput v) {
	VertexOutputWS o;
	o.pos = float4(v.vertex.xy, 0, 1);
	o.uv = FlipUVs(v.texcoord.xy);
	float3 vsRay = ScreenToViewPos(GetScreenPos(o.uv, 0.5));
	vsRay /= vsRay.z;
	o.wsRay = ViewToWorldPos(vsRay);
	o.wsRay = UnityWorldSpaceViewDir(o.wsRay);
	return o;
}

struct VertexOutputFull {
	float4 pos : POSITION;
	float2 uv : TEXCOORD0;
	float3 vsRay : TEXCOORD1;
	float3 wsRay : TEXCOORD2;
};
VertexOutputFull vertFull(VertexInput v) {
	VertexOutputFull o;
	o.pos = float4(v.vertex.xy, 0, 1);
	o.uv = FlipUVs(v.texcoord.xy);
	o.vsRay = ScreenToViewPos(GetScreenPos(o.uv, 0.5));
	o.vsRay /= o.vsRay.z;
	o.wsRay = ViewToWorldPos(o.vsRay);
	o.wsRay = UnityWorldSpaceViewDir(o.wsRay);
	return o;
}

float2	GetVelocity(float2 uv) { return !_IsVelocityPredictionEnabled ? 0 : Tex2Dlod(_CameraMotionVectorsTexture, uv, 0).xy; } // - _TAAJitterVelocity * 0; // Does not seem to be neccessary in Unity2017.2f3

///
/// <param name='fogParams'> <c>= half3(Intensity, StartDistance, EndDistance);</c> </param>
/// 
half getFogDensity(float depth, half3 fogParams) {
	half density = 0;
	#if FOG_LINEAR
		density = InvLerpSat(fogParams.y, fogParams.z, depth);
	#elif FOG_EXP
		density = 1 - exp2(-fogParams.x * depth);
	#elif FOG_EXP_SQR
		density = 1 - exp2(-Pow2(fogParams.x * depth));
	#endif
	density = depth*1.0125 < _ProjectionParams.z ? density : 0; // if not isSkybox, apply fog
	return density;
}

///
/// <remarks> requires _DepthTexture.</remarks>
/// <param name='fogParams'> <c>= half3(Intensity, StartDistance, EndDistance);</c> </param>
/// 
half getFogDensity(float2 uv, half3 fogParams) {
	float depth = sampleEyeDepth(_DepthTexture, uv);
	return getFogDensity(depth, fogParams);
}

#endif // POST_PROCESSING_COMMON_INCLUDED
