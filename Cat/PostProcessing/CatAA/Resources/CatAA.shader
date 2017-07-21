Shader "Hidden/CatAA" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "black" {}
		_Intensity("Intensity", FLOAT) = 1
		_ObjectThickness("Object Thickness", FLOAT) = 00.5
		_isBackFaceCullingEnabled("is BackFace Culling Enabled", FLOAT) = 1
		_NoiseStrength("Noise Strength", FLOAT) = 0.5
		_MaxReflectionDistance("Max Reflection Distance", FLOAT) = 100
	}
	
	CGINCLUDE
	
    #define VELOCITY_WEIGHT_SCALE _VelocityWeightScale
    
	#include "UnityCG.cginc"
	//#include "UnityPBSLighting.cginc"
	//#include "UnityStandardBRDF.cginc"
	#include "UnityStandardUtils.cginc"
	#include "../../Includes/PostProcessingCommon.cginc"
//	#include "../inc/CatAssetsShaderLighting.cginc"

	
	sampler2D _HistoryTex1; float4 _HistoryTex1_TexelSize;
	sampler2D _HistoryTex2;
	sampler2D _HistoryTex3;

	float _Sharpness;
    float _VelocityWeightScale;
	float _Response;
	float _ToleranceMargin;

//	bool _EnableVelocityPrediction;
 //   float2 _TAAJitterVelocity;
	float2 _Directionality;

	static const float2x2 vibrationCols = float2x2( float2( 1.00, 1.00), float2( 1.00,-1.00) ); //
	static const float2x2 vibrationRows = float2x2( float2( 1.00, 1.00), float2(-1.00, 1.00) ); //
	static const float2x2 vibrationFrms = float2x2( float2( 0.50, 0.25), float2(-0.25, 0.50) ); // frames A & frame B

	static const float2x2 vibration2Cols = float2x2( float2(-0.50, 1.00), float2(-1.00,-0.50) ); //
	static const float2x2 vibration2Frms = float2x2( float2( 1.00, 1.00), float2(-1.00,-1.00) ); // frames A & frame B
	
	static const float2x2 devibrationCols = float2x2( float2(-1.00,-1.00), float2(-1.00, 1.00) ); //
	static const float2x2 devibrationRows = float2x2( float2( 1.00, 1.00), float2(-1.00, 1.00) ); //
	static const float2x2 devibrationFrms = float2x2( float2( 1.00, 0.00), float2( 0.00, 1.00) ); // frames A & frame B
	
	static const float2x2 vibrRws[2] = {
		float2x2( float2(+0.50, +0.25), float2(-0.50, +0.25) ), // frame A
		float2x2( float2(-0.25, +0.50), float2(+0.25, +0.50) )  // frame B
	};
	
	
	float2 Vibrate(float2 uv, int frameCounter, float4 texelSize, half vibrationStrength, bool pixelSnap) {
		// snap to pixel center:
		float2 uvSnap = SnapToPixel(uv, texelSize);
		
		float2 pos = uv * texelSize.zw;
		float2 posSnap = float2(uvSnap * texelSize.zw);
		float2 uvOffset = texelSize.xy;
//		uvOffset *= frac(posSnap.x * 0.5) < 0.5 ? vibrationCols[0] : vibrationCols[1];
//		uvOffset *= frac(posSnap.y * 0.5) < 0.5 ? vibrationRows[0] : vibrationRows[1];
		
		uvOffset *= 0.3333333*(frac((posSnap.x+posSnap.y) * 0.5) < 0.5 ? vibration2Cols[0] : vibration2Cols[1]);
		uvOffset *= frameCounter % 2     == 0.0 ? vibration2Frms[0] : vibration2Frms[1];
		//float2 uvOffset = vibrationCols[posSnap.x % 2] * vibrationRows[frameCounter % 2][posSnap.y % 2] * texelSize.xy;
		//float2 posOffset = float2(int(posSnap.y) % 2, int(posSnap.x) % 2) * float2(1, 1);
		
		return (pixelSnap ? uvSnap : uv) + vibrationStrength * uvOffset;
	}
	
	float2 DeVibrate(float2 uv, int frameCounter, float4 texelSize, half vibrationStrength, bool pixelSnap) {
		float2x2 rows[2] = {
			float2x2( float2(-1.0, 0.0), float2(+1.0, 0.0) ), // frame A
			float2x2( float2( 0.0,-1.0), float2( 0.0,-1.0) )  // frame B
		};
		// snap to pixel center:
		float2 uvSnap = SnapToPixel(uv, texelSize);
		
		float2 pos = uv * texelSize.zw;
		float2 posSnap = uvSnap * texelSize.zw;
		float2 uvOffset = texelSize.xy;
		uvOffset *= frac(posSnap.x * 0.5) < 0.5 ? devibrationCols[0] : devibrationCols[1];
		uvOffset *= frac(posSnap.y * 0.5) < 0.5 ? devibrationRows[0] : devibrationRows[1];
		uvOffset *= frameCounter % 2     == 0.0 ? devibrationFrms[0] : devibrationFrms[1];
		//float2 uvOffset = vibrationCols[posSnap.x % 2] * rows[frameCounter % 2][posSnap.y % 2] * texelSize.xy;
		
		return (pixelSnap ? uvSnap : uv) + vibrationStrength * uvOffset;
	}

	float2 GetClosestFragment(float2 uv) {
		const float2 k = _CameraDepthTexture_TexelSize.xy;
		const float4 neighborhood = float4(
			SAMPLE_DEPTH_TEXTURE(_DepthTexture, uv - k),
			SAMPLE_DEPTH_TEXTURE(_DepthTexture, uv + float2(k.x, -k.y)),
			SAMPLE_DEPTH_TEXTURE(_DepthTexture, uv + float2(-k.x, k.y)),
			SAMPLE_DEPTH_TEXTURE(_DepthTexture, uv + k)
			);
		
		#if defined(UNITY_REVERSED_Z)
			#define COMPARE_DEPTH(a, b) step(b, a)
		#else
			#define COMPARE_DEPTH(a, b) step(a, b)
		#endif
		
		float3 result = float3(0.0, 0.0, SAMPLE_DEPTH_TEXTURE(_DepthTexture, uv));
		result = lerp(result, float3(-1.0, -1.0, neighborhood.x), COMPARE_DEPTH(neighborhood.x, result.z));
		result = lerp(result, float3( 1.0, -1.0, neighborhood.y), COMPARE_DEPTH(neighborhood.y, result.z));
		result = lerp(result, float3(-1.0,  1.0, neighborhood.z), COMPARE_DEPTH(neighborhood.z, result.z));
		result = lerp(result, float3( 1.0,  1.0, neighborhood.w), COMPARE_DEPTH(neighborhood.w, result.z));
		
		return (uv + result.xy * k);
}
	
	#define TAA_DILATE_MOTION_VECTOR_SAMPLE 0
	
	struct FragOutput {
		half4 color   : SV_Target0;
		half4 history : SV_Target1;
	};
	
	float4 combine(VertexOutput i ) : SV_Target {
		const float4 texelSize = _MainTex_TexelSize;
		float2 uv = i.uv;
		
		#if TAA_DILATE_MOTION_VECTOR_SAMPLE
			float2 velocity = GetVelocity(GetClosestFragment(uv));
		#else
			// Don't dilate in ortho !
			float2 velocity = GetVelocity(uv).xy;
		#endif
		float2 uvPrev = uv - velocity.xy;
		float confidence = all(uvPrev == saturate(uvPrev));
        
		float4 history = tex2D(_HistoryTex1, uvPrev);
		confidence *= any(history);
		float4 mainTex = tex2D(_MainTex, uv);
		float2 tx = texelSize.xy * _Directionality;
		float4 corner1 = tex2D(_MainTex, uv - tx * 0.52);
		float4 corner2 = tex2D(_MainTex, uv + tx * 0.52);
		float4 corners = (corner1 + corner2);
		
		if (0) {
			return lerp(mainTex, history, 0.9);
		}
		
		float4 cornerR = 4.0 * (corners) - 2.0 * mainTex;
		
		// Sharpen output
		mainTex += (mainTex - (cornerR * 0.166667)) * 2.718282*0.25 * _Sharpness;
		mainTex = max(0.0, mainTex);
		
		history.rgb /= 1 + DisneyLuminance(history.rgb);
		mainTex.rgb /= 1 + DisneyLuminance(mainTex.rgb);
		corner1.rgb /= 1 + DisneyLuminance(corner1.rgb);
		corner2.rgb /= 1 + DisneyLuminance(corner2.rgb);
		corners.rgb = corners.rgb / (2 + DisneyLuminance(corners.rgb));
		
		mainTex.a = 10*length(velocity);
		
		float delta = abs(mainTex.a - history.a);
		//delta = step(0.01, delta));
		float weight = saturate(1 - delta * VELOCITY_WEIGHT_SCALE);//saturate(1.0 - delta * VELOCITY_WEIGHT_SCALE * 2);
		
		half3 maxCorners = max(corner1.rgb, corner2.rgb);
		half3 minCorners = min(corner1.rgb, corner2.rgb);
		half3 maxOverAll = max(maxCorners,  mainTex.rgb);
		half3 minOverAll = min(minCorners,  mainTex.rgb);
		half3 diff = maxOverAll - minOverAll;
		
	//	float3 nudge = lerp(6.28318530718, 0.5, saturate(2.0 * history.a*0.01)) * abs(corner1.rgb - corner2.rgb);
		
		float historya = saturate(smoothstep(0.002 * _MainTex_TexelSize.z, 0.0035 * _MainTex_TexelSize.z, history.a / 10.0));
		
		float3 nudge = lerp(6.28318530718, 0.5, saturate(2 * historya)) * (diff + 0.0001);
		history.rgb = clamp(history.rgb, minCorners - nudge, maxCorners + nudge);
		
		float2 minHistoryStrength = {0.250125, 0.25};
		float2 maxHistoryStrength = {0.95, 0.6666667};
		
		half2 selector = lerp(minHistoryStrength, maxHistoryStrength, weight*confidence);
		//half speedSelector = 0.95*lerp(weight*confidence, 1, minHistoryStrength.y);
		
		float4 result = lerp(mainTex, history, selector.xxxy);
		result.a *= 0.850;
	//	result = mainTex;
		
		result.rgb /= 1 - DisneyLuminance(result.rgb);

		return result	;
	}
	
		#define TAA_DILATE_MOTION_VECTOR_SAMPLE_2 1
	float4 combineTemporal(CAT_ARGS_TEX_INFO(currentTex), CAT_ARGS_TEX_INFO(historyTex), float2 uv) {
		#if TAA_DILATE_MOTION_VECTOR_SAMPLE_2
			float2 velocity = GetVelocity(GetClosestFragment(uv));
		#else
			// Don't dilate in ortho !
			float2 velocity = GetVelocity(uv).xy;
		#endif
		
		if (0) {
			return float4(pow(abs(float3(abs(velocity.xy)>0.001, 0)), 2.2), 1);
		}
		
		
		float2 uvPrev = uv - velocity.xy;
		float confidence = all(uvPrev == saturate(uvPrev));
		
		float2 tx = currentTex_TexelSize.xy;
		float2 ty = currentTex_TexelSize.xy * float2(-1, 1);
		float4 history = tex2D(historyTex, uvPrev);
		confidence *= any(history);
		float4 mainTex = tex2D(currentTex, uv);
		float4 corner1 = tex2D(currentTex, uv + tx*float2( 0.5, -1.0));
		float4 corner2 = tex2D(currentTex, uv + tx*float2( 1.0,  0.5));
		float4 corner3 = tex2D(currentTex, uv + tx*float2(-0.5,  1.0));
		float4 corner4 = tex2D(currentTex, uv + tx*float2(-1.0, -0.5));
		
		//corner1 = tex2D(currentTex, uv - tx);
		//corner2 = tex2D(currentTex, uv + tx);
		//corner3 = tex2D(currentTex, uv - ty);
		//corner4 = tex2D(currentTex, uv + ty);
		
		mainTex.a = 10*length(velocity);
		
		float3 corners = (corner1 + corner2);
		// Sharpen output
		float3 cornerR = 4.0 * corners - 2.0 * mainTex;
		mainTex.rgb =  max(0.0, mainTex.rgb + (mainTex.rgb - (cornerR * 0.166667)) * 2.718282*0.25 * _Sharpness);
		
		history.rgb /= 1 + DisneyLuminance(history.rgb);
		mainTex.rgb /= 1 + DisneyLuminance(mainTex.rgb);
		corner1.rgb /= 1 + DisneyLuminance(corner1.rgb);
		corner2.rgb /= 1 + DisneyLuminance(corner2.rgb);
		corner3.rgb /= 1 + DisneyLuminance(corner3.rgb);
		corner4.rgb /= 1 + DisneyLuminance(corner4.rgb);
		
		half3 maxCorners = max(corner1, max(corner2, max(corner3, corner4))).rgb;
		half3 minCorners = min(corner1, min(corner2, min(corner3, corner4))).rgb;
		half3 maxOverAll = max(maxCorners,  mainTex).rgb;
		half3 minOverAll = min(minCorners,  mainTex).rgb;
		
		float delta = abs(mainTex.a - history.a);
		float nudge = saturate(1 - VELOCITY_WEIGHT_SCALE * delta);
		
		float3 lerpUp = (maxOverAll - minOverAll + 0.0) * nudge * _ToleranceMargin;
		history.rgb = clamp(history, minOverAll - lerpUp, maxOverAll + lerpUp);
		
		//float weight = (1-lerp(0.25, _Response, nudge)) * confidence;
		//float4 result = lerp(mainTex, history, weight);
		float weight = (0.75 - nudge * (_Response - 0.25)) * confidence;
		float4 result = weight * (history - mainTex) + mainTex;
		result.a = lerp(mainTex.a, history.a, 0.5);
		
		result.rgb /= 1 - DisneyLuminance(result.rgb);
		return result;
	}
	
	float4 fragCombineTemporal(VertexOutput i) : SV_Target {
		return combineTemporal(CAT_PASS_TEX_INFO(_MainTex), CAT_PASS_TEX_INFO(_HistoryTex1), i.uv);
	}
		
	
	float4 removeSpeed(VertexOutput i ) : SV_Target {
		return float4(tex2D(_MainTex, i.uv).rgb, 0);
	}
	

	ENDCG 
	
	Subshader {
		ZTest Always Cull Off ZWrite Off
		Fog { Mode off }
		
		CGINCLUDE
		ENDCG

		// 0 combine
		Pass {
		//	Blend One Zero, One Zero //SrcAlpha OneMinusSrcAlpha
			CGPROGRAM
			#pragma target 3.0
			
			
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma vertex vert
			#pragma fragment fragCombineTemporal
			ENDCG
		}
        
		// 1 removeSpeed
		Pass {
		//	Blend One Zero, One Zero //SrcAlpha OneMinusSrcAlpha
			CGPROGRAM
			#pragma target 3.0
			
			
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma vertex vert
			#pragma fragment removeSpeed
			ENDCG
		}
        
		
		
		
	}
	Fallback Off
}
