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

#if !defined(CAT_RAYTRACE_LIB_INCLUDED)
#define CAT_RAYTRACE_LIB_INCLUDED

#include "../../../Includes/CatCommon.cginc"

// returns sMaxOld
float sortDepths(inout float sMin, inout float sMax) {
	float sMaxOld = sMax;
	#if UNITY_REVERSED_Z
		if (sMax > sMin)
	#else
		if (sMax < sMin)
	#endif
	{
	//	sMax = sMin;
	//	sMin = sMaxOld;
	}
	return sMaxOld;
}

bool isHittingObject(float sampleDepth, float sMin, float sMax, float invTz, float wz) {
	bool hasHitObject = false;
	#if UNITY_REVERSED_Z
		hasHitObject = (sampleDepth - sMin) * invTz <= (sampleDepth + wz) * (sMin + wz);
		hasHitObject = hasHitObject && (sMax <= sampleDepth);
		
	//	sMax = LinearEyeDepth(sMax);
	//	sMin = LinearEyeDepth(sMin);
	//	float eyeSampleDepth = LinearEyeDepth(sampleDepth);
	//	hasHitObject = (sMax >= eyeSampleDepth) && sMin <= eyeSampleDepth+0.5;
	#else
		hasHitObject = (sampleDepth - sMin) * invTz >= (sampleDepth + wz) * (sMin + wz);
		hasHitObject = hasHitObject && (sMax >= sampleDepth);
	#endif
	return hasHitObject;
}

bool shouldKeepRunning(float sampleDepth, float sMin, float sMax, float invTz, float wz) {
	bool hasHitObject = false;
		float3 right  = float3(sampleDepth, sampleDepth, sMin);
		float3 left = float3(-sMin, wz, wz);
		float4 val = float4(left + right, invTz);
		float4 val2 =  float4(val.xy * val.wz, sMax, sampleDepth);
	#if UNITY_REVERSED_Z
		hasHitObject = any(val2.xz > val2.yw);
	#else
		hasHitObject = any(val2.xz < val2.yw);
	#endif
	return hasHitObject;
}

void doBinaryStep(float diff, float3 rayStep, inout float3 samplePos) {
	#if UNITY_REVERSED_Z
		samplePos -= rayStep * sign(diff);// * -sign(rayStep.z); // do step
	#else
		samplePos += rayStep * sign(diff);// * -sign(rayStep.z); // do step
	#endif
}


float4 RayTrace(int stepCount, float objectThickness, float pixelStride, float3 ssStartPos, float3 ssRay, float4 zBufferParams, CAT_ARGS_DEPTHT_EXTURE(depthTexture), float4 texelSize, half jitter) {
	
		float invStepCount = 1 / float(stepCount);
		
		//float newLength = maxPixelStride;
		//float3 ray = ssRay;
		//ray.xy *= 0.5;
		//float rayLength = length(ray.xy * texelSize.zw);
		//float3 fullRayStep = ray / rayLength * newLength;
		
		
		float3 fullRayStep = ssRay.xyz;// / float(stepCount);
		fullRayStep.xy *= 0.5;
		float rayLengthInPixels = MaxC(abs(fullRayStep.xy * texelSize.zw)); // number of pixels, the ray covers.
		float pixelStrideTimesTwo = pixelStride*2; // number of pixels, on step should cover.
		fullRayStep = fullRayStep.xyz / rayLengthInPixels * pixelStrideTimesTwo;
		
		
		//fullRayStep = fullRayStep + fullRayStep * jitter;
		float3 rayStart = float3(ssStartPos.xy*0.5+0.5, ssStartPos.z);

		int clampedStepCount = stepCount;

	
	float3 samplePos = rayStart;
	samplePos.z += fullRayStep.z * 0.5;
	float sMin = saturate(samplePos.z); // this is (samplePos + localRayStep) - 0.5 * localRayStep;
	
	const float invTz = 1 / (zBufferParams.z * objectThickness);
	const float wz = 0;// zBufferParams.w / zBufferParams.z;
	const float ssLayerThicknessSmall = 6;
	
	//CHANGE  float asSmall = ssLayerThicknessSmall * max(abs(normRayStep.z), 0.0000152587890625*3);//0.000244140625);
	
	bool hasConqueredObject = false;
	bool keepRunning = true;
	bool wasBehindObject = false;
	bool hasBeenBehindObject = false;
	int i = 0;
	
#if !USE_BINARY_RAYTRACER
		samplePos += fullRayStep * jitter;
		for (; keepRunning && i < clampedStepCount; i++) {
			samplePos += fullRayStep; // do step
			float sMax = samplePos.z;
			float sampleDepth = sampleDepthLod(depthTexture, samplePos.xy, 0);
			float4 Comparer = float4((sampleDepth - sMin) * invTz, sMax, (sampleDepth + wz) * (sMin + wz), sampleDepth);
			#if UNITY_REVERSED_Z
				keepRunning = any(Comparer.xy > Comparer.zw);
			#else
				keepRunning = any(Comparer.xy < Comparer.zw);
			#endif
			sMin = sMax;
		}
		
		samplePos.z -= fullRayStep.z * 0.5;
		bool hasHitObject = !keepRunning;
	#else
		bool hasHitObject = false;
		samplePos += fullRayStep; // do step
		for (; i < clampedStepCount && all(saturate(samplePos.xy) == samplePos.xy); i++) {
			float sampleDepth = sampleDepthLod(depthTexture, samplePos.xy, 0);
		
			if (hasConqueredObject) {
				float diff = sampleDepth - saturate(samplePos.z);
				
				if (!all(saturate(samplePos.xy) == samplePos.xy)) {
			//		break;
				}
				
				if (asSmall > abs(diff)) {
					hasHitObject = true;
					break;
				}
				
				if (sMin < 1) {
					samplePos.z += fullRayStep.z * 3.5;
					samplePos.xy += fullRayStep.xy*3;
					sMin = samplePos;
					samplePos += fullRayStep;
					hasConqueredObject = false;
					continue;
				}
				sMin *= 0.5;		// Magic Number! theoretically this should be 2, but 1.5 seems to work best.
				samplePos -= normRayStep * sMin * sign(diff);
			} else {
				float sMax = saturate(samplePos.z);
				float sMaxOld = sortDepths(/*inout*/sMin, /*inout*/sMax);
				
				hasConqueredObject = isHittingObject(sampleDepth, sMin, sMax, invTz, wz);

				if (hasConqueredObject) {
					sMin = pixelStride;
					samplePos.z -= fullRayStep.z * 0.5;
					continue;
				}
				sMin = sMaxOld;
				samplePos += fullRayStep; // do step
			}
			
		}
	#endif
	
	
	//return float4(samplePos.xyz, k / 6.0);
	//return float4(samplePos.xyz, i / float(stepCount));
	//return float4(samplePos.xyz, float(i) / float(stepCount));
	return float4(samplePos.xyz, (hasHitObject) ? i: stepCount);
}

