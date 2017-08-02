﻿using System;
using UnityEngine;
using UnityEngine.Rendering;
using Cat.Common;

namespace Cat.PostProcessing {
	[RequireComponent(typeof (Camera))]
	[ExecuteInEditMode]
	[ImageEffectAllowedInSceneView]
	[AddComponentMenu("Cat/PostProcessing/Tempral Anti-Alialising")]
	public class CatAA : PostProcessingBaseImageEffect {
		private const bool disableTAAInSceneView = true;

		[Serializable]
		public struct Settings {

			//[Range(0.0f, 2.0f)]
			public const float jitterStrength = 1f;

			[Range(0.0f, 2.0f)]
			public float sharpness;

			public const bool enableVelocityPrediction = true;

			[CustomLabelRange(0.0f, 80.0f, "Velocity Scale")]
			public float velocityWeightScale;

			[Range(1e-3f, 1)]
			public float response;

			[Range(0, 5)]
			public float toleranceMargin;

			public JitterMatrixType jitterMatrix;

			[CustomLabelRange(4, 16, "Halton Seq. Length")]
			public int haltonSequenceLength;

			public static Settings defaultSettings { 
				get {
					return new Settings {
					//	jitterStrength = 1f,
						sharpness = 0.075f,
					//	enableVelocityPrediction = true,
						velocityWeightScale = 40,
						response			= 0.075f,
						toleranceMargin		= 1,
						jitterMatrix = JitterMatrixType.HaltonSequence,
						haltonSequenceLength = 8
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

		public enum JitterMatrixType {
			ps0, ps, psy, HaltonSequence, psx, ps4
		}

		private readonly RenderTextureContainer lastFrame1 = new RenderTextureContainer();

		override protected string shaderName { get { return "Hidden/CatAA"; } }
		override public string effectName { get { return "Cat Temporal Antialialising"; } }
		override internal DepthTextureMode requiredDepthTextureMode { get { return DepthTextureMode.MotionVectors | DepthTextureMode.Depth; } }
		override public bool isActive { get { return true; } }

		static class PropertyIDs {
			// jitterStrength
			internal static readonly int Sharpness_f					= Shader.PropertyToID("_Sharpness");
			internal static readonly int IsVelocityPredictionEnabled_b	= Shader.PropertyToID("_IsVelocityPredictionEnabled");
			internal static readonly int VelocityWeightScale_f			= Shader.PropertyToID("_VelocityWeightScale");
			internal static readonly int Response_f						= Shader.PropertyToID("_Response");
			internal static readonly int ToleranceMargin_f				= Shader.PropertyToID("_ToleranceMargin");
			// jitterMatrix
			// haltonSequenceLengt

			internal static readonly int History1_t						= Shader.PropertyToID("_HistoryTex1");
			internal static readonly int History2_t						= Shader.PropertyToID("_HistoryTex2");
			internal static readonly int History3_t						= Shader.PropertyToID("_HistoryTex3");
			internal static readonly int Temp_t							= Shader.PropertyToID("__IDTemp_t__");

			internal static readonly int TAAJitterVelocity_v			= Shader.PropertyToID("_TAAJitterVelocity");
			internal static readonly int Directionality_v				= Shader.PropertyToID("_Directionality");
		}

		override protected void UpdateRenderTextures(VectorInt2 cameraSize) {
			CreateCopyRT(lastFrame1, cameraSize, RenderTextureFormat.DefaultHDR, FilterMode.Bilinear);
			material.SetTexture(PropertyIDs.History1_t, lastFrame1);

			//	CreateRT(ref lastFrame2, cameraSize, RenderTextureFormat.DefaultHDR, FilterMode.Bilinear);
			//	material.SetTexture(PropertyIDs.History2_t, lastFrame2);
			//	CreateRT(ref lastFrame3, cameraSize, RenderTextureFormat.DefaultHDR, FilterMode.Bilinear);
			//	Smaterial.SetTexture(PropertyIDs.History3_t, lastFrame3);
		}

		int TMSAACounter = 0;

		override protected void UpdateCameraMatricesPerFrame(Camera camera) {
			var settings = this.settings;
			if (postProcessingManager.isSceneView && disableTAAInSceneView) {
				return;
			}
			
			Vector2[] jitterVectors = {
				Vector2.zero, Vector2.zero,
				Vector2.zero, Vector2.zero
			};
			switch (settings.jitterMatrix) {
			case JitterMatrixType.ps0:
				jitterVectors = ps0; break;
			case JitterMatrixType.ps:
				jitterVectors = ps; break;
			case JitterMatrixType.psy:
				jitterVectors = psy; break;
			case JitterMatrixType.HaltonSequence:
				jitterVectors = HaltonSequence; break;
			case JitterMatrixType.psx:
				jitterVectors = psx; break;
			case JitterMatrixType.ps4:
				jitterVectors = ps4; break;
			}
			//	camera.ResetProjectionMatrix();
			if (settings.jitterMatrix == JitterMatrixType.HaltonSequence) {
				TMSAACounter = (TMSAACounter + 1) % settings.haltonSequenceLength ;
			} else {
				TMSAACounter = (TMSAACounter + 1) % 4 ;
			}

			var newP = jitterVectors[TMSAACounter] * Settings.jitterStrength;


			newP.x /= (float)postProcessingManager.cameraSize.x;
			newP.y /= (float)postProcessingManager.cameraSize.y;
			if (camera.orthographic) {
				camera.projectionMatrix = GetOrthographicProjectionMatrix(newP);
			} else {
				camera.projectionMatrix = GetPerspectiveProjectionMatrix(newP);
			}
			Shader.SetGlobalVector(PropertyIDs.TAAJitterVelocity_v, postProcessingManager.isSceneView ? Vector2.zero : newP);

		}

		override protected void UpdateMaterialPerFrame(Material material) {
			material.SetVector(PropertyIDs.Directionality_v, new Vector2(TMSAACounter % 2 == 0 ? 1 : -1, 1));
			setMaterialDirty();
		}

		override protected void UpdateMaterial(Material material) {
			var allowVelocityPrediction = Settings.enableVelocityPrediction && !postProcessingManager.isSceneView;
			material.SetFloat(PropertyIDs.Sharpness_f, settings.sharpness);
			material.SetInt(PropertyIDs.IsVelocityPredictionEnabled_b, allowVelocityPrediction ? 1 : 0);
			material.SetFloat(PropertyIDs.VelocityWeightScale_f, settings.velocityWeightScale);
			material.SetFloat(PropertyIDs.Response_f, settings.response);
			material.SetFloat(PropertyIDs.ToleranceMargin_f, settings.toleranceMargin);
			material.SetTexture(PropertyIDs.History1_t, lastFrame1);
		}

		override protected void OnPostRender() {
			postProcessingManager.camera.ResetProjectionMatrix();
			base.OnPostRender();
		}

		//[ImageEffectTransformsToLDR]
		void OnRenderImage(RenderTexture source, RenderTexture destination) {
			if (postProcessingManager.isSceneView && disableTAAInSceneView) {
				Blit(source, destination);
				return;
			}

			if (isFirstFrame) {
		//		Blit(source, lastFrame1, material, 1);
			}

			var tempTex = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);
			Blit(source, tempTex, material, 0);
			//	Debug.LogFormat("{0}", destination != null);
			Blit(tempTex, lastFrame1);
			Blit(tempTex, destination);

			//Graphics.Blit(source, tempTex, material, 3);
			//Graphics.Blit(tempTex, lastFrame1, material, 4);
			//Graphics.Blit(tempTex, destination, material, 4);

			RenderTexture.ReleaseTemporary(tempTex);
		}


		override protected void OnDisable() {
			//camera.ResetWorldToCameraMatrix();
			postProcessingManager.camera.ResetProjectionMatrix();
			Shader.SetGlobalVector(PropertyIDs.TAAJitterVelocity_v, Vector2.zero);
			base.OnDisable();
		}
			
		void OnValidate () {
			setMaterialDirty();
		}

		// Adapted heavily from Unitys TAA code
		// https://github.com/Unity-Technologies/PostProcessing/blob/v1/PostProcessing/Runtime/Components/TaaComponent.cs
		Matrix4x4 GetPerspectiveProjectionMatrix(Vector2 offset){
			float vertical = Mathf.Tan(0.5f * Mathf.Deg2Rad * postProcessingManager.camera.fieldOfView);
			float horizontal = vertical * postProcessingManager.camera.aspect;

			float n = postProcessingManager.camera.nearClipPlane;
			float f = postProcessingManager.camera.farClipPlane;

			var matrix = Matrix4x4.zero;

			matrix[0, 0] = 1 / horizontal;
			// matrix[0, 1] = 0;
			matrix[0, 2] = -2 * offset.x;
			// matrix[0, 3] = 0;

			// matrix[1, 0] = 0;
			matrix[1, 1] = 1 / vertical;
			matrix[1, 2] = -2 * offset.y;
			// matrix[1, 3] = 0;

			// matrix[2, 0] = 0;
			// matrix[2, 1] = 0;
			matrix[2, 2] = (f + n) / (n - f);
			matrix[2, 3] = 2 * f * n / (n - f);

			// matrix[3, 0] = 0;
			// matrix[3, 1] = 0;
			matrix[3, 2] = -1;
			// matrix[3, 3] = 0;

			return matrix;
		}

		// Adapted heavily from Unitys TAA code
		// https://github.com/Unity-Technologies/PostProcessing/blob/v1/PostProcessing/Runtime/Components/TaaComponent.cs
		Matrix4x4 GetOrthographicProjectionMatrix(Vector2 offset) {
			float vertical = postProcessingManager.camera.orthographicSize;
			float horizontal = vertical * postProcessingManager.camera.aspect;

			float n = postProcessingManager.camera.nearClipPlane;
			float f = postProcessingManager.camera.farClipPlane;

			var matrix = Matrix4x4.zero;

			matrix[0, 0] = 1 / horizontal;
			// matrix[0, 1] = 0;
			// matrix[0, 2] = 0;
			matrix[0, 3] = 2 * offset.x;

			// matrix[1, 0] = 0;
			matrix[1, 1] = 1 / vertical;
			// matrix[1, 2] = 0;
			matrix[1, 3] = 2 * offset.y;

			// matrix[2, 0] = 0;
			// matrix[2, 1] = 0;
			matrix[2, 2] = 2 / (n - f);
			matrix[2, 3] = (f + n) / (n - f);

			// matrix[3, 0] = 0;
			// matrix[3, 1] = 0;
			// matrix[3, 2] = 0;
			matrix[3, 3] = 1;

			return matrix;
		}

		static readonly Vector2[] HaltonSequence = {
			new Vector2(0.500000000f, 0.333333333f),
			new Vector2(0.250000000f, 0.666666667f),
			new Vector2(0.750000000f, 0.111111111f),
			new Vector2(0.125000000f, 0.444444444f),
			new Vector2(0.625000000f, 0.777777778f),
			new Vector2(0.375000000f, 0.222222222f),
			new Vector2(0.875000000f, 0.555555556f),
			new Vector2(0.062500000f, 0.888888889f),
			new Vector2(0.562500000f, 0.037037037f),
			new Vector2(0.312500000f, 0.370370370f),
			new Vector2(0.812500000f, 0.703703704f),
			new Vector2(0.187500000f, 0.148148148f),
			new Vector2(0.687500000f, 0.481481481f),
			new Vector2(0.437500000f, 0.814814815f),
			new Vector2(0.937500000f, 0.259259259f),
			new Vector2(0.031250000f, 0.592592593f),
			new Vector2(0.531250000f, 0.925925926f),
			new Vector2(0.281250000f, 0.074074074f),
			new Vector2(0.781250000f, 0.407407407f),
			new Vector2(0.156250000f, 0.740740741f),
			new Vector2(0.656250000f, 0.185185185f),
			new Vector2(0.406250000f, 0.518518519f),
			new Vector2(0.906250000f, 0.851851852f),
			new Vector2(0.093750000f, 0.296296296f),
			new Vector2(0.593750000f, 0.629629630f),
			new Vector2(0.343750000f, 0.962962963f),
			new Vector2(0.843750000f, 0.012345679f),
			new Vector2(0.218750000f, 0.345679012f),
			new Vector2(0.718750000f, 0.679012346f),
			new Vector2(0.468750000f, 0.123456790f),
			new Vector2(0.968750000f, 0.456790123f),
			new Vector2(0.015625000f, 0.790123457f)
		};

		static readonly Vector2[] ps0 = new Vector2[] {
			new Vector2(-0.75f, -0.50f),
			new Vector2( 0.50f, -0.75f),
			new Vector2( 0.75f,  0.50f),
			new Vector2(-0.50f,  0.75f)
		};
		
		static readonly Vector2[] ps = new Vector2[] {
			new Vector2(-0.5f , -0.25f),
			new Vector2( 0.25f, -0.5f ),
			new Vector2( 0.5f ,  0.25f),
			new Vector2(-0.25f,  0.5f )
		};
		
		static readonly Vector2[] psy = new Vector2[] {
			new Vector2(-0.667f, -0.667f),
			new Vector2( 0.250f, -0.25f ),
			new Vector2( 0.667f,  0.25f),
			new Vector2(-0.250f,  0.667f )
		};
		
		static readonly Vector2[] psx = new Vector2[] {
			new Vector2(-1/3f, -1/3f),
			new Vector2( 1/3f, -1/3f),
			new Vector2( 1/3f,  1/3f),
			new Vector2(-1/3f,  1/3f)
		};
        
		static readonly Vector2[] ps4 = new Vector2[] {
			new Vector2( 0.19134f, -0.46194f),
			new Vector2( 0.46194f,  0.19134f),
			new Vector2(-0.19134f,  0.46194f),
			new Vector2(-0.46194f, -0.19134f)
		};
	}

}