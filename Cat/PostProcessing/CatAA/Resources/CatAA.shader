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

Shader "Hidden/CatAA" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "black" {}
		_Intensity("Intensity", FLOAT) = 1
		_ObjectThickness("Object Thickness", FLOAT) = 0.5
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
	#include "../../../Includes/CatCommon.cginc"
//	#include "../inc/CatAssetsShaderLighting.cginc"

	

	sampler2D _MainTex;			float4 _MainTex_TexelSize;
//	sampler2D __IDTemp_t__;	float4 __IDTemp_t___TexelSize;
//	uniform sampler2D _CameraGBufferTexture3;	uniform float4	_CameraGBufferTexture3_TexelSize;
//	#define _MainTex _CameraGBufferTexture3
//	#define _MainTex_TexelSize _CameraGBufferTexture3_TexelSize
	
	sampler2D _HistoryTex1;
	sampler2D _HistoryTex2;
	sampler2D _HistoryTex3;

    float _VelocityWeightScale;
	float _Sharpness;

	bool _EnableVelocityPrediction;
	float2 _AAOffset;
    float2 _JitterVelocity;
	float2 _Directionality;

	struct VertexInput {
		float4 vertex : POSITION;
		float2 texcoord : TEXCOORD;
	};

	struct VertexOutput {
		float4 pos : POSITION;
		float2 uv : TEXCOORD0;
	};

	VertexOutput vert( VertexInput v ) {
		VertexOutput o;
		o.pos = float4(v.vertex.xy, 0, 1);
		o.uv = v.texcoord.xy;
		#if UNITY_UV_STARTS_AT_TOP
//			if (_MainTex_TexelSize.y < 0) {
				o.uv.y = 1-o.uv.y;
//			}
		#endif
		return o;
	}

	VertexOutput vertProj( VertexInput v ) {
		VertexOutput o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = v.texcoord;

		#if UNITY_UV_STARTS_AT_TOP
			if (_MainTex_TexelSize.y < 0) {
				o.uv.y = 1.0 - v.texcoord.y;
			}
		#endif
		return o;
	}
	
	uniform sampler2D_half	_CameraMotionVectorsTexture;
	uniform sampler2D		_CameraDepthTexture;			uniform float4 _CameraDepthTexture_TexelSize;
	float4	GetVelocity(float2 uv) { return Tex2Dlod(_CameraMotionVectorsTexture, uv, 0); }
	float2 getClampedVelocity(float2 uv) {
		float4 rawVelocity = GetVelocity(uv);
		return !_EnableVelocityPrediction ? 0 : (_JitterVelocity*0.5 - clamp(rawVelocity.xy, -8, 8));
	}
	
	float2 snapToPixel(float2 uv, float4 texelSize) {
		float2 pos = uv * texelSize.zw;
		// snap to pixel center:
		return ( floor(pos - 0) + 0.5 ) * texelSize.xy;
	}
	
