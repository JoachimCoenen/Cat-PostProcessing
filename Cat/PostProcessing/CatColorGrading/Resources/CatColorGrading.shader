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
		
		
		float _Exposure;
		float _Contrast;
		float _Saturation;
		
		float _Temperature;
		float _Tint;
		float _BlackPoint;
		float _WhitePoint;
		
		float4 _CurveParams;
		
		float _Strength;
		
		static const bool _IsDebugOn = false;
		
		
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
		
		
		// An analytical model of chromaticity of the standard illuminant, by Judd et al.
		// http://en.wikipedia.org/wiki/Standard_illuminant#Illuminant_series_D
		// Slightly modifed to adjust it with the D65 white point (x=0.31271, y=0.32902).
		float StandardIlluminantY(float x) {
			return 2.87 * x - 3 * x * x - 00.27509507;
		}
		
		// CIE xy chromaticity to CAT02 LMS.
		// http://en.wikipedia.org/wiki/LMS_color_space#CAT02
		float3 XYtoLMS(float x, float y){
			float3 xyz = float3(1 * x / y, 1, 1 * (1 - x - y) / y);
		
			return XYZtoLMS(xyz);
		}
		
		float3 CalculateColorBalance(float temperature, float tint) {
			// Range ~[-1.8;1.8] ; using higher ranges is unsafe
			float t1 = temperature / 0.55;
			float t2 = tint / 0.55;
			
			// Get the CIE xy chromaticity of the reference white point.
			// Note: 0.31271 = x value on the D65 white point
			float x = 0.31271 - t1 * (t1 < 0 ? 0.1 : 0.05);
			float y = StandardIlluminantY(x) + t2 * 0.05;
			
			// Calculate the coefficients in the LMS space.
			float3 w1 = float3(0.949237, 1.03542, 1.08728); // D65 white point
			float3 w2 = XYtoLMS(x, y);
			return float3(w1.x / w2.x, w1.y / w2.y, w1.z / w2.z);
		}
		
		
		
		void Exposure(inout float3 rgb, float exposure) {
			rgb = rgb * exp2(exposure);
		}
		
		void ContrastSaturation(inout float3 acescc, float contrast, float saturation) { // contrastLog = ]-Infinity...+Infinity[, [-1... 1] recomended.
		//	saturation = exp2((saturation-contrast) * 2);
		//	contrast   = exp2(contrast              * 2);
			contrast   = contrast + Pow2(max(0, contrast)) + 1;
			contrast   = max(EPSILON, contrast);
			
		//	contrast   = max(EPSILON, (contrast+1)*2);
			//saturation = (saturation + 1) / contrast;
			saturation = (saturation + Pow2(max(0, saturation)) + 1) / contrast;
			
			float luma = AcesLuma(acescc);
			acescc = (acescc - luma          ) * saturation + luma;
			acescc = (acescc - ACEScc_MIDGRAY) * contrast   + ACEScc_MIDGRAY;
		}
		
		void ColorBalance(inout float3 lms, float temperature, float tint) {
			lms *= CalculateColorBalance(temperature, tint);
		}
		
		void BlackWhitePoint(inout float3 sRGB, float blackPoint, float whitePoint) {
			whitePoint = 1 + whitePoint * 0.25;
			blackPoint = 0 + blackPoint * 0.25;
			sRGB = (sRGB - blackPoint) / (whitePoint - blackPoint);
			sRGB = max(00, sRGB);
		}
		
		void Curves(inout float3 sHSV, float4 curveParams) {
			float x = sHSV.z;
			x = (curveParams.w + (curveParams.z + (curveParams.y + curveParams.x * x) * x) * x) * x;
			sHSV.z = x;
			//sHSV.y = 0.0;//value * 0.5;
			
		
		}
		
		
		half4 ToneMapping(VertexOutput i) : SV_Target {
			float4 color = Tex2Dlod(_MainTex, i.uv, 0);
			
			float3 rgb = color.rgb;
			
			Exposure(/*inout*/rgb, _Exposure);
			
			float3 aces = unity_to_ACES(rgb);
			float3 acescc = ACES_to_ACEScc(aces);
			
			ContrastSaturation(/*inout*/acescc, _Contrast , _Saturation);
			
			aces = ACEScc_to_ACES(acescc);
			float3 acescg = ACES_to_ACEScg(aces);
			float3 lms = ACEScgToLMS(acescg);
			
			ColorBalance(/*inout*/lms, _Temperature, _Tint);
			
			acescg = LMStoACEScg(lms);
			aces = ACEScg_to_ACES(acescg);
			rgb = ACES_to_unity(aces);
			
			rgb *= 0.5;
			rgb = CompressBy(rgb,     rgb * Compress(    rgb * Compress(    rgb)));
			rgb *= 2;
			
			rgb = saturate(rgb);
			float3 sRGB = LinearToGammaSpace(rgb);
			
			BlackWhitePoint(/*inout*/sRGB, _BlackPoint, _WhitePoint);
			
			sRGB = saturate(sRGB);
			
			float3 sHSV = RgbToHsv(sRGB);
			
			Curves(/*inout*/sHSV, _CurveParams);
			
			sRGB = HsvToRgb(sHSV);
			rgb = GammaToLinearSpace(sRGB);
			
			color.rgb = saturate(rgb);
			return color;
			
		}
		
		half4 ToneMappingX(VertexOutput i) : SV_Target {
			float3 color = Tex2Dlod(_MainTex, i.uv, 0);
			float disneyLum = DisneyLuminance(color);
			float AcesLum = dot(color, half3(0.2126, 0.7152, 0.0722));
			
			const float RESULT_COUNT = 5;
			half3 results[RESULT_COUNT] = {
				CompressBy(disneyLum.xxx, disneyLum * Compress(disneyLum * Compress(disneyLum))),
				CompressBy(    color.rgb, disneyLum * Compress(disneyLum * Compress(disneyLum))),
				CompressBy(    color.rgb,     color * Compress(    color * Compress(    color))),
				CompressBy(    color.rgb,     color * Compress(    color)),
				saturate(color.rgb),
				
			};
			
			int selector = min(RESULT_COUNT-1, floor(_Strength * RESULT_COUNT));
			float3 rgb = results[selector];
			
			//rgb = HsvToRgb(float3(RgbToHsv(rgb).xy, 0.443*0.2));
			//rgb = 0.443*0.2;
			
			float3 xyz = ACEScgToXYZ(rgb);
			float3 xyzN = xyz / 3.0;
			float3 lms = ACEScgToLMS(rgb);
			float3 lmsN = lms / 3.0;
			half3 aces = unity_to_ACES(rgb);
			// ACEScc (log) space
			half3 acescc = ACES_to_ACEScc(aces);
			
			float3 hsv = RgbToHsv(acescc);
			
			//hsv.y = _Saturation < 0 ? (hsv.y * (1+_Saturation)) : saturate(hsv.y / max(1-_Saturation, EPSILON));
			hsv.y *= _Saturation+1;
			
			acescc = HsvToRgb(hsv);
			
			acescc = (acescc - ACEScc_MIDGRAY) * (_Contrast+1) + ACEScc_MIDGRAY;
			
			aces = ACEScc_to_ACES(acescc);
			
			rgb = ACES_to_unity(aces);//XYZtoACEScg(lms1);
			
			rgb *= (CalculateColorBalance(_Temperature, _Tint));
			
			return float4(saturate(rgb), 1);
			
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
			#pragma fragment ToneMapping
			ENDCG
		}
	}
	Fallback "Diffuse"
}
