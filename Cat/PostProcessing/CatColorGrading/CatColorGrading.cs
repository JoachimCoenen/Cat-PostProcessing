using System;
using UnityEngine;
using Cat.Common;

namespace Cat.PostProcessing {
	[RequireComponent(typeof(Camera))]
	[ExecuteInEditMode]
	[ImageEffectAllowedInSceneView]
	[AddComponentMenu("Cat/PostProcessing/Color Grading")]
	public class CatColorGrading : PostProcessingBaseImageEffect {

		[Serializable]
		public struct Settings {
			[Header("Color Grading")]
			[Range(-3, 3)]
			public float exposure;

			[Range(-1, 1)]
			public float contrast;

			[Range(-1, 1)]
			public float saturation;

			[Header("Color Correction")]
			[Range(-1, 1)]
			public float temperature;

			[Range(-1, 1)]
			public float tint;

			[Header("Curves")]
			[Range(-1, 1)]
			public float blackPoint;

			[Range(-1, 1)]
			public float midPoint;

			[Range(-1, 1)]
			public float whitePoint;

			[Space(10)]
			[Range(-1, 1)]
			public float shadows;

			[Range(-1, 1)]
			public float highlights;

			//[Header("Tone Mapping")]
			//[Range(0, 1)]
			//public float strength;



			public static Settings defaultSettings { 
				get {
					return new Settings {
						exposure = 0,
						contrast = 0,
						saturation = 0,

						temperature = 0,
						tint = 0,
						blackPoint = 0,
						midPoint = 0,
						whitePoint = 0,

						shadows = 0,
						highlights = 0,

						//strength = 0.5f,
					};
				}
			}
		
			public Vector4 GetCurveParams() {
				// var black = 0.0f + settings.blackPoint * 0.25f;
				var gray  = 0.5f + midPoint  * 0.125f;
				// var white = 1.0f + settings.whitePoint * 0.5f;

				var slopeShadows    = shadows*2+1;// Mathf.Pow(2f,  1.5f * shadows);
				var slopeHighlights = -highlights*2+1;// Mathf.Pow(2f, -1.5f * highlights);

				float G = gray*gray-gray;
				float a = (0.5f + gray*(-slopeShadows + gray*((slopeHighlights + 2*slopeShadows - 3) + gray*-(slopeHighlights + slopeShadows - 2)))) / (G*G);
				float b = -2 - 2*a + slopeHighlights + slopeShadows;
				float c =  3 + a - slopeHighlights - 2*slopeShadows;
				float d = slopeShadows;
				return new Vector4(a, b, c, d);
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

		override protected string shaderName { get { return "Hidden/Cat Color Grading"; } }
		override public string effectName { get { return "Color Grading"; } }
		override internal DepthTextureMode requiredDepthTextureMode { get { return DepthTextureMode.None; } }
		override public bool isActive { get { return true; } }

		static class PropertyIDs {
			internal static readonly int Exposure_f		= Shader.PropertyToID("_Exposure");
			internal static readonly int Contrast_f		= Shader.PropertyToID("_Contrast");
			internal static readonly int Saturation_f	= Shader.PropertyToID("_Saturation");

			internal static readonly int Temperature_f	= Shader.PropertyToID("_Temperature");
			internal static readonly int Tint_f			= Shader.PropertyToID("_Tint");

			internal static readonly int BlackPoint_f	= Shader.PropertyToID("_BlackPoint");
			internal static readonly int WhitePoint_f	= Shader.PropertyToID("_WhitePoint");
			internal static readonly int CurveParams_v	= Shader.PropertyToID("_CurveParams");

			internal static readonly int Strength_f		= Shader.PropertyToID("_Strength");

			internal static readonly int MainTex_t		= Shader.PropertyToID("_MainTex");
		}

		override protected void UpdateMaterialPerFrame(Material material, Camera camera, VectorInt2 cameraSize) {
			setMaterialDirty();
		}

		override protected void UpdateMaterial(Material material, Camera camera, VectorInt2 cameraSize) {

			material.SetFloat(PropertyIDs.Exposure_f,		settings.exposure);
			material.SetFloat(PropertyIDs.Contrast_f,		settings.contrast);
			material.SetFloat(PropertyIDs.Saturation_f,		settings.saturation);
			
			material.SetFloat(PropertyIDs.Temperature_f,	settings.temperature);
			material.SetFloat(PropertyIDs.Tint_f,			settings.tint);
			material.SetFloat(PropertyIDs.BlackPoint_f,		settings.blackPoint);
			material.SetFloat(PropertyIDs.WhitePoint_f,		settings.whitePoint);
			
			material.SetVector(PropertyIDs.CurveParams_v,	settings.GetCurveParams());
			
			//material.SetFloat(PropertyIDs.Strength_f,		settings.strength);
		}


		internal override void RenderImage(RenderTexture source, RenderTexture destination) {
			Blit(source, destination, material, 0);
		}

		public void OnValidate () {
			setMaterialDirty();
		}
	}

}