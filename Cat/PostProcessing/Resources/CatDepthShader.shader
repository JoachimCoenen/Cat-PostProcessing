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

Shader "Hidden/Cat Depth Shader" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "black" {}
	}
	
	CGINCLUDE
		#include "UnityCG.cginc"
		#include "../../Includes/CatCommon.cginc"
		
		
		uniform UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture); 	uniform float4	_CameraDepthTexture_TexelSize;
		static const bool _IsDebugOn = false;
		
		
		struct VertexInput {
			float4 vertex : POSITION;
			float2 texcoord : TEXCOORD;
		};
		
		struct VertexOutput {
			float4 pos : POSITION;
			float2 uv : TEXCOORD0;
		};
		
		VertexOutput vert(VertexInput v) {
			VertexOutput o;
			o.pos = UnityObjectToClipPos(v.vertex);
			o.uv = v.texcoord.xy;
			
			return o;
		}

		
		half4 frag(VertexOutput i) : SV_Target {
			return float4(sampleDepthLod(_CameraDepthTexture, i.uv.xy, 0), 0, 0, 1);
	
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
			#pragma fragment frag
			ENDCG
		}
	}
	Fallback "Diffuse"
}