/*
	float2 getPrevUV(float2 uv, out float confidence) {
		float2 velocity1 = clamp(GetVelocity(uv).xy, -8, 8) * _EnableVelocityPrediction;
        
		confidence = (1-saturate(length(velocity1)*40));
        velocity1 *= 1-saturate(length(velocity1)*2);
		float2 prevUV = uv - velocity1.xy;
        
		float2 velocity2 = clamp(GetVelocity(prevUV).xy, -8, 8) * _EnableVelocityPrediction;
        
		confidence *= (1-saturate(length(velocity2)*30));
        velocity2 *= 1-saturate(length(velocity2)*2);
		prevUV = uv - min(abs(velocity1), abs(velocity2)) * sign(velocity1);
        
		confidence *= all(prevUV == saturate(prevUV));
		return prevUV;
	}
*/
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
		float2 uvSnap = snapToPixel(uv, texelSize);
		
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
		float2 uvSnap = snapToPixel(uv, texelSize);
		
		float2 pos = uv * texelSize.zw;
		float2 posSnap = uvSnap * texelSize.zw;
		float2 uvOffset = texelSize.xy;
		uvOffset *= frac(posSnap.x * 0.5) < 0.5 ? devibrationCols[0] : devibrationCols[1];
		uvOffset *= frac(posSnap.y * 0.5) < 0.5 ? devibrationRows[0] : devibrationRows[1];
		uvOffset *= frameCounter % 2     == 0.0 ? devibrationFrms[0] : devibrationFrms[1];
		//float2 uvOffset = vibrationCols[posSnap.x % 2] * rows[frameCounter % 2][posSnap.y % 2] * texelSize.xy;
		
		return (pixelSnap ? uvSnap : uv) + vibrationStrength * uvOffset;
	}
	
	half4 combineOld( VertexOutput i ) : SV_Target {
		float2 uv = i.uv;
		float4 rawVelocity = GetVelocity(uv);
        float2 velocity = getClampedVelocity(uv);
		float2 uvPrev = snapToPixel(uv + velocity, _MainTex_TexelSize);
		float confidence = all(uvPrev == saturate(uvPrev));
        

		float4 history0 = tex2D(_MainTex, uv);
		float4 history1 = tex2D(_HistoryTex1, uvPrev);
		float4 history2 = tex2D(_HistoryTex2, uvPrev);
		float4 history3 = tex2D(_HistoryTex3, uvPrev);
		
		history0.a = 4 - (10 * length(velocity));
		history0.rgb /= 1 + DisneyLuminance(history0.rgb);
		history1.rgb /= 1 + DisneyLuminance(history1.rgb);
		history2.rgb /= 1 + DisneyLuminance(history2.rgb);
		history3.rgb /= 1 + DisneyLuminance(history3.rgb);
		
        float delta1 = abs(history1.a - history0.a);
        float delta2 = abs(history2.a - history0.a);
        float delta3 = abs(history3.a - history0.a);

        float weight0 = 1;
        float weight1 = (history1.a > 4 ? 0 : saturate(1.0 - (delta1) * VELOCITY_WEIGHT_SCALE*2));
        float weight2 = (history2.a > 4 ? 0 : saturate(1.0 - (delta2) * VELOCITY_WEIGHT_SCALE*2));
        float weight3 = (history3.a > 4 ? 0 : saturate(1.0 - (delta3) * VELOCITY_WEIGHT_SCALE*2));
		
		float weightSum = weight0 + (weight1 + weight2 + weight3) * confidence;
		float4 result = (history0 * weight0 + (history1 * weight1 + history2 * weight2 + history3 * weight3) * confidence) / weightSum;
		
		result.rgb /= 1 - DisneyLuminance(result.rgb);
	//	result.a = 2 - result.a;
		return result;
	}
	
	float2 GetClosestFragment(float2 uv) {
		const float2 k = _CameraDepthTexture_TexelSize.xy;
		const float4 neighborhood = float4(
			SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv - k),
			SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv + float2(k.x, -k.y)),
			SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv + float2(-k.x, k.y)),
			SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv + k)
			);
		
		#if defined(UNITY_REVERSED_Z)
			#define COMPARE_DEPTH(a, b) step(b, a)
		#else
			#define COMPARE_DEPTH(a, b) step(a, b)
		#endif
		
		float3 result = float3(0.0, 0.0, SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv));
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
	
	float4 combine( VertexOutput i ) : SV_Target {
		const float4 texelSize = _MainTex_TexelSize;
		float2 uv = i.uv;
		
		#if TAA_DILATE_MOTION_VECTOR_SAMPLE
			float4 rawVelocity = GetVelocity(GetClosestFragment(uv));
		#else
			// Don't dilate in ortho !
			float4 rawVelocity = GetVelocity(uv);
		#endif
		float2 velocity = !_EnableVelocityPrediction ? 0 : rawVelocity.xy - _JitterVelocity;
		float2 uvPrev = uv - velocity.xy;
		//uvPrev = snapToPixel(uvPrev, _MainTex_TexelSize);
		float confidence = all(uvPrev == saturate(uvPrev));
        

		float4 history = tex2D(_HistoryTex1, uvPrev);
		confidence *= any(history);
		float4 mainTex = tex2D(_MainTex, uv);
		float2 tx = texelSize.xy * _Directionality;
		float4 corner1 = tex2D(_MainTex, uv - tx * 0.5);
		float4 corner2 = tex2D(_MainTex, uv + tx * 0.5);
		float4 corners = (corner1 + corner2);
		
		if (false) {
			return tex2D(_MainTex, uv);//lerp(mainTex, history, _Sharpness);
		}
		
		float4 cornerR = 4.0 * (corners) - 2.0 * mainTex;
		
		// Sharpen output
		mainTex += (mainTex - (cornerR * 0.166667)) * 2.718282 * _Sharpness;
		mainTex = max(0.0, mainTex);
		
		
		history.rgb /= 1 + DisneyLuminance(history.rgb);
		mainTex.rgb /= 1 + DisneyLuminance(mainTex.rgb);
		corner1.rgb /= 1 + DisneyLuminance(corner1.rgb);
		corner2.rgb /= 1 + DisneyLuminance(corner2.rgb);
		corners.rgb = corners.rgb / (2 + DisneyLuminance(corners.rgb));
		
		mainTex.a = 10*length(velocity);
		
	//	float speed = length(velocity * normalize(texelSize.zw));
	//	float speed = sqrt((Pow2(v.x * t.z) + Pow2(v.y * t.w))*dotT )
		float dotT = 1 / dot(texelSize.zw, texelSize.zw);
		float2 velo = velocity.xy * texelSize.zw;
		float speed = sqrt(dot(velo, velo)*dotT);
		
	//	mainTex.a = 10*speed;
	//	mainTex.a = saturate(smoothstep(0.002 * _MainTex_TexelSize.z, 0.0035 * _MainTex_TexelSize.z, length(velocity)));
	//	mainTex.a = saturate(smoothstep(0.002 * texelSize.zw, 0.0035 * texelSize.zw, speed));
	//	mainTex.a = saturate(smoothstep(0.002 * texelSize.z, 0.0035 * texelSize.z, length(speed)));
		
	//	smoothstep (float edge0, float edge1, float x) {
	//		// Scale, bias and saturate x to 0..1 range
	//		x = saturate((x - edge0) / (edge1 – edge0)); 
	//		return x*x*(3-2*x);
	//	} 
		
		
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
		result.a *= 0.85;
	//	result = mainTex;
		
	//	weight = 
	//	clamp(
	//		lerp(
	//			0.95, 
	//			0.85,
	//			mainTex.a * 100 * 6
	//		), 
	//		0.85, 
	//		0.95
	//	);
	//	result = lerp(mainTex, history, weight);
	//	result.a *= 0.85;
		
		result.rgb /= 1 - DisneyLuminance(result.rgb);

		return result;
	}
	
	half4 resolve( VertexOutput i ) : SV_Target {
		float2 uv = i.uv;
		float4 rawVelocity = GetVelocity(uv);
        float2 velocity = getClampedVelocity(uv);
		float2 uvPrev = uv + velocity;
		//uvPrev = snapToPixel(uv + velocity, _MainTex_TexelSize);
        
        float4 color = tex2D(_MainTex, uvPrev);
		
		color.a = 4 - color.a;
		
        color.a = max(color.a, (10 * length(velocity)));
        color.a += 4 - 4 * all(uvPrev == saturate(uvPrev));
        
		color.a = 4 - color.a;
		return color;
	}
	
	half4 resolveTwo( VertexOutput i ) : SV_Target {
        float2 uv = i.uv;
		float4 rawVelocity = GetVelocity(uv);
        float2 velocity = getClampedVelocity(uv);
		
		float4 color = tex2D(_MainTex, uv);
		color.a = (10 * length(velocity));
		
		color.a = 4 - color.a;
		return color;
	}

