#if !defined(LIGHTING_COMPATIBILITY_INTERFACE_INCLUDED)
#define LIGHTING_COMPATIBILITY_INTERFACE_INCLUDED
// LightingInterface.cginc

#include "HLSLSupport.cginc"
#include "UnityCG.cginc"
#include "CatCommon.cginc"

#define USE_UNITY_STANDARD_LIGHTING_O		1
#define USE_ADVANCED_MATERIAL_LIGHTING_O	0
#define USE_LUX_PERSONAL_LIGHTING_O			0

#define SUM_OF_ACTIVATED_LIGHTINGS (0		\
		+ USE_UNITY_STANDARD_LIGHTING_O		\
		+ USE_ADVANCED_MATERIAL_LIGHTING_O	\
		+ USE_LUX_PERSONAL_LIGHTING_O		\
)
#if (SUM_OF_ACTIVATED_LIGHTINGS != 1)
	#error You have to decide on ONE lighting feature to use
#endif

#if USE_UNITY_STANDARD_LIGHTING_O
	#include "UnityGBuffer.cginc"
	#include "UnityPBSLighting.cginc"
	#include "UnityStandardUtils.cginc"
	
	struct GBufferData {
		half3	diffuse;
		half3	specular;
		half	smoothness;
		half	occlusion;
		half3	normal;
		half3	emission;
	};
	
	GBufferData unpackGBuffer(half4 gbuffer0, half4 gbuffer1, half4 gbuffer2, half4 gbuffer3) {
		UnityStandardData unityD = UnityStandardDataFromGbuffer(gbuffer0, gbuffer1, gbuffer2);
		GBufferData d;
		d.diffuse		= unityD.diffuseColor;
		d.specular		= unityD.specularColor;
		d.smoothness	= unityD.smoothness;
		d.occlusion		= unityD.occlusion;
		d.normal		= normalize(unityD.normalWorld);
		d.emission		= gbuffer3.rgb;
		return d;
	}
	
	half3 getReflectionVector(GBufferData d, half3 wsViewDir) {
		return reflect(-wsViewDir, d.normal);
	}
	
	half3 getDiffuseColor(GBufferData d) {
		return d.diffuse;
	}
	
	half3 getSpecularColor(GBufferData d) {
		return d.specular;
	}
	
	half4 applyLighting(GBufferData d, half3 wsViewDir, UnityGI gi) {
		gi.indirect.diffuse *= d.occlusion;
		gi.indirect.specular *= d.occlusion;
		half oneMinusReflectivity = 1 - SpecularStrength(d.specular);
		return UNITY_BRDF_PBS(d.diffuse, d.specular, oneMinusReflectivity, d.smoothness, d.normal, wsViewDir, gi.light, gi.indirect);
	}
	
#endif

#if USE_ADVANCED_MATERIAL_LIGHTING_O
	#define unpackGBuffer(a, b, c, d) unpackGBufferNative(a, b, c, d)