#endif // CAT_RAYTRACE_LIB_INCLUDED


	//	for (; keepRunning && i < clampedStepCount; i++) {
	//		samplePos += fullRayStep; // do step
	//		float sMax = (samplePos.z);
	//		float sampleDepth = sampleDepthLod(depthTexture, samplePos.xy, 0);
	//		
	//	//	float3 right = float3(sampleDepth, sampleDepth, sMin);
	//	//	float3 left  = float3(-sMin, wz, wz);
	//	//	float4 val  = float4(left + right, invTz);
	//	//	float4 val2 = float4(val.xy * val.wz, sMax, sampleDepth);
	//		#if UNITY_REVERSED_Z
	//	//		keepRunning = any(val2.xz > val2.yw);
	//			
	//			
	//				keepRunning = (sampleDepth - sMin) * invTz > (sampleDepth + wz) * (sMin + wz);
	//				keepRunning = keepRunning || (sMax > sampleDepth);
	//				
	//			//	float2 leftC  = { (sampleDepth - sMin) * invTz, sMax };
	//			//	float2 rightC = { (sampleDepth + wz) * (sMin + wz), sampleDepth };
	//			//	keepRunning = any(leftC > rightC);
	//			
	//		#else
	//			keepRunning = any(val2.xz < val2.yw);
	//		#endif
	//		
	//		sMin = sMax;
	//	}


//	for (; !hasHitObject && i < clampedStepCount; i++) {
//		samplePos += rayStep; // do step
//		float sMax = (saturate(samplePos.z));
//		float sMaxOld = sortDepths(/*inout*/sMin, /*inout*/sMax);
//		
//		float sampleDepth = sampleDepthLod(depthTexture, samplePos.xy, 0);
//		
//		hasHitObject = isHittingObject(sampleDepth, sMin, sMax, invTz, wz);
//		sMin = sMaxOld;
//	}
//	samplePos.z -= rayStep.z * 0.5;
//	
//	if (hasHitObject) {
//		hasHitObject = false;
//		for (; k <= maxBinayStepCounter; k++) {
//			float sampleDepth = sampleDepthLod(depthTexture, samplePos.xy, 0);
//			float rayPosSat = saturate(samplePos.z);
//			if (
//				abs(sampleDepth - saturate(samplePos.z)) < asSmall /* (0.0 < diff && diff < 0.05) */) {
//				hasHitObject = true;
//				break;
//			}
//			
//			rayStep *= 0.5;
//			doBinaryStep(saturate(samplePos.z), sampleDepth, rayStep, /*inout*/samplePos);
//		}
//	}
//	




