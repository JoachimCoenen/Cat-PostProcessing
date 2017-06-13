using System;
using UnityEngine;
using UnityEngine.Rendering;
using Cat.Common;

//using UnityEngineInternal;
//using UnityEditor;
//using UnityEditorInternal;

namespace Cat.PostProcessing {
	[RequireComponent(typeof (Camera))]
	[ExecuteInEditMode]
	[ImageEffectAllowedInSceneView]
	[AddComponentMenu("Cat/PostProcessing/Screen Space Reflections")]
	public class CatSSR : PostProcessingBaseCommandBuffer {
		[Range(0, 1)]		public	float Intensity = 1;
		[Range(1, 100)]		public float maxReflectionDistance = 100;
		[Range(0, 1)]		public float reflectionDistanceFade = 0.25f;
		[Range(0, 1)]		public float rayLengthFade = 0.25f;
		[Range(1e-3f, 1)]	public float reflectionEdgeFactor = 0.25f;
							public const bool cullBackFaces = false;
							[Space(15)]
		[Range(16, 256)]	public int numSteps = 120;
							public bool isExactPixelStride = false;
		[Range(1, 32)]		public int minPixelStride = 3;
		[Range(1, 32)]		public int maxPixelStride = 16;
		[Range(0, 1)]		public float noiseStrength = 0f;
							[Space(15)]
							public bool smoothReflections = true;
		[Range(0, 1)]		public const float simpleBlurStrength = 0.75f;
							public bool useMipMap = true;
							public bool useTemporalSampling = true;
						//	[Space(15)]
							[Space(15)]
							public const bool upSampleHitTexture = true;
							public int rayTraceDownSample = 1;
							public int reflectionDownSample = 1;
							public const int temporalDownSample = 1;
							[Space(15)]
							public bool debugOn = false;
							public DebugMode debugMode;
		[Range(0, 4)]		public int mipLevelForDebug = 1;


		public enum DebugMode {
			ComposedReflectionsRGB = 0,
			ComposedReflectionsA = 1,
			MipsRGB = 2,
			MipsA = 3,
			MipLevel = 4,
			RayLength = 5,
			RayTraceConfidence = 6,
			AppliedReflections = 7
		}
	//	private Matrix4x4 projectionMatrix;
	//	private Matrix4x4 viewProjectionMatrix;
	//	private Matrix4x4 inverseViewProjectionMatrix;
	//	private Matrix4x4 worldToCameraMatrix;
		
		private RenderTexture lastFrame;

		private VectorInt2 temporalRTSize;
		private VectorInt2 rayTraceRTSize;
		private VectorInt2 HitTextureSize;
		private VectorInt2 reflRTSize;
		/*{
			get {
			#if UNITY_EDITOR
				if (m_camera == null) {
					return VectorInt2.zero;
				}
				var isSceneView = UnityEditor.SceneView.currentDrawingSceneView != null && UnityEditor.SceneView.currentDrawingSceneView.camera == m_camera;
				m_camera.tex
				return new VectorInt2(m_camera.activeTexture.width, m_camera.activeTexture.height);
			#else
				return (m_camera == null) ? VectorInt2.zero : new VectorInt2(m_camera.pixelWidth, m_camera.pixelHeight); 
			#endif
			}
		}*/

		/*
		private int IDHit_t;
		private int IDCompRefl_t;
		private int[] IDMips_t;
		private int IDBlurDir_v;
		private int IDUseMips_b		;
		//private int IDSmoothnessRange_f;
		private int IDRayLengthFade_f;
		private int IDEdgeFactor_f;
		private int IDMaxReflectionDistance_f;
		private int IDReflectionDistanceFade_f;
		private int IDNumSteps_i;
		private int IDIntensity_f;

		private int IDDebugMode_i;
		private int IDMipLevelForDebug_i;

		private int IDPixelsPerMeterAtOneMeter_f;

		private int IDViewProjectionMatrix_m;
		private int IDInverseProjectionMatrix_m;
		private int IDInverseViewProjectionMatrix_m;

		private int IDnormalsPacked_t;
//		private int normalsMipID;
		private int IDtempBuffer0_t;
		private int IDtempBuffer1_t;
		private int IDtempBuffer2_t;
		private int[] IDtempBuffers_t;
		*/

