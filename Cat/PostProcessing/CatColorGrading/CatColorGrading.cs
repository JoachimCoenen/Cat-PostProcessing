using System;
using UnityEngine;
using Cat.Common;

namespace Cat.PostProcessing {
	public class CatColorGradingRenderer : PostProcessingBaseImageEffect<CatColorGrading> {

		override protected string shaderName { get { return "Hidden/Cat Color Grading"; } }
		override public string effectName { get { return "Color Grading"; } }
		override internal DepthTextureMode requiredDepthTextureMode { get { return DepthTextureMode.None; } }
		override public int queueingPosition {
			get { return 3500; } 
		}

		static class PropertyIDs {
			internal static readonly int Response_f		= Shader.PropertyToID("_Response");
			internal static readonly int Gain_f			= Shader.PropertyToID("_Gain");

			internal static readonly int Exposure_f		= Shader.PropertyToID("_Exposure");
			internal static readonly int Contrast_f		= Shader.PropertyToID("_Contrast");
			internal static readonly int Saturation_f	= Shader.PropertyToID("_Saturation");

			internal static readonly int Temperature_f	= Shader.PropertyToID("_Temperature");
			internal static readonly int Tint_f			= Shader.PropertyToID("_Tint");
			internal static readonly int ColorBalance_v	= Shader.PropertyToID("_ColorBalance");

			internal static readonly int ColorMixerMatrix_m	= Shader.PropertyToID("_ColorMixerMatrix");


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


		readonly Matrix4x4 M_AP1_2_AP0 = new Matrix4x4() {
			m00 =  0.6954522414f, m01 = 0.1406786965f, m02 =  0.1638690622f, m03 = 0,
			m10 =  0.0447945634f, m11 = 0.8596711185f, m12 =  0.0955343182f, m13 = 0,
			m20 = -0.0055258826f, m21 = 0.0040252103f, m22 =  1.0015006723f, m23 = 0,
			m30 =  0      , m31 = 0      , m32 =  0      , m33 = 0
		};
		readonly Matrix4x4 M_LMS_2_LIN = new Matrix4x4() {
			m00 =  2.85847e+0f, m01 = -1.62879e+0f, m02 = -2.48910e-2f, m03 = 0,
			m10 = -2.10182e-1f, m11 =  1.15820e+0f, m12 =  3.24281e-4f, m13 = 0,
			m20 = -4.18120e-2f, m21 = -1.18169e-1f, m22 =  1.06867e+0f, m23 = 0,
			m30 =  0      , m31 = 0      , m32 =  0      , m33 = 0
		};
		readonly Matrix4x4 M_AP0_2_sRGB = new Matrix4x4() {
			m00 =  2.52169f, m01 = -1.13413f, m02 = -0.38756f, m03 = 0,
			m10 = -0.27648f, m11 =  1.37272f, m12 = -0.09624f, m13 = 0,
			m20 = -0.01538f, m21 = -0.15298f, m22 =  1.16835f, m23 = 0,
			m30 =  0      , m31 = 0      , m32 =  0      , m33 = 0
		};
		readonly Matrix4x4 M_LIN_2_LMS = new Matrix4x4() {
			m00 = 3.90405e-1f, m01 = 5.49941e-1f, m02 = 8.92632e-3f, m03 = 0,
			m10 = 7.08416e-2f, m11 = 9.63172e-1f, m12 = 1.35775e-3f, m13 = 0,
			m20 = 2.31082e-2f, m21 = 1.28021e-1f, m22 = 9.36245e-1f, m23 = 0,
			m30 =  0      , m31 = 0      , m32 =  0      , m33 = 0
		};
		readonly Matrix4x4 M_AP0_2_AP1 = new Matrix4x4() {
			m00 =  1.4514393161f, m01 = -0.2365107469f, m02 = -0.2149285693f, m03 = 0,
			m10 = -0.0765537734f, m11 =  1.1762296998f, m12 = -0.0996759264f, m13 = 0,
			m20 =  0.0083161484f, m21 = -0.0060324498f, m22 =  0.9977163014f, m23 = 0,
			m30 =  0      , m31 = 0      , m32 =  0      , m33 = 0
		};

