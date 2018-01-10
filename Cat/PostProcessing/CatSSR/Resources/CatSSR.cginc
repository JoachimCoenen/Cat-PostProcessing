//The MIT License(MIT)

//Copyright(c) 2017 Joachim Coenen

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

#if !defined(CAT_SSR_INCLUDED)
#define CAT_SSR_INCLUDED

#include "UnityCG.cginc"
#include "UnityStandardUtils.cginc"
#include "UnityGlobalIllumination.cginc"

#include "../../Includes/PostProcessingCommon.cginc"

#include "CatRayTraceLib.cginc"

int				_StepCount;
int				_MinPixelStride;
int				_MaxPixelStride;
float			_NoiseStrength;
bool			_CullBackFaces;
float			_MaxReflectionDistance;
				// rayTraceResol
				// upSampleHitTexture

float			_Intensity;
float			_ReflectionDistanceFade;
float			_RayLengthFade;
float			_EdgeFade;
int				_UseRetroReflections;
bool			_UseReflectionMipMap;
				// reflectionResolution

				// useImportanceSampling
int				_ResolveSampleCount;
float			_ImportanceSampleBias;
bool			_UseCameraMipMap;
				// suppressFlickering	

				// useTemporalSampling
float			_Response;
float			_ToleranceMargin;

				// debugOn
int				_DebugMode;
int				_MipLevelForDebug;

half3			_FogColor;
half3			_FogParams;
//		bool			_IsVelocityPredictionEnabled;
float2			_FrameCounter;
float4			_BlurDir;
float			_MipLevel;
float			_PixelsPerMeterAtOneMeter;

sampler2D		_HitTex;						float4	_HitTex_TexelSize;
sampler2D		_HistoryTex;
sampler2D		_ReflectionsTex;				float4	_ReflectionsTex_TexelSize;

sampler2D		_BlueNoise;						float4 _BlueNoise_TexelSize;

	/*	sampler2D		_NormalsPacked;	// */ #define _NormalsPacked _CameraGBufferTexture2
float3 GetNormal(float2 uv) { return Tex2Dlod(_NormalsPacked, uv, 0).xyz * 2 - 1; }

//----------------------------------------------------------------------------------------------------------------------
//----------------------------------------------------------------------------------------------------------------------

float GGXnoPi(float nh, float m2) {
	float d = (nh * m2 - nh) * nh + 1;
	return m2 / max(1e-9, Pow2(d));
}

// Brian Karis, Epic Games "Real Shading in Unreal Engine 4" 
// http://blog.selfshadow.com/publications/s2013-shading-course/karis/s2013_pbs_epic_slides.pdf
float3 ImportanceSample(float2 Xi, float roughness, out float pdf) {
	float m2 = max(1e-5, Pow4(roughness));
	float Phi = 2 * UNITY_PI * Xi.x;
				 
	float CosTheta = sqrt((1.0 - Xi.y) / (1.0 + (m2 - 1.0) * Xi.y));
	float SinTheta = sqrt(max(1e-5, 1.0 - CosTheta * CosTheta));
				 
	float3 H;
	H.x = SinTheta * cos(Phi);
	H.y = SinTheta * sin(Phi);
	H.z = CosTheta;
	
	pdf = GGXnoPi(CosTheta, m2) * UNITY_INV_PI * CosTheta;
	
	return H; 
}

float3 rotateVector(float3 v, float3 r) {
	float3 up = float3(0, 1, 0);
	float3 n = v;
	float3 t = normalize(cross(n, up));
	float3 b = cross(t, n);
	return mul(r, float3x3(b, t, n));
}


bool AreReflectionsAllowed(float3 viewDir, float3 normal, float reflectionDistance, float smoothness) {
	return (true
			&& (reflectionDistance <= (_MaxReflectionDistance)) 
			&& (!_CullBackFaces || dot(viewDir, normal) <= cos(45/180.0*UNITY_PI)) 
		//	&& (!_CullBackFaces || dot(viewDir, reflect(viewDir, normal)) >= 0) 
			&& (!(MIN_SMOOTHNESS > 0) || smoothness >= MIN_SMOOTHNESS) 
	);
}