//	float4 RayTrace(int stepCount, float objectThickness, float minPixelStride, float maxPixelStride, float3 ssRay, float3 ssStartPos, float4 zBufferParams, CAT_ARGS_DEPTHT_EXTURE(depthTexture), float4 texelSize, half jitter) {
//		float invStepCount = 1.0 / float(stepCount);
//		float3 fullRayStep  = ssRay * invStepCount;
//		fullRayStep.xy *= 0.5;
//		
//		float2 rayStepPixels = fullRayStep.xy * texelSize.zw;
//		
//		float pixelStride = MaxC(abs(rayStepPixels)); // number of pixels, he step covers.
//		float3 normRayStep = fullRayStep / pixelStride; 				 // step now covers only one pixel.
//		pixelStride = clamp(pixelStride, minPixelStride, maxPixelStride); // clamp pixelsStride to min-max
//		fullRayStep = normRayStep * pixelStride;
//	//	fullRayStep *= (1 + jitter*0.5*0);
//		
//		float3 rayStart = float3(ssStartPos.xy*0.5+0.5, ssStartPos.z);
//		
//		//const float layerThickness = 0.0050003125;
//		const float layerThickness = 0.005;
//		const float ssLayerThickness = 5;
//		const float ssLayerThicknessSmall = 2;
//		float3 rayStep = fullRayStep;
//		
//		float3 samplePos = rayStart;
//		float sMin = LinearEyeDepth(samplePos.z); // this is (samplePos + localRayStep) - 0.5 * localRayStep;
//		samplePos.z += fullRayStep.z * 0.5;
//		
//		float wz = zBufferParams.w / zBufferParams.z;
//		float wzs = wz + (abs(normRayStep.z) - 0.00);
//		float asSmall = ssLayerThicknessSmall * max(abs(normRayStep.z), 0.0000152587890625*3);//0.000244140625);
//		float as      = ssLayerThickness      * max(abs(normRayStep.z), 0.0000152587890625*3);//0.000244140625);
//		float aswz = as * wz;
//		
//		bool hasConqueredObject = false;
//		bool hasHitObject = false;
//		int i = 0;
//		int binayStepCounter = 0;
//		const int maxBinayStepCounter = ceil(log2(pixelStride)) + 1;
//		for (; i < stepCount && all(samplePos.xyz == saturate(samplePos.xyz)); i++) {
//			if (!hasConqueredObject) {
//				samplePos += fullRayStep; // do step
//				float sMax = LinearEyeDepth(saturate(samplePos.z));
//				float sMaxOld = sMax;
//				if (sMax < sMin) {
//					sMax = sMin;
//					sMin = sMaxOld;
//				}
//				
//				float eyeSampleDepth = sampleEyeDepthLod(depthTexture, samplePos.xy, 0);
//				
//				if ((sMax >= eyeSampleDepth) && (sMin <= eyeSampleDepth + objectThickness)) {
//					hasConqueredObject = true;
//					samplePos.z -= fullRayStep.z * 0.5;
//					
//				//	hasHitObject = true;
//				//	break;
//				}
//				
//				sMin = sMaxOld;
//			} else {
//				float eyeRayDepth = LinearEyeDepth(saturate(samplePos.z));
//				float sampleDepth = sampleDepthLod(depthTexture, samplePos.xy, 0);
//				float eyeSampleDepth = LinearEyeDepth(sampleDepth);
//				float diff = eyeSampleDepth - eyeRayDepth;
//			//	hasHitObject = abs(diff) < layerThickness * eyeRayDepth;
//				float rayPosSat = saturate(samplePos.z);
//			//	hasHitObject = abs((sampleDepth - rayPosSat) * (wzs + rayPosSat)) < (aswz + as * sampleDepth);
//			//	hasHitObject = abs(1/(z * d + w) - 1/(z * p + w)) < LayerThickness * abs((1/(z * (p + s) + w) - 1/(z * p + w)))
//			//	hasHitObject = abs(1/(zBufferParams.z * sampleDepth + zBufferParams.w) - 1/(zBufferParams.z * samplePos.z + zBufferParams.w)) < layerThickness * abs((1/(zBufferParams.z * (samplePos.z + normRayStep.z) + zBufferParams.w) - 1/(zBufferParams.z * samplePos.z + zBufferParams.w)));
//				hasHitObject = hasHitObject | (abs(sampleDepth - rayPosSat) < as);
//			//	hasHitObject = abs(LinearEyeDepth(samplePos.z) - LinearEyeDepth(sampleDepth)) < ssLayerThickness*abs(LinearEyeDepth(samplePos.z) - LinearEyeDepth(samplePos.z + normRayStep.z));
//				
//				if (abs(sampleDepth - rayPosSat) < asSmall) {
//					break;
//				}
//				
//				binayStepCounter++;
//				if (binayStepCounter > maxBinayStepCounter) {
//					if (0 || hasHitObject) {
//				//		hasHitObject = false;
//						break;
//					}
//					binayStepCounter = 0;
//					hasConqueredObject = false;
//					rayStep = fullRayStep;
//					samplePos += fullRayStep;
//					sMin = LinearEyeDepth(saturate(samplePos.z));
//					samplePos.z += fullRayStep.z * 0.5;
//				//	hasHitObject = true;
//				//	break;
//					continue;
//				}
//				rayStep *= 0.5;
//				#if UNITY_REVERSED_Z
//				samplePos += rayStep * sign(rayPosSat - sampleDepth); // do step
//				#else
//				samplePos += rayStep * sign(sampleDepth - rayPosSat); // do step
//				#endif
//			}
//		}
//		
//		return float4(samplePos.xyz, (hasConqueredObject && all(samplePos.xyz == saturate(samplePos.xyz))) ? 0*i * pixelStride / maxPixelStride : stepCount);
//	}
