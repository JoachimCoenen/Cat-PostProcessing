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

Shader "Hidden/Cat Bloom" {
	Properties {
	//	_MainTex ("Base (RGB)", 2D) = "black" {}
	}
	
	CGINCLUDE
		#define MODIFIER 2
		
		#include "UnityCG.cginc"
		#include "UnityStandardUtils.cginc"
		#include "UnityGlobalIllumination.cginc"
		
		#include "../../Includes/PostProcessingCommon.cginc"
		
		float	_MinLuminance;
		float	_KneeStrength;
		float	_Intensity;
		float _MipLevelForRadius;
		
		float4	_BlurDir;
		float	_MipLevel;

				// debugOn
		int		_DebugMode;
		float	_MipLevelForDebug;
		
		//----------------------------------------------------------------------------------------------------------------------
		//----------------------------------------------------------------------------------------------------------------------
		
		float4 fragBloomIntensity(VertexOutput i) : SV_Target {
			float4 color = Tex2Dlod(_MainTex, i.uv, 0);
			float luminance = MaxC(color.rgb);
			// response = luminance * pow(max(0, luminance - _MinLuminance) / (luminance + 1e-1), _KneeStrength+1) / luminance;
			float response = 1 * pow(max(0, luminance - _MinLuminance) / (luminance + 1e-1), _KneeStrength + 1);
			color.rgb *= response / MODIFIER;
			color.a = 1;//response;
			color.rgb = CompressBy(color.rgb, MaxC(color.rgb));
			return color;
		}
		
		//----------------------------------------------------------------------------------------------------------------------
		//----------------------------------------------------------------------------------------------------------------------
		
		static const int CAMERA_BLUR_SAMPLE_COUNT = 9;
		
		float4 fragBloomBlur(VertexOutput i) : SV_Target {
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
		}
		
		//----------------------------------------------------------------------------------------------------------------------
		//----------------------------------------------------------------------------------------------------------------------
		
		static const int CAMERA_BLUR_SAMPLE_COUNT2 = 9;
		
		float4 fragBloomBlurX(VertexOutput i) : SV_Target {
			const int sampleRadius = CAMERA_BLUR_SAMPLE_COUNT2 / 2;
			 float sumWeights = 0; const float sW = 1;
			const float normWeights1[9] = { 0.09071798/sW, 0.27803726/sW, 0.61878383/sW, 1.00000000/sW, 1.17351180/sW, 1.00000000/sW, 0.61878383/sW, 0.27803726/sW, 0.09071798/sW };
		//	const float normWeights[9] = { 0.0525,        0.075,         0.110,         0.150,         0.225,         0.150,         0.110,         0.075,         0.0525 };
		//	const float normWeights[9] = { 0,             0.001f,        0.028f,        0.233f,        0.474f,        0.233f,        0.028f,        0.001f,        0 };
			const float normWeights2[9] = { 0.001f,        0.003f,        0.028f,        0.233f,        0.474f,        0.233f,        0.028f,        0.003f,        0.001f };
			//							   0.01826357314, 0.05597516427, 0.12457512541, 0.20132252876, 0.23625436311,
			
			const float a = 1;
			const float b = 1 - a;
			
			float4 uvTap = i.uv.xyxy;
			float4 color1 = 0, color2 = 0;
			float4 sumColor = 0;
			
			color1   = Tex2Dlod(_MainTex, uvTap.xy, _MipLevel);
			sumColor = color1 * (a*normWeights1[0 + 4] + b*normWeights2[0 + 4]);
			sumWeights =        (a*normWeights1[0 + 4] + b*normWeights2[0 + 4])*0.5;
			UNITY_UNROLL
			for (int k = 1; k <= sampleRadius; k++) {
				uvTap    = uvTap + _BlurDir*1;
				color1   = Tex2Dlod(_MainTex, uvTap.xy, _MipLevel);
				color2   = Tex2Dlod(_MainTex, uvTap.zw, _MipLevel);
				color1   = color1 + color2;
				sumColor = sumColor + color1 * (a*normWeights1[k + 4] + b*normWeights2[k + 4]);
				sumWeights = sumWeights +      (a*normWeights1[k + 4] + b*normWeights2[k + 4]);
			}
			
			return sumColor / sumWeights * 0.5;
		}
		
		//----------------------------------------------------------------------------------------------------------------------
		//----------------------------------------------------------------------------------------------------------------------
		
		float4 fragApplyBloomX(VertexOutput i) : SV_Target {
			float4 color1 = Tex2Dlod(_MainTex, i.uv, floor(_MipLevelForRadius));
			color1.rgb = DeCompressBy(color1.rgb, MaxC(color1.rgb));
			
			float4 color2 = Tex2Dlod(_MainTex, i.uv, ceil(_MipLevelForRadius));
			color2.rgb = DeCompressBy(color2.rgb, MaxC(color2.rgb));
			
			float4 color = lerp(color1, color2, _MipLevelForRadius - floor(_MipLevelForRadius));
			
			return float4(color.rgb * _Intensity * MODIFIER, color.a);
		}
		
		float4 applyBloom(float2 uv, float maxMip) {
			maxMip += +0.01;
			float4 color = 0;
			float4 color1 = 0;
			float weight = 0.5;
			float weight1 = 0;
			for (float k = 1; k < ceil(maxMip)+0.5; k += 1) {
				color1 = Tex2Dlod(_MainTex, uv, k);
				color1.rgb = DeCompressBy(color1.rgb, MaxC(color1.rgb));
				
				weight1 = 1 / (1 + MaxC(color1.rgb));
				color += color1 * weight1;
				weight += weight1;
			}
			color += color1 * weight1 * (maxMip - ceil(maxMip));
			weight += weight1 * (maxMip - ceil(maxMip));
			color = weight > 0 ? color / weight : 0;
		//	color -= min(color, Tex2Dlod(_MainTex, uv, 0));
			return float4(color.rgb * _Intensity * MODIFIER, 1);
		}
		
		float4 fragApplyBloom(VertexOutput i) : SV_Target {
			return applyBloom(i.uv, _MipLevelForRadius);
		}
		
		//----------------------------------------------------------------------------------------------------------------------
		//----------------------------------------------------------------------------------------------------------------------
		
		half4 fragDebug(VertexOutputFull i ) : SV_Target {
			//public enum DebugMode {
			//	BloomTextureCompressed = 0,
			//	BloomTexture = 1,
			//}
			float4 color = applyBloom(i.uv, _MipLevelForDebug) ;
			float3 decompColor = color;
		//	decompColor = DeCompressBy(color.rgb, MaxC(color.rgb));
			half3 values[2] = {
		/*0*/	half3(color.rgb * color.a),
		/*1*/	half3(decompColor * color.a * MODIFIER),
			};
			
			float3 result = values[(int)_DebugMode];
			
			//result.x = GammaToLinearSpaceExact(result.x);
			//result.y = GammaToLinearSpaceExact(result.y);
			//result.z = GammaToLinearSpaceExact(result.z);
			
			//result = uv.x > 0.0 ? float3(1, 1, 1)*0.5 : result;
			return half4(result, 1);
		//	return pow(half4(abs(frag.rgb), 0), 1);
		//	return pow(half4(abs(frag.rgb), 0), 1);
		}
		
		
	ENDCG 
	
	Subshader {
		ZTest Always Cull Off ZWrite Off
		Fog { Mode off }
		
		//Pass 0 fragBloomIntensity
		Pass {
			
			CGPROGRAM
			#pragma target 3.0
			
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma vertex vert
			#pragma fragment fragBloomIntensity
			ENDCG
		}
		
		//Pass 1 BloomBlur
		Pass {
			
			CGPROGRAM
			#pragma target 3.0
			
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma vertex vert
			#pragma fragment fragBloomBlur
			ENDCG
		}
		
		//Pass 2 ApplyBloom
		Pass {
			Blend SrcAlpha One, Zero One
			
			CGPROGRAM
			#pragma target 3.0
			
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma vertex vert
			#pragma fragment fragApplyBloom
			ENDCG
		}

		//Pass 5 debug
		Pass {
			CGPROGRAM
			#pragma target 3.0
			
			#pragma vertex vertFull
			#pragma fragment fragDebug
			ENDCG
		}
		
	}
	Fallback Off
}
