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

Shader "Hidden/Cat DepthOfField" {
	Properties {
		_Dummy ("DUMMY (-)", 2D) = "black" {}
	}
	
	CGINCLUDE
		#define ANTI_FLICKER 1
		
		#include "UnityCG.cginc"
		#include "UnityStandardUtils.cginc"
		#include "UnityGlobalIllumination.cginc"
		
		#include "../../Includes/PostProcessingCommon.cginc"
		
		float	_Intensity;
		float	_fStop;
		float	_FocusDistance;
		float	_Radius;
		
		float4	_BlurDir;
		float	_MipLevel;
		float	_Weight;
		
		sampler2D	_BlurTex;
		float4		_BlurTex_TexelSize;
		
				// debugOn
		int		_DebugMode;
		
		//----------------------------------------------------------------------------------------------------------------------
		//----------------------------------------------------------------------------------------------------------------------
		
		// Height of the 35mm full-frame format (36mm x 24mm)
		static const float k_FilmHeight = 0.024;
		static const float2 k_FilmSize = {0.036, 0.024};
		
		float get_S1() {
			return _FocusDistance;
		};
		
		float3 get_V1() {
			// V1 := view-frustum at distance 1 {W1, H1, 1} = float3(2, 2, 1) * outerRay / outerRay.z
			float3 outerRay = ScreenToViewPos(GetScreenPos(float2(1, 1), 0.5));
			return float3(2 * outerRay.xy / outerRay.z, 1);
		};
		
		float get_S2() {
			// R  := outerRay; h := height of film, eg 0.024 (24mm); Hs1 := height of view-frustum at distance of Focal Plane (S1)
			// V1 := view-frustum at distance 1 {W1, H1, 1} = float3(2, 2, 1) * outerRay / outerRay.z
			// V1 := {2 * R.xy / R.z, 1}
			//    Hs1 / 2 == R.y / R.z * S1
			// && h / Hs1 == S2 / S1
			// => H  == V1.y * S1
			// && S2 == h / V1.y
			
			float3 V1 = get_V1();
			float2 S2 = k_FilmSize / V1.xy;
			return S2.y;
		};
		
		float get_focalLength() {
			return 1 / (1/get_S1() + 1/get_S2());
		};
		
		float get_d1(float2 uv) {
			return sampleEyeDepth(_DepthTexture, uv);
		};
		
		float get_fStop() {
			return _fStop;
		};
		
		float get_apertureDiameter() { // = diameter of the entrance pupil (effective aperture) in [m]
			return get_focalLength() / get_fStop();
		};
		
		float get_coeff() {
			//	float S1 = max(get_S1(), f);
			//	float coeff = f / ((S1 - f) * k_FilmHeight * 2) * dAperture;
			//	float coeff = f / (max(0, S1 - f) * k_FilmHeight * 2) * dAperture;
			//	  coeff = f / ((S1 - f) * k_FilmHeight * 2) * dAperture;
			//	  f = 1 / (1/S1 + 1/S2);
			//	  coeff = 1 / (1/S1 + 1/S2) / (S1*2*k_FilmHeight - 1 / (1/S1 + 1/S2)*2*k_FilmHeight) * dAperture;
			//	  coeff = 1 / (1/S_1 + 1/S_2) / (S_1*2*h - 1 / (1/S_1 + 1/S_2)*2*h) * A;
			// => coeff = A * S_2 / (2 * h * S_1)
			//	  S_2 = k_FilmSize / V_1 =>> S_2 = h / V_1
			// => coeff = A * h / V_1 / (2 * h * S_1)
			// => coeff = A / V_1 / (2 * S_1)
			 
			float A = get_apertureDiameter();
			float V1 = get_V1().y;
			float S1 = get_S1();
			return A / (2 * V1 * S1);
		};
		
		float get_coc(float2 uv) {
			float d1 = get_d1(uv);
			float S1 = get_S1();
			float coeff = get_coeff();
			return (d1 - S1) * coeff / max(d1, 1e-5);
		}
		
		float getBlurRadius(float2 uv) {
			half coc = get_coc(uv);
			
			//float radius = _Intensity * abs(coc)*2 * _MainTex_TexelSize.w;
			//float maxRadius = (_Radius+2);
			//float a = 0.5;
			//float x = radius / maxRadius;
			//radius = ((x + a*0.25 - 1) - sqrt(a + Pow2(x + a*0.25 - 1)))*0.5 + 1;
			//radius *= maxRadius;
			//return radius;
			
			float radius = _Intensity * abs(coc) * _MainTex_TexelSize.w;
			float maxRadius = (_Radius+2);
			float x = radius / maxRadius - 0.4375;
			radius = x - sqrt(0.125 + Pow2(x)) + 1;
			radius *= maxRadius;
			return radius;
		}
		//----------------------------------------------------------------------------------------------------------------------
		//----------------------------------------------------------------------------------------------------------------------
		
		float getMipLevel(float blurRadius) {
			//    totalRadius = 2^(mip) * (1 + _Radius) - _Radius
			// => mip = log2((_Radius + totalRadius) / (_Radius + 1))
			return log2((_Radius + max(1, blurRadius)) / (_Radius + 1));
		}
		
		float getMipLevel(float2 uv) {
			return getMipLevel(getBlurRadius(uv));
		}
		
		// !!! kDiskKernel taken from unity s PostProcessing Stack v1. see their Github. !!!
		#define KERNEL_MEDIUM
#if defined(KERNEL_SMALL)

// rings = 2
// points per ring = 5
static const int kSampleCount = 16;
static const float2 kDiskKernel[kSampleCount] = {
    float2(0,0),
    float2(0.54545456,0),
    float2(0.16855472,0.5187581),
    float2(-0.44128203,0.3206101),
    float2(-0.44128197,-0.3206102),
    float2(0.1685548,-0.5187581),
    float2(1,0),
    float2(0.809017,0.58778524),
    float2(0.30901697,0.95105654),
    float2(-0.30901703,0.9510565),
    float2(-0.80901706,0.5877852),
    float2(-1,0),
    float2(-0.80901694,-0.58778536),
    float2(-0.30901664,-0.9510566),
    float2(0.30901712,-0.9510565),
    float2(0.80901694,-0.5877853),
};

#endif

#if defined(KERNEL_MEDIUM)

// rings = 3
// points per ring = 7
static const int kSampleCount = 22;
static const float2 kDiskKernel[kSampleCount] = {
    float2(0,0),
    float2(0.53333336,0),
    float2(0.3325279,0.4169768),
    float2(-0.11867785,0.5199616),
    float2(-0.48051673,0.2314047),
    float2(-0.48051673,-0.23140468),
    float2(-0.11867763,-0.51996166),
    float2(0.33252785,-0.4169769),
    float2(1,0),
    float2(0.90096885,0.43388376),
    float2(0.6234898,0.7818315),
    float2(0.22252098,0.9749279),
    float2(-0.22252095,0.9749279),
    float2(-0.62349,0.7818314),
    float2(-0.90096885,0.43388382),
    float2(-1,0),
    float2(-0.90096885,-0.43388376),
    float2(-0.6234896,-0.7818316),
    float2(-0.22252055,-0.974928),
    float2(0.2225215,-0.9749278),
    float2(0.6234897,-0.7818316),
    float2(0.90096885,-0.43388376),
};

#endif

#if defined(KERNEL_LARGE)

// rings = 4
// points per ring = 7
static const int kSampleCount = 43;
static const float2 kDiskKernel[kSampleCount] = {
    float2(0,0),
    float2(0.36363637,0),
    float2(0.22672357,0.28430238),
    float2(-0.08091671,0.35451925),
    float2(-0.32762504,0.15777594),
    float2(-0.32762504,-0.15777591),
    float2(-0.08091656,-0.35451928),
    float2(0.22672352,-0.2843024),
    float2(0.6818182,0),
    float2(0.614297,0.29582983),
    float2(0.42510667,0.5330669),
    float2(0.15171885,0.6647236),
    float2(-0.15171883,0.6647236),
    float2(-0.4251068,0.53306687),
    float2(-0.614297,0.29582986),
    float2(-0.6818182,0),
    float2(-0.614297,-0.29582983),
    float2(-0.42510656,-0.53306705),
    float2(-0.15171856,-0.66472363),
    float2(0.1517192,-0.6647235),
    float2(0.4251066,-0.53306705),
    float2(0.614297,-0.29582983),
    float2(1,0),
    float2(0.9555728,0.2947552),
    float2(0.82623875,0.5633201),
    float2(0.6234898,0.7818315),
    float2(0.36534098,0.93087375),
    float2(0.07473,0.9972038),
    float2(-0.22252095,0.9749279),
    float2(-0.50000006,0.8660254),
    float2(-0.73305196,0.6801727),
    float2(-0.90096885,0.43388382),
    float2(-0.98883086,0.14904208),
    float2(-0.9888308,-0.14904249),
    float2(-0.90096885,-0.43388376),
    float2(-0.73305184,-0.6801728),
    float2(-0.4999999,-0.86602545),
    float2(-0.222521,-0.9749279),
    float2(0.07473029,-0.99720377),
    float2(0.36534148,-0.9308736),
    float2(0.6234897,-0.7818316),
    float2(0.8262388,-0.56332),
    float2(0.9555729,-0.29475483),
};

#endif
		
		float4 fragBlur(VertexOutput i) : SV_Target {
			//float radiusFull = Tex2Dlod(_MainTex, i.uv, _MipLevel-1).a * _MainTex_TexelSize.w;
			float radiusFull = tex2D(_MainTex, i.uv).a;

			float radius = clamp(radiusFull-2, 0, _Radius);
			
			float4 sumColor = 0;
			float sumWeights = 0;
			int sampleCount = kSampleCount;
			for (int k = 0; k < sampleCount; k++) {
				float2 uvTap    = i.uv + kDiskKernel[k] * _MainTex_TexelSize.xy * radius;// * exp2(_MipLevel)*1;
				
				float4 color = tex2D(_MainTex, uvTap);
				float weight = color.a * 100;
	
				sumColor += color * weight;
				sumWeights += weight;
			}
			
			//return radius / MinC(_MainTex_TexelSize.xy);
			return sumColor / max(EPSILON, sumWeights);
		}

		float4 fragSmooth(VertexOutput i) : SV_Target {
			float4 d = _MainTex_TexelSize.xyxy * float4(-1.0, -1.0, 1.0, 1.0) * 0.5;
			float4 sumColor = 0;
			sumColor += Tex2Dlod(_MainTex, i.uv + d.xy, 0);
			sumColor += Tex2Dlod(_MainTex, i.uv + d.zy, 0);
			sumColor += Tex2Dlod(_MainTex, i.uv + d.xw, 0);
			sumColor += Tex2Dlod(_MainTex, i.uv + d.zw, 0);
			sumColor *= (0.25 * 1);
			//sumColor = Tex2Dlod(_MainTex, i.uv, 0);
			return sumColor;
		}

		//----------------------------------------------------------------------------------------------------------------------
		//----------------------------------------------------------------------------------------------------------------------
		
		float4 fragApply(VertexOutput i) : SV_Target {
			float4 d = _BlurTex_TexelSize.xyxy * float4(-1.0, -1.0, 1.0, 1.0) * 0.5;
			float4 color2 = 0;
			color2 += Tex2Dlod(_BlurTex, i.uv + d.xy, 0);
			color2 += Tex2Dlod(_BlurTex, i.uv + d.zy, 0);
			color2 += Tex2Dlod(_BlurTex, i.uv + d.xw, 0);
			color2 += Tex2Dlod(_BlurTex, i.uv + d.zw, 0);
			color2 *= (0.25 * 1);
			float4 color1 = tex2D(_MainTex, i.uv);
			
			float radiusFull = color2.a;
			float selector = InvLerpSat(0, 2, radiusFull);
			float4 color = lerp(color1, color2, selector);
			
			return float4(color.rgb, color1.a);
		}

		//----------------------------------------------------------------------------------------------------------------------
		//----------------------------------------------------------------------------------------------------------------------
		
				// 3-tap median filter by 
		half3 Median(half3 a, half3 b, half3 c) {
			return a + b + c - min(min(a, b), c) - max(max(a, b), c);
		}
		
		#define ANTI_FLICKER 1
		float4 fragPreFilterX(VertexOutput i) : SV_Target {
			float blurRadius = getBlurRadius(i.uv+_TAAJitterVelocity);
			float mip = getMipLevel(blurRadius);
		#if ANTI_FLICKER
			float4 d = _MainTex_TexelSize.xyxy * float4(+0.5, -1.5, +1.5, +0.5);// * InvLerpSat(0, 3, blurRadius * _MainTex_TexelSize.w);
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
			color.a = blurRadius;
		
			return color;
		}
		
		float4 fragPreFilter(VertexOutput i) : SV_Target {
			float blurRadius = getBlurRadius(i.uv+_TAAJitterVelocity);
			float4 d = _MainTex_TexelSize.xyxy * float4(+0.5, -1.5, +1.5, +0.5) * Clamp11(blurRadius);//float4(-1.0, -1.0, 1.0, 1.0);
			float3 sumColor = 0;
			sumColor += Tex2Dlod(_MainTex, i.uv + d.xy, 0).rgb;
			sumColor += Tex2Dlod(_MainTex, i.uv - d.xy, 0).rgb;
			sumColor += Tex2Dlod(_MainTex, i.uv + d.zw, 0).rgb;
			sumColor += Tex2Dlod(_MainTex, i.uv - d.zw, 0).rgb;
			sumColor += Tex2Dlod(_MainTex, i.uv, 0).rgb;
			sumColor *= (0.20 * 1);
			return float4(sumColor, blurRadius);
		}
		
		float4 fragPreFilterZ(VertexOutput i) : SV_Target {
			float blurRadius = getBlurRadius(i.uv+_TAAJitterVelocity);
			float4 d = _MainTex_TexelSize.xyxy * float4(-1.0, -1.0, 1.0, 1.0);
			float4 sumColor = 0;
			sumColor += Tex2Dlod(_MainTex, i.uv + d.xy, 0);
			sumColor += Tex2Dlod(_MainTex, i.uv + d.zy, 0);
			sumColor += Tex2Dlod(_MainTex, i.uv + d.xw, 0);
			sumColor += Tex2Dlod(_MainTex, i.uv + d.zw, 0);
			sumColor += Tex2Dlod(_MainTex, i.uv, 0);
			sumColor *= (0.20 * 1);
			sumColor.a = blurRadius;
			return sumColor;
		}
		
		float4 fragPreFilterY(VertexOutput i) : SV_Target {
			float blurRadius = getBlurRadius(i.uv+_TAAJitterVelocity);
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
			
			color *= (1.0 / 16.0) * 1;
			color.a = blurRadius;
			return color;
		}
		
		
		//----------------------------------------------------------------------------------------------------------------------
		//----------------------------------------------------------------------------------------------------------------------
		
		half4 fragDebug(VertexOutputFull i ) : SV_Target {
			VertexOutput vo = {i.pos, i.uv};
			float4 bkg = Tex2Dlod(_MainTex, i.uv, 0)*0.875+0.125;
			bkg.rgb = DisneyLuminance(bkg.rgb)*1.00;
		//	bkg.rgb = dot(bkg.rgb, half3(0.2126, 0.7152, 0.0722));
			bkg.rgb = CompressBy(bkg.rgb, bkg.rgb*Compress(bkg.rgb*Compress(bkg.rgb)));
			//bkg.rgb /= 1 + bkg.rgb;
			half coc = get_coc(i.uv);
			coc *= 80;
			
			float3 white = 1;
			float3 red = float3(1, 0, 0);
			float3 data = Lerp3(white, red, white, pow(abs(Clamp11(coc)), 1/2.2));
			data = pow(data, 2.2);
			bkg.rgb *= data;
			
			
			return half4(bkg.rgb, bkg.a);
		}
		
		
	ENDCG 
	
	Subshader {
		ZTest Always Cull Off ZWrite Off
		Fog { Mode off }

		//Pass 0 PreFilter
		Pass {
			CGPROGRAM
			#pragma target 3.0
			
			#pragma vertex vert
			#pragma fragment fragPreFilter
			ENDCG
		}

		//Pass 1 Blur
		Pass {
			CGPROGRAM
			#pragma target 3.0
			
			#pragma vertex vert
			#pragma fragment fragBlur
			ENDCG
		}

		//Pass 3 Apply
		Pass {
			CGPROGRAM
			#pragma target 3.0
			
			#pragma vertex vert
			#pragma fragment fragApply
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


	}
	Fallback Off
}
