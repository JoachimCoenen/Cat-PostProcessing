using System;
using UnityEngine;
using Cat.Common;

namespace Cat.PostProcessing {
	[RequireComponent(typeof(Camera))]
	[ExecuteInEditMode]
	[ImageEffectAllowedInSceneView]
	[AddComponentMenu("Cat/PostProcessing/Chromatic Aberration")]
	public class CatChromaticAberration : PostProcessingBaseImageEffect<CatChromaticAberrationSettings> {

		override protected string shaderName { get { return "Hidden/Cat Chromatic Aberration"; } }
		override public string effectName { get { return "Chromatic Aberration"; } }
		override internal DepthTextureMode requiredDepthTextureMode { get { return DepthTextureMode.None; } }
		override public int queueingPosition {
			get { return 2850; } 
		}

		static class PropertyIDs {
			internal static readonly int Strength_f	= Shader.PropertyToID("_Strength");
			internal static readonly int MainTex_t	= Shader.PropertyToID("_MainTex");
		}

		override protected void UpdateMaterialPerFrame(Material material, Camera camera, VectorInt2 cameraSize) {
			setMaterialDirty();
		}

		override protected void UpdateMaterial(Material material, Camera camera, VectorInt2 cameraSize) {
			material.SetFloat(PropertyIDs.Strength_f, settings.strength);
		}

		internal override void RenderImage(RenderTexture source, RenderTexture destination) {
			Blit(source, destination, material, 0);
		}

		public void OnValidate () {
			setMaterialDirty();
		}
	}

	[Serializable]
	[SettingsForPostProcessingEffect(typeof(CatChromaticAberration))]
	public class CatChromaticAberrationSettings : PostProcessingSettingsBase {

		override public string effectName { 
			get { return "Chromatic Aberration"; } 
		}

		[Range(0, 1)]
		public float strength = 0.5f;

		public static CatChromaticAberrationSettings defaultSettings { 
			get {
				return new CatChromaticAberrationSettings {
					strength = 0.5f
				};
			}
		}
	}
}