//The MIT License(MIT)

//Copyright(c) 2016 Charles Greivelding Thomas

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


#ifndef STOCHASTIC_SSR_RAY_TRACE_LIB_INCLUDED
#define STOCHASTIC_SSR_RAY_TRACE_LIB_INCLUDED

/*float4 RayMarch(sampler2D tex, float4x4 _ProjectionMatrix, float3 viewDir, int NumSteps, float3 viewPos, float3 screenPos, float2 uv, float stepSize)
{
	float depth = LinearEyeDepth(UNITY_SAMPLE_DEPTH(tex2D(tex, uv)));

	float4 rayProj = mul (_ProjectionMatrix, float4(viewDir + viewPos, 1.0f));

	float3 rayDir = normalize( rayProj.xyz / rayProj.w - screenPos );
	rayDir.xy *= 0.5f;

	//float3 rayDir = float3(viewDir.xy - viewPos.xy / viewPos.z * viewDir.z, viewDir.z / viewPos.z) * _Project;

	float sampleMask = 0.0f;

	float3 rayStart = float3(uv, screenPos.z);

    float3 project = _Project;

	float3 samplePos = rayStart + rayDir * stepSize;

	float mask = 0;
	for (int i = 0;  i < NumSteps; i++)
	{
		float sampleDepth  = (UNITY_SAMPLE_DEPTH(tex2Dlod (tex, float4(samplePos.xy,0,0))));
				
		//float thickness = (project.z) / depth;
		float delta = (samplePos.z) - sampleDepth;

		if ( sampleDepth < (samplePos.z) )
		{  
			mask = 1; //TO FIX !!!
			break;
		}
		else
			samplePos += rayDir * stepSize;
		
	}
	return float4(samplePos, mask);
}*/

float4 RayMarch(sampler2D tex, float4x4 _ProjectionMatrix, float3 viewDir, int NumSteps, float3 viewPos, float3 screenPos, float2 uv, float stepSize)
{
	float depth = LinearEyeDepth(UNITY_SAMPLE_DEPTH(tex2D(tex, uv)));

	float4 rayProj = mul (_ProjectionMatrix, float4(viewDir + viewPos, 1.0f));

	float3 rayDir = normalize( rayProj.xyz / rayProj.w - screenPos );
	rayDir = normalize( ViewToScreenPos(viewDir + viewPos) - screenPos );
	rayDir.xy *= 0.5f;

	//float3 rayDir = float3(viewDir.xy - viewPos.xy / viewPos.z * viewDir.z, viewDir.z / viewPos.z) * _Project;

	float sampleMask = 0.0f;

	float3 rayStart = float3(uv, screenPos.z);

 //   float3 project = _Project;

	float3 samplePos = rayStart + rayDir * stepSize;

	float mask = 0;
	int i = 0;
	for (i = 0;  i < NumSteps; i++)
	{
		float sampleDepth  = LinearEyeDepth(UNITY_SAMPLE_DEPTH(tex2Dlod (tex, float4(samplePos.xy,0,0))));
				
		float thickness = 0.1; //LinearEyeDepth(project.z) / depth;
		float delta = LinearEyeDepth(samplePos.z) - sampleDepth;

		if ( sampleDepth < LinearEyeDepth(samplePos.z) )
		//if ( 0.0 < delta && delta < thickness )  
		{  
				
			//float thickness = LinearEyeDepth(project.z) / depth;
			//float delta = LinearEyeDepth(samplePos.z) - sampleDepth;
			//if (abs(sampleDepth - LinearEyeDepth(samplePos.z) ) < 0.3)
			if (0.0 < delta && delta < thickness)
			{
				mask = 1;
				break;
			}
			else
			{
				rayDir *= 0.5;
				samplePos = rayStart + rayDir * stepSize; 
			} 
		}
		else
		{
		        rayStart = samplePos;
		        samplePos += rayDir * stepSize;
		}
	}
	mask = pow(i / (float)NumSteps, 2.2);
	return float4(samplePos, mask);
}

//================================================================================================================
	
	#include "../../../Includes/NoiseLib.cginc"
	#define PI UNITY_PI
	#define sqr Pow2
	#include "../../../../Extensions/StochasticSSR/Shaders/Resources/BRDFLib.cginc"
	static const float _SmoothnessRange = 1;
	static const float _UseTemporal = 1;
	
	float3 GetViewNormal (float3 normal)
	{
		float3 viewNormal = WorldToViewDir(normal.rgb);
		return normalize(viewNormal);
	}
	float3 GetViewPos (float3 screenPos)
	{
		return ScreenToViewPos(screenPos);
		float4 viewPos = mul(unity_CameraInvProjection, float4(screenPos, 1));
		return viewPos.xyz / viewPos.w;
	}

	void rayCast ( VertexOutput i, 	out half4 outRayCast : SV_Target0/*, out half4 outRayCastMask : SV_Target1*/) 
	{	
		float2 uv = i.uv;
		int2 pos = uv /* _RayCastSize.xy*/;

		float4 worldNormal = tex2D(_CameraGBufferTexture2, uv) * 2 - 1;
		float3 viewNormal = GetViewNormal (worldNormal);
		float4 specular = tex2D(_CameraGBufferTexture1, uv);
		float roughness = max(min(_SmoothnessRange, 1 - specular.a), 0.05f);

		float depth = UNITY_SAMPLE_DEPTH (tex2Dlod(_CameraDepthTexture, float4(uv, 0, 0)));
		float3 screenPos = GetScreenPos(uv, depth);
		float3 viewPos = GetViewPos(screenPos);

		float2 random = RandN2(pos, _FrameCounter * _UseTemporal);

		float2 jitter = tex2Dlod(_BlueNoise, float4((uv + random) *  _HitTex_TexelSize.zw * _BlueNoise_TexelSize.xy, 0, -255)); // Blue noise generated by https://github.com/bartwronski/BlueNoiseGenerator/

		float2 Xi = jitter;

		Xi.y = lerp(Xi.y, 0.0, 1-_ImportanceSampleBias);

		float4 H = TangentToWorld(viewNormal, ImportanceSampleGGX(Xi, roughness));
		float3 dir = reflect(normalize(viewPos), H.xyz);

		jitter += 0.5f;
		jitter = 0;
		
		float stepSize = (1.0 / (float)_StepCount);
		stepSize = stepSize * (jitter.x + jitter.y) + stepSize;

		float2 rayTraceHit = 0.0;
		float rayTraceZ = 0.0;
		float rayPDF = 0.0;
		float rayMask = 0.0;
		float4 rayTrace = RayMarch(_CameraDepthTexture, UNITY_MATRIX_P, dir, _StepCount, viewPos, screenPos, uv, stepSize);

		rayTraceHit = rayTrace.xy;
		rayTraceZ = rayTrace.z;
		rayPDF = H.w;
		rayMask = rayTrace.w;

		// outRayCast = float4(float3(rayTraceHit, rayTraceZ), rayPDF);
		// outRayCastMask = rayMask;
		outRayCast = float4(float3(rayTraceHit, rayMask), rayPDF);
	}


#endif // STOCHASTIC_SSR_RAY_TRACE_LIB_INCLUDED
