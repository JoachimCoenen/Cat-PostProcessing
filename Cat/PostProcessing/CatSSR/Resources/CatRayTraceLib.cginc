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

float4 RayTrace(
		int stepCount, float objectThickness, float minPixelStride, float maxPixelStride, float3 ssRay, float3 ssPos, 
		CAT_ARGS_DEPTHT_EXTURE(depthTexture), float4 texelSize, half jitter
) {
	float invStepCount = 1.0 / float(stepCount);
	float3 rayStep  = ssRay * invStepCount;
	rayStep.xy *= 0.5;

	float stepComp = MaxC(abs(rayStep.xy * texelSize.zw));
	float rstp = clamp(stepComp, minPixelStride, maxPixelStride);
	rayStep *= rstp / stepComp;
	rayStep *= (1 + jitter);
	
    
	bool hitMask = false;
	float sampleDepth = 0;
	
	float3 samplePos = float3(ssPos.xy*0.5+0.5, ssPos.z);
	samplePos.z += rayStep.z * 0.5;
	float sMin = LinearEyeDepth(samplePos.z); // this is (samplePos + localRayStep) - 0.5 * localRayStep;
	samplePos += rayStep; // do first step
	//UNITY_LOOP
	int i = 0;
	for (i = 0;  i < stepCount && !hitMask && all(samplePos.xyz == saturate(samplePos.xyz)); i++) {
		float sMax = LinearEyeDepth(samplePos.z);
		float sMaxOld = sMax;
		if (sMax < sMin) {
			sMax = sMin;
			sMin = sMaxOld;
		}
		sampleDepth = sampleDepthLod(depthTexture, samplePos.xy, 0);
		float eyeSampleDepth = LinearEyeDepth(sampleDepth);
    	hitMask = (sMax >= eyeSampleDepth) && (sMin <= eyeSampleDepth + objectThickness);
        
		sMin = sMaxOld;
		samplePos += rayStep; // do step
	}
	samplePos -= rayStep; // undo last step
	samplePos.z  -= rayStep.z  * 0.5;
	
//	rayStep *= 0.5;
	float sel = InvLerp(0, +rayStep.z, (samplePos.z - sampleDepth));
//	rayStep *= 0.5;
	sel = clamp(sel, -1, 1);
//	samplePos += lerp(0, +rayStep, sel) * sign(rayStep.z);
//	samplePos.xy += rayStep.xy * 0.5;
//	samplePos.z  -= rayStep.z  * 0.5;
	
	//samplePos.z = i*(1.0/256.0);
	//hitMask *= all(saturate(0.5-abs(samplePos.xy-0.5)));
	return float4(samplePos.xyz, hitMask ? i * rstp / maxPixelStride : stepCount);
}

#endif // CAT_RAYTRACE_LIB_INCLUDED





