		override protected void UpdateMaterial(Material material, Camera camera, VectorInt2 cameraSize) {
			const float EPSILON = 1e-4f;
			var response 	= Mathf.Pow(2, settings.response);
			var gain 		= Mathf.Pow(2, settings.gain);
			var exposure    = Mathf.Pow(2, settings.exposure);
			var contrast    = Mathf.Max(EPSILON, settings.contrast + Mathf.Max(0, settings.contrast) * Mathf.Max(0, settings.contrast) + 1);
			var saturation  = (settings.saturation + Mathf.Max(0, settings.saturation) * Mathf.Max(0, settings.saturation) + 1) / contrast;
			var blackPoint  = 0 + settings.blackPoint * 0.25f;
			var whitePoint  = 1 +settings. whitePoint * 0.25f;




			var mat1 = M_AP0_2_sRGB * (M_AP1_2_AP0 * M_LMS_2_LIN);
			var mat2 = M_LIN_2_LMS * M_AP0_2_AP1;
			var colorBalance = CalculateColorBalance(settings.temperature, settings.tint);
			mat2.SetRow(0, mat2.GetRow(0) * colorBalance[0]);
			mat2.SetRow(1, mat2.GetRow(1) * colorBalance[1]);
			mat2.SetRow(2, mat2.GetRow(2) * colorBalance[2]);
			var mat3 = mat1 * mat2;

			var colorMixMatrix = Matrix4x4.identity;
			switch (settings.colorMixer) {
				case CatColorGrading.ColorMixer.Off:
					break;
				case CatColorGrading.ColorMixer.Sepia: {
						var column0 = Vector3.one * 0.4392157f;
						var column1 = Vector3.one * 0.2588235f;
						var column2 = Vector3.one * 0.0784314f;
						colorMixMatrix = new Matrix4x4(column0, column1, column2, Vector4.zero);
					//	colorMixMatrix = NormalizeColorMatrix(colorMixMatrix);
					}
					break;
				case CatColorGrading.ColorMixer.Mono: {
						var column = new Vector4(0.3333333f, 0.3333333f, 0.3333333f, 0f);
						colorMixMatrix = new Matrix4x4(column, column, column, Vector4.zero);
					} 
					break;
				case CatColorGrading.ColorMixer.Noir: {
						var column = new Vector4(0.2126729f, 0.7151522f, 0.0721750f, 0f);
						colorMixMatrix = new Matrix4x4(column, column, column, Vector4.zero);
					} 
					break;
				case CatColorGrading.ColorMixer.Custom: {
						colorMixMatrix = new Matrix4x4(settings.red, settings.green, settings.blue, Vector4.zero);
						if (settings.isColorMatrixNormalized) {
							colorMixMatrix = NormalizeColorMatrix(colorMixMatrix);
						}
					} 
					break;
				default:
					break;
			}
			colorMixMatrix = colorMixMatrix.transpose;

			colorMixMatrix.SetRow(3, Vector4.zero);
			colorMixMatrix.SetColumn(3, Vector4.zero);

			colorMixMatrix = colorMixMatrix * mat3;


			switch (settings.tonemapper) {
				case CatColorGrading.Tonemapper.Off: 
					material.DisableKeyword("TONEMAPPING_FILMIC");
					material.DisableKeyword("TONEMAPPING_NEUTRAL");
					material.DisableKeyword("TONEMAPPING_UNCHARTED_2");
					break;
				case CatColorGrading.Tonemapper.Filmic:
					material.EnableKeyword("TONEMAPPING_FILMIC");
					material.DisableKeyword("TONEMAPPING_NEUTRAL");
					material.DisableKeyword("TONEMAPPING_UNCHARTED_2");
					break;
				case CatColorGrading.Tonemapper.Neutral:
					material.DisableKeyword("TONEMAPPING_FILMIC");
					material.EnableKeyword("TONEMAPPING_NEUTRAL");
					material.DisableKeyword("TONEMAPPING_UNCHARTED_2");
					break;
				case CatColorGrading.Tonemapper.Uncharted2: 
					material.DisableKeyword("TONEMAPPING_FILMIC");
					material.DisableKeyword("TONEMAPPING_NEUTRAL");
					material.EnableKeyword("TONEMAPPING_UNCHARTED_2");
					break;
				default:
					break;
			}

			material.SetFloat(PropertyIDs.Response_f,			response);
			material.SetFloat(PropertyIDs.Gain_f,				gain);

			material.SetFloat(PropertyIDs.Exposure_f,			exposure);
			material.SetFloat(PropertyIDs.Contrast_f,			contrast);
			material.SetFloat(PropertyIDs.Saturation_f,			saturation);

			material.SetMatrix(PropertyIDs.ColorMixerMatrix_m,	colorMixMatrix);

			material.SetFloat(PropertyIDs.BlackPoint_f,			blackPoint);
			material.SetFloat(PropertyIDs.WhitePoint_f,			whitePoint);

			material.SetVector(PropertyIDs.CurveParams_v,		settings.GetCurveParams());

			material.SetTexture(PropertyIDs.blueNoise_t, 		PostProcessingManager.blueNoiseTexture);

			//material.SetFloat(PropertyIDs.Strength_f,		settings.strength);
		}