//	#include "../CatShadingSystem/Shaders/inc/AdvancedMaterialGBuffer.cginc"
//	#include "../CatShadingSystem/Shaders/inc/CatAssetsShaderLighting.cginc"
	#undef unpackGBuffer
	
	struct GBufferData {
		half3 albedo;				// base (diffuse AND specular) color (albedo := duffuse + specular)
		half  alpha;				// Albedo Transparency
		
		half  metalMask;			// 0 = non-metal, 1 = metal, (R) of material property texture
		half  smoothness;			// 0 = rough, 1 = smooth, (G) of material property texture
	//	half  sheen;				// (B) of material property texture
	//	half  sheenTint;			// (A) of material property texture
		half  specularity;			// Amount of dielectric Specularity (0.02...0.22, plastic is ~0.04)
		half  translucency;			// thin sheet translucency
		
		half3 normal;				// tangent space normal, if written
		half3 lowFreqNormal;		// smoothed tangent space normal, if written (for SubsurfaceScattering)
		half3 tangent;				// Tangent for anisotopic lighting
		half  aspect;				// (default 1)
		half  occlusion;			// occlusion (default 1)
		
		half  height;				// for rain accumulation
		half3 emission;				//
		
		half4 subsurface;			// subsurface color (RGB), Thickness (A) = (log(realThickness) + 5) / 12
		half  subsurfaceScattering;	// = (1/(realScattering + 1) - 0.9999) / (-0.9199826)
	};
	
	GBufferData unpackGBuffer(half4 gbuffer0, half4 gbuffer1, half4 gbuffer2, half4 gbuffer3) {
		SurfaceOutputAdvancedMaterial advancedMaterialD = unpackGBufferNative(gbuffer0, gbuffer1, gbuffer2, gbuffer3);
		GBufferData d;
		d.albedo				= advancedMaterialD.Albedo;
		d.alpha					= advancedMaterialD.Alpha;
		d.metalMask				= advancedMaterialD.MetalMask;
		d.smoothness			= advancedMaterialD.Smoothness;
	//	d.sheen					= advancedMaterialD.Sheen;
	//	d.sheenTint				= advancedMaterialD.SheenTint;
		d.specularity			= advancedMaterialD.Specularity;
		d.translucency			= advancedMaterialD.Translucency;
		d.normal				= advancedMaterialD.Normal;
		d.lowFreqNormal			= advancedMaterialD.LowFreqNormal;
		d.tangent				= advancedMaterialD.Tangent;
		d.aspect				= advancedMaterialD.Aspect;
		d.occlusion				= advancedMaterialD.Occlusion;
		d.height				= advancedMaterialD.Height;
		d.emission				= advancedMaterialD.Emission;
		d.subsurface			= advancedMaterialD.Subsurface;
		d.subsurfaceScattering	= advancedMaterialD.SubsurfaceScattering;
		return d;
	}
	
	half3 getReflectionVector(GBufferData d, half3 wsViewDir) {
		return getReflectionVector(d.normal, d.tangent, d.aspect, wsViewDir);
	}
	
	half3 getDiffuseColor(GBufferData d) {
		half4 diffuseColor, specularColor;
		getMaterialColors(d.albedo, 1, d.metalMask, d.specularity, /*out*/diffuseColor, /*out*/specularColor);
		return diffuseColor;
	}
	
	half3 getSpecularColor(GBufferData d) {
		half4 diffuseColor, specularColor;
		getMaterialColors(d.albedo, 1, d.metalMask, d.specularity, /*out*/diffuseColor, /*out*/specularColor);
		return specularColor;
	}
	
	half4 applyLighting(GBufferData d, half3 wsViewDir, UnityGI gi) {
		SurfaceOutputAdvancedMaterial s;
		s.Albedo				= d.albedo;
		s.Alpha					= d.alpha;
		s.MetalMask				= d.metalMask;
		s.Smoothness			= d.smoothness;
	//	s.Sheen					= d.sheen;
	//	s.SheenTint				= d.sheenTint;
		s.Specularity			= d.specularity;
		s.Translucency			= d.translucency;
		s.Normal				= d.normal;
		s.LowFreqNormal			= d.lowFreqNormal;
		s.Tangent				= d.tangent;
		s.Aspect				= d.aspect;
		s.Occlusion				= d.occlusion;
		s.Height				= d.height;
		s.Emission				= d.emission;
		s.Subsurface			= d.subsurface;
		s.SubsurfaceScattering	= d.subsurfaceScattering;
		return LightingCatAdvanced(s, wsViewDir, gi);
	}
	
#endif

#if USE_LUX_PERSONAL_LIGHTING_O
	#include "UnityGBuffer.cginc"
//	#include "../Lux 2.01 Personal/Lux Shaders/Lux Core/Lux BRDFs/LuxStandardBRDF.cginc"
		
	struct GBufferData {
		half3	diffuse;
		half3	specular;
		half	smoothness;
		half	occlusion;
		half3	normal;
		half3	emission;
	};
	
	GBufferData unpackGBuffer(half4 gbuffer0, half4 gbuffer1, half4 gbuffer2, half4 gbuffer3) {
		UnityStandardData unityD = UnityStandardDataFromGbuffer(gbuffer0, gbuffer1, gbuffer2);
		GBufferData d;
		d.diffuse		= unityD.diffuseColor;
		d.specular		= unityD.specularColor;
		d.smoothness	= unityD.smoothness;
		d.occlusion		= unityD.occlusion;
		d.normal		= unityD.normalWorld;
		d.emission		= gbuffer3.rgb;
		return d;
	}
	
	half3 getReflectionVector(GBufferData d, half3 wsViewDir) {
		return reflect(-wsViewDir, d.normal);
	}
	
	half3 getDiffuseColor(GBufferData d) {
		return d.diffuse;
	}
	
	half3 getSpecularColor(GBufferData d) {
		return d.specular;
	}
	
	half4 applyLighting(GBufferData d, half3 wsViewDir, UnityGI gi) {
		gi.indirect.diffuse *= d.occlusion;
		gi.indirect.specular *= d.occlusion;
		half oneMinusReflectivity = 1 - SpecularStrength(d.specular);
		half shadow = 1;
		
		// Add support for "real" lambert lighting
		half specularIntensity = (d.specular.r == 0.0) ? 0.0 : 1;
		
		half3 halfDir = Unity_SafeNormalize (gi.light.dir + wsViewDir);
		half nh = saturate(dot(d.normal, halfDir));
		half nv = saturate(dot(d.normal, wsViewDir));
		half lv = saturate(dot(gi.light.dir, wsViewDir));
		half lh = saturate(dot(gi.light.dir, halfDir));
		half nl = saturate(dot(gi.light.dir, d.normal));
		gi.light.ndotl = nl;
		half ndotlDiffuse = nl;
		return Lux_BRDF1_PBS(d.diffuse, d.specular, oneMinusReflectivity, d.smoothness, d.normal, wsViewDir,
							  halfDir, nh, nv, lv, lh, ndotlDiffuse, gi.light, gi.indirect, specularIntensity, shadow
		);
	}
	
#endif

GBufferData unpackGBuffer(half4 gbuffer0, half4 gbuffer1, half4 gbuffer2) {
	return unpackGBuffer(gbuffer0, gbuffer1, gbuffer2, 0);
}

#endif // LIGHTING_COMPATIBILITY_INTERFACE_INCLUDED