		override protected CameraEvent cameraEvent { 
			get { return CameraEvent.AfterImageEffectsOpaque; }
		}
		override protected string shaderName { 
			get { return "Hidden/Cat SSR"; } 
		}
		override public string effectName { 
			get { return "Screen Space Reflections"; } 
		}
		override internal DepthTextureMode requiredDepthTextureMode { 
			get { return DepthTextureMode.Depth | DepthTextureMode.MotionVectors; } 
		}
		override public bool isActive { 
			get { return true; } 
		}



		static class PropertyIDs {
			/*
			internal static readonly int _RayStepSize                = Shader.PropertyToID("_RayStepSize");
			internal static readonly int _AdditiveReflection         = Shader.PropertyToID("_AdditiveReflection");
			internal static readonly int _BilateralUpsampling        = Shader.PropertyToID("_BilateralUpsampling");
			internal static readonly int _TreatBackfaceHitAsMiss     = Shader.PropertyToID("_TreatBackfaceHitAsMiss");
			internal static readonly int _AllowBackwardsRays         = Shader.PropertyToID("_AllowBackwardsRays");
			internal static readonly int _TraceBehindObjects         = Shader.PropertyToID("_TraceBehindObjects");
			internal static readonly int _MaxSteps                   = Shader.PropertyToID("_MaxSteps");
			internal static readonly int _FullResolutionFiltering    = Shader.PropertyToID("_FullResolutionFiltering");
			internal static readonly int _HalfResolution             = Shader.PropertyToID("_HalfResolution");
			internal static readonly int _HighlightSuppression       = Shader.PropertyToID("_HighlightSuppression");
			internal static readonly int _PixelsPerMeterAtOneMeter   = Shader.PropertyToID("_PixelsPerMeterAtOneMeter");
			internal static readonly int _ScreenEdgeFading           = Shader.PropertyToID("_ScreenEdgeFading");
			internal static readonly int _ReflectionBlur             = Shader.PropertyToID("_ReflectionBlur");
			internal static readonly int _MaxRayTraceDistance        = Shader.PropertyToID("_MaxRayTraceDistance");
			internal static readonly int _FadeDistance               = Shader.PropertyToID("_FadeDistance");
			internal static readonly int _LayerThickness             = Shader.PropertyToID("_LayerThickness");
			internal static readonly int _SSRMultiplier              = Shader.PropertyToID("_SSRMultiplier");
			internal static readonly int _FresnelFade                = Shader.PropertyToID("_FresnelFade");
			internal static readonly int _FresnelFadePower           = Shader.PropertyToID("_FresnelFadePower");
			internal static readonly int _ReflectionBufferSize       = Shader.PropertyToID("_ReflectionBufferSize");
			internal static readonly int _ScreenSize                 = Shader.PropertyToID("_ScreenSize");
			internal static readonly int _InvScreenSize              = Shader.PropertyToID("_InvScreenSize");
			internal static readonly int _ProjInfo                   = Shader.PropertyToID("_ProjInfo");
			internal static readonly int _CameraClipInfo             = Shader.PropertyToID("_CameraClipInfo");
			internal static readonly int _ProjectToPixelMatrix       = Shader.PropertyToID("_ProjectToPixelMatrix");
			internal static readonly int _WorldToCameraMatrix        = Shader.PropertyToID("_WorldToCameraMatrix");
			internal static readonly int _CameraToWorldMatrix        = Shader.PropertyToID("_CameraToWorldMatrix");
			internal static readonly int _Axis                       = Shader.PropertyToID("_Axis");
			internal static readonly int _CurrentMipLevel            = Shader.PropertyToID("_CurrentMipLevel");
			internal static readonly int _NormalAndRoughnessTexture  = Shader.PropertyToID("_NormalAndRoughnessTexture");
			internal static readonly int _HitPointTexture            = Shader.PropertyToID("_HitPointTexture");
			internal static readonly int _BlurTexture                = Shader.PropertyToID("_BlurTexture");
			internal static readonly int _FilteredReflections        = Shader.PropertyToID("_FilteredReflections");
			internal static readonly int _FinalReflectionTexture     = Shader.PropertyToID("_FinalReflectionTexture");
			internal static readonly int _TempTexture                = Shader.PropertyToID("_TempTexture");
			*/
			internal static readonly int Hit_t							= Shader.PropertyToID("_HitTex");
			internal static readonly int CompRefl_t						= Shader.PropertyToID("_ComposedReflections");
			internal static readonly int[] Mips_t = new int[] {
				Shader.PropertyToID("_ReflectionMip0"),
				Shader.PropertyToID("_ReflectionMip1"),
				Shader.PropertyToID("_ReflectionMip2"),
				Shader.PropertyToID("_ReflectionMip3"),
				Shader.PropertyToID("_ReflectionMip4"),
			};
			internal static readonly int normalsPacked_t				= Shader.PropertyToID("_NormalsPacked");