		Matrix4x4 NormalizeColorMatrix(Matrix4x4 colorMatrix) {
			var row0 = colorMatrix.GetRow(0);
			var row1 = colorMatrix.GetRow(1);
			var row2 = colorMatrix.GetRow(2);
			var maxMagnitude = Mathf.Max(row0.x + row0.y + row0.z, row1.x + row1.y + row1.z, row2.x + row2.y + row2.z);
			if (maxMagnitude > Mathf.Epsilon) {
				maxMagnitude = 1 / maxMagnitude;
				colorMatrix.SetRow(0, row0 * maxMagnitude);
				colorMatrix.SetRow(1, row1 * maxMagnitude);
				colorMatrix.SetRow(2, row2 * maxMagnitude);
			} else {
				colorMatrix = Matrix4x4.identity;
			}
			return colorMatrix;
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

	[Serializable]
	[SettingsForPostProcessingEffect(typeof(CatColorGradingRenderer))]
	public class CatColorGrading : PostProcessingSettingsBase {

		override public string effectName { 
			get { return "Color Grading"; } 
		}

		public enum Tonemapper {
			Off = 0,
			Filmic,
			Neutral,
			Uncharted2
		}

		public enum ColorMixer {
			Off = 0,
			Sepia,
			Mono,
			Noir,
			Custom
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


		[Header("Color Mixer")]
		public ColorMixer colorMixer;
		public Color red;
		public Color green;
		public Color blue;
		[CustomLabel("Normalized")]
		public bool isColorMatrixNormalized;


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

		public CatColorGrading() {
			tonemapper = Tonemapper.Off;
			response = 0f;
			gain = 0f;

			exposure = 0;
			contrast = 0;
			saturation = 0;

			temperature = 0;
			tint = 0;

			colorMixer = ColorMixer.Off;
			red = Color.red;
			green = Color.green;
			blue = Color.blue;
			isColorMatrixNormalized = false;

			blackPoint = 0;
			midPoint = 0;
			whitePoint = 0;

			shadows = 0;
			highlights = 0;

		}

		public static CatColorGrading defaultSettings { 
			get {
				return new CatColorGrading {
					tonemapper = Tonemapper.Off,
					response = 0f,
					gain = 0f,

					exposure = 0,
					contrast = 0,
					saturation = 0,

					temperature = 0,
					tint = 0,

					colorMixer = ColorMixer.Off,
					red = Color.red,
					green = Color.green,
					blue = Color.blue,
					isColorMatrixNormalized = false,

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

}