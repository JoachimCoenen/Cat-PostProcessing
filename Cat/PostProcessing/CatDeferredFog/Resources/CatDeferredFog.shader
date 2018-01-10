// Note to self: here's no MIT license, since here's no magic happening at all...

Shader "Hidden/Cat Deferred Fog" {
	Properties {
	}
	CGINCLUDE
		#include "UnityCG.cginc"
		#include "../../Includes/PostProcessingCommon.cginc"
		
        #pragma multi_compile FOG_LINEAR FOG_EXP FOG_EXP_SQR
		
		float3	_FogColor;
		float3	_FogParams;
		
		float4 fragFog(VertexOutput i) : SV_Target {
			float depth = sampleEyeDepth(_DepthTexture, i.uv);
			half density = getFogDensity(depth, _FogParams);
			
			float4 color = Tex2Dlod(_MainTex, i.uv, 0);
			color.rgb = lerp(color.rgb, _FogColor.rgb, density);
			return color;
		}
	ENDCG 
	
	Subshader {
		ZTest Always Cull Off ZWrite Off
		Pass {
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment fragFog
			ENDCG
		}
	}
	Fallback Off
}
