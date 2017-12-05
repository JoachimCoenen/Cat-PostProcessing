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
			public enum Tonemapper {
				Off,
				Filmic,
				Neutral
			}

			[Header("Tonemapping")]
			public Tonemapper tonemapper;

			[Range(-3, 3)]
			public float response;

			[Range(-3, 3)]
			public float gain;


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
						tonemapper = Tonemapper.Off,
						response = 0f,
						gain = 0f,

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
			internal static readonly int Response_f		= Shader.PropertyToID("_Response");
			internal static readonly int Gain_f			= Shader.PropertyToID("_Gain");

			internal static readonly int Exposure_f		= Shader.PropertyToID("_Exposure");
			internal static readonly int Contrast_f		= Shader.PropertyToID("_Contrast");
			internal static readonly int Saturation_f	= Shader.PropertyToID("_Saturation");

			internal static readonly int Temperature_f	= Shader.PropertyToID("_Temperature");
			internal static readonly int Tint_f			= Shader.PropertyToID("_Tint");
			internal static readonly int ColorBalance_v	= Shader.PropertyToID("_ColorBalance");

			internal static readonly int BlackPoint_f	= Shader.PropertyToID("_BlackPoint");
			internal static readonly int WhitePoint_f	= Shader.PropertyToID("_WhitePoint");
			internal static readonly int CurveParams_v	= Shader.PropertyToID("_CurveParams");

			internal static readonly int Strength_f		= Shader.PropertyToID("_Strength");

			internal static readonly int MainTex_t		= Shader.PropertyToID("_MainTex");

			internal static readonly int blueNoise_t	= Shader.PropertyToID("_BlueNoise");
		}

		override protected void UpdateMaterialPerFrame(Material material, Camera camera, VectorInt2 cameraSize) {
			setMaterialDirty();
		}

		override protected void UpdateMaterial(Material material, Camera camera, VectorInt2 cameraSize) {
			const float EPSILON = 1e-4f;
			var response 	= Mathf.Pow(2, settings.response);
			var gain 		= Mathf.Pow(2, settings.gain);
			var exposure    = Mathf.Pow(2, settings.exposure);
			var contrast    = Mathf.Max(EPSILON, settings.contrast + Mathf.Max(0, settings.contrast) * Mathf.Max(0, settings.contrast) + 1);
			var saturation  = (settings.saturation + Mathf.Max(0, settings.saturation) * Mathf.Max(0, settings.saturation) + 1) / contrast;
			var blackPoint  = 0 + settings.blackPoint * 0.25f;
			var whitePoint  = 1 +settings. whitePoint * 0.25f;

			switch (settings.tonemapper) {
				case Settings.Tonemapper.Off: 
					material.DisableKeyword("TONEMAPPING_FILMIC");
					material.DisableKeyword("TONEMAPPING_NEUTRAL");
					break;
				case Settings.Tonemapper.Filmic:
					material.EnableKeyword("TONEMAPPING_FILMIC");
					material.DisableKeyword("TONEMAPPING_NEUTRAL");
					break;
				case Settings.Tonemapper.Neutral:
					material.DisableKeyword("TONEMAPPING_FILMIC");
					material.EnableKeyword("TONEMAPPING_NEUTRAL");
					break;
				default:
					break;
			}

			material.SetFloat(PropertyIDs.Response_f,		response);
			material.SetFloat(PropertyIDs.Gain_f,			gain);

			material.SetFloat(PropertyIDs.Exposure_f,		exposure);
			material.SetFloat(PropertyIDs.Contrast_f,		contrast);
			material.SetFloat(PropertyIDs.Saturation_f,		saturation);
			
			material.SetVector(PropertyIDs.ColorBalance_v, CalculateColorBalance(settings.temperature, settings.tint));
			material.SetFloat(PropertyIDs.BlackPoint_f,		blackPoint);
			material.SetFloat(PropertyIDs.WhitePoint_f,		whitePoint);

			material.SetVector(PropertyIDs.CurveParams_v,	settings.GetCurveParams());

			material.SetTexture(PropertyIDs.blueNoise_t, PostProcessingManager.blueNoiseTexture);

			//material.SetFloat(PropertyIDs.Strength_f,		settings.strength);
		}


		internal override void RenderImage(RenderTexture source, RenderTexture destination) {
			Blit(source, destination, material, 0);
		}

		public void OnValidate () {
			setMaterialDirty();
		}
	
		// An analytical model of chromaticity of the standard illuminant, by Judd et al.
		// http://en.wikipedia.org/wiki/Standard_illuminant#Illuminant_series_D
		// Slightly modifed to adjust it with the D65 white point (x=0.31271, y=0.32902).
		float StandardIlluminantY(float x) {
			return 2.87f * x - 3 * x * x - 00.27509507f;
		}

		readonly Matrix4x4 M_CAT02_XYZ_TO_LMS = new Matrix4x4() {
			m00 =  0.7328f, m01 = 0.4296f, m02 = -0.1624f, m03 = 0,
			m10 = -0.7036f, m11 = 1.6975f, m12 =  0.0061f, m13 = 0,
			m20 =  0.0030f, m21 = 0.0136f, m22 =  0.9834f, m23 = 0,
			m30 =  0      , m31 = 0      , m32 =  0      , m33 = 0
			// 0.7328  ,  0.4296  , -0.1624  ,
			// -0.7036  ,  1.6975  ,  0.0061  ,
			// 0.0030  ,  0.0136  ,  0.9834
		};

		Vector3 XYZtoLMS(Vector3 xyz) {
			return M_CAT02_XYZ_TO_LMS.MultiplyVector(xyz);
		}
		// CIE xy chromaticity to CAT02 LMS.
		// http://en.wikipedia.org/wiki/LMS_color_space#CAT02
		Vector3 XYtoLMS(float x, float y){
			var xyz = new Vector3(x / y, 1, (1 - x - y) / y);
			return XYZtoLMS(xyz);
		}

		Vector3 CalculateColorBalance(float temperature, float tint) {
			// Range ~[-1.8;1.8] ; using higher ranges is unsafe
			float t1 = temperature / 0.55f;
			float t2 = tint / 0.55f;

			// Get the CIE xy chromaticity of the reference white point.
			// Note: 0.31271 = x value on the D65 white point
			float x = 0.31271f - t1 * (t1 < 0 ? 0.1f : 0.05f);
			float y = StandardIlluminantY(x) + t2 * 0.05f;

			// Calculate the coefficients in the LMS space.
			var w1 = new Vector3(0.949237f, 1.03542f, 1.08728f); // D65 white point
			var w2 = XYtoLMS(x, y);
			w2.Set(1/w2.x, 1/w2.y, 1/w2.z);
			w2.Scale(w1);
			return w2;
		}


	
	}

}