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

Shader "Hidden/Cat Ambient Occlusion" {
	Properties {
	//	_MainTex ("Base (RGB)", 2D) = "black" {}
		_Intensity("Intensity", FLOAT) = 1
		_IsBackFaceCullingEnabled("is BackFace Culling Enabled", FLOAT) = 1
		_NoiseStrength("Noise Strength", FLOAT) = 00.5
		_MaxReflectionDistance("Max Reflection Distance", FLOAT) = 100
	}
	
	CGINCLUDE
		
		#include "UnityCG.cginc"
		#include "UnityStandardUtils.cginc"
		#include "UnityGlobalIllumination.cginc"
		#include "../../Includes/PostProcessingCommon.cginc"
		
/***** Constant values and description from: https://github.com/Unity-Technologies/PostProcessing Ambient Occlusion Shader *****/
		// The constant below determines the contrast of occlusion. This allows
		// users to control over/under occlusion. At the moment, this is not exposed
		// to the editor because it is rarely useful.
		static const float kContrast = 0.6;
		
		// The constants below are used in the AO estimator. Beta is mainly used
		// for suppressing self-shadowing noise, and Epsilon is used to prevent
		// calculation underflow. See the paper (Morgan 2011 http://goo.gl/2iz3P)
		// for further details of these constants.
		static const float kBeta = 0.002;


		#define _NormalsPacked _CameraGBufferTexture2
	//	uniform sampler2D		_OcclusionNormals;		float4	_OcclusionNormals_TexelSize;
		
		int				_SampleCount;
		half			_Radius;
		half			_Intensity;
		float2			_BlurDir;
	//	float 			_Downsample = 1;
		
		
		float4 packAONormal(float ao, float3 normal) {
			return fixed4(normal * 0.5 + 0.5, ao);
		}
		
		float unpackAO(float4 aoNormal) {
			return aoNormal.w;
		}
		float3 unpackAONormal(float4 aoNormal) {
			return aoNormal.xyz * 2 - 1;
		}
		
		// Interleaved gradient function from Jimenez 2014 http://goo.gl/eomGso
		float GradientNoise(float2 uv) {
			uv = floor(uv * _ScreenParams.xy);
			float f = dot(float2(0.06711056, 0.00583715), uv);
			return frac(52.9829189 * frac(f));
		}
		
		// Sample point picker, adapted from: https://github.com/Unity-Technologies/PostProcessing Ambient Occlusion Shader
		float3 PickSamplePoint(float2 uv, float index) {
			// Uniformaly distributed points on a unit sphere http://goo.gl/X2F1Ho
			float gn = GradientNoise(uv * 1);
			float u = frac(noiseSimple(float2(0, index))*0.5+0.5 + gn) * 2 - 1;
			float theta = (noiseSimple(float2(1, index))*0.5+0.5 + gn) * UNITY_PI * 2;

			float sn, cs;
			sincos(theta, sn, cs);
			float3 v = float3(float2(cs, sn) * sqrt(1 - Pow2(u)), u);
			// Make them distributed between [0, _Radius]
			float l = sqrt((index + 1) / _SampleCount) * _Radius;
			return v * l;
		}
		
		// Sample point picker , custom Variation
		float3 PickSamplePointS(float2 uv, float index) {
			float l = sqrt((index + 1.0) / _SampleCount) * _Radius;
			float3 v;
			v = noiseSimple3(uv + index, float3(0, index * 0.4142012, index * 0.971843));
			v = noiseSimple3(floor(uv * _ScreenParams.xy), float3(index, _SampleCount-index, 10.156));
			return v *l;
		}
		
		float4 sampleProximity(VertexOutput i) : SV_Target {
			float2 uv = i.uv;

			CAT_GET_GBUFFER(g, uv)
			float depth = sampleDepthLod(_DepthTexture, uv, 0);
		//	depth -= _ProjectionParams.z / 65536.0 * 0.00001;
			float3 ssPos = GetScreenPos(uv, depth);
			float3 vsPos = ScreenToViewPos(ssPos);
			float3 vsNormal = normalize(WorldToViewDir(g.normal));
			
			float ao = 0.0;
			int sampleCount = _SampleCount*1;
			for (int s = 0; s < sampleCount; s++)
			{
				// Sample point
		#if defined(SHADER_API_D3D11)
				// This 'floor(1.0001 * s)' operation is needed to avoid a NVidia
				// shader issue. This issue is only observed on DX11.
				float3 v_s1 = (PickSamplePoint(uv, floor(1.0001 * s)));
		#else
				float3 v_s1 = (PickSamplePoint(uv, s));
		#endif
				v_s1 = faceforward(v_s1, -vsNormal, v_s1);
				float3 vsPos_s1 = vsPos + v_s1;

				// Reproject the sample point
				float3 ssPos_s1 = ViewToScreenPos(vsPos_s1);
			//	float theta = noiseSimple(uv + s) * UNITY_PI;
			//	float d = 1-Pow2(1-abs(noiseSimple(uv.yx+s)));
			//	ssPos_s1 = ssPos;
			//	ssPos_s1.xy += _MainTex_TexelSize.xy * float2(sin(theta), cos(theta))*2 * d * 100 * _Radius;
				float2 uv_s1 = ssPos_s1.xy * 0.5 + 0.5;

				// Depth at the sample point
				float depth_s1 = all(saturate(uv_s1) == uv_s1) 
						? sampleDepthLod(_DepthTexture, uv_s1, 0)
						: 0;

				// Relative position of the sample point
				float3 vsPos_s2 = ScreenToViewPos(float3(ssPos_s1.xy, depth_s1));
				float3 vsPosDiff = (vsPos_s2 - vsPos);

				// Estimate the obscurance value
				float pdn = dot(vsPosDiff, vsNormal);
				float gamma = LinearEyeDepth(depth) + 0*0.8 / max(0.1, pdn);
				float a1 = max(pdn - kBeta * gamma, 0.0);
				float a2 = dot(vsPosDiff, vsPosDiff) + 1e-4;
				float ax = a1 / a2;
				ao += ax;// * (all(abs(ssPos_s1.xy)<=1) ? 1 : 0.00);
			}
			ao *= _Radius; // intensity normalization
			ao *= _Intensity * 0.5;
			ao /= float(sampleCount);
			// Apply other parameters.
			ao = pow(max(0, ao), kContrast);
			return packAONormal(ao, vsNormal);
		}
		
				
		half normalWeight(half3 normal, half3 normalPivot) {
			half temp = (dot(normal, normalPivot));
			temp = smoothstep(0.8, 1.0, temp);
			return Pow2(temp);
		}
		
		
		float3 getSamplePositionAndWeight(float2 uv, float2 blurDir, int k) {
			const float weights[9] = 
		//	{ 1.00000000, 1.00000000, 1.00000000, 1.00000000, 1.00000000, 1.00000000, 1.00000000, 1.00000000, 1.00000000, };		
		//	{ 0.00000000, 0.00000000, 0.00000000, 0.00000000, 1.00000000, 0.00000000, 0.00000000, 0.00000000, 0.00000000, };		
		//	{ 0.05250000, 0.07500000, 0.11000000, 0.15000000, 0.22500000, 0.15000000, 0.11000000, 0.07500000, 0.05250000, };	// Postprocessing Stack
		//	{ 0.00000000, 0.00100000, 0.02800000, 0.23300000, 0.47400000, 0.23300000, 0.02800000, 0.00100000, 0.00000000, };		// UnitySSR
	//		{ 0.00190727, 0.0179156 , 0.0887367 , 0.231753  , 0.319154, 0.231753, 0.0887367 , 0.0179156 , 0.00190727, };
	//		{ 0.04367230, 0.13384917, 0.29788706, 0.48140731, 0.56493716*0.5, 0.48140731, 0.29788706, 0.13384917, 0.04367230, };
		//	{ 0.09071798, 0.27803726, 0.61878383, 1.00000000, 1.17351180*0.5, 1.00000000, 0.61878383, 0.27803726, 0.09071798, };
			{ 0.09071798, 0.27803726, 0.61878383, 1.00000000, 1.17351180*0.5, 1.00000000, 0.61878383, 0.27803726, 0.09071798, };
		//	{ sqrt(0.00190727), sqrt(0.0179156), sqrt(0.0887367), sqrt(0.231753), sqrt(0.319154), sqrt(0.231753), sqrt(0.0887367), sqrt(0.0179156), sqrt(0.00190727) };
			
			float2 uvTap = uv + 2*blurDir * k - blurDir * 0.55 * sign(k);
			return float3(uvTap, weights[k + 4]);
		}
		
		half tapBlur(sampler2D _Texture, float2 uv, float2 blurDir, int k, float3 pivotNormal, out half weight) {
			float3 uvWeight = getSamplePositionAndWeight(uv, blurDir, k);
			half4 tapAONormal = tex2D(_Texture, uvWeight.xy);
			weight = uvWeight.z * (0 == k ? 1 : normalWeight(tapAONormal.xyz * 2 - 1, pivotNormal));
			return tapAONormal.w;
		}
		
		struct BlurAccumulation {
			float aoPure;
			float aoWeighted;
			float weight;
		};
		
		void addTap(half ao, half weight, inout BlurAccumulation sum) {
			sum.aoPure += ao;
			sum.aoWeighted += ao * weight;
			sum.weight += weight;
		
		}
		
		float4 aoBlur(float2 rawBlurDir, CAT_ARGS_TEX_INFO(_Texture), float2 uv) {
			const int sampleCount = 5;
			const int sampleRadius = sampleCount / 2;
			
			float3 pivotNormal = tex2D(_Texture, uv).xyz * 2 - 1;;
			float2 blurDir = _Texture_TexelSize.xy * rawBlurDir.xy;
			
			BlurAccumulation sum = {0, 0, 0};
			UNITY_UNROLL
			for (int k = - sampleRadius; k <= sampleRadius; k++) {
				half weight;
				half ao = tapBlur(_Texture, uv, blurDir, k, pivotNormal, /*out*/ weight);
				addTap(ao, weight, /*inout*/ sum);
			}
			
			float result =  sum.aoWeighted / sum.weight;
			return float4(pivotNormal, result);
		}
		
		float4 aoBlurHorizontal(VertexOutput i) : SV_Target {
			return aoBlur(_BlurDir, CAT_PASS_TEX_INFO(_MainTex), i.uv);
		}
		
		float4 simpleBlur2(VertexOutput i) : SV_Target {
			float2 uv = i.uv;
			
			float t = 0.5;//_SmoothnessRange;
			float u = 0.5*t*1;
			float v = 1.5*t*1;
			
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
			half3 n0 = refl0.xyz*2-1;
			half w0 =1;
			half w1 = normalWeight(n0, refl1.xyz*2-1);
			half w2 = normalWeight(n0, refl2.xyz*2-1);
			half w3 = normalWeight(n0, refl3.xyz*2-1);
			half w4 = normalWeight(n0, refl4.xyz*2-1);
			
			refl0 *= w0;
			refl1 *= w1;
			refl2 *= w2;
			refl3 *= w3;
			refl4 *= w4;
			
			half4 sumAO = refl0 + refl1 + refl2 + refl3 + refl4;
			half  sumW = w0 + w1 + w2 + w3 + w4;
			sumAO /= sumW;
			
			return (sumAO);
		}
				
		float4 resolve(VertexOutput i) : SV_Target {
			float2 delta = _MainTex_TexelSize.xy *1;
			//half ao = BlurSmall(_OcclusionTexture, i.uv, delta);
			half4 ao = aoBlur(_BlurDir, CAT_PASS_TEX_INFO(_MainTex), i.uv);
		//	ao = simpleBlur2(i);
		//	ao = tex2D(_MainTex, i.uv);

			return float4(1, 1, 1, ao.w);
		}

		float4 multiplyAlpha(VertexOutput i) : SV_Target {	
			return float4(1, 1, 1, tex2D(_MainTex, i.uv).w);
		}

	ENDCG 
	
	Subshader {
		ZTest Always Cull Off ZWrite Off
		Fog { Mode off }
		
		//Pass 0 sampleProximity
		Pass {
		//	Stencil {
		//		ref [_StencilNonBackground]
		//		readmask [_StencilNonBackground]
		//		// Normally just comp would be sufficient, but there's a bug and only front face stencil state is set (case 583207)
		//		compback equal
		//		compfront equal
		//	}
			
			CGPROGRAM
			#pragma target 3.0

			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma vertex vert
			#pragma fragment sampleProximity
			ENDCG
		}
		
		//Pass 1 aoBlurHorizontal
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
			#pragma fragment aoBlurHorizontal
			ENDCG
		}
		
		//Pass 2 Resolve
		Pass {
			Stencil {
				ref [_StencilNonBackground]
				readmask [_StencilNonBackground]
				// Normally just comp would be sufficient, but there's a bug and only front face stencil state is set (case 583207)
				compback equal
				compfront equal
			}
			
			Blend Zero OneMinusSrcAlpha, One Zero
			
			CGPROGRAM
			#pragma target 3.0

			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma vertex vert
			#pragma fragment resolve
			ENDCG
		}
				
		//Pass 3 ResolveDebug
		Pass {
			Stencil {
				ref [_StencilNonBackground]
				readmask [_StencilNonBackground]
				// Normally just comp would be sufficient, but there's a bug and only front face stencil state is set (case 583207)
				compback equal
				compfront equal
			}
			
			Blend OneMinusSrcAlpha Zero, Zero One
			
			CGPROGRAM
			#pragma target 3.0

			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma vertex vert
			#pragma fragment resolve
			ENDCG
		}
				
		//Pass 4 MultiplyAlpha
		Pass {
			Stencil {
				ref [_StencilNonBackground]
				readmask [_StencilNonBackground]
				// Normally just comp would be sufficient, but there's a bug and only front face stencil state is set (case 583207)
				compback equal
				compfront equal
			}
			
			Blend Zero One, Zero OneMinusSrcAlpha
			
			CGPROGRAM
			#pragma target 3.0

			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma vertex vert
			#pragma fragment multiplyAlpha
			ENDCG
		}
		
		
		
		
	}
	Fallback Off
}