bool AreReflectionsAllowedScreenSpace(float3 viewDir, float3 normal, float depth, float smoothness) {
	return (true
			&& (
			//	1 / (_ZBufferParams.z * depth + _ZBufferParams.w) <= _MaxReflectionDistance
				_MaxReflectionDistance * (_ZBufferParams.z * depth + _ZBufferParams.w) >= 1
			) 
			&& (!_CullBackFaces || dot(viewDir, normal) <= cos(45/180.0*UNITY_PI)) 
			&& (!(MIN_SMOOTHNESS > 0) || smoothness >= MIN_SMOOTHNESS) 
	);
}

float CullBackHits(float3 hitNormal, float3 rayDir) { // JCO@@@TODO: find a better name for this!
	return (!CULL_RAY_HITS_ON_BACK_SIDE || dot(rayDir, hitNormal) <= 0);
}

float RayAttenLength(float actualStepCount) {
	return 1 - Pow2(InvLerpSat(_RayLengthFade * _StepCount, 0, _StepCount - actualStepCount));
}

float RayAttenReflectionDistance(float reflectionDistance) {
	return 1 - Pow2(InvLerpSat(_ReflectionDistanceFade * _MaxReflectionDistance, 0, _MaxReflectionDistance - reflectionDistance));
}

float RayAttenReflectionDistanceScreenSpace(float depth) {
	float dzwm = (depth * _ZBufferParams.z + _ZBufferParams.w) * _MaxReflectionDistance;
	return 1 - Pow2( 1 - InvLerpSat(0, _ReflectionDistanceFade * dzwm, dzwm - 1));
}

float RayAttenBorder(float2 uvHit, float2 dir, float value) {
	half2 rborder = (dir > 0) ? 1 : 0;
	float dist = MinC(sign(dir)*(rborder - uvHit));
	return 1 - Pow5(InvLerpSat(value, 0, dist));
	
//	float2 pos2 = abs(uvHit * 2 - 1);
//	pos2 = InvLerpSat(1, 1-value, pos2);
//	return MinC(pos2);
}

float4 fragRayTrace(VertexOutputVS i) : SV_Target {
	float2 uv = i.uv;
	
	CAT_GET_GBUFFER(s, uv)
//	float3 vsNormal = normalize(WorldToViewDir(s.normal));
	s = TransformGBufferData(s, unity_WorldToCamera);
	
	float depth = sampleDepthLod(_DepthTexture, uv, 0);
	float3 ssStartPos = GetScreenPos(uv, depth);
	float3 vsStartPos = (i.vsRay.xyz * LinearEyeDepth(depth));
	float3 vsViewDir = -normalize(vsStartPos);
	
	float invReflectionDistance = rsqrt(dot(vsStartPos, vsStartPos));
	float reflectionDistance = 1 / invReflectionDistance;
	if (!AreReflectionsAllowed(vsViewDir, s.normal, reflectionDistance, s.smoothness)) {
		return float4(uv, 0, +1e5);
	}
	
	
	float2 pos = uv * _HitTex_TexelSize.zw;
	float2 noise1D = -round(noiseSimple(0 + (_FrameCounter)) * _HitTex_TexelSize.wz);
	//noise1D = 4.00 * (m - m * m);
	float3 noise2D = Tex2Dlod(_BlueNoise, (pos + noise1D) *_BlueNoise_TexelSize.xy, 0).rgb;
	noise2D.y *= _ImportanceSampleBias;
	
	float pdf = 1;
	s.normal = rotateVector(s.normal, ImportanceSample(noise2D.xy, 1-s.smoothness, /*out*/pdf));
	
	vsStartPos = vsStartPos + s.normal * max(0.005*LinearEyeDepth(depth), 0.001);
	ssStartPos = ViewToScreenPos(vsStartPos);
	
	float3 vsRayDir = getReflectionVector(s, vsViewDir);
//	float3 vsRayDir = WorldToViewDir(wsRayDir);
//	float3 ssRayDir = normalize(ViewToScreenPos(vsRayDir + vsStartPos) - ssStartPos);
	
	float3 ssRayDir = 0;
	ssRayDir.z = -sign(ViewToScreenPos(-vsRayDir + vsStartPos).z);
	ssRayDir = ssRayDir.z * (ViewToScreenPos(ssRayDir.z*vsRayDir + vsStartPos).xyz - ssStartPos);
	ssRayDir = normalize(ssRayDir);
	float jitter = 0;//_NoiseStrength * (noise2D.z*1-0.5);//noiseSimple(uv + depth);
	////////////////////////////////////////////////////////////////////////////////////////////////////////////
	float4 rayHit = RayTrace(
			_StepCount, 0.5, 
			_MinPixelStride, 
			_MaxPixelStride, 
			ssStartPos, ssRayDir, 
			_ZBufferParams, CAT_PASS_DEPTHT_EXTURE(_DepthTexture), 
			_GBuffer_TexelSize, jitter
	);
	float2 uvHit = rayHit.xy;
	
	float confidence = 0;
	if (rayHit.w < _StepCount) {
		confidence = 1;
		float3 hitNormal = GetNormal(rayHit.xy);
		hitNormal = WorldToViewDir(hitNormal);
		confidence *= CullBackHits(hitNormal, vsRayDir);
		
		confidence *= RayAttenReflectionDistance(reflectionDistance);

		confidence *= RayAttenLength(rayHit.w);
		
		confidence *= RayAttenBorder(uvHit, ssRayDir.xy, _EdgeFade);
	//	confidence = ssRayDir.z > 0;
	//confidence = 1;
	}
	//confidence = pow(rayHit.w / (float)_StepCount, 2.2);
	
	return float4(uvHit, confidence, pdf);
}

