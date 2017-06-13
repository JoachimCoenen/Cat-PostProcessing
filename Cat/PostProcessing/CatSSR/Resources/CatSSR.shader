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

Shader "Hidden/Cat SSR" {
	Properties {
	//	_MainTex ("Base (RGB)", 2D) = "black" {}
		_Intensity("Intensity", FLOAT) = 1
		_IsBackFaceCullingEnabled("is BackFace Culling Enabled", FLOAT) = 1
		_NoiseStrength("Noise Strength", FLOAT) = 00.5
		_MaxReflectionDistance("Max Reflection Distance", FLOAT) = 100
	}
	
	CGINCLUDE
		#define MIN_SMOOTHNESS 0.25*0
		
		#include "UnityCG.cginc"
		#include "UnityStandardUtils.cginc"
		#include "UnityGlobalIllumination.cginc"

		#include "../../../Includes/LightingInterface.cginc"
		
		
		#define CAT_GET_GBUFFER(s, uv)													\
			half4 gbuffer0##s = tex2Dlod(_CameraGBufferTexture0,    float4(uv, 0, 0));	\
			half4 gbuffer1##s = tex2Dlod(_CameraGBufferTexture1,    float4(uv, 0, 0));	\
			half4 gbuffer2##s = tex2Dlod(_CameraGBufferTexture2,    float4(uv, 0, 0));	\
			half4 gbuffer3##s = tex2Dlod(_CameraReflectionsTexture, float4(uv, 0, 0));	\
			GBufferData s = unpackGBuffer(gbuffer0##s, gbuffer1##s, gbuffer2##s, gbuffer3##s);

		
				sampler2D		_MainTex;						float4	_MainTex_TexelSize;
	/*	uniform sampler2D		_NormalsPacked;	// */ #define _NormalsPacked _CameraGBufferTexture2
		uniform sampler2D		_HitTex;				uniform float4	_HitTex_TexelSize;
		uniform sampler2D		_ReflectionMip0;		uniform float4	_ReflectionMip0_TexelSize;
		uniform sampler2D		_ReflectionMip1;		uniform float4	_ReflectionMip1_TexelSize;
		uniform sampler2D		_ReflectionMip2;		uniform float4	_ReflectionMip2_TexelSize;
		uniform sampler2D		_ReflectionMip3;		uniform float4	_ReflectionMip3_TexelSize;
		uniform sampler2D		_ReflectionMip4;		uniform float4	_ReflectionMip4_TexelSize;
		uniform sampler2D		_ComposedReflections;
		uniform sampler2D		_CameraGBufferTexture0;	uniform float4	_CameraGBufferTexture0_TexelSize;
		uniform sampler2D		_CameraGBufferTexture1;
		uniform sampler2D		_CameraGBufferTexture2;
		uniform sampler2D		_CameraReflectionsTexture;
		uniform sampler2D_half	_CameraMotionVectorsTexture;
		uniform UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

	//	float4x4		_ViewProjectionMatrix;
	//	float4x4		_InverseProjectionMatrix;
	//	float4x4		_InverseViewProjectionMatrix;
		
		half			_UseMips;
		half			_PixelsPerMeterAtOneMeter;
		
		half			_RayLengthFade;
		float			_MaxReflectionDistance;
		float			_ReflectionDistanceFade;
		float			_EdgeFactor; 
		
		half			_Intensity;
		half			_MinPixelStride;
		half			_MaxPixelStride;
		float			_NoiseStrength;
		int				_IsBackFaceCullingEnabled;
		//float			_SmoothnessRange;
		int				_IsVelocityPredictionEnabled;
		int				_NumSteps;
		
		uniform float	_NormalsScale;
		float2			_BlurDir;
		int				_MipLvl;
		
		int				_DebugMode;
		int				_MipLevelForDebug;
		
		#define _GBuffer_TexelSize _CameraGBufferTexture0_TexelSize

		float2 snapToPixel(float2 uv, float4 texelSize) {
			float2 pos = uv * texelSize.zw;
			// snap to pixel center:
			return ( floor(pos - 0) + 0.5 ) * texelSize.xy;
		}

		float2 snapBetweenPixel(float2 uv, float4 texelSize) {
			float2 pos = uv * texelSize.zw;
			// snap to pixel center:
			return ( floor(pos - 0.5) + 1.0 ) * texelSize.xy;
		}
		
		#include "CatRayTraceLib.cginc"
		
		
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
		
		float3 ScreenToViewPos(float3 screenPos) {
			float linEyeZ = -LinearEyeDepth(screenPos.z);
			float3 vsPos;
			vsPos.x = (-screenPos.x - unity_CameraProjection[0][2]) / unity_CameraProjection[0][0] * linEyeZ;
			vsPos.y = (-screenPos.y + unity_CameraProjection[1][2]) / unity_CameraProjection[1][1] * linEyeZ;
			vsPos.z = -linEyeZ;
			return vsPos;
		}
		
		float3 WorldToScreenPos(float3 worldPos) {
		//	float4 screenPos = mul(_ViewProjectionMatrix, float4(worldPos, 1));
		
	//	eyeZ = 1 / (_ZBufferParams.z * z + _ZBufferParams.w)
	//	_ZBufferParams.z * z + _ZBufferParams.w = 1 / eyeZ
	//	_ZBufferParams.z * z = 1 / eyeZ - _ZBufferParams.w
	//	z = (1 / eyeZ - _ZBufferParams.w) / _ZBufferParams.z
			
			
			float4 vsPos = mul(unity_WorldToCamera, float4(worldPos, 1));
			return ViewToScreenPos(vsPos);
		}
		
		float3 ScreenToWorldPos(float3 screenPos) {
			float3 vsPos = ScreenToViewPos(screenPos);
			
			float4 wsPos = mul(unity_CameraToWorld, float4(vsPos, 1));
			return wsPos.xyz / wsPos.w;
		}
		
		float RayAttenBorder (float2 pos, float2 dir, float value) {
			half2 rborder = (dir > 0) ? 1 : 0;
			return InvLerpSat(0, value, MinC(abs(rborder - pos)) * 1);//.41);
			
		//	float2 pos2 = abs(pos * 2 - 1);
		//	pos2 = InvLerpSat(1, 1-value, pos2);
		//	return MinC(pos2);
		}

		
		struct VertexInput {
			float4 vertex : POSITION;
			float2 texcoord : TEXCOORD;
		};

		struct VertexOutput {
			float4 pos : POSITION;
			float2 uv : TEXCOORD0;
//			float4 worldPos : TEXCOORD1;
		};

		VertexOutput vert( VertexInput v ) {
			VertexOutput o;
		//	o.pos = UnityObjectToClipPos(v.vertex);
		//	o.uv = v.texcoord;
			
			o.pos = float4(v.vertex.xy, 0, 1);
//			o.pos = UnityObjectToClipPos(v.vertex);
			o.uv = v.texcoord.xy;
			#if UNITY_UV_STARTS_AT_TOP
				o.uv.y = 1-o.uv.y;
			#endif
			
		//	float4x4 ipm = _InverseViewProjectionMatrix;
		//	float4x4 ixm = { ipm._m00_m10_m20_m30, ipm._m01_m11_m21_m31, ipm._m02_m12_m22_m32, ipm._m03_m13_m23_m33 } ;
		//	float2 ssPos = GetScreenPos(o.uv, 0).xy;
		//	o.worldPos = mul(_InverseViewProjectionMatrix, float4(ssPos, 0, 1));
			return o;
		}

		VertexOutput vert2( VertexInput v ) {
			VertexOutput o;
			o.pos = UnityObjectToClipPos(v.vertex);
			o.uv = v.texcoord;
			return o;
		}

		float2 getPrevUV(float2 uv, out float confidence) {
			float2 velocity = !_IsVelocityPredictionEnabled ? 0 : clamp(tex2D(_CameraMotionVectorsTexture, uv).xy, -2.0, 2.0);
			velocity = abs(velocity) >= 1.99 ? 0 : velocity;

			float2 prevUV = uv - velocity.xy;
			confidence = (1-any(abs(prevUV-0.5) > 0.5)) * (1-saturate(length(velocity)*3));
			return prevUV;
		}

		float encodeRayLength(float len) {
			return len / (1 + len);
		}
		
		float decodeRayLength(float len) {
			return len / (1 - len);
		}

		static const float2x2 vibrationCols = float2x2( float2( 1.00, 1.00), float2( 1.00,-1.00) ); //
		static const float2x2 vibrationRows = float2x2( float2( 1.00, 1.00), float2(-1.00, 1.00) ); //
		static const float2x2 vibrationFrms = float2x2( float2( 0.50, 0.25), float2(-0.25, 0.50) ); // frames A & frame B

		static const float2x2 vibration2Cols = float2x2( float2(-0.50, 1.00), float2(-1.00,-0.50) ); //
		static const float2x2 vibration2Frms = float2x2( float2( 1.00, 1.00), float2(-1.00,-1.00) ); // frames A & frame B
		
		static const float2x2 devibrationCols = float2x2( float2(-1.00,-1.00), float2(-1.00, 1.00) ); //
		static const float2x2 devibrationRows = float2x2( float2( 1.00, 1.00), float2(-1.00, 1.00) ); //
		static const float2x2 devibrationFrms = float2x2( float2( 1.00, 0.00), float2( 0.00, 1.00) ); // frames A & frame B
		

		
		float4 DownsampleNormalsX(VertexOutput i) : SV_Target {
			float2 uv = i.uv;
			half radius = _NormalsScale * 0.5 - 0.5;
			int rMin = - radius;
			int rMax = radius;// + 0.25;
			
			float3 normalSum = 0;
			for (int i = rMin; i <= rMax; i+=1) {
				float2 uv_sample = uv + float2(i * _MainTex_TexelSize.x, 0);
				normalSum += Tex2Dlod(_MainTex, uv_sample, 0).xyz * 2 - 1;
			}
			half4 result = 0;
			result.xyz = normalize(normalSum) * 0.5 + 0.5;
			return result;
		}
		
		float4 DownsampleNormalsY(VertexOutput i) : SV_Target {
			float2 uv = i.uv;
			half radius = _NormalsScale * 0.5 - 0.5;
			int rMin = - radius;
			int rMax = radius;// + 0.25;
			
			float3 normalSum = 0;
			for (int i = rMin; i <= rMax; i+=1) {
				float2 uv_sample = uv + float2(0, i * _MainTex_TexelSize.y);
				normalSum += Tex2Dlod(_MainTex, uv_sample, 0).xyz * 2 - 1;
			}
			half4 result = 0;
			result.xyz = normalize(normalSum) * 0.5 + 0.5;
			return result;
		}
		
		float4 PackNormals(VertexOutput i) : SV_Target {
			float2 uv = i.uv;
			CAT_GET_GBUFFER(s, uv)
			return float4(s.normal * 0.5 + 0.5, 0);
		}
		
		
		float4 rayTrace(VertexOutput i) : SV_Target {
			float2 uv = i.uv;
			float encodedRayLength = 0;
			float confidence = 0;
			float2 uvHit = uv;
			
			CAT_GET_GBUFFER(s, uv)
			
			float2 pos = uv * _HitTex_TexelSize.zw;

			float depth = sampleDepthLod(_CameraDepthTexture, uv, 0);
			float3 ssPos = GetScreenPos(uv, depth);
			float3 wsPos = ScreenToWorldPos(ssPos);
			
			float3 wsViewDir = (UnityWorldSpaceViewDir(wsPos));
			float3 wsRayDir = normalize(getReflectionVector(s, wsViewDir));
			float3 ssRay = 0;
			ssRay.z = -sign(WorldToScreenPos(-wsRayDir + wsPos).xyz).z;
			ssRay = ssRay.z*normalize(WorldToScreenPos(ssRay.z*wsRayDir + wsPos).xyz - ssPos);
		//	ssRay.xyz *= sign(ssRay.z);
			
			float3 rbor = (ssRay.xyz >= 0.0f) ? float3(1, 1, 0.25) : float3(-1, -1, 1e-5);
			rbor -= ssPos.xyz;
			rbor /= abs(ssRay.xyz);
			rbor = abs(rbor);
			ssRay *= clamp(MinC(rbor.xyz), 0.125, 16);
			
			half reflectionDistance = length(UnityWorldSpaceViewDir(wsPos));
			if (reflectionDistance <= _MaxReflectionDistance && s.smoothness >= MIN_SMOOTHNESS) {
				
				float3 wsPosBumped = wsPos + normalize(s.normal) * max(0.01*LinearEyeDepth(depth), 0.001);
				float3 ssPosBumped = WorldToScreenPos(wsPosBumped);
				float jitter = 0.5*(noiseSimple(uv + depth) * _NoiseStrength);
				////////////////////////////////////////////////////////////////////////////////////////////////////////////
				float4 rayHit = RayTrace(
						_NumSteps, 0.5, 
						_MinPixelStride, 
						_MaxPixelStride, 
						ssRay, ssPosBumped, CAT_PASS_DEPTHT_EXTURE(_CameraDepthTexture), 
						_GBuffer_TexelSize, jitter
				);
				
				if (rayHit.w < _NumSteps) {
					// rayHit.xyz += (ssPos - ssPosBumped)*0.5;
					uvHit = rayHit.xy;
					
					confidence = 1;
					if (_IsBackFaceCullingEnabled) {
						//CAT _GET_GBUFFER(hitS, rayHit.xy)
						float3 hitNormal = Tex2Dlod(_NormalsPacked, rayHit.xy, 0).xyz * 2 - 1;
						confidence *= step(0, -dot(wsRayDir, hitNormal));
					}
					confidence *= Pow2(1 - Pow2(1 - InvLerpSat(0, _RayLengthFade * _NumSteps, _NumSteps - rayHit.w)));
					confidence *= (1 - Pow2(1 - (InvLerpSat(0, _ReflectionDistanceFade * _MaxReflectionDistance, _MaxReflectionDistance - reflectionDistance))));
					// Fake fresnel fade
					float3 csE = -normalize(UnityWorldSpaceViewDir(wsPos));
				//	confidence *= lerp(0.0, 1, abs(dot(wsRayDir, -csE)) );
				
				
					half border = RayAttenBorder(uvHit, ssRay.xy, _EdgeFactor);
					confidence *= Pow2(1 - Pow2(1 - (border)));
					confidence = saturate(confidence);
				//	if (!any(gbuffer0s.rgba)) {
				//		rayHit.w = min(rayHit.w, 0.5);
				//	}
			
					float3 wsSamplePos = ScreenToWorldPos(GetScreenPos(rayHit.xy, rayHit.z));
					float3 wsRay = (wsSamplePos - wsPos);
					encodedRayLength = encodeRayLength(length(wsRay.xyz));
				}
			}
			
			half3 wsRayMax = (ScreenToWorldPos(ssRay + ssPos) - wsPos);
			half maxRayLength = length(wsRayMax);
			float encodedFinalRayLength = lerp(encodeRayLength(maxRayLength), encodedRayLength, confidence);
			return float4(uvHit, encodedFinalRayLength, confidence);
		}
				
		static const int NUM_RESOLVE_TAPS = 4;
		// Same as used in CameraMotionBlur.shader
		static const float2 resolveSamples[NUM_RESOLVE_TAPS] = {
			float2(-0.326212,-0.40581),
	//		float2(-0.840144,-0.07358),
	//		float2(-0.695914,0.457137),
		//	float2(-0.203345,0.620716),
	//		float2(0.96234,-0.194983),
	//		float2(0.473434,-0.480026),
			float2(0.519456,0.767022),
	//		float2(0.185461,-0.893124),
	//		float2(0.507431,0.064425),
			float2(0.89642,-0.59771),
	//		float2(-0.32194,-0.932615),
			float2(-0.791559,0.412458)
		};

		void resolve( VertexOutput i, out float4 outReflection) {
			float2 uv = i.uv;
			
			float4 hitPacked = tex2D(_HitTex, uv);
			
			float confidence = 1;
			#if defined(CAT_TEMPORAL_SSR_ON)
				float2 uvHit = getPrevUV(hitPacked.xy, /*out*/confidence);
			#else
				float2 uvHit = hitPacked.xy;
			#endif
	//		uvHit = snapBetweenPixel(uvHit, _MainTex_TexelSize);
			
			confidence *= saturate(hitPacked.w);

			
		#if 0
			half3 reflection = tex2D(_MainTex, uvHit).rgb;
		#else
			half3 reflection = 0;
			float2 sampleRadius = _MainTex_TexelSize.xy;// * 0.6666667;
			UNITY_UNROLL
			for (int k = 0; k < NUM_RESOLVE_TAPS; k++) {
				float2 p = uvHit + resolveSamples[k] * sampleRadius;
				float3 tap = Tex2Dlod(_MainTex, p, 0).rgb;
				tap.rgb /= 1 + DisneyLuminance(tap.rgb);
				reflection += tap;
			}
			reflection /= NUM_RESOLVE_TAPS - DisneyLuminance(reflection);
		#endif
			
	//		CUM = Em + Or - Rp * Or.a
	//		REF = (1-c) * Rp + c * CUM
	//		REF = (1-c) * Rp + c * (Em + Or - Rp*Or.a)
	//		REF = Rp + (Em + Or - Rp*Or.a - Rp)*c
			
			float lRefl = max(0.001, DisneyLuminance(reflection));
			float3 cRefl = reflection / lRefl;
//			lRefl = min(lRefl, max(1, 2 * Pow2(lRefl) / (1 + Pow2(lRefl)) ));
			lRefl = min(lRefl, max(1, 3 * Pow2(lRefl) / (2.25 + Pow2(lRefl)) ));
	//		reflection = cRefl * lRefl;
			
			confidence = saturate(confidence * _Intensity);
			confidence = sqrt(confidence);
			
			
			float3 reflProbes = tex2D(_CameraReflectionsTexture, uv);
			outReflection.rgb = lerp(reflProbes, reflection, confidence );
			outReflection = float4(outReflection.rgb, (confidence));
		}
		void resolve1( VertexOutput i, out float4 outReflection : SV_Target0) {
			resolve(i, /*out*/ outReflection);
		}

		half normalWeight(half3 normal, half3 normalPivot) {
			half temp = saturate((dot(normal, normalPivot)));
			return 1;//Pow2(temp);
		}
		
		float4 upsampleRayHits(VertexOutput i) : SV_Target0 {
			float2 uv = i.uv;
			
			float2 pos = uv * _MainTex_TexelSize.zw;
			float2 texel1Centered = floor(pos - 0.5) + 0.5;

			float3 pivotNormal = Tex2Dlod(_NormalsPacked, uv, 0).xyz * 2 - 1;
		
			float2 uvs[4] = {
				(texel1Centered + 1 * float2(0, 0)) * _MainTex_TexelSize.xy,
				(texel1Centered + 1 * float2(0, 1)) * _MainTex_TexelSize.xy,
				(texel1Centered + 1 * float2(1, 0)) * _MainTex_TexelSize.xy,
				(texel1Centered + 1 * float2(1, 1)) * _MainTex_TexelSize.xy
			};

			float4 sumHit = 0;
			float4 sumMis = 0;
			float weightHit = 0;
			float weightMis = 0;
			
			UNITY_UNROLL_N(4)
			for (int i = 0; i < 4; i++) {
				float4 hit = Tex2Dlod(_MainTex, uvs[i], 0);
				float3 normal = Tex2Dlod(_NormalsPacked, uvs[i], 0).xyz * 2 - 1;
				
				float2 bilateralWeights = 1 - 1 * abs(uvs[i] * _MainTex_TexelSize.zw  - pos);
				
				float weight = 1;
				weight *= Pow2(normalWeight(normal, pivotNormal));
				weight = max(0.000001, weight);
				weight *= (bilateralWeights.x * bilateralWeights.y);
				
				if (hit.w > -0) {
					sumHit += hit * weight;
					weightHit += weight;
				} else {
					sumMis += hit * weight;
					weightMis += weight;
				}
			}
		
			float4 result = ((weightHit >= weightMis) ? sumHit : sumMis) / max(weightHit, weightMis);
			result.zw = (sumHit.zw + sumMis.zw) / (weightHit + weightMis);
			return result;
		}
		
		
		float PDF(float x, float radius) {
			float sigma = radius;
			//return sqrt(1 / sqrt(2*Pow2(sigma)*UNITY_PI) * exp(-Pow2(x-0) / (2*sigma)));
			
			//const float gaussWeights[5] = { 0.225, 0.150, 0.110, 0.075, 0.0525 };
			//return gaussWeights[(int)abs(x)];
			half r4 = 0.875;///Pow4(roughness*0.5+0.5);
			return r4 * 0.25 / ( 1 * Pow2( 1 - Pow2(saturate(1-abs(x)/radius)) * (1 - r4) ));
			
		}
		float PDF1(int i, half radius) {
			half r4 = (0.4);
			half x = half(i) / (radius + 1.0);
			half theta = saturate(1-x*x);//cos(x * UNITY_PI * 0.5);
			return 1 * r4 * 0.25 / ( 1 * Pow2( 1 - Pow2(theta) * (1 - r4) ));
			
		}
		float PDF2(int i, half radius) {
			float sigma = 1;
			half x = half(i) / radius * 2.0;
			return 1 / (sigma * sqrt(2*UNITY_PI)) * exp(-0.5 * Pow2(x / sigma));
			
			//const float gaussWeights[5] = { 0.225, 0.150, 0.110, 0.075, 0.0525 };
			//return gaussWeights[(int)abs(x)];
			
		}
		float PDF3(int i, half radius) {
			float sigma = 1;
			half x = half(i) / radius * 3.0;
			return sqrt( 1 / (sigma * sqrt(2*UNITY_PI)) * exp(-0.5 * Pow2(x / sigma)) );
			// 86
			//const float gaussWeights[5] = { 0.225, 0.150, 0.110, 0.075, 0.0525 };
			//return gaussWeights[(int)abs(x)];
			
		}
		float PDF4(int i) {
		//	const float gaussWeights[5] = { 0.225, 0.150, 0.110, 0.075, 0.0525 };
			const float gaussWeights[5] = { 0.225, 0.150, 0.110, 0.0525, 0.0025 };
			return gaussWeights[abs(i)];
		}
		float PDF5(int i) {
			const float weights[7] = {0.001f, 0.028f, 0.233f, 0.474f, 0.233f, 0.028f, 0.001f};
			return weights[3-abs(i)];
		}
		float PDF6(int i) {
			const float weights[5] = {
			//	sqrt(0.00190727056117187),
			//	sqrt(0.0179156242358743 ),
			//	sqrt(0.0887366677435645 ),
			//	sqrt(0.231753242209186  ),
				sqrt(0.319153824321146  ) / (2.4785692826521797),
				sqrt(0.231753242209186  ) / (2.4785692826521797),
				sqrt(0.0887366677435645 ) / (2.4785692826521797),
				sqrt(0.0179156242358743 ) / (2.4785692826521797),
				sqrt(0.00190727056117187) / (2.4785692826521797),
			};
			return weights[abs(i)];
		}

		void sampleForBlur(float weight, float2 uv, half3 pivotNormal, inout float4 sumColorPure, inout float4 sumColorWeight, inout float sumWeight) {
			half4 color = Tex2Dlod(_MainTex, uv, 0);
			half3 normal = Tex2Dlod(_NormalsPacked, uv, 0).xyz * 2 - 1;
			weight *= normalWeight(normal, pivotNormal);
			
			color.rgb /= 1 + DisneyLuminance(color.rgb);
			sumColorPure += color;
			sumColorWeight += color * weight;
			sumWeight += weight;
		}
		
		float2 getBlurDirection(VertexOutput i, GBufferData g) {
		//	float3 wsViewDir = normalize(i.worldPos.xyz);
		//	half aspect = abs(dot(wsViewDir, g.normal));
		//	
		//	half2 ssn = 0;
		//	ssn.x = dot(_ViewProjectionMatrix[0].xyz, g.normal);
		//	ssn.y = dot(_ViewProjectionMatrix[1].xyz, g.normal);
		//	ssn = normalize(ssn * _GBuffer_TexelSize.zw);
		//	
		//	float aspMul = (0.5+0.5*aspect);
		//	half2 dir1 = ssn.yx * half2(1,-1);
		//	half2 dir2 = ssn.xy * half2(1, 1);
		//	
		//	float2 blurDir = _MainTex_TexelSize.xy * ((_BlurDir.x > 0) ? dir1 * 1 : dir2 / aspMul);
		//	float2 dirAdd = _MainTex_TexelSize.xy * 0.25 * ((_BlurDir.x > 0) ? dir1 : dir2);
		//	
			float2 blurDir = _MainTex_TexelSize.xy * _BlurDir.xy;
			return blurDir;
		}
		
		float3 getSamplePositionAndWeight(float2 uv, float2 blurDir, int k) {
			const float weights[9] = 
		//	{ 1.00000000, 1.00000000, 1.00000000, 1.00000000, 1.00000000, 1.00000000, 1.00000000, 1.00000000, 1.00000000, };		
		//	{ 0.00000000, 0.00000000, 0.00000000, 0.00000000, 1.00000000, 0.00000000, 0.00000000, 0.00000000, 0.00000000, };		
		//	{ 0.05250000, 0.07500000, 0.11000000, 0.15000000, 0.22500000, 0.15000000, 0.11000000, 0.07500000, 0.05250000, };	// Postprocessing Stack
		//	{ 0.00000000, 0.00100000, 0.02800000, 0.23300000, 0.47400000, 0.23300000, 0.02800000, 0.00100000, 0.00000000, };		// UnitySSR
		//	{ 0.00190727, 0.0179156 , 0.0887367 , 0.231753  , 0.319154  , 0.231753  , 0.0887367 , 0.0179156 , 0.00190727, };
		//	{ 0.04367230, 0.13384917, 0.29788706, 0.48140731, 0.56493716, 0.48140731, 0.29788706, 0.13384917, 0.04367230, };
	//		{ 0.09071798, 0.27803726, 0.61878383, 1.00000000, 1.17351180*0.5, 1.00000000, 0.61878383, 0.27803726, 0.09071798, };
			{ 0.09071798, 0.27803726, 0.61878383, 1.00000000, 1.17351180*1.5, 1.00000000, 0.61878383, 0.27803726, 0.09071798, };
		//	{ sqrt(0.00190727), sqrt(0.0179156), sqrt(0.0887367), sqrt(0.231753), sqrt(0.319154), sqrt(0.231753), sqrt(0.0887367), sqrt(0.0179156), sqrt(0.00190727) };
			
			float2 uvTap = uv + 2*blurDir * k;// - blurDir * 0.5 * sign(k);
			return float3(uvTap, weights[k + 4]);
		}
		
		half4 tapBlur(float2 uv, float2 blurDir, int k, GBufferData g, out half weight) {
			float3 uvWeight = getSamplePositionAndWeight(uv, blurDir, k);
			
			half3 tapNormal = tex2D(_NormalsPacked, uvWeight.xy).xyz * 2 - 1;
			weight = uvWeight.z * (0 == k ? 1 : normalWeight(tapNormal, g.normal));
			return tex2D(_MainTex, uvWeight.xy);
		}
		
		struct BlurAccumulation {
			float4 colorPure;
			float4 colorWeighted;
			float weight;
		};
		
		void addTap(half4 color, half weight, inout BlurAccumulation sum) {
			sum.colorPure += color;
			sum.colorWeighted += color * weight;
			sum.weight += weight;
		
		}
		
		float4 mipMapBlurCompressor(VertexOutput i) : SV_Target {
			const int sampleCount = 7;
			CAT_GET_GBUFFER(pivotS, i.uv)
			pivotS.normal = tex2D(_NormalsPacked, i.uv).xyz * 2 - 1;;
			if (pivotS.smoothness < MIN_SMOOTHNESS) {
				return tex2D(_MainTex, i.uv);
			}
			
			float2 blurDir = getBlurDirection(i, pivotS);
			BlurAccumulation sum = {{0, 0, 0, 0}, {0, 0, 0, 0}, 0};
			const int sampleRadius = sampleCount / 2;
			UNITY_UNROLL
			for (int k = - sampleRadius; k <= sampleRadius; k++) {
				half weight;
				half4 color = tapBlur(i.uv, blurDir, k, pivotS, /*out*/ weight);
				color.rgb /= 1 + DisneyLuminance(color.rgb);
				addTap(color, weight, /*inout*/ sum);
			}
			
			float4 result = 0;
			if (sum.weight > 0.001) {
				//sumColor.a = min(sumColor.a * sqrt(max(1/sumWeight, 4.0)), 1.0);
				result = sum.colorWeighted / sum.weight;
			} else {
				result = float4(sum.colorPure / float(sampleCount));
			}
			
			result.rgb /= 1 - DisneyLuminance(result.rgb);
			return result;
		}
		
		float4 mipMapBlurVanilla(VertexOutput i) : SV_Target {
			const int sampleCount = 7;
			CAT_GET_GBUFFER(pivotS, i.uv)
			pivotS.normal = tex2D(_NormalsPacked, i.uv).xyz * 2 - 1;;
			if (pivotS.smoothness < MIN_SMOOTHNESS) {
				return tex2D(_MainTex, i.uv);
			}
			
			float2 blurDir = getBlurDirection(i, pivotS);
			BlurAccumulation sum = {{0, 0, 0, 0}, {0, 0, 0, 0}, 0};
			const int sampleRadius = sampleCount / 2;
			UNITY_UNROLL
			for (int k = - sampleRadius; k <= sampleRadius; k++) {
				half weight;
				half4 color = tapBlur(i.uv, blurDir, k, pivotS, /*out*/ weight);
				addTap(color, weight, /*inout*/ sum);
			}
			
			float4 result = 0;
			if (sum.weight > 0.001) {
				//sumColor.a = min(sumColor.a * sqrt(max(1/sumWeight, 4.0)), 1.0);
				result = sum.colorWeighted / sum.weight;
			} else {
				result = float4(sum.colorPure / float(sampleCount));
			}
			
			return result;
		}

		
		static const int NUM_POISSON_TAPS = 4;
		// Same as used in CameraMotionBlur.shader
		static const float2 poissonSamples[NUM_POISSON_TAPS] = {
			float2(-0.326212,-0.40581),
	//		float2(-0.840144,-0.07358),
	//		float2(-0.695914,0.457137),
		//	float2(-0.203345,0.620716),
	//		float2(0.96234,-0.194983),
	//		float2(0.473434,-0.480026),
			float2(0.519456,0.767022),
	//		float2(0.185461,-0.893124),
	//		float2(0.507431,0.064425),
			float2(0.89642,-0.59771),
	//		float2(-0.32194,-0.932615),
			float2(-0.791559,0.412458)
		};

		float4 simpleBlur(VertexOutput i) : SV_Target {
			// Could improve perf by not computing blur when we won't be sampling the highest level anyways
			float4 sum = 0.0;
			float2 sampleRadius = _MainTex_TexelSize.xy * 0.5;

			for (int k = 0; k < NUM_POISSON_TAPS; k++) {
				float2 p = i.uv + poissonSamples[k] * sampleRadius;

				float4 tap = tex2D(_MainTex, p);
				tap.rgb /= 1 + DisneyLuminance(tap.rgb);

				sum += tap;
			}

			float4 result = sum;
			result.a /= float(NUM_POISSON_TAPS);
			result.rgb /= NUM_POISSON_TAPS - DisneyLuminance(result.rgb);
			return result;
		}

		float4 simpleBlur1(VertexOutput i) : SV_Target {
			float2 uv = i.uv;
			
			const half radius = 1;
			int sampleCount = 2 * radius + 1;

			CAT_GET_GBUFFER(pivotS, uv)
			
			half3 pivotNormal = Tex2Dlod(_NormalsPacked, uv, 0).xyz * 2 - 1;
			
			float t = 0.7;
			float u = 1.0*t*1.5;
			float v = 1.5*t*0;
			float w = 1;//0.3333333;
			//float2 blurDir = _BlurDir * float2(1, 1) * _MainTex_TexelSize.xy;
			float2 blurDir = 0.5 * _MainTex_TexelSize.xy;// * _SmoothnessRange;
			
			float3 uvs[4] = {
				float3(float2(-1, -1) * blurDir, w),
				float3(float2(-1, +1) * blurDir, w),
				float3(float2(+1, -1) * blurDir, w),
				float3(float2(+1, +1) * blurDir, w),
			};
			/*
			float3 uvs[3] = {
				float3(uv + -u * blurDir, w),
				float3(uv +  0 * blurDir, 1),
				float3(uv + +u * blurDir, w),
			};
			*/
			
			float4 sumColorPure = 0;
			float4 sumColor = 0;
			float sumWeight = 0;
			
		//	UNITY_UNROLL
			for (int i = 0; i < 4; i++) {
				half4 color = (Tex2Dlod(_MainTex, uv + uvs[i].xy, 0))*1;
				float weight = uvs[i].z;//PDF6(i*2, radius);
				
				half3 normal = Tex2Dlod(_NormalsPacked, uv + 2*uvs[i].xy, 0).xyz * 2 - 1;
				weight *= normalWeight(normal, pivotNormal);
				
				sumColorPure += color;
				color.rgb /= 1 + DisneyLuminance(color.rgb);
				sumColor += color * weight;
				sumWeight += weight;
			}
			
			if (sumWeight > 0.01) {
				//sumColor.a = min(sumColor.a * sqrt(max(1/sumWeight, 2.0)), 1.0);
				sumColor.rgba /= sumWeight;
				sumColor.rgb /= 1 - DisneyLuminance(sumColor.rgb);
			}
			else {
				sumColor = float4(sumColorPure.rgb / (2.0 * radius + 1.0), 0);
				//return float4(1, 0, 1, 1);
			}
			
			return (sumColor);
		}

		float4 simpleBlur2(VertexOutput i) : SV_Target {
			float2 uv = i.uv;
			
			float t = 0.5;//_SmoothnessRange;
			float u = 0.5*t*1;
			float v = 1.5*t*1;
			float w = 1;
			
			float2 uv0 = uv;
			float2 uv1 = uv + _MainTex_TexelSize.xy * float2(-u, +v);
			float2 uv2 = uv + _MainTex_TexelSize.xy * float2(+v, +u);
			float2 uv3 = uv + _MainTex_TexelSize.xy * float2(+u, -v);
			float2 uv4 = uv + _MainTex_TexelSize.xy * float2(-v, -u);
			
			half4 refl0 = Tex2Dlod(_MainTex, uv0, 0);
			half4 refl1 = Tex2Dlod(_MainTex, uv1, 0);
			half4 refl2 = Tex2Dlod(_MainTex, uv2, 0);
			half4 refl3 = Tex2Dlod(_MainTex, uv3, 0);
			half4 refl4 = Tex2Dlod(_MainTex, uv4, 0);
			/*
			refl0 /= 1 + DisneyLuminance(refl0);
			refl1 /= 1 + DisneyLuminance(refl1);
			refl2 /= 1 + DisneyLuminance(refl2);
			refl3 /= 1 + DisneyLuminance(refl3);
			refl4 /= 1 + DisneyLuminance(refl4);
			*/
			half4 sumColor = refl0 + w * (refl1 + refl2 + refl3 + refl4);
			sumColor /= 1 + (4 * w);
			//reflection /= 1 - DisneyLuminance(reflection);
			
			return (sumColor);
		}
		
		
		float4 getTexelSize(int mip) {
			if (mip == 4) {
				return _ReflectionMip4_TexelSize;
			} else if (mip == 3) {
				return _ReflectionMip3_TexelSize;
			} else if (mip == 2) {
				return _ReflectionMip2_TexelSize;
			} else if (mip == 1) {
				return _ReflectionMip1_TexelSize;
			} else {
				return _ReflectionMip0_TexelSize;
			}
		}
		
		half4 sampleReflection(float2 uv, int mip) {
			if (mip == 4) {
				return Tex2Dlod(_ReflectionMip4, uv, 0);
			} else if (mip == 3) {
				return Tex2Dlod(_ReflectionMip3, uv, 0);
			} else if (mip == 2) {
				return Tex2Dlod(_ReflectionMip2, uv, 0);
			} else if (mip == 1) {
				return Tex2Dlod(_ReflectionMip1, uv, 0);
			} else {
				return Tex2Dlod(_ReflectionMip0, uv, 0);
			}
		}
		
		half4 composeReflectionsMip(float2 uv, int mip, half3 normalPivot) {
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
			
			UNITY_UNROLL_N(4)
			for (int i = 0; i < 4; i++) {
				half4 color = sampleReflection(uvs[i], mip);
				half3 normal = Tex2Dlod(_NormalsPacked, uvs[i], 0).xyz * 2 - 1;
				
				float2 bilateralWeights = ((1-(uvs[i] * texelSize.zw - texel1Centered)) - (pos - texel1Centered));
				float weight = 1;
				weight *= normalWeight(normal, normalPivot);
	//			weight *= max(0.125, color.a);
				weight = max(0.00001, weight);
				weight *= abs(bilateralWeights.x * bilateralWeights.y);
				
				color.rgb *= color.a;
				color.rgb /= 1 + DisneyLuminance(color.rgb);
				sumColor += color * weight;
				sumWeight += weight;
			}
			return sumColor / sumWeight;
			
		}
		
		static const half maxMipLevel = 5.0 - 1.0;
		half getMipLvl(float2 uv, half smoothness, out half confidence) {
			float rayLen = decodeRayLength(Tex2Dlod(_HitTex, uv, 0).z);
			rayLen += 0.0625;
			float3 wsPos = ScreenToWorldPos(GetScreenPos(uv, sampleDepthLod(_CameraDepthTexture, uv, 0)));
		//	float viewDist = rayLen + -vsPos.z;
			float viewDist = rayLen + length(UnityWorldSpaceViewDir(wsPos));
			
			float roughness = 1 - smoothness;
		//	roughness = pow(roughness, 4.0/3.0);
		//	JCO@@@TODO:
			half area = abs(roughness * rayLen * _PixelsPerMeterAtOneMeter / viewDist);
			half mip = log2(area/16.0 + 15.0/16.0);
			confidence = 1 - Pow3(InvLerpSat(0.6666667*(1-MIN_SMOOTHNESS), 1-MIN_SMOOTHNESS, roughness));
			return clamp(mip, 0, maxMipLevel);
		}
		half getMipLvl(float2 uv, out half confidence) {
			CAT_GET_GBUFFER(s, uv)
			return getMipLvl(uv, s.smoothness, /*out*/confidence);
		}
		
		half4 composeReflections(VertexOutput i) : SV_Target {
			//i.uv = snapToPixel(i.uv, _HitTex_TexelSize);

			half confidence = 1;
			half mipLvl = getMipLvl(i.uv, /*out*/ confidence);
			
			int mipMin = floor(mipLvl);
			int mipMax = min(maxMipLevel, mipMin+1);
			
			half3 pivotNormal = Tex2Dlod(_NormalsPacked, i.uv, 0).xyz * 2 - 1;
			
			mipMin = _UseMips ? mipMin : 0;
			half4 result = composeReflectionsMip(i.uv, mipMin, pivotNormal);
			if (_UseMips) {
				half4 result2 = composeReflectionsMip(i.uv, mipMax, pivotNormal);
				result = lerp(result, result2, saturate(mipLvl - mipMin));
			}
			return max(0, result * confidence);
		}
		
		float4 applyReflections(float2 uv) {
			float depth = sampleDepthLod(_CameraDepthTexture, uv, 0);
			float3 ssPos = GetScreenPos(uv, depth);
			float3 wsPos = ScreenToWorldPos(ssPos);
			float3 wsViewDir = normalize(UnityWorldSpaceViewDir(wsPos));
			
			// Let core Shader functions do the dirty work of applying the BRDF
			CAT_GET_GBUFFER(s, uv)
			
			CAT_DECLARE_OUTPUT(UnityGI, gi);
			ResetUnityGI(/*out*/ gi);
			half4 reflection = Tex2Dlod(_ComposedReflections, uv, 0);
			gi.indirect.specular = reflection.rgb;
			
			half3 reflectionFinal = applyLighting(s, wsViewDir, gi).rgb;
			return float4(reflectionFinal, reflection.a);
		}
		
		float4 applyReflections(VertexOutput i) : SV_Target {
			float4 reflections = applyReflections(i.uv);
			float3 reflProbes = tex2D(_CameraReflectionsTexture, i.uv).rgb;
			return float4(reflections.rgb - reflProbes * reflections.a, 0);
			
	//		float2 uv = i.uv;
			
	//		float3 viewDir = normalize(unity_WorldToCamera[2].xyz);
    //
	//		float depth = sampleDepthLod(_CameraDepthTexture, uv, 0);
	//		float3 ssPos = GetScreenPos(uv, depth);
	//		float3 wsPos = ScreenToWorldPos(ssPos);
    //
    //
	//		// Let core Shader functions do the dirty work of applying the BRDF
	//		CAT_GET_GBUFFER(s, uv)
	//		half4 gbuffer3 = tex2D(_MainTex, uv); // NormalWorld (rgb), Height (a)
    //
	//		float3 wsViewDir = normalize(UnityWorldSpaceViewDir(wsPos));
    //
	//		float3 reflProbes = tex2D(_CameraReflectionsTexture, uv);
	//		half4 reflection = Tex2Dlod(_ComposedReflections, uv, 0);//lerp(reflection1, reflection2, cRef2);
	//	//	reflection.rgb /= 1 + reflection.rgb;
	//	//	reflection.rgb /= 1 - reflection.rgb;
    //
	//		half confidence = reflection.a;
	//		//reflection.rgb *= confidence;
	//		
	//		CAT_DECLARE_OUTPUT(UnityGI, gi);
	//		ResetUnityGI(/*out*/ gi);
	//		
	//		#if 0 && defined(CAT_TEMPORAL_SSR_ON)
	//			reflection.rgb += reflProbes * (1-confidence);
	//			gi.indirect.specular = reflection.rgb;
	//			half3 reflectionFinal = applyLighting(s, wsViewDir, gi).rgb;
	//		#else 
	//			gi.indirect.specular = reflection.rgb;
	//			half3 reflectionFinal = applyLighting(s, wsViewDir, gi).rgb;
	//			reflectionFinal -= reflProbes * confidence;
	//		#endif
			
	//		gbuffer3.rgb += reflectionFinal;
	//		return max(0, gbuffer3);
		}
		
		half4 composeAndApplyReflections(VertexOutput i) : SV_Target {
			//i.uv = snapToPixel(i.uv, _HitTex_TexelSize);
			CAT_GET_GBUFFER(s, i.uv)

			half confidence = 1;
			half mipLvl = getMipLvl(i.uv, s.smoothness, /*out*/ confidence);
			
			int mipMin = floor(mipLvl);
			int mipMax = min(maxMipLevel, mipMin+1);
			
			mipMin = _UseMips ? mipMin : 0;
			half4 composedRefl = composeReflectionsMip(i.uv, mipMin, s.normal);
			if (_UseMips) {
				half4 composedRefl2 = composeReflectionsMip(i.uv, mipMax, s.normal);
				half lerpVal = saturate(mipLvl - mipMin);
				composedRefl *= (1-lerpVal);
				composedRefl2 *= lerpVal;
				composedRefl += composedRefl2;
			}
			
			composedRefl.rgb /= 1 - DisneyLuminance(composedRefl.rgb);
			composedRefl = max(0, composedRefl * confidence);
			
			float depth = sampleDepthLod(_CameraDepthTexture, i.uv, 0);
			float3 ssPos = GetScreenPos(i.uv, depth);
			float3 wsPos = ScreenToWorldPos(ssPos);
			float3 wsViewDir = normalize(UnityWorldSpaceViewDir(wsPos));
			
			// Let core Shader functions do the dirty work of applying the BRDF
			
			CAT_DECLARE_OUTPUT(UnityGI, gi);
			ResetUnityGI(/*out*/ gi);
			gi.indirect.specular = composedRefl.rgb;
			
			half3 reflectionFinal = applyLighting(s, wsViewDir, gi).rgb;
			
			float3 reflProbes = tex2D(_CameraReflectionsTexture, i.uv).rgb;
			return float4(reflectionFinal - reflProbes * composedRefl.a, 0);
		}
		
		
		half4 debug( VertexOutput i ) : SV_Target {
	//		float2 uv = i.uv;
	//	//	uv = snapToPixel(uv, _ReflectionMip0_TexelSize);
			half4 frag = 0;

	//	public enum DebugMode {
	//		ComposedReflectionsRGB = 0,
	//		ComposedReflectionsA = 1,
	//		MipsRGB = 2,
	//		MipsA = 3,
	//		MipLevel = 4,
	//		RayLength = 5,
	//		RayTraceConfidence = 6,
	//		AppliedReflections = 7
	//	}
		
		
		float4 reflections = composeAndApplyReflections(i);
		float3 reflProbes = tex2D(_CameraReflectionsTexture, i.uv).rgb;
		half dummyConfidence;
		half3 values[8] = {
			half3(sampleReflection(i.uv, _UseMips ? _MipLevelForDebug : 0).rgb),
			half3(sampleReflection(i.uv, _UseMips ? _MipLevelForDebug : 0).aaa),
			half3(sampleReflection(i.uv, _UseMips ? _MipLevelForDebug : 0).rgb),
			half3(sampleReflection(i.uv, _UseMips ? _MipLevelForDebug : 0).aaa),
			half3(getMipLvl(i.uv, /*out*/dummyConfidence).xxx / maxMipLevel),
			half3(tex2D(_HitTex, i.uv).zzz),
			half3(tex2D(_HitTex, i.uv).www),
			half3(reflections.rgb + reflProbes * (1 - reflections.a)),
		};
		return half4(values[(int)_DebugMode], 1);
	//	return pow(half4(abs(frag.rgb), 0), 1);
		}


	ENDCG 
	
	Subshader {
		ZTest Always Cull Off ZWrite Off
		Fog { Mode off }
		
		//Pass 0 rayTrace
		Pass {
			Stencil {
				ref [_StencilNonBackground]
				readmask [_StencilNonBackground]
				// Normally just comp would be sufficient, but there's a bug and only front face stencil state is set (case 583207)
				compback equal
				compfront equal
			}
			
			CGPROGRAM
			#pragma target 3.0

			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma vertex vert
			#pragma fragment rayTrace
			ENDCG
		}
		
		//Pass 1 resolve
		Pass {
			Blend One Zero, One Zero
			
			CGPROGRAM
			#pragma target 3.0
			
			#pragma multi_compile _ CAT_TEMPORAL_SSR_ON

			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma vertex vert
			#pragma fragment resolve1
			ENDCG
		}
		
		//Pass 2 simpleBlur
		Pass {
			Stencil {
				ref [_StencilNonBackground]
				readmask [_StencilNonBackground]
				// Normally just comp would be sufficient, but there's a bug and only front face stencil state is set (case 583207)
				compback equal
				compfront equal
			}
			
			CGPROGRAM
			#pragma target 3.0

			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma vertex vert
			#pragma fragment simpleBlur
			ENDCG
		}
		
		//Pass 3 mipMapBlurComressor
		Pass {
			Stencil {
				ref [_StencilNonBackground]
				readmask [_StencilNonBackground]
				// Normally just comp would be sufficient, but there's a bug and only front face stencil state is set (case 583207)
				compback equal
				compfront equal
			}
			
			CGPROGRAM
			#pragma target 3.0

			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma vertex vert
			#pragma fragment mipMapBlurCompressor
			ENDCG
		}
		
		//Pass 4 mipMapBlurVanilla
		Pass {
			Stencil {
				ref [_StencilNonBackground]
				readmask [_StencilNonBackground]
				// Normally just comp would be sufficient, but there's a bug and only front face stencil state is set (case 583207)
				compback equal
				compfront equal
			}
			
			CGPROGRAM
			#pragma target 3.0

			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma vertex vert
			#pragma fragment mipMapBlurVanilla
			ENDCG
		}
		/*
		//Pass 5 compose reflections
		Pass {
			Blend One Zero, One Zero
			Stencil {
				ref [_StencilNonBackground]
				readmask [_StencilNonBackground]
				// Normally just comp would be sufficient, but there's a bug and only front face stencil state is set (case 583207)
				compback equal
				compfront equal
			}
			
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment composeReflections
			ENDCG
		}
		
		//Pass 6 apply reflections
		Pass {
			Blend One One
			Stencil {
				ref [_StencilNonBackground]
				readmask [_StencilNonBackground]
				// Normally just comp would be sufficient, but there's a bug and only front face stencil state is set (case 583207)
				compback equal
				compfront equal
			}
			
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment applyReflections
			ENDCG
		}
		*/
		//Pass 5 debug
		Pass {
			CGPROGRAM
			#pragma target 3.0
			
			#pragma vertex vert
			#pragma fragment debug
			ENDCG
		}
		
		//Pass 6 Upsample RayHits
		Pass {
			Blend One Zero
			Stencil {
				ref [_StencilNonBackground]
				readmask [_StencilNonBackground]
				// Normally just comp would be sufficient, but there's a bug and only front face stencil state is set (case 583207)
				compback equal
				compfront equal
			}
			
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment upsampleRayHits
			ENDCG
		}

		//Pass 7 Pack Normals
		Pass {
			Blend One Zero
			Stencil {
				ref [_StencilNonBackground]
				readmask [_StencilNonBackground]
				// Normally just comp would be sufficient, but there's a bug and only front face stencil state is set (case 583207)
				compback equal
				compfront equal
			}
			
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment PackNormals
			ENDCG
		}
		
		//Pass 8 compose and apply reflections
		Pass {
			Blend One One, One Zero
			Stencil {
				ref [_StencilNonBackground]
				readmask [_StencilNonBackground]
				// Normally just comp would be sufficient, but there's a bug and only front face stencil state is set (case 583207)
				compback equal
				compfront equal
			}
			
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment composeAndApplyReflections
			ENDCG
		}
		
		
		
		
	}
	Fallback Off
}
