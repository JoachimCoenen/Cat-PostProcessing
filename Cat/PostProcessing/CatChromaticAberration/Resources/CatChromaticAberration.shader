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

Shader "Hidden/Cat Chromatic Aberration" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "black" {}
		_Strength ("_Strength", FLOAT) = 1
	}
	
	CGINCLUDE
		#include "UnityCG.cginc"
		#include "../../Includes/PostProcessingCommon.cginc"
		
		
		float _Strength;
		static const bool _IsDebugOn = false;
		
		
		half4 chromaticAberration(VertexOutput i) : SV_Target {
			float2 uv = i.uv;
			float2 ssPos = GetScreenPos(uv, 0).xy;
			float strength = 0.03125 * _Strength;
			
			float2 shift = ssPos * _MainTex_TexelSize.zw / length(_MainTex_TexelSize.zw) * strength;
			
			float amount = length(ssPos * _MainTex_TexelSize.zw) / length(_MainTex_TexelSize.zw);
			amount = Pow2(amount);
			amount *= strength;
			shift = -normalize(ssPos) * amount;
			
			
			half3 sumColor = 0;
			half3 sumWeight = 0;
			int samples = clamp(int(length(_MainTex_TexelSize.zw * shift / 2)), 3, 16);
			float3 stepSize = float3(shift, 1) / float3(samples.xx, samples-1);
			float3 uvTap = float3(uv, 0);
			float P = 1.33333;
			
			for (int k = 0; k < samples; k++) {
				float4 tap = Tex2Dlod(_MainTex, uvTap.xy, 0).rgba;
				float3 weight = lerp(float2(P, -P), float2(-P, P), uvTap.z).xyy;
				weight.g = 1 - abs(weight.g);
				
				sumColor += tap * saturate(weight);
				sumWeight += saturate(weight);
				uvTap += stepSize;
			}
			sumColor /= sumWeight;
			return float4(_IsDebugOn ? amount.xxx/0.03125 : sumColor, 0);
		}
		
		half4 chromaticAberration2(VertexOutput i) : SV_Target {
			float2 uv = i.uv;
			float2 uvX = i.uv * 2 - 1;
			float strength = 0.03125 * _Strength;
			
			float2 aspect = _MainTex_TexelSize.zw * MinC(_MainTex_TexelSize.xy);
			float2 uvY = uvX * aspect; 
			float dotX = sqrt(dot(uvY, uvY));
			float2 amount = abs(uvX)*dotX;
			amount = amount <= 1 ? amount : pow(amount, 1/3.0)*3-2;
			amount *= strength*sign(-uvX);
			
			half3 sumColor = 0;
			half3 sumWeight = 0;
			int samples = clamp(int(length(_MainTex_TexelSize.zw * amount / 2)), 3, 16);
			float3 stepSize = float3(amount, 1) / float3(samples.xx, samples-1);
			float3 uvTap = float3(uv, 0);
			
			float P = 1.33333;
		//	UNITY_UNROLL
			for (int k = 0; k < samples; k++) {
				float4 tap = Tex2Dlod(_MainTex, uvTap.xy, 0).rgba;
				float3 weight = lerp(float2(P, -P), float2(-P, P), uvTap.z).xyy;
				weight.g = 1 - abs(weight.g);
				
				sumColor += tap * saturate(weight);
				sumWeight += saturate(weight);
				uvTap += stepSize;
			}
			sumColor /= sumWeight;
			return float4(sumColor, 0);
		}

		VertexOutput vertX(VertexInput v) {
			VertexOutput o;
			o.pos = float4(v.vertex.xy, 0, 1);
			o.uv = v.texcoord.xy;
			//	o.uv.y = 1-o.uv.y;
			return o;
		}
		
	ENDCG 
	
	Subshader {
		ZTest Always Cull Off ZWrite Off
		Fog { Mode off }
		
		//Pass 0 ChromaticAberration
		Pass {
			
			CGPROGRAM
			#pragma target 3.0

			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma vertex vert
			#pragma fragment chromaticAberration
			ENDCG
		}
	}
	Fallback "Diffuse"
}