//----------------------------------------------------------------------------------------------------------------------
//----------------------------------------------------------------------------------------------------------------------
float getConeTangent(float smoothness, float nv) {
	float roughness = 1-smoothness;
	return lerp(0.0, roughness * _ImportanceSampleBias, pow(saturate(nv), 1.5) * sqrt(roughness));
}

static const half maxCameraMipLevel = 8.0 - 1.0;
float getMipLevelResolve(float coneTangent, float2 hitUV, float2 uv, float maxMipLevel) {
	float intersectionCircleRadius = coneTangent * length(hitUV - uv);
	return clamp(log2(intersectionCircleRadius * MaxC(_MainTex_TexelSize.zw)), 0, maxMipLevel);
}

float getMipLevelResolve(float3 vsHitPos, float rayLength, float smoothness, float maxMipLevel) {
	float roughness = pow(1 - smoothness, 4.0/3.0);
	float hitDistance = 0 + length(vsHitPos);
	float area = abs(roughness * (rayLength + Pow2(roughness)) * _PixelsPerMeterAtOneMeter / hitDistance * 1) * (_ImportanceSampleBias);
	
	float mip = log2(area/16.0 + 15.0/16.0);
	return clamp(mip, 0, maxMipLevel);
}


float BRDFWeightUnity(float3 V, float3 L, float3 N, float smoothness) {
	float3 H = normalize(L + V);
	
	float nh = saturate(dot(N,H));
	float nl = saturate(dot(N,L));
	float nv = saturate(dot(N,V));
	
//	half d = (NdotH * m2 - NdotH) * NdotH + 1.0f; // 2 mad
//	half D = m2 / (d * d + 1e-7f); // This function is not intended to be running on Mobile,
//       D = D / UNITY_PI;
	float roughness = 1 - smoothness;
	float m = Pow2(roughness);
	half G = SmithJointGGXVisibilityTerm (nl, nv, m);
	float m2 = max(1e-7, Pow2(m));
	half D = GGXnoPi(nh, m2); 
	return D * G * 0.25;
//	half D = GGXTerm(nh, roughness); return D * G * UNITY_PI * 0.25;
}

static const float O_S = 2.35 * 0.75;//2.35;
static const float2 offsets[7] = { {0, 0},
//	float2(2, -2),
//	float2(-2, -2),
//	float2(0, 2), 
{0.8660254*O_S, -0.5*O_S}, {0, O_S}, {-0.8660254*O_S, -0.5*O_S}, {0.8660254*O_S, 0.5*O_S}, {-0.8660254*O_S, 0.5*O_S}, {0, -O_S} };

