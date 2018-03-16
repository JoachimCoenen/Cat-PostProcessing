using System;
using UnityEngine;
using Cat.Common;

namespace Cat.PostProcessing {
	[RequireComponent(typeof(Camera))]
	[ExecuteInEditMode]
	[ImageEffectAllowedInSceneView]
	[AddComponentMenu("Cat/PostProcessing/Chromatic Aberration")]
	public class CatChromaticAberrationRenderer : PostProcessingBaseImageEffect<CatChromaticAberration> {

		override protected string shaderName { get { return "Hidden/Cat Chromatic Aberration"; } }
		override public string effectName { get { return "Chromatic Aberration"; } }
		override internal DepthTextureMode requiredDepthTextureMode { get { return DepthTextureMode.None; } }

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
	[SettingsForPostProcessingEffect(typeof(CatChromaticAberrationRenderer))]
	public class CatChromaticAberration : PostProcessingSettingsBase {

		override public string effectName { 
			get { return "Chromatic Aberration"; } 
		}
		override public int queueingPosition {
			get { return 2900; } 
		}

		[Range(0, 1)]
		public FloatProperty strength = new FloatProperty();

		public CatChromaticAberration() {
			strength.rawValue = 0.5f;
		}

		public static CatChromaticAberration defaultSettings { 
			get {
				return new CatChromaticAberration();
			}
		}
	}
}