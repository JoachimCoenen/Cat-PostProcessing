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
		float3 _ColorBalance;
		
		float _BlackPoint;
		float _WhitePoint;
		
		float4 _CurveParams;
		
		float _Strength;
		
		float3 _HSV;
		
		
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
		
		void Curves(inout float3 sRGB, float blackPoint, float whitePoint, float4 curveParams) {
			//whitePoint = 1 + whitePoint * 0.25;
			//blackPoint = 0 + blackPoint * 0.25;
			
			sRGB = (sRGB - blackPoint) / (whitePoint - blackPoint);
			
			float value = MaxC(sRGB);
			sRGB *= 1.0 / max(value, EPSILON);
			
			//value = (value - blackPoint) / (whitePoint - blackPoint);
			value = saturate(value);
			value =  (curveParams.w + (curveParams.z + (curveParams.y + curveParams.x * value) * value) * value) * value;
			value = saturate(value);
			
			sRGB *= value;
		}
		
		half4 ToneMapping(VertexOutput i) : SV_Target {
			float4 color = Tex2Dlod(_MainTex, i.uv, 0);
			float3 rgb = color.rgb;
			
			Exposure(/*inout*/rgb, _Exposure);
			
			float3 aces = unity_to_ACES(rgb);
			float3 acescc = ACES_to_ACEScc_optimized(aces);
			
			ContrastSaturation(/*inout*/acescc, _Contrast , _Saturation);
			
			aces = ACEScc_to_ACES_optimized(acescc);
			float3 lms = ACEStoLMS(aces);
			
			ColorBalance(/*inout*/lms, _ColorBalance);
			
			rgb = LMStoUnity(lms);
			
			//rgb *= 0.5;
			//rgb = CompressBy(rgb,     rgb * Compress(    rgb * Compress(    rgb)));
			//rgb *= 2;
			
			rgb = saturate(rgb);
			float3 sRGB = LinearToGammaSpace(rgb);
			
			Curves(/*inout*/sRGB, _BlackPoint, _WhitePoint, _CurveParams);
			
			rgb = GammaToLinearSpace(sRGB);
			
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
			#pragma fragment ToneMapping
			ENDCG
		}
	}
	Fallback Off
	//"Diffuse"
}