		//	internal static readonly int ViewProjectionMatrix_m			= Shader.PropertyToID("_ViewProjectionMatrix");
		//	internal static readonly int InverseViewProjectionMatrix_m	= Shader.PropertyToID("_InverseViewProjectionMatrix");

			internal static readonly int MaxReflectionDistance_f		= Shader.PropertyToID("_MaxReflectionDistance");
			internal static readonly int NumSteps_i						= Shader.PropertyToID("_NumSteps");
			internal static readonly int MinPixelStride_i				= Shader.PropertyToID("_MinPixelStride");
			internal static readonly int MaxPixelStride_i				= Shader.PropertyToID("_MaxPixelStride");
			internal static readonly int NoiseStrength_f				= Shader.PropertyToID("_NoiseStrength");

			internal static readonly int IsBackFaceCullingEnabled_b		= Shader.PropertyToID("_IsBackFaceCullingEnabled");
			internal static readonly int ReflectionDistanceFade_f		= Shader.PropertyToID("_ReflectionDistanceFade");
			internal static readonly int RayLengthFade_f				= Shader.PropertyToID("_RayLengthFade");
			internal static readonly int EdgeFactor_f					= Shader.PropertyToID("_EdgeFactor");

			internal static readonly int IsVelocityPredictionEnabled_b	= Shader.PropertyToID("_IsVelocityPredictionEnabled");

			internal static readonly int BlurDir_v						= Shader.PropertyToID("_BlurDir");
			internal static readonly int PixelsPerMeterAtOneMeter_f		= Shader.PropertyToID("_PixelsPerMeterAtOneMeter");
			internal static readonly int UseMips_b						= Shader.PropertyToID("_UseMips");
			internal static readonly int Intensity_f					= Shader.PropertyToID("_Intensity");

			internal static readonly int DebugMode_i					= Shader.PropertyToID("_DebugMode");
			internal static readonly int MipLevelForDebug_i				= Shader.PropertyToID("_MipLevelForDebug");

			//internal static readonly int normalsMipID					= Shader.PropertyToID("_NormalsMip");
			internal static readonly int tempBuffer0_t					= Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced0x");
			internal static readonly int tempBuffer1_t					= Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced1x");
			internal static readonly int tempBuffer2_t					= Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced0xNo2");
			internal static readonly int[] tempBuffers_t				= new int[] {
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced1"),
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced2"),
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced3"),
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced4"),
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced5"),
			};
		}