float4 fragResolveAdvanced(VertexOutputVS i) : SV_Target {
	float2 uv = i.uv;
	
	CAT_GET_GBUFFER(s, uv)
	float3 vsNormal = normalize(WorldToViewDir(s.normal));
	
	float depth = sampleDepthLod(_DepthTexture, uv, 0);
	float3 ssPos = GetScreenPos(uv, depth);
	float3 vsPos = i.vsRay * LinearEyeDepth(depth);
	float invReflectionDistance =  rsqrt(dot(vsPos, vsPos));
	float3 vsViewDir = -vsPos * invReflectionDistance;
	
	if (!AreReflectionsAllowed(vsViewDir, vsNormal, 1/invReflectionDistance, s.smoothness)) {
		return 0;
	}
	
	float2 pos = uv * _MainTex_TexelSize.zw;
	float2 blueNoise = tex2D(_BlueNoise, pos *_BlueNoise_TexelSize.xy + _FrameCounter).ba * 2.0 - 1.0; // works better with [-1, 1] range
	
	float2x2 offsetRotationMatrix = float2x2(blueNoise.x, blueNoise.y, -blueNoise.y, blueNoise.x);
	
	float coneTangent = getConeTangent(s.smoothness, dot(vsNormal, vsViewDir));
	
	float maxMipLevel = _UseCameraMipMap ? maxCameraMipLevel : 0;
	
	
	float rayLength = 0; // Its value is used after the loop, so it is declared here.
	
	float4 result = 0.0;
	float weightSum = 0.0;	
	int resolveSteps = _ResolveSampleCount;
	for(int i = 0; i < resolveSteps; i++) {
		float2 offsetUV = offsets[i] * _HitTex_TexelSize.xy;
		offsetUV = mul(offsetRotationMatrix, offsetUV);
		
		// "uv" is the location of the current (or "local") pixel. We want to resolve the local pixel using
		// intersections spawned from neighboring pixels. The neighboring pixel is this one:
		float2 neighborUv = uv + offsetUV;
	//	neighborUv = SnapToPixel(neighborUv, _HitTex_TexelSize);
	
		// Now we fetch the intersection point and the PDF that the neighbor's ray hit.
		float4 hitPacked = Tex2Dlod(_HitTex, neighborUv, 0);
		float2 hitUV = hitPacked.xy;
		if (_UseRetroReflections) {
			hitUV += -GetVelocity(hitUV) * float2(1, 1);
		}
		float hitZ   = sampleDepthLod(_DepthTexture, hitUV, 0);
		float hitPDF = hitPacked.w;
		float confidence = hitPacked.z;
		
		
		float3 vsHitPos = ScreenToViewPos(GetScreenPos(hitUV, hitZ));
		rayLength = length(vsHitPos - vsPos);
		// We assume that the hit point of the neighbor's ray is also visible for our ray, and we blindly pretend
		// that the current pixel shot that ray. To do that, we treat the hit point as a tiny light source. To calculate
		// a lighting contribution from it, we evaluate the BRDF. Finally, we need to account for the probability of getting
		// this specific position of the "light source", and that is approximately 1/PDF, where PDF comes from the neighbor.
		// Finally, the weight is BRDF/PDF. BRDF uses the local pixel's normal and roughness, but PDF comes from the neighbor.
		float weight = BRDFWeightUnity(vsViewDir /*V*/, (vsHitPos - vsPos) / rayLength /*L*/, vsNormal /*N*/, max(0.001, s.smoothness)) / max(1e-5, hitPDF);
	//	weight = 1;
		
		float mip = getMipLevelResolve(vsHitPos, rayLength, s.smoothness, maxMipLevel);
	//	float mip = getMipLevelResolve(coneTangent, hitUV, uv, maxMipLevel);
		 
		float4 sampleColor = float4(Tex2Dlod(_MainTex, hitUV, mip).rgb, 1);
		
//				sampleColor.rgb /= 2 + DisneyLuminance(sampleColor.rgb);
		sampleColor.a = confidence;
	//	weight *= confidence;

		result += sampleColor * weight;
		weightSum += weight;
	}
	result /= weightSum;
	result.rgb = lerp(result.rgb, _FogColor.rgb, getFogDensity(rayLength, _FogParams));
	result.a = Pow2(result.a);
//		result.rgb /= 0.5 - 0.5*DisneyLuminance(result.rgb);
//	result.rgb *= result.a;
//	result.rgb += tex2D(_CameraReflectionsTexture, uv).rgb * (1-result.a);
	
	
	return max(0, result);
}

