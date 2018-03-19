using System;
using UnityEngine;
using UnityEngine.Rendering;
using Cat.Common;

// Inspired By: Kino/Bloom v2 - Bloom filter for Unity:
// https://github.com/keijiro/KinoBloom

namespace Cat.PostProcessing {
	[RequireComponent(typeof(Camera))]
	[ExecuteInEditMode]
	[ImageEffectAllowedInSceneView]
	[AddComponentMenu("Cat/PostProcessing/Deferred Fog")]
	public class CatDeferredFogRenderer : PostProcessingBaseImageEffect<CatDeferredFog> {

		override public string effectName { 
			get { return "Deferred Fog"; } 
		}
		override protected string shaderName { 
			get { return "Hidden/Cat Deferred Fog"; } 
		}
		override internal DepthTextureMode requiredDepthTextureMode { 
			get { return DepthTextureMode.Depth; } 
		}
		override public bool enabled { 
			get { return RenderSettings.fog && base.enabled && m_currentRenderingPath == RenderingPath.DeferredShading; } 
		}

		private RenderingPath m_currentRenderingPath;

		static class PropertyIDs {
			internal static readonly int FogColor_c			= Shader.PropertyToID("_FogColor");
			internal static readonly int FogParams_v		= Shader.PropertyToID("_FogParams");
		}

		override protected void UpdateMaterialPerFrame(Material material, Camera camera, VectorInt2 cameraSize) {
			m_currentRenderingPath = camera.actualRenderingPath;

			var isGammaColorSpace = QualitySettings.activeColorSpace == ColorSpace.Gamma;
			var fogColor = RenderSettings.fogColor;
			material.SetColor(PropertyIDs.FogColor_c, isGammaColorSpace ? fogColor : fogColor.linear);
			material.SetVector(PropertyIDs.FogParams_v, new Vector3(RenderSettings.fogDensity, RenderSettings.fogStartDistance, RenderSettings.fogEndDistance));

			switch (RenderSettings.fogMode) {
				case FogMode.Linear:
					material.EnableKeyword("FOG_LINEAR");
					material.DisableKeyword("FOG_EXP");
					material.DisableKeyword("FOG_EXP_SQR");
					break;
				case FogMode.Exponential:
					material.DisableKeyword("FOG_LINEAR");
					material.EnableKeyword("FOG_EXP");
					material.DisableKeyword("FOG_EXP_SQR");
					break;
				case FogMode.ExponentialSquared:
					material.DisableKeyword("FOG_LINEAR");
					material.DisableKeyword("FOG_EXP");
					material.EnableKeyword("FOG_EXP_SQR");
					break;
			}
			Shader.DisableKeyword("FOG_EXP_SQR");
		}

		internal override void RenderImage(RenderTexture source, RenderTexture destination) {
			var material = this.material;
			Blit(source, destination, material, 0);
		}

		public void OnValidate () {
			setMaterialDirty();
		}
	}

	[Serializable]
	[SettingsForPostProcessingEffect(typeof(CatDeferredFogRenderer))]
	public class CatDeferredFog : PostProcessingSettingsBase {

		override public bool enabled { get { return true; } }

		override public string effectName { 
			get { return "Deferred Fog"; } 
		}
		override public int queueingPosition {
			get { return 1990; } 
		}

		public override void Reset() { }
	}
}