		/*
		void getShaderPropertyIDs() {
			IDHit_t				= Shader.PropertyToID("_HitTex");
			IDCompRefl_t		= Shader.PropertyToID("_ComposedReflections");
			IDMips_t = new int[] {
				Shader.PropertyToID("_ReflectionMip0"),
				Shader.PropertyToID("_ReflectionMip1"),
				Shader.PropertyToID("_ReflectionMip2"),
				Shader.PropertyToID("_ReflectionMip3"),
				Shader.PropertyToID("_ReflectionMip4"),
			};
			IDBlurDir_v						= Shader.PropertyToID("_BlurDir");
			IDUseMips_b						= Shader.PropertyToID("_UseMips");
		//	IDSmoothnessRange_f				= Shader.PropertyToID("_SmoothnessRange");
			IDRayLengthFade_f				= Shader.PropertyToID("_RayLengthFade");
			IDEdgeFactor_f					= Shader.PropertyToID("_EdgeFactor");
			IDMaxReflectionDistance_f		= Shader.PropertyToID("_MaxReflectionDistance");
			IDReflectionDistanceFade_f		= Shader.PropertyToID("_ReflectionDistanceFade");

			IDNumSteps_i					= Shader.PropertyToID("_NumSteps");
			IDIntensity_f					= Shader.PropertyToID("_Intensity");

			IDDebugMode_i					= Shader.PropertyToID("_DebugMode");
			IDMipLevelForDebug_i			= Shader.PropertyToID("_MipLevelForDebug");

			IDPixelsPerMeterAtOneMeter_f 	= Shader.PropertyToID("_PixelsPerMeterAtOneMeter");

			IDViewProjectionMatrix_m		= Shader.PropertyToID("_ViewProjectionMatrix");
			IDInverseProjectionMatrix_m		= Shader.PropertyToID("_InverseProjectionMatrix");
			IDInverseViewProjectionMatrix_m	= Shader.PropertyToID("_InverseViewProjectionMatrix");

			IDnormalsPacked_t		= Shader.PropertyToID("_NormalsPacked");
			//normalsMipID			= Shader.PropertyToID("_NormalsMip");
			IDtempBuffer0_t			= Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced0x");
			IDtempBuffer1_t			= Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced1x");
			IDtempBuffer2_t			= Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced0xNo2");
			IDtempBuffers_t			= new int[] {
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced1"),
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced2"),
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced3"),
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced4"),
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced5"),
			};
		}
		*/


		override protected void UpdateRenderTextures(VectorInt2 cameraSize) {
			// Get RenderTexture sizes:
			rayTraceRTSize = cameraSize / rayTraceDownSample;
			reflRTSize = cameraSize / reflectionDownSample;
			HitTextureSize = upSampleHitTexture ? reflRTSize : rayTraceRTSize;
			temporalRTSize = cameraSize / temporalDownSample;

        //    ReleaseRT(ref lastFrame);
			if (useTemporalSampling) {
				CreateRT(ref lastFrame, temporalRTSize, RenderTextureFormat.DefaultHDR, FilterMode.Bilinear);
			}
			setBufferDirty();
		}

		override protected void UpdateMaterialPerFrame(Material material) {
			var camera = this.camera;
		//	worldToCameraMatrix = camera.worldToCameraMatrix;

		//	projectionMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
		//	viewProjectionMatrix = projectionMatrix * worldToCameraMatrix;
		//	inverseViewProjectionMatrix = viewProjectionMatrix.inverse;

		//	ssrMaterial.SetMatrix("_ProjectionMatrix", projectionMatrix);
		//	material.SetMatrix(PropertyIDs.ViewProjectionMatrix_m, viewProjectionMatrix);
		//	material.SetMatrix(PropertyIDs.InverseViewProjectionMatrix_m, inverseViewProjectionMatrix);

			// The height in pixels of a 1m object if viewed from 1m away.
			float pixelsPerMeterAtOneMeter = reflRTSize.x / (-2.0f * (float)(Mathf.Tan(camera.fieldOfView / 180.0f * Mathf.PI * 0.5f)));
			material.SetFloat(PropertyIDs.PixelsPerMeterAtOneMeter_f, pixelsPerMeterAtOneMeter);
		//	material.SetFloat("_PixelsPerMeterAtOneMeter", pixelsPerMeterAtOneMeter);

			
		}