//----------------------------------------------------------------------------------------------------------------------
//----------------------------------------------------------------------------------------------------------------------
sampler2D _TempTexture_This_texture_is_never_going_to_be_directly_referenced0x;		float4 _TempTexture_This_texture_is_never_going_to_be_directly_referenced0x_TexelSize;
float4 combineTemporal(CAT_ARGS_TEX_INFO(currentTex), sampler2D historyTex, float2 uv) {
	const float k = 1;//0.03125*0.03125;
	float2 velocity = GetVelocity(uv).xy;
	float2 uvPrev = uv - velocity.xy;
	float confidence = all(uvPrev == saturate(uvPrev));
	
	float2 tx = currentTex_TexelSize.xy * float2(1, 1);
	float2 ty = currentTex_TexelSize.xy * float2(-1, 1);
	float4 history = tex2D(historyTex, uvPrev);
	float4 mainTex = tex2D(currentTex, uv);
	float4 corner1 = tex2D(currentTex, uv + tx*float2( 0.5, -1.0));
	float4 corner2 = tex2D(currentTex, uv + tx*float2( 1.0,  0.5));
	float4 corner3 = tex2D(currentTex, uv + tx*float2(-0.5,  1.0));
	float4 corner4 = tex2D(currentTex, uv + tx*float2(-1.0, -0.5));
	
	//corner1 = tex2D(currentTex, uv - tx);
	//corner2 = tex2D(currentTex, uv + tx);
	//corner3 = tex2D(currentTex, uv - ty);
	//corner4 = tex2D(currentTex, uv + ty);
	
	history.rgb *= (k + history.a);
	mainTex.rgb *= (k + mainTex.a);
	corner1.rgb *= (k + corner1.a);
	corner2.rgb *= (k + corner2.a);
	corner3.rgb *= (k + corner3.a);
	corner4.rgb *= (k + corner4.a);
	
	float4 corners = (corner1 + corner2);
	confidence *= any(history);
	
	const float _Sharpness = 0.00;
	// Sharpen output
	float4 cornerR = 4.0 * corners - 2.0 * mainTex;
//	mainTex =  max(0.0, mainTex + (mainTex - (cornerR * 0.166667)) * 2.718282 * _Sharpness);
	
	
//	history.rgb /= 1 + DisneyLuminance(history.rgb);
//	mainTex.rgb /= 1 + DisneyLuminance(mainTex.rgb);
//	corner1.rgb /= 1 + DisneyLuminance(corner1.rgb);
//	corner2.rgb /= 1 + DisneyLuminance(corner2.rgb);
//	corners.rgb = corners.rgb / (2 + DisneyLuminance(corners.rgb));
	
	
	half4 maxCorners = max(corner1, max(corner2, max(corner3, corner4))).rgba;
	half4 minCorners = min(corner1, min(corner2, min(corner3, corner4))).rgba;
	half4 maxOverAll = max(maxCorners,  mainTex).rgba;
	half4 minOverAll = min(minCorners,  mainTex).rgba;
	half4 diff = maxOverAll - minOverAll;
	
const float VELOCITY_WEIGHT_SCALE = 1*40;
float delta = length(velocity);
float nudge = saturate(1 - VELOCITY_WEIGHT_SCALE * delta);

float4 lerpUp = (maxOverAll - minOverAll + 0.05) * nudge * _ToleranceMargin;
history = clamp(history, minOverAll - lerpUp, maxOverAll + lerpUp);


//	float4 center = (minOverAll + maxOverAll) * 0.5f;
//	minOverAll = (minOverAll - center) * _ToleranceMargin + center;
//	maxOverAll = (maxOverAll - center) * _ToleranceMargin + center;
//
//	history = clamp(history, minOverAll, maxOverAll);
	
//		float delta = length(velocity);
//		float4 median = (minOverAll + maxOverAll)*0.5;
//		float nudge = saturate(1 - 50*delta) * _ToleranceMargin;
//		float4 nudgeDown = median + (minOverAll - median-0.05) * nudge;
//		float4 nudgeUp   = median + (maxOverAll - median+0.05) * nudge;
//		//history = clamp(history, minCorners - nudge, maxCorners + nudge);
//		history = clamp(history, nudgeDown, nudgeUp);
	
	float4 result = lerp(mainTex, history, confidence - confidence * _Response);
	
//	result.rgb /= 1 - DisneyLuminance(result.rgb);

	result.rgb =  result.rgb / (k + result.a);

	return result;
}

