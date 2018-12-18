using System;
using UnityEngine;
using UnityEngine.Rendering;
using Cat.Common;

namespace Cat.PostProcessing {
	public class CatAARenderer : PostProcessingBaseCommandBuffer<CatAA> {
		private const bool disableTAAInSceneView = true;

		private readonly RenderTextureContainer[] history = new[] {new RenderTextureContainer(), new RenderTextureContainer()};

		override protected string shaderName { get { return "Hidden/CatAA"; } }
		override public string effectName { get { return "Cat Temporal Antialialising"; } }
		override internal DepthTextureMode requiredDepthTextureMode { get { return DepthTextureMode.MotionVectors | DepthTextureMode.Depth; } }
		override protected CameraEvent cameraEvent { 
			get { return CameraEvent.BeforeImageEffects; }
		}


		static class PropertyIDs {
			// jitterStrength
			internal static readonly int Sharpness_f					= Shader.PropertyToID("_Sharpness");
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

		override protected void UpdateRenderTextures(Camera camera, VectorInt2 cameraSize) {
			CreateCopyRT(history[0], cameraSize, RenderTextureFormat.DefaultHDR, FilterMode.Bilinear);
			CreateCopyRT(history[1], cameraSize, RenderTextureFormat.DefaultHDR, FilterMode.Bilinear);
			setBufferDirty();
		}

		int TMSAACounter = 0;

		override protected void UpdateCameraMatricesPerFrame(Camera camera, VectorInt2 cameraSize) {
			setBufferDirty();
			var isSceneView = postProcessingManager.isSceneView;
			if (isSceneView && disableTAAInSceneView) {
				return;
			}
			
			Vector2[] jitterVectors = {
				Vector2.zero, Vector2.zero,
				Vector2.zero, Vector2.zero
			};
			switch (settings.jitterMatrix.rawValue) {
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

			var newP = jitterVectors[TMSAACounter];

			if (settings.jitterMatrix == JitterMatrixType.HaltonSequence) {
				newP = newP - new Vector2(0.5f, 0.5f);
			}

			newP *= CatAA.jitterStrength;


			newP.x /= (float)cameraSize.x;
			newP.y /= (float)cameraSize.y;
			
            camera.nonJitteredProjectionMatrix = camera.projectionMatrix;
			if (camera.orthographic) {
				camera.projectionMatrix = GetOrthographicProjectionMatrix(newP, camera);
			} else {
				camera.projectionMatrix = GetPerspectiveProjectionMatrix(newP, camera);
			}
			camera.useJitteredProjectionMatrixForTransparentRendering = false;
			
			Shader.SetGlobalVector(PropertyIDs.TAAJitterVelocity_v, isSceneView ? Vector2.zero : newP);
		}

		override protected void UpdateMaterialPerFrame(Material material, Camera camera, VectorInt2 cameraSize) {
			material.SetVector(PropertyIDs.Directionality_v, new Vector2(TMSAACounter % 2 == 0 ? 1 : -1, 1));
			setMaterialDirty();
		}

		override protected void UpdateMaterial(Material material, Camera camera, VectorInt2 cameraSize) {
			var isSceneView = postProcessingManager.isSceneView;
			material.SetFloat(PropertyIDs.Sharpness_f, settings.sharpness);
			material.SetFloat(PropertyIDs.VelocityWeightScale_f, settings.velocityWeightScale);
			material.SetFloat(PropertyIDs.Response_f, settings.response);
			material.SetFloat(PropertyIDs.ToleranceMargin_f, settings.toleranceMargin);
		}

		internal override void PostRender() {
			postProcessingManager.camera.ResetProjectionMatrix();
		}

		//[ImageEffectTransformsToLDR]
		override protected void PopulateCommandBuffer(CommandBuffer buffer, Material material, VectorInt2 cameraSize) {
			var isSceneView = postProcessingManager.isSceneView;
			if (false || isSceneView && disableTAAInSceneView) {
				//Blit(source, destination);
				return;
			}

			// GetTemporaryRT(buffer, PropertyIDs.Temp_t, cameraSize, RenderTextureFormat.ARGBHalf, FilterMode.Bilinear, RenderTextureReadWrite.Linear);
			// Blit(buffer, BuiltinRenderTextureType.CameraTarget, PropertyIDs.Temp_t, material, 0);
			//var rts = new[] { BuiltinRenderTextureType.CameraTarget, lastFrame1 };

			buffer.SetGlobalTexture(PropertyIDs.History1_t, history[0]);
			Blit(buffer, BuiltinRenderTextureType.CameraTarget, history[1], material, 0);
			Blit(buffer, history[1], BuiltinRenderTextureType.CameraTarget);

			var tmp = history[0];
			history[0] = history[1];
			history[1] = tmp;
			// ReleaseTemporaryRT(buffer, PropertyIDs.Temp_t);

			//postProcessingManager.camera.ResetProjectionMatrix();
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
		Matrix4x4 GetPerspectiveProjectionMatrix(Vector2 offset, Camera camera){
			float vertical = Mathf.Tan(0.5f * Mathf.Deg2Rad * camera.fieldOfView);
			float horizontal = vertical * camera.aspect;

			float n = camera.nearClipPlane;
			float f = camera.farClipPlane;

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
		Matrix4x4 GetOrthographicProjectionMatrix(Vector2 offset, Camera camera) {
			float vertical = camera.orthographicSize;
			float horizontal = vertical * camera.aspect;

			float n = camera.nearClipPlane;
			float f = camera.farClipPlane;

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

	public enum JitterMatrixType {
		ps0, ps, psy, HaltonSequence, psx, ps4
	}

	[Serializable]
	public class JitterMatrixTypeProperty : PropertyOverride<JitterMatrixType> {}

	[Serializable]
	[SettingsForPostProcessingEffect(typeof(CatAARenderer))]
	public class CatAA : PostProcessingSettingsBase {
		override public bool enabled { get { return m_isEnabled.rawValue; } }

		override public string effectName { 
			get { return "Temporal Antialialising"; } 
		}
		override public int queueingPosition {
			get { return 2800; } 
		}

		public BoolProperty m_isEnabled = new BoolProperty();
		//[Range(0.0f, 2.0f)]
		public const float jitterStrength = 1f;

		[Range(0.0f, 2.0f)]
		public FloatProperty sharpness = new FloatProperty();
	
		[CustomLabelRange(0.0f, 80.0f, "Velocity Scale")]
		public FloatProperty velocityWeightScale = new FloatProperty();

		[Range(1e-3f, 1)]
		public FloatProperty response = new FloatProperty();

		[Range(0, 5)]
		public FloatProperty toleranceMargin = new FloatProperty();

		public JitterMatrixTypeProperty jitterMatrix = new JitterMatrixTypeProperty();

		[CustomLabelRange(4, 16, "Halton Seq. Length")]
		public IntProperty haltonSequenceLength = new IntProperty();

		public override void Reset() {
			m_isEnabled.rawValue = false;
			//	jitterStrength.rawValue = 1f,
			sharpness.rawValue = 0.075f;
			velocityWeightScale.rawValue = 40;
			response.rawValue = 0.075f;
			toleranceMargin.rawValue = 1;
			jitterMatrix.rawValue = JitterMatrixType.HaltonSequence;
			haltonSequenceLength.rawValue = 8;
		}
	}
}