		override protected void UpdateMaterial(Material material) {
		//	Shader.EnableKeyword("CAT_SSR_ON"); 
			if (useTemporalSampling) {
				material.EnableKeyword("CAT_TEMPORAL_SSR_ON"); 
			} else {
				material.DisableKeyword("CAT_TEMPORAL_SSR_ON");
			}
		//	Shader.DisableKeyword("CAT_SSR_ON");

			material.SetFloat(PropertyIDs.Intensity_f, Intensity);
			material.SetFloat(PropertyIDs.RayLengthFade_f, rayLengthFade);
			material.SetFloat(PropertyIDs.EdgeFactor_f, reflectionEdgeFactor);
			material.SetFloat(PropertyIDs.MaxReflectionDistance_f, maxReflectionDistance);
			material.SetFloat(PropertyIDs.ReflectionDistanceFade_f, reflectionDistanceFade);

			material.SetInt(PropertyIDs.NumSteps_i, numSteps);
			material.SetFloat(PropertyIDs.MinPixelStride_i, isExactPixelStride ? minPixelStride : Mathf.Min(minPixelStride, maxPixelStride));
			material.SetFloat(PropertyIDs.MaxPixelStride_i, isExactPixelStride ? minPixelStride : Mathf.Max(minPixelStride, maxPixelStride));
            
			material.SetFloat(PropertyIDs.UseMips_b, useMipMap ? 1f : 0f);

			material.SetFloat(PropertyIDs.IsBackFaceCullingEnabled_b, cullBackFaces ? 1 : 0);
			material.SetFloat(PropertyIDs.NoiseStrength_f, noiseStrength * 0.1f);
		//	ssrMaterial.SetFloat(IDSmoothnessRange_f, simpleBlurStrength);

			var isSceneView = false;
		#if UNITY_EDITOR
			isSceneView = UnityEditor.SceneView.currentDrawingSceneView != null && UnityEditor.SceneView.currentDrawingSceneView.camera == camera;
		#endif
			var allowVelocityPrediction = !isSceneView;
			material.SetInt(PropertyIDs.IsVelocityPredictionEnabled_b, allowVelocityPrediction ? 1 : 0);

			material.SetInt(PropertyIDs.DebugMode_i, (int)debugMode);
			material.SetInt(PropertyIDs.MipLevelForDebug_i, mipLevelForDebug);

		}

		private enum SSRPass {
			RayTrace            = 0,
			ResolveHitPoints    ,
			SimpleBlur          ,
			MipMapBlurComressor ,
			MipMapBlurVanilla   ,
		//	ComposeReflections  ,
		//	ApplyReflections    ,
			Debug               ,
			UpsampleRayHits     ,
			PackNormals         ,
			ComposeAndApplyReflections ,
		}