//	half4 debug( VertexOutput i ) : SV_Target {
//		float2 uv = i.uv;
//		float4 posit = i.posit;
//		float3 history = half3(posit.x, posit.y, posit.z) / posit.w * 0.5 + 0.5;
//		history.z = uv.y;
//			   //history = half4(0.75, 0.25, 0.5);
//		return float4(history, 1);
//	}


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
			#pragma fragment combine
			ENDCG
		}
        
		// 1 resolve
		Pass {
			CGPROGRAM
			#pragma target 3.0

			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma vertex vert
			#pragma fragment resolve
			ENDCG
		}
        
		// 2 resolve, too
		Pass {
			CGPROGRAM
			#pragma target 3.0

			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma vertex vert
			#pragma fragment resolveTwo
			ENDCG
		}

		// 3 combine proj
		Pass {
		ZTest Always Cull Off ZWrite Off
			CGPROGRAM
			#pragma target 3.0

			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma vertex vertProj
			#pragma fragment combine
			ENDCG
		}
		
		// 4 simple proj
		Pass {
		ZTest Always Cull Off ZWrite Off
			CGPROGRAM
			#pragma target 3.0

			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma vertex vert
			#pragma fragment simple
			
			FragOutput simple( VertexOutput i ) {
				float2 uv = i.uv;
			FragOutput result;
		//	result.color = float4(uv, 0, 1);
			result.color = tex2D(_MainTex, uv);
			result.history = result.color;
			return result;
			}
			
			ENDCG
		}
		
		
		
		
	}
	Fallback Off
}