float4 fragCombineTemporal(VertexOutput i) : SV_Target {
	return combineTemporal(CAT_PASS_TEX_INFO(_MainTex), _ReflectionsTex, i.uv);
//	return combineTemporal(CAT_PASS_TEX_INFO(_TempTexture_This_texture_is_never_going_to_be_directly_referenced0x), _MainTex, i.uv);
}

//----------------------------------------------------------------------------------------------------------------------
//----------------------------------------------------------------------------------------------------------------------

static const int CAMERA_BLUR_SAMPLE_COUNT = 7;
float4 fragCameraBlur(VertexOutput i) : SV_Target {
	const int sampleRadius = CAMERA_BLUR_SAMPLE_COUNT / 2;
	const float sumWeights[5] = { 1.17351180, 3.1735118, 4.41107946,  4.96715398, 5.14858994 }; const float sW = sumWeights[sampleRadius];
	const float normWeights[9] = { 0.09071798/sW, 0.27803726/sW, 0.61878383/sW, 1.00000000/sW, 1.17351180/sW, 1.00000000/sW, 0.61878383/sW, 0.27803726/sW, 0.09071798/sW };
	//							   0.01826357314, 0.05597516427, 0.12457512541, 0.20132252876, 0.23625436311,
	float4 uvTap = i.uv.xyxy;
	float4 color1 = 0, color2 = 0;
	float4 sumColor = 0;
	
	color1   = Tex2Dlod(_MainTex, uvTap.xy, _MipLevel);
	sumColor = color1 * normWeights[0 + 4];
	UNITY_UNROLL
	for (int k = 1; k <= sampleRadius; k++) {
		uvTap    = uvTap + _BlurDir*1;
		color1   = Tex2Dlod(_MainTex, uvTap.xy, _MipLevel);
		color2   = Tex2Dlod(_MainTex, uvTap.zw, _MipLevel);
		color1   = color1 + color2;
		sumColor = sumColor + color1 * normWeights[k + 4];
	}
	
	return sumColor;
} //484-460

//----------------------------------------------------------------------------------------------------------------------
//----------------------------------------------------------------------------------------------------------------------

half normalWeight(half3 normal, half3 normalPivot) {
	half temp = saturate((dot(normal, normalPivot)));
	return Pow5(Pow5(temp));
}

static const int REFLECTION_BLUR_SAMPLE_COUNT = 7;
float4 fragReflectionBlur(VertexOutput i) : SV_Target {
	const int sampleRadius = REFLECTION_BLUR_SAMPLE_COUNT / 2;
	const float sumWeights[5] = { 1.17351180, 3.1735118, 4.41107946,  4.96715398, 5.14858994 }; const float sW = sumWeights[sampleRadius];
	const float normWeights[9] = { 0.09071798/sW, 0.27803726/sW, 0.61878383/sW, 1.00000000/sW, 1.17351180/sW, 1.00000000/sW, 0.61878383/sW, 0.27803726/sW, 0.09071798/sW };
	//							   0.01826357314, 0.05597516427, 0.12457512541, 0.20132252876, 0.23625436311,
	float4 uvTap = i.uv.xyxy;
	float4 color1 = 0, color2 = 0;
	float weight1 = 0, weight2 = 0;
	float4 sumColor = 0;
	float  sumWeight = 0;
	
	color1    = Tex2Dlod(_MainTex, uvTap.xy, _MipLevel);
	weight1   = normWeights[0 + 4];
	sumColor  = color1 * weight1;
	sumWeight = weight1;
	float3 pivotNormal = GetNormal(uvTap.xy);
	UNITY_UNROLL
	for (int k = 1; k <= sampleRadius; k++) {
		uvTap     = uvTap + _BlurDir*0.5;
		color1    = Tex2Dlod(_MainTex, uvTap.xy, _MipLevel);
		color2    = Tex2Dlod(_MainTex, uvTap.zw, _MipLevel);
		weight1   = normalWeight(GetNormal(uvTap.xy), pivotNormal) * normWeights[k + 4];
		weight2   = normalWeight(GetNormal(uvTap.zw), pivotNormal) * normWeights[k + 4];
		color1    = color1 * weight1 + color2 * weight2;
		weight1   = weight1 + weight2;
		sumWeight = sumWeight + weight1;
		sumColor  = sumColor  + color1 ;
	}
	
	return sumColor / max(1e-5, sumWeight);
}