		override protected void PopulateCommandBuffer(CommandBuffer buffer, Material material) {
			//buffer.DrawMesh(blitQuad, Matrix4x4.identity, material, 0, 0);
			var quad = blitQuad;
			var mipRTSizes = new VectorInt2[5] {
				reflRTSize / 1,
				reflRTSize / 2,
				reflRTSize / 4,
				reflRTSize / 8,
				reflRTSize / 16,
			};
			var maxMipLvl = useMipMap ? 5 : 1;
			#if USE_ADVANCED_MATERIAL_SHADING
			buffer.GetTemporaryRT(PropertyIDs.normalsPacked_t, cameraSize.x, cameraSize.y, 0, FilterMode.Point, RenderTextureFormat.ARGB2101010, RenderTextureReadWrite.Linear, 1);
			#endif
			buffer.GetTemporaryRT(PropertyIDs.Hit_t, HitTextureSize.x, HitTextureSize.y, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear, 1);
			for (int i = 0; i < maxMipLvl; ++i) {
				buffer.GetTemporaryRT(PropertyIDs.Mips_t[i], mipRTSizes[i].x, mipRTSizes[i].y, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear, 1);
			}

			#if USE_ADVANCED_MATERIAL_SHADING
			buffer.Blit(BuiltinRenderTextureType.None, PropertyIDs.normalsPacked_t, material, (int)SSRPass.PackNormals);
			#endif
			if (rayTraceDownSample > reflectionDownSample && upSampleHitTexture) {
				buffer.GetTemporaryRT(PropertyIDs.tempBuffer0_t, rayTraceRTSize.x, rayTraceRTSize.y, 0, FilterMode.Point, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear, 1);
				Blit(buffer, PropertyIDs.tempBuffer0_t, material, (int)SSRPass.RayTrace);
				Blit(buffer, PropertyIDs.tempBuffer0_t, PropertyIDs.Hit_t, material, (int)SSRPass.UpsampleRayHits);
				buffer.ReleaseTemporaryRT(PropertyIDs.tempBuffer0_t);		// release temporary RT
			} else {
				Blit(buffer, PropertyIDs.Hit_t, material, (int)SSRPass.RayTrace);
			}

			if (smoothReflections) {
				buffer.GetTemporaryRT(PropertyIDs.tempBuffers_t[0], reflRTSize.x, reflRTSize.y, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear, 1);
			}
			Blit(buffer, useTemporalSampling ? new RenderTargetIdentifier(lastFrame) : BuiltinRenderTextureType.CameraTarget, 
				smoothReflections ? PropertyIDs.tempBuffers_t[0] : PropertyIDs.Mips_t[0], material, (int)SSRPass.ResolveHitPoints);

			if (smoothReflections) {
				Blit(buffer, PropertyIDs.tempBuffers_t[0], PropertyIDs.Mips_t[0], material, (int)SSRPass.SimpleBlur);
				buffer.ReleaseTemporaryRT(PropertyIDs.tempBuffers_t[0]);
			}

			for (int i = 1; i < maxMipLvl; ++i) {
				buffer.GetTemporaryRT(PropertyIDs.tempBuffers_t[i], mipRTSizes[i - 1].x, mipRTSizes[i - 1].y, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear, 1);
				var pass = i <= 1 ? (int)SSRPass.MipMapBlurComressor : (int)SSRPass.MipMapBlurVanilla;

				buffer.SetGlobalVector(PropertyIDs.BlurDir_v, new Vector2(0, 1));
				Blit(buffer, PropertyIDs.Mips_t[i - 1], PropertyIDs.tempBuffers_t[i], material, pass);

				buffer.SetGlobalVector(PropertyIDs.BlurDir_v, new Vector2(1f, 0f));
				Blit(buffer, PropertyIDs.tempBuffers_t[i], PropertyIDs.Mips_t[i], material, pass);

				buffer.ReleaseTemporaryRT(PropertyIDs.tempBuffers_t[i]);	// release temporary RT
			}

			Blit(buffer, BuiltinRenderTextureType.CameraTarget, material, (int)SSRPass.ComposeAndApplyReflections);

			if (useTemporalSampling) {
				Blit(buffer, BuiltinRenderTextureType.CameraTarget, lastFrame);
			}                   

			if (debugOn) {
				Blit(buffer, lastFrame, BuiltinRenderTextureType.CameraTarget, material, (int)SSRPass.Debug);
			}

			// release temporary RTs
			for (int i = 0; i < maxMipLvl; ++i) {
				buffer.ReleaseTemporaryRT(PropertyIDs.Mips_t[i]);
			}
			buffer.ReleaseTemporaryRT(PropertyIDs.Hit_t);

			#if USE_ADVANCED_MATERIAL_SHADING
			buffer.ReleaseTemporaryRT(PropertyIDs.normalsPacked_t);
			#endif
		}

		void Awake() {
		//	getShaderPropertyIDs();
		}
	
		public void OnValidate () {
		//	getShaderPropertyIDs();
			setMaterialDirty();
			setRenderTextureDirty();
		}
	}

}
