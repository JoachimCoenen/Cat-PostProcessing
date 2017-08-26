using System;
using UnityEngine;
using Cat.Common;

namespace Cat.PostProcessing {
	[RequireComponent(typeof(Camera))]
	[ExecuteInEditMode]
	[ImageEffectAllowedInSceneView]
	[AddComponentMenu("Cat/PostProcessing/Chromatic Aberration")]
	public class CatChromaticAberration : PostProcessingBaseImageEffect {

		[Serializable]
		public struct Settings {
			[Range(0, 1)]
			public float strength;

			public static Settings defaultSettings { 
				get {
					return new Settings {
						strength = 0.5f
					};
				}
			}
		}

		[SerializeField]
		[Inlined]
		private Settings m_Settings = Settings.defaultSettings;
		public Settings settings {
			get { return m_Settings; }
			set { 
				m_Settings = value;
				OnValidate();
			}
		}

		override protected string shaderName { get { return "Hidden/Cat Chromatic Aberration"; } }
		override public string effectName { get { return "Chromatic Aberration"; } }
		override internal DepthTextureMode requiredDepthTextureMode { get { return DepthTextureMode.None; } }
		override public bool isActive { get { return true; } }

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

}