//----------------------------------------------------------------------------------------------------------------------
//----------------------------------------------------------------------------------------------------------------------

half4 fragComposeAndApplyReflections(VertexOutputFull i) : SV_Target {
	//i.uv = SnapToPixel(i.uv, _HitTex_TexelSize);
	CAT_GET_GBUFFER(s, i.uv)
	
	float roughness = 1 - s.smoothness;
	half confidence = 1 - Pow3(InvLerpSat(0.875*(1-MIN_SMOOTHNESS), 1-MIN_SMOOTHNESS, roughness));
	
	half4 composedRefl = Tex2Dlod(_ReflectionsTex, i.uv, 0);
//	composedRefl.a = Pow2(composedRefl.a);
	composedRefl.a = max(0, composedRefl.a * confidence * _Intensity);
	
	float3 wsViewDir = normalize(i.wsRay);
	
	if (_CullBackFaces) {
		float3 wsReflDir = normalize(reflect(wsViewDir, normalize(s.normal)));
		composedRefl.a *= saturate(dot(wsViewDir, wsReflDir) * 2.0);
	}
	
	
	// Let core Shader functions do the dirty work of applying the BRDF
	CAT_DECLARE_OUTPUT(UnityGI, gi);
	ResetUnityGI(/*out*/ gi);
	gi.indirect.specular = composedRefl.rgb;
	
	s.occlusion = 1-(0.5-0.5*s.occlusion) ; // JCO@@@INVESTIGATE: is this REALLY correct? I believe so, but nor 100 % shure...
	half3 reflectionFinal = applyLighting(s, wsViewDir, gi).rgb;

	float3 reflProbes = tex2D(_CameraReflectionsTexture, i.uv).rgb;
	return float4(reflectionFinal - reflProbes, 1-(1-composedRefl.a));
}

//----------------------------------------------------------------------------------------------------------------------
//----------------------------------------------------------------------------------------------------------------------
		
float4 getTexelSize(float mip) {
	float4 texelSize = _ReflectionsTex_TexelSize;
	float mult = exp2(mip);
	texelSize.xy *= mult;
	texelSize.zw /= mult;
	return texelSize;
}

half4 sampleReflection(float2 uv, float mip) {
		return Tex2Dlod(_ReflectionsTex, uv, mip);
}

half4 composeReflectionsMip(float2 uv, float mip, half3 normalPivot) {
	float4 texelSize = getTexelSize(mip);
	float2 pos = uv * texelSize.zw;
	float2 texel1Centered = floor(pos - 0.5) + 0.5;

	float2 uvs[4] = {
		(texel1Centered + float2(0, 0)) * texelSize.xy,
		(texel1Centered + float2(0, 1)) * texelSize.xy,
		(texel1Centered + float2(1, 0)) * texelSize.xy,
		(texel1Centered + float2(1, 1)) * texelSize.xy
	};

	float4 sumColor = 0;
	float sumWeight = 0;
	
//	UNITY_UNROLL_N(4)
	UNITY_LOOP
	for (int i = 0; i < 4; i++) {
		half4 color = sampleReflection(uvs[i], mip);
		half3 normal = GetNormal(uvs[i]);
		
		float weight = 1;
		weight *= normalWeight(normal, normalPivot);
//			weight *= max(0.125, color.a);
		weight = max(0.00001, weight);
		float2 bilateralWeights = ((1-(uvs[i] * texelSize.zw - texel1Centered)) - (pos - texel1Centered));
		weight *= abs(bilateralWeights.x * bilateralWeights.y);
		
		color.rgb *= color.a;
		sumColor += color * weight;
		sumWeight += weight;
	}
	sumColor /= sumWeight;
//	sumColor *= sumColor.a;
	return sumColor;
	
}

