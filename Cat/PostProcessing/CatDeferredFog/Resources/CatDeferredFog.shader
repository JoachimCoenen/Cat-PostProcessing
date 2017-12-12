// Note to self: here's no MIT license, since here's no magic happening at all...

Shader "Hidden/Cat Deferred Fog" {
	Properties {
	}
	CGINCLUDE
		#include "UnityCG.cginc"
		#include "../../Includes/PostProcessingCommon.cginc"
		
        #pragma multi_compile FOG_LINEAR FOG_EXP FOG_EXP_SQR
		
		float	_Intensity;
		float3	_Color;
		float	_StartDistance;
		float	_EndDistance;
		
		float4 fragFog(VertexOutput i) : SV_Target {
			float depth = sampleEyeDepth(_DepthTexture, i.uv);
			
			#if FOG_LINEAR
				half density = InvLerpSat(_StartDistance, _EndDistance, depth);
			#elif FOG_EXP
				half density = 1 - exp2(-_Intensity * depth);
			#elif FOG_EXP_SQR
				half density = 1 - exp2(-Pow2(_Intensity * depth));
			#endif
			density = depth*1.0125 < _ProjectionParams.z ? density : 0; // if not isSkybox, apply fog
			
			float4 color = Tex2Dlod(_MainTex, i.uv, 0);
			color.rgb = lerp(color.rgb, _Color.rgb, density);
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
