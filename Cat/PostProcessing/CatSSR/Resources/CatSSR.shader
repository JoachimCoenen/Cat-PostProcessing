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
	}
	
	CGINCLUDE
		#define MIN_SMOOTHNESS 0.0 					// [0...1]       default = 0
		#define CULL_RAY_HITS_ON_BACK_SIDE false	// [false, true] default = false
		#define USE_BINARY_RAYTRACER 00				// [0, 1]        default = 0
		#define REFLECT_SKYBOX 00					// [0, 1]        default = 0
		
		#include "UnityCG.cginc"
		#include "UnityStandardUtils.cginc"
		#include "UnityGlobalIllumination.cginc"
		
		#include "../../Includes/PostProcessingCommon.cginc"
		
		#include "CatRayTraceLib.cginc"

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
			#pragma vertex vertVSRaytrace
			#pragma fragment fragRayTrace
			#include "CatSSR.cginc"
			ENDCG
		}
		
		//Pass 1 resolveAdvanced
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
			#pragma multi_compile __ FOG_LINEAR FOG_EXP FOG_EXP_SQR
			
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma vertex vertVS
			#pragma fragment fragResolveAdvanced
			#include "CatSSR.cginc"
			ENDCG
		}
		
		//Pass 2 combineTemporal
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
			#pragma fragment fragCombineTemporal
			#include "CatSSR.cginc"
			ENDCG
		}
		
		//Pass 3 Median
		Pass {
			CGPROGRAM
			#pragma target 3.0
			
			#pragma vertex vert
			#pragma fragment fragMedian
			#include "CatSSR.cginc"
			ENDCG
		}
		
		//Pass 4 MipMapBlur
		Pass {
			CGPROGRAM
			#pragma target 3.0
			
			#pragma vertex vert
			#pragma fragment fragCameraBlur
			#include "CatSSR.cginc"
			ENDCG
		}
		
		//Pass 5 compose and apply reflections
		Pass {
			Blend SrcAlpha One, One Zero
		//	Blend One One, One Zero
			Stencil {
				ref [_StencilNonBackground]
				readmask [_StencilNonBackground]
				// Normally just comp would be sufficient, but there's a bug and only front face stencil state is set (case 583207)
				compback equal
				compfront equal
			}
			
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vertFull
			#pragma fragment fragComposeAndApplyReflections
			#include "CatSSR.cginc"
			ENDCG
		}
		
		//Pass 6 debug
		Pass {
			CGPROGRAM
			#pragma target 3.0
			
			#pragma vertex vertFull
			#pragma fragment fragDebug
			#include "CatSSR.cginc"
			ENDCG
		}
	}
	Fallback Off
}