static const half maxReflectionMipLevel = 2.0 - 1.0;
half getMipLvl(float3 vsRay, half smoothness, float2 uv) {
	float4 hitPacked = Tex2Dlod(_HitTex, uv, 0);
	float2 hitUV = hitPacked.xy;
	float hitZ   = sampleDepthLod(_DepthTexture, hitUV, 0);
	
	float3 vsHitPos = ScreenToViewPos(GetScreenPos(hitUV, hitZ));
	float3 vsPos = vsRay * sampleEyeDepthLod(_DepthTexture, uv, 0);
	
	float rayLen = length(vsHitPos - vsPos);	//rayLen += 0.0625;
	float viewDist = length(vsPos);
	
	float roughness = 1 - smoothness;
	roughness = pow(roughness, 4.0/3.0);
//	JCO@@@TODO:
	half area = abs(roughness * rayLen * _PixelsPerMeterAtOneMeter) / (viewDist + rayLen);
	half mip = log2(area/16.0 + 15.0/16.0);
	return clamp(mip, 0, maxReflectionMipLevel);
}

half4 composeReflections(float3 vsRay, float3 pivotNormal, half smoothness, float2 uv) {
	float mipLevel = 0;
	float mipMin = 0;
	float mipMax = 0;
	if (_UseReflectionMipMap) {
		mipLevel = getMipLvl(vsRay, smoothness, uv);
		mipMin = floor(mipLevel);
		mipMax = min(maxReflectionMipLevel, mipMin+1);
	}
	half4 composedRefl = composeReflectionsMip(uv, mipMin, pivotNormal);
	if (_UseReflectionMipMap) {
		half4 composedRefl2 = composeReflectionsMip(uv, mipMax, pivotNormal);
		composedRefl = lerp(composedRefl, composedRefl2, saturate(mipLevel - mipMin));
	}
	
	half confidence = 1 - Pow3(InvLerpSat(0.875*1 - 0.875*MIN_SMOOTHNESS, 1-MIN_SMOOTHNESS, 1 - smoothness));
	composedRefl *= max(0, confidence * _Intensity);
	return composedRefl;
}

//----------------------------------------------------------------------------------------------------------------------
//----------------------------------------------------------------------------------------------------------------------

half4 fragDebug(VertexOutputFull i ) : SV_Target {
	half4 frag = 0;
	
//	public enum DebugMode {
//		AppliedReflections = 6,
//		AppliedReflectionsAndCubeMap = 7,
//		RayTraceConfidence = 5,
//		PDF = 3,
//		MipsRGB = 2,
//		MipLevel = 4,
//	}
	CAT_GET_GBUFFER(s, i.uv)
	
	float depth = sampleDepthLod(_DepthTexture, i.uv, 0);
	float3 vsPos = ScreenToViewPos(GetScreenPos(i.uv, depth));
	
	float4 hitPacked = Tex2Dlod(_HitTex, i.uv, 0);
	float2 hitUV = hitPacked.xy;
	float hitZ   = sampleDepthLod(_DepthTexture, hitUV, 0);
	float3 vsHitPos = ScreenToViewPos(GetScreenPos(hitUV, hitZ));
	float rayLength = length(vsHitPos - vsPos);
	
	float4 pureRefl = Tex2Dlod(_ReflectionsTex, i.uv, clamp(_MipLevelForDebug, 0, maxReflectionMipLevel));
	pureRefl *= pureRefl.a;
	
	float4 reflections = fragComposeAndApplyReflections(i);
	float3 reflProbes = tex2D(_CameraReflectionsTexture, i.uv).rgb;
	half dummyConfidence;
	half3 values[7] = {
/*7*/	half3(reflections.rgb)*reflections.a + reflProbes,
/*6*/	half3(reflections.rgb + reflProbes)*reflections.a,
/*1*/	half3(pureRefl.rgb),
/*5*/	half3(tex2D(_HitTex, i.uv).zzz),
/*3*/	half3(Pow5(Compress(tex2D(_HitTex, i.uv).w)).xxx),
/*2*/	half3(Tex2Dlod(_MainTex, i.uv, _UseCameraMipMap ? _MipLevelForDebug : 0).rgb),
/*4*/	half3(getMipLevelResolve(vsHitPos, rayLength, s.smoothness, maxCameraMipLevel).xxx / maxCameraMipLevel),
	};
	
	float3 result = values[(int)_DebugMode];
	
	//result.x = GammaToLinearSpaceExact(result.x);
	//result.y = GammaToLinearSpaceExact(result.y);
	//result.z = GammaToLinearSpaceExact(result.z);
	
	//result = uv.x > 0.0 ? float3(1, 1, 1)*0.5 : result;
	return half4(result, 1);
//	return pow(half4(abs(frag.rgb), 0), 1);
}


#endif // CAT_SSR_INCLUDED