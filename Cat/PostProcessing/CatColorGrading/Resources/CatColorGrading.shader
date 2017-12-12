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

Shader "Hidden/Cat Color Grading" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "black" {}
		_Strength ("_Strength", FLOAT) = 1
	}
	
	CGINCLUDE
		#include "UnityCG.cginc"
		#include "../../Includes/PostProcessingCommon.cginc"
		#include "ACES.cginc"
		
		#pragma multi_compile __ TONEMAPPING_NEUTRAL TONEMAPPING_FILMIC TONEMAPPING_UNCHARTED_2
		
		float _Response;
		float _Gain;
		
		float _Exposure;
		float _Contrast;
		float _Saturation;
		
		float4x4 _ColorMixerMatrix;
		
		float _BlackPoint;
		float _WhitePoint;
		
		float4 _CurveParams;
		
		float _Strength;
		
		float3 _HSV;
		
		sampler2D _BlueNoise;	float4 _BlueNoise_TexelSize;
		
		
		static const half3x3 LIN_2_LMS_MAT = {
			3.90405e-1, 5.49941e-1, 8.92632e-3,
			7.08416e-2, 9.63172e-1, 1.35775e-3,
			2.31082e-2, 1.28021e-1, 9.36245e-1
		};

		static const half3x3 LMS_2_LIN_MAT = {
			 2.85847e+0, -1.62879e+0, -2.48910e-2,
			-2.10182e-1,  1.15820e+0,  3.24281e-4,
			-4.18120e-2, -1.18169e-1,  1.06867e+0
		};
				
		/** {
		 *              [  0.7328  0.4296  -0.1624 ]
		 *    M_CAT02 = [ -0.7036  1.6975   0.0061 ]
		 *              [  0.0030  0.0136   0.9834 ]
		 *
		 *              [  1.096124 -0.278869 0.182745 ]
		 * M^-1_CAT02 = [  0.454369  0.473533 0.072098 ]
		 *              [ -0.009628 -0.005698 1.015326 ]
		} */
		static const float3x3 M_CAT02_XYZ_TO_LMS = {
			 0.7328  ,  0.4296  , -0.1624  ,
			-0.7036  ,  1.6975  ,  0.0061  ,
			 0.0030  ,  0.0136  ,  0.9834
		};
		static const float3x3 M_CAT02_LMS_TO_XYZ = {
			 1.096124, -0.278869,  0.182745,
			 0.454369,  0.473533,  0.072098,
			-0.009628, -0.005698,  1.015326
		};
		
		/** {
		 *    [X]               [ 0.49000  0.31000   0.20000 ]   [R]
		 *    [Y] = 1/0.17697 * [ 0.17697  0.81240   0.010630] X [G]
		 *    [Z]               [ 0.0000   0.010000  0.99000 ]   [B]
		 *                                                            
		 *    [R]   [ 0.41847    -0.15866   -0.082835]   [X]
		 *    [G] = [-0.091169    0.25243    0.015708] X [Y]
		 *    [B]   [ 0.00092090 -0.0025498  0.17860 ]   [Z]
		} */
		static const float3x3 ACEScg_TO_XYZ = 1/0.17697 * float3x3(
			 0.49000,  0.31000 ,  0.20000 ,
			 0.17697,  0.81240 ,  0.010630,
			 0.0000 ,  0.010000,  0.99000 
		);
		static const float3x3 XYZ_TO_ACEScg = {
			 0.41847   , -0.15866  , -0.082835,
			-0.091169  ,  0.25243  ,  0.015708,
			 0.00092090, -0.0025498,  0.17860 
		};
		
	// ???	static const float3x3 ACEScg_TO_LMS = mul(M_CAT02_XYZ_TO_LMS, AP1_2_XYZ_MAT);
	// ???	static const float3x3 LMS_TO_ACEScg = mul(XYZ_2_AP1_MAT, M_CAT02_LMS_TO_XYZ);
		
		// using CIECAM02's transformation matrix
		inline half3 ACEScgToLMS(half3 rgb) {
			return mul(LIN_2_LMS_MAT, rgb);
		}
		inline half3 LMStoACEScg(half3 lms) {
			return mul(LMS_2_LIN_MAT, lms);
		}
		
		inline half3 ACEStoLMS(half3 aces) {
			return mul(mul(LIN_2_LMS_MAT, AP0_2_AP1_MAT), aces);
		}
		inline half3 LMStoACES(half3 lms) {
			return mul(mul(AP1_2_AP0_MAT, LMS_2_LIN_MAT), lms);
		}
		
		inline half3 LMStoUnity(half3 lms) {
			return mul(mul(AP0_2_sRGB, mul(AP1_2_AP0_MAT, LMS_2_LIN_MAT)), lms);
		}

		
		inline half3 ACEScgToXYZ(half3 rgb) {
			return mul(ACEScg_TO_XYZ, rgb);
		}
		inline half3 XYZtoACEScg(half3 xyz) {
			return mul(XYZ_TO_ACEScg, xyz);
		}
		
		// using CIECAM02's transformation matrix
		inline half3 XYZtoLMS(half3 xyz) {
			return mul(M_CAT02_XYZ_TO_LMS, xyz);
		}
		inline half3 LMStoXYZ(half3 lms) {
			return mul(M_CAT02_LMS_TO_XYZ, lms);
		}
		
		 // https://en.wikipedia.org/wiki/Rec._709#Luma_coefficients
		static const float3 ACES_LUMA_COEFFICIENTS = { 0.2126, 0.7152, 0.0722 };
		float AcesLuma(float3 acescc) {
			return dot(acescc, ACES_LUMA_COEFFICIENTS);
		}
		
		
		void Exposure(inout float3 rgb, float exposure) {
			rgb = rgb * (exposure);
		}
		
		void ContrastSaturation(inout float3 acescc, float contrast, float saturation) { // contrastLog = ]-Infinity...+Infinity[, [-1... 1] recomended.
		//	contrast   = contrast + Pow2(max(0, contrast)) + 1;
		//	contrast   = max(EPSILON, contrast);
		//	saturation = (saturation + Pow2(max(0, saturation)) + 1) / contrast;
			
			float luma = AcesLuma(acescc);
			acescc = (acescc - luma          ) * saturation + luma;
			acescc = (acescc - ACEScc_MIDGRAY) * contrast   + ACEScc_MIDGRAY;
		}
		
		void ColorBalance(inout float3 lms, float3 colorBalance) {
			lms *= colorBalance;
		}
		
		void ColorMixer(inout float3 rgb, float3x3 mixingMatrix) {
			rgb = mul(mixingMatrix, rgb);
		}
		 
		
		void Curves(inout float3 sRGB, float blackPoint, float whitePoint, float4 curveParams) {
			//whitePoint = 1 + whitePoint * 0.25;
			//blackPoint = 0 + blackPoint * 0.25;
			
			sRGB = max(0, (sRGB - blackPoint) / (whitePoint - blackPoint));
			
			float value = MaxC(sRGB);
			sRGB *= 1.0 / max(value, EPSILON);
			
			//value = (value - blackPoint) / (whitePoint - blackPoint);
			value = saturate(value);
			value =  (curveParams.w + (curveParams.z + (curveParams.y + curveParams.x * value) * value) * value) * value;
			value = saturate(value);
			
			sRGB *= value;
		}
		
		void Dithering(inout float3 rgb, float2 uv) {
			float3 noise2D = Tex2Dlod(_BlueNoise, uv * _MainTex_TexelSize.zw * _BlueNoise_TexelSize.xy, 0).rrr * 2 - 1;
			noise2D = sign(noise2D) * (1.0 - sqrt(1.0 - abs(noise2D))) * 0.5;
			noise2D /= 255.0;
			rgb += noise2D;
		}
		
		
		// ACES fitting shamefully copied from Unity
		// https://github.com/Unity-Technologies/PostProcessing/blob/v1/PostProcessing/Resources/Shaders/Tonemapping.cginc
		void FilmicToneMapping(inout float3 rgb) {
			float3 aces = unity_to_ACES(rgb);
			
			// --- Glow module --- //
			half saturation = rgb_2_saturation(aces);
			half ycIn = rgb_2_yc(aces);
			half s = sigmoid_shaper((saturation - 0.4) / 0.2);
			half addedGlow = 1.0 + glow_fwd(ycIn, RRT_GLOW_GAIN * s, RRT_GLOW_MID);
			aces *= addedGlow;

			// --- Red modifier --- //
			half hue = rgb_2_hue(aces);
			half centeredHue = center_hue(hue, RRT_RED_HUE);
			half hueWeight;
			{
				//hueWeight = cubic_basis_shaper(centeredHue, RRT_RED_WIDTH);
				hueWeight = Pow2(smoothstep(0.0, 1.0, 1.0 - abs(2.0 * centeredHue / RRT_RED_WIDTH)));
			}

			aces.r += hueWeight * saturation * (RRT_RED_PIVOT - aces.r) * (1.0 - RRT_RED_SCALE);

			// --- ACES to RGB rendering space --- //
			half3 acescg = max(0.0, ACES_to_ACEScg(aces));

			// --- Global desaturation --- //
			//acescg = mul(RRT_SAT_MAT, acescg);
			acescg = lerp(dot(acescg, AP1_RGB2Y).xxx, acescg, RRT_SAT_FACTOR.xxx);

			// Luminance fitting of *RRT.a1.0.3 + ODT.Academy.RGBmonitor_100nits_dim.a1.0.3*.
			// https://github.com/colour-science/colour-unity/blob/master/Assets/Colour/Notebooks/CIECAM02_Unity.ipynb
			// RMSE: 0.0012846272106
			const half a = 278.5085;
			const half b = 10.7772;
			const half c = 293.6045;
			const half d = 88.7122;
			const half e = 80.6889;
			half3 x = acescg;
			half3 rgbPost = (x * (a * x + b)) / (x * (c * x + d) + e);

			// Scale luminance to linear code value
			// half3 linearCV = Y_2_linCV(rgbPost, CINEMA_WHITE, CINEMA_BLACK);

			// Apply gamma adjustment to compensate for dim surround
			half3 linearCV = darkSurround_to_dimSurround(rgbPost);

			// Apply desaturation to compensate for luminance difference
			//linearCV = mul(ODT_SAT_MAT, color);
			linearCV = lerp(dot(linearCV, AP1_RGB2Y).xxx, linearCV, ODT_SAT_FACTOR.xxx);

			// Convert to display primary encoding
			// Rendering space RGB to XYZ
			half3 XYZ = mul(AP1_2_XYZ_MAT, linearCV);

			// Apply CAT from ACES white point to assumed observer adapted white point
			XYZ = mul(D60_2_D65_CAT, XYZ);

			// CIE XYZ to display primaries
			linearCV = mul(XYZ_2_REC709_MAT, XYZ);

			rgb = linearCV;
		}
		
		
		void CustomToneMapping(inout float3 rgb) {
			rgb = rgb*(rgb + 1) / (rgb*(rgb + 1.125) + 1);
		}
		
		void NeutralToneMapping(inout float3 rgb, float response, float gain) {
			// rgb = rgb * (1.235 * rgb + 0.1235) / (rgb * (rgb + 1.035) + 0.1235);
			
			rgb = max(0, rgb);
			// rgb = gain * 1.235 * rgb / ((1.235 - rgb / (0.1 + rgb) * 0.3) / response * gain + rgb);
			rgb = response*gain*rgb*(0.1235 + 1.235*rgb) / (gain*(0.1235 + 0.935*rgb) + response*rgb*(0.1 + rgb));
		}
		
		
		float3 NeutralCurve(float3 rgb, float a, float b, float c, float d, float e, float f)
		{
			return ((rgb * (a * rgb + c * b) + d * e) / (rgb * (a * rgb + b) + d * f)) - e / f;
		}
		
		void Uncharted2ToneMapping(inout float3 rgb) {
			const float a = 0.15;
			const float b = 0.50;
			const float c = 0.10;
			const float d = 0.20;
			const float e = 0.02;
			const float f = 0.30;
			const float W = 11.21;
			const float exposureBias = 2.00;

			// Tonemap
			float whiteScale = 1 / NeutralCurve(W, a, b, c, d, e, f).x;
			rgb = NeutralCurve(rgb * exposureBias, a, b, c, d, e, f);
			rgb *= whiteScale;
		}
		
		half4 ColorGrading(VertexOutput i) : SV_Target {
			float4 color = Tex2Dlod(_MainTex, i.uv, 0);
			float3 rgb = color.rgb;
			
			Exposure(/*inout*/rgb, _Exposure);
			
			
			#if TONEMAPPING_FILMIC 
				FilmicToneMapping(/*inout*/ rgb);
			#elif TONEMAPPING_NEUTRAL
				NeutralToneMapping(/*inout*/ rgb, _Response, _Gain);
			#elif TONEMAPPING_UNCHARTED_2
				Uncharted2ToneMapping(/*inout*/ rgb);
			#endif
			
			
			
			float3 aces = unity_to_ACES(rgb);
			float3 acescc = ACES_to_ACEScc_optimized(aces);
			
			ContrastSaturation(/*inout*/acescc, _Contrast , _Saturation);
			
			aces = ACEScc_to_ACES_optimized(acescc);
			
			rgb = mul((float3x3)_ColorMixerMatrix, aces);
			
			rgb = saturate(rgb);
			float3 sRGB = LinearToGammaSpace(rgb);
			
			Curves(/*inout*/sRGB, _BlackPoint, _WhitePoint, _CurveParams);
			
			rgb = GammaToLinearSpace(sRGB);
			rgb = saturate(rgb);
			
			Dithering(/*inout*/rgb, i.uv);
			
			//rgb = HSVtoRGB(float3(i.uv.x, _HSV.y, i.uv.y));
			
			color.rgb = saturate(rgb);
			return color;
			
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
			#pragma fragment ColorGrading
			ENDCG
		}
	}
	Fallback Off
	//"Diffuse"
}
