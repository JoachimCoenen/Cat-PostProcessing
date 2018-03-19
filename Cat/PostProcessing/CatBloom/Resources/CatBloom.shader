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

// Inspired by Keijiro Takahashi's Kino/Bloom v2 - Bloom filter for Unity:
// https://github.com/keijiro/KinoBloom

Shader "Hidden/Cat Bloom" {
	Properties {
		_DirtTexture ("Dirt Texture (RGB)", 2D) = "black" {}
	}
	
	CGINCLUDE
		#define ANTI_FLICKER 1
		
		#include "UnityCG.cginc"
		#include "UnityStandardUtils.cginc"
		#include "UnityGlobalIllumination.cginc"
		
		#include "../../Includes/PostProcessingCommon.cginc"
		
		float	_MinLuminance;
		float	_KneeStrength;
		float	_Intensity;
		sampler2D	_DirtTexture;
		float	_DirtIntensity;
		
		float4	_BlurDir;
		float	_MipLevel;
		float	_Weight;

				// debugOn
		int		_DebugMode;
		
		//----------------------------------------------------------------------------------------------------------------------
		//----------------------------------------------------------------------------------------------------------------------
		// 3-tap median filter by 
		half3 Median(half3 a, half3 b, half3 c) {
			return a + b + c - min(min(a, b), c) - max(max(a, b), c);
		}
		
		float4 fragBloomIntensity(VertexOutput i) : SV_Target {
		#if ANTI_FLICKER
			float4 d = _MainTex_TexelSize.xyxy * float4(+0.5, -1.5, +1.5, +0.5);
			half4 color = Tex2Dlod(_MainTex, i.uv, 0);
			half3 color1 = Tex2Dlod(_MainTex, i.uv + d.xy, 0).rgb;
			half3 color2 = Tex2Dlod(_MainTex, i.uv - d.xy, 0).rgb;
			color.rgb = Median(color.rgb, color1, color2);
			half3 color3 = Tex2Dlod(_MainTex, i.uv + d.zw, 0).rgb;
			half3 color4 = Tex2Dlod(_MainTex, i.uv - d.zw, 0).rgb;
			color.rgb = Median(color.rgb, color3, color4);
		#else
			float4 color = Tex2Dlod(_MainTex, i.uv, 0);
		#endif
		
			float luminance = MaxC(color.rgb);
			float lumDiff = max(0, luminance - _MinLuminance);
			float response = lumDiff * pow(lumDiff / (lumDiff + 1e-1), max(1e-5, _KneeStrength*4)) / max(1e-5, luminance);
			
			color.a = response;
	//		color.rgb = CompressBy(color.rgb, MaxC(color.rgb));
			color.rgb *= color.a;
			return color;
		}
		
		//----------------------------------------------------------------------------------------------------------------------
		//----------------------------------------------------------------------------------------------------------------------

		float4 fragDownsample(VertexOutput i) : SV_Target {
			float4 s = { 0, +2, -2, +2 };
			s.yzw *= _MainTex_TexelSize.xxy * 1;
			
			float4 color = 0;
			color += Tex2Dlod(_MainTex, i.uv + s.xx, 0);
			color += color;
			color += Tex2Dlod(_MainTex, i.uv + s.xw, 0);
			color += Tex2Dlod(_MainTex, i.uv - s.xw, 0);
			color += Tex2Dlod(_MainTex, i.uv + s.yx, 0);
			color += Tex2Dlod(_MainTex, i.uv - s.yx, 0);
			color += color;
			color += Tex2Dlod(_MainTex, i.uv + s.yw, 0);
			color += Tex2Dlod(_MainTex, i.uv - s.yw, 0);
			color += Tex2Dlod(_MainTex, i.uv + s.zw, 0);
			color += Tex2Dlod(_MainTex, i.uv - s.zw, 0);
			
			return color * (1.0 / 16.0) * 1;
		}
		
		//----------------------------------------------------------------------------------------------------------------------
		//----------------------------------------------------------------------------------------------------------------------
		
		float4 fragUpsample(VertexOutput i) : SV_Target {
			float4 d = _MainTex_TexelSize.xyxy * float4(-1.0, -1.0, 1.0, 1.0);
			float4 sumColor = 0;
			sumColor += Tex2Dlod(_MainTex, i.uv + d.xy, 0);
			sumColor += Tex2Dlod(_MainTex, i.uv + d.zy, 0);
			sumColor += Tex2Dlod(_MainTex, i.uv + d.xw, 0);
			sumColor += Tex2Dlod(_MainTex, i.uv + d.zw, 0);
			return sumColor * (0.25 * _Weight);
		}
		
		//----------------------------------------------------------------------------------------------------------------------
		//----------------------------------------------------------------------------------------------------------------------
		
		float4 getDirt(float2 uv) {
			float3 dirt = _Intensity*0.5 + 2 * _DirtIntensity * Tex2Dlod(_DirtTexture, uv, 0).rgb;
			return float4(dirt, MaxC(dirt));// * MaxC(dirt);
		}
		
		static const float MAX_MIP_LEVEL = 7;

		float4 applyBloom(float2 uv) {
			float mip = _MipLevel;
			float4 color = Tex2Dlod(_MainTex, uv, 0);
			float4 dirt = getDirt(uv) * 1	;
			float invMip = 1/ mip;
			
			color.rgba *= dirt.rgba * invMip * 1;
			color.a = 1 / (color.a + 1);
			color.rgb *= color.a;
			
			//dirt.rgba *= color.a * invMip;
			//color.rgb *= dirt.rgb * invMip * 1;
			//color.a = 1 / (dirt.a + 1);
			//color.rgb *= color.a;
			
			return float4(min(color.rgb, 65504.0), color.a);
		}
		
		float4 fragApplyBloom(VertexOutput i) : SV_Target {
			return applyBloom(i.uv);
		}
		
		//----------------------------------------------------------------------------------------------------------------------
		//----------------------------------------------------------------------------------------------------------------------
		
		half4 fragDebug(VertexOutputFull i ) : SV_Target {
			return half4( applyBloom(i.uv).rgb, 1);
		}
		
		//----------------------------------------------------------------------------------------------------------------------
		//----------------------------------------------------------------------------------------------------------------------
		
		static const int CAMERA_BLUR_SAMPLE_COUNT = 7;
		
		float4 fragBloomBlur(VertexOutput i) : SV_Target {
			const int sampleRadius = CAMERA_BLUR_SAMPLE_COUNT / 2;
			 float sumWeights = 0; const float sW = 1;
			const float normWeights1[9] = { 0.09071798/sW, 0.27803726/sW, 0.61878383/sW, 1.00000000/sW, 1.17351180/sW, 1.00000000/sW, 0.61878383/sW, 0.27803726/sW, 0.09071798/sW };
		//	const float normWeights[9]  = { 0.0525,        0.075,         0.110,         0.150,         0.225,         0.150,         0.110,         0.075,         0.0525 };
		//	const float normWeights[9]  = { 0,             0.001f,        0.028f,        0.233f,        0.474f,        0.233f,        0.028f,        0.001f,        0 };
		//	const float normWeights2[9] = { 0.001f,        0.003f,        0.028f,        0.233f,        0.474f,        0.233f,        0.028f,        0.003f,        0.001f };
			//							   0.01826357314, 0.05597516427, 0.12457512541, 0.20132252876, 0.23625436311,
			
			const float a = 1;
			const float b = 1 - a;
			
			float4 uvTap = i.uv.xyxy;
			float4 color1 = 0, color2 = 0;
			float weight1 = 0;
			float4 sumColor = 0;
			
			color1   = Tex2Dlod(_MainTex, uvTap.xy, _MipLevel);
			sumColor = color1 * normWeights1[0 + 4];
			sumWeights =        normWeights1[0 + 4];
			UNITY_UNROLL
			for (int k = 1; k <= sampleRadius; k++) {
				uvTap    = uvTap + _BlurDir*1;
				
				color1   = Tex2Dlod(_MainTex, uvTap.xy, _MipLevel);
				weight1  = normWeights1[k + 4] * all(saturate(uvTap.xy) == uvTap.xy);
				sumColor = sumColor + color1 * weight1;
				sumWeights = sumWeights + weight1;
				
				color1   = Tex2Dlod(_MainTex, uvTap.zw, _MipLevel);
				weight1  = normWeights1[k + 4] * all(saturate(uvTap.zw) == uvTap.zw);
				sumColor = sumColor + color1 * weight1;
				sumWeights = sumWeights + weight1;
			}
			
			return sumColor / sumWeights;
		}

		
	ENDCG 
	
	Subshader {
		ZTest Always Cull Off ZWrite Off
		Fog { Mode off }
		
		//Pass 0 BloomIntensity
		Pass {
			
			CGPROGRAM
			#pragma target 3.0
			
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma vertex vert
			#pragma fragment fragBloomIntensity
			ENDCG
		}
		
		//Pass 1 Downsample
		Pass {
			CGPROGRAM
			#pragma target 3.0
			
			#pragma vertex vert
			#pragma fragment fragDownsample
			ENDCG
		}

		//Pass 2 Upsample
		Pass {
			Blend One One, One One
			
			CGPROGRAM
			#pragma target 3.0
			
			#pragma vertex vert
			#pragma fragment fragUpsample
			ENDCG
		}

		//Pass 3 ApplyBloom
		Pass {
			//Blend One OneMinusSrcAlpha, Zero One
			Blend One SrcAlpha, Zero One
			
			CGPROGRAM
			#pragma target 3.0
			
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma vertex vert
			#pragma fragment fragApplyBloom
			ENDCG
		}

		//Pass 4 debug
		Pass {
			CGPROGRAM
			#pragma target 3.0
			
			#pragma vertex vertFull
			#pragma fragment fragDebug
			ENDCG
		}
		/*
		//Pass 5 BloomBlur
		Pass {
			
			CGPROGRAM
			#pragma target 3.0
			
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma vertex vert
			#pragma fragment fragBloomBlur
			ENDCG
		}
		*/
	}
	Fallback Off
}
