using System;
using UnityEngine;
using UnityEngine.Rendering;
using Cat.Common;

//using UnityEngineInternal;
//using UnityEditor;
//using UnityEditorInternal;

namespace Cat.PostProcessing {
	[RequireComponent(typeof(Camera))]
	[ExecuteInEditMode]
	[ImageEffectAllowedInSceneView]
	[AddComponentMenu("Cat/PostProcessing/Screen Space Reflections")]
	public class CatSSR : PostProcessingBaseCommandBuffer {
			public enum DebugMode {
				AppliedReflections = 0,
				AppliedReflectionsAndCubeMap = 1,
				ComposedReflectionsRGB = 2,
				ComposedReflectionsA = 3,
				ReflectionsRGB = 4,
				RayTraceConfidence = 5,
				PDF = 6,
				MipsRGB = 7,
				MipLevel = 8,
		}

		[Serializable]
		public struct Settings {
			[Header("RayTracing")]
			[CustomLabel("Ray Trace Resol.")]
			public TextureResolution	rayTraceResol;

			[CustomLabelRange(16, 256, "Step Count")]
			public int					stepCount;

			public const bool			isExactPixelStride = false;

			[Range(1, 64)]
			public int					minPixelStride;

			[Range(1, 64)]
			public int					maxPixelStride;

			//[Range(0, 1)]
			public const float			noiseStrength = 0.5f;

			public bool					cullBackFaces;

			[CustomLabelRange(1, 100, "Max Refl. Distance")]
			public float				maxReflectionDistance;

			public const bool			upSampleHitTexture = false;


			[Header("Reflections")]
			[CustomLabel("Reflection Resol.")]
			public TextureResolution	reflectionResolution;

			[Range(0, 1)]
			public float				intensity;

			[CustomLabelRange(0, 1, "Distance Fade")]
			public float				reflectionDistanceFade;

			[Range(0, 1)]
			public float				rayLengthFade;

			[CustomLabelRange(0*1e-5f, 1, "Screen Edge Fade")]
			public float				edgeFade;

			[CustomLabel("Retro Reflections")]
			public bool					useRetroReflections;

			//[CustomLabel("Use Mip Map")]
			public bool					useReflectionMipMap { get { return false; } }


			[CustomLabel("Use Import. Sampling")]
			public const bool		useImportanceSampling = true;
			[Header("Importance sampling")]

			[CustomLabelRange(1, 7, "Sample Count")]
			public int					resolveSampleCount;

			[CustomLabelRange(0, 1, "Bias (Spread)")]
			public float				importanceSampleBias;

			[CustomLabel("Use Mip Map")]
			public bool					useCameraMipMap;

			public const bool			suppressFlickering = true;


			[Header("Temporal")]
			[CustomLabel("Use Temporal")]
			public bool					useTemporalSampling;

			[Range(1e-3f, 1)]
			public float				response;

			[Range(0, 5)]
			public float				toleranceMargin;


			[Header("Debugging")]
			public bool					debugOn;

			public DebugMode			debugMode;

			[Range(0, 7)]
			public int					mipLevelForDebug;


			public static Settings defaultSettings { 
				get {
					return new Settings {
						rayTraceResol			= TextureResolution.FullResolution,
						stepCount				= 96,
					//	isExactPixelStride		= false,
						minPixelStride			= 3,
						maxPixelStride			= 12,
						//noiseStrength			= 0.5f,
						cullBackFaces			= true,
						maxReflectionDistance	= 100,
					//	upSampleHitTexture		= false,
						

						reflectionResolution	= TextureResolution.FullResolution,
						intensity				= 1,
						reflectionDistanceFade	= 0.5f,
						rayLengthFade			= 0.25f,
						edgeFade				= 0.125f,
						useRetroReflections		= true,
					//	useReflectionMipMap		= false,
						

					//	useImportanceSampling	= true,
						resolveSampleCount		= 4,
						importanceSampleBias	= 0.75f,
						useCameraMipMap			= true,
					//	suppressFlickering		= true,
						
						
						useTemporalSampling		= true,
						response				= 0.05f,
						toleranceMargin			= 2,

						
						debugOn					= false,
						debugMode				= DebugMode.MipLevel,
						mipLevelForDebug		= 0,
//=======================================================================================
				//		intensity = 1,
				//		maxReflectionDistance = 100,
				//		reflectionDistanceFade = 0.5f,
				//		rayLengthFade = 0.25f,
				//		edgeFade = 0.125f,
				//		useRetroReflections = true,
				//		cullBackFaces = false,
				//		
				//		stepCount = 96,
				//	//	isExactPixelStride = false,
				//		minPixelStride = 3,
				//		maxPixelStride = 12,
				//		noiseStrength = 1,
				//
				//		useMipMap = true,
				//	//	suppressFlickering = false,
				//		useTemporalSampling = true,
				//
				//	//	upSampleHitTexture = true,
				//		rayTraceResol = TextureResolution.FullResolution,
				//		reflectionResolution = TextureResolution.FullResolution,
				//
				//		debugOn = false,
				//		debugMode = DebugMode.MipLevel,
				//		mipLevelForDebug = 0,
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
		private Settings lastSettings;

		private readonly RenderTextureContainer lastFrame = new RenderTextureContainer();
		private readonly RenderTextureContainer history = new RenderTextureContainer();

	//	[SerializeField]
		private VectorInt2 rayTraceRTSize = VectorInt2.zero;
	//	[SerializeField]
		private VectorInt2 HitTextureSize = VectorInt2.zero;
	//	[SerializeField]
		private VectorInt2 reflRTSize = VectorInt2.zero;

		private bool isFirstFrame = true;

		override protected CameraEvent cameraEvent { 
			get { return CameraEvent.BeforeImageEffectsOpaque; }
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
			internal static readonly int StepCount_i					= Shader.PropertyToID("_StepCount");
			internal static readonly int MinPixelStride_i				= Shader.PropertyToID("_MinPixelStride");
			internal static readonly int MaxPixelStride_i				= Shader.PropertyToID("_MaxPixelStride");
			internal static readonly int NoiseStrength_f				= Shader.PropertyToID("_NoiseStrength");
			internal static readonly int CullBackFaces_b				= Shader.PropertyToID("_CullBackFaces");
			internal static readonly int MaxReflectionDistance_f		= Shader.PropertyToID("_MaxReflectionDistance");
			// rayTraceResol
			// upSampleHitTexture

			internal static readonly int Intensity_f					= Shader.PropertyToID("_Intensity");
			internal static readonly int ReflectionDistanceFade_f		= Shader.PropertyToID("_ReflectionDistanceFade");
			internal static readonly int RayLengthFade_f				= Shader.PropertyToID("_RayLengthFade");
			internal static readonly int EdgeFade_f						= Shader.PropertyToID("_EdgeFade");
			internal static readonly int UseRetroReflections_b			= Shader.PropertyToID("_UseRetroReflections");
			internal static readonly int UseReflectionMipMap_b			= Shader.PropertyToID("_UseReflectionMipMap");
			// reflectionResolution

			// useImportanceSampling
			internal static readonly int ResolveSampleCount_i			= Shader.PropertyToID("_ResolveSampleCount");
			internal static readonly int ImportanceSampleBias_f			= Shader.PropertyToID("_ImportanceSampleBias");
			internal static readonly int UseCameraMipMap_b				= Shader.PropertyToID("_UseCameraMipMap");
			// suppressFlickering	

			// useTemporalSampling
			internal static readonly int Response_f						= Shader.PropertyToID("_Response");
			internal static readonly int ToleranceMargin_f				= Shader.PropertyToID("_ToleranceMargin");

			// debugOn
			internal static readonly int DebugMode_i					= Shader.PropertyToID("_DebugMode");
			internal static readonly int MipLevelForDebug_i				= Shader.PropertyToID("_MipLevelForDebug");


			internal static readonly int IsVelocityPredictionEnabled_b	= Shader.PropertyToID("_IsVelocityPredictionEnabled");
			internal static readonly int FrameCounter_f					= Shader.PropertyToID("_FrameCounter");
			internal static readonly int BlurDir_v						= Shader.PropertyToID("_BlurDir");
			internal static readonly int MipLevel_f						= Shader.PropertyToID("_MipLevel");
			internal static readonly int PixelsPerMeterAtOneMeter_f		= Shader.PropertyToID("_PixelsPerMeterAtOneMeter");

			internal static readonly int Hit_t							= Shader.PropertyToID("_HitTex");
			internal static readonly int History_t						= Shader.PropertyToID("_HistoryTex");
			internal static readonly int Refl_t							= Shader.PropertyToID("_ReflectionsTex");

			internal static readonly int normalsPacked_t				= Shader.PropertyToID("_NormalsPacked");
			internal static readonly int Depth_t						= Shader.PropertyToID("_DepthTexture");
			internal static readonly int blueNoise_t					= Shader.PropertyToID("_BlueNoise");

			internal static readonly int tempBuffer0_t					= Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced0x");
			internal static readonly int tempBuffer1_t					= Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced1x");
			internal static readonly int[] tempBuffers_t				= new int[] {
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced1"),
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced2"),
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced3"),
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced4"),
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced5"),
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced6"),
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced7"),
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced8"),
			};
		}

		override protected void UpdateRenderTextures(Camera camera, VectorInt2 cameraSize) {
			// Get RenderTexture sizes:
			rayTraceRTSize = cameraSize * settings.rayTraceResol;
			reflRTSize = cameraSize * settings.reflectionResolution;
			HitTextureSize = Settings.upSampleHitTexture ? reflRTSize : rayTraceRTSize;

			CreateCopyRT(lastFrame, reflRTSize, 0, settings.useCameraMipMap, RenderTextureFormat.DefaultHDR, FilterMode.Trilinear, RenderTextureReadWrite.Default, TextureWrapMode.Clamp, "lastFrame");
			CreateRT(    history,   reflRTSize, 0, settings.useReflectionMipMap, RenderTextureFormat.DefaultHDR, FilterMode.Bilinear, RenderTextureReadWrite.Default, TextureWrapMode.Clamp, "history");
		//	material.SetTexture(PropertyIDs.History_t, history);
			material.SetTexture(PropertyIDs.Refl_t, history);
			isFirstFrame = true;
			setBufferDirty();
		}

		Vector2 frameCounter = new Vector2(0, 0);
		Vector2 frameCounterHelper = new Vector2(0.618256431f, +1-Mathf.Sqrt(2));//0.618256431f);
		override protected void UpdateMaterialPerFrame(Material material, Camera camera, VectorInt2 cameraSize) {
			var isSceneView = postProcessingManager.isSceneView;
			if (!isSceneView) {
				//frameCounter = (frameCounter + 0.618256431f) % 172.5f;
				frameCounter = frameCounter + frameCounterHelper;
				frameCounter.Set(frameCounter.y % 172.5f, frameCounter.x % 172.5f);
				frameCounterHelper.Set(frameCounterHelper.y, frameCounterHelper.x);
			}
			material.SetVector(PropertyIDs.FrameCounter_f, frameCounter);

			float pixelsPerMeterAtOneMeter = reflRTSize.x / (-2.0f * (float)(Mathf.Tan(camera.fieldOfView / 180.0f * Mathf.PI * 0.5f)));
			material.SetFloat(PropertyIDs.PixelsPerMeterAtOneMeter_f, pixelsPerMeterAtOneMeter);

			setMaterialDirty();
		}

		override protected void UpdateMaterial(Material material, Camera camera, VectorInt2 cameraSize) {
			var settings = this.settings;
			var isSceneView = postProcessingManager.isSceneView;
		//	Shader.EnableKeyword("CAT_SSR_ON"); 
			if (settings.useTemporalSampling) {
				material.EnableKeyword("CAT_TEMPORAL_SSR_ON"); 
			} else {
				material.DisableKeyword("CAT_TEMPORAL_SSR_ON");
			}
			material.SetTexture(PropertyIDs.Refl_t, history);

			material.SetFloat(PropertyIDs.MaxReflectionDistance_f, settings.maxReflectionDistance);
			material.SetInt(PropertyIDs.StepCount_i, settings.stepCount);
			// isExactPixelStride
			var maxStride = Settings.isExactPixelStride ? settings.minPixelStride : settings.maxPixelStride;
			var minPixelStride = Math.Min(settings.minPixelStride, maxStride);
			var maxPixelStride = Math.Max(settings.minPixelStride, maxStride);
			material.SetFloat(PropertyIDs.MinPixelStride_i, minPixelStride);
			material.SetFloat(PropertyIDs.MaxPixelStride_i, maxPixelStride);
			material.SetFloat(PropertyIDs.NoiseStrength_f, Settings.noiseStrength);
			material.SetFloat(PropertyIDs.CullBackFaces_b, settings.cullBackFaces ? 1 : 0);
			// rayTraceResol
			// upSampleHitTexture

			material.SetInt(PropertyIDs.UseRetroReflections_b, settings.useRetroReflections ? 1 : 0);
			material.SetFloat(PropertyIDs.Intensity_f, settings.intensity);
			material.SetFloat(PropertyIDs.ReflectionDistanceFade_f, settings.reflectionDistanceFade);
			material.SetFloat(PropertyIDs.RayLengthFade_f, settings.rayLengthFade);
			material.SetFloat(PropertyIDs.EdgeFade_f, settings.edgeFade);
			material.SetFloat(PropertyIDs.UseReflectionMipMap_b, settings.useReflectionMipMap ? 1 : 0);

			// reflectionResolution

			// useImportanceSampling
			material.SetInt(PropertyIDs.ResolveSampleCount_i, settings.resolveSampleCount);
			material.SetFloat(PropertyIDs.ImportanceSampleBias_f, settings.importanceSampleBias);
			material.SetFloat(PropertyIDs.UseCameraMipMap_b, settings.useCameraMipMap ? 1 : 0);
			// suppressFlickering

			// useTemporalSampling
			material.SetFloat(PropertyIDs.Response_f, settings.response);
			material.SetFloat(PropertyIDs.ToleranceMargin_f, settings.toleranceMargin);

			// debugOn
			material.SetInt(PropertyIDs.DebugMode_i, (int)settings.debugMode);
			material.SetInt(PropertyIDs.MipLevelForDebug_i, settings.mipLevelForDebug);


			material.SetTexture(PropertyIDs.blueNoise_t, PostProcessingManager.blueNoiseTexture);

			var allowVelocityPrediction = true && !isSceneView;
			material.SetInt(PropertyIDs.IsVelocityPredictionEnabled_b, allowVelocityPrediction ? 1 : 0);

		}

		private enum SSRPass {
			RayTrace            = 0,
			ResolveAdvanced,
			CombineTemporal,
			MipMapBlur,
			ComposeAndApplyReflections,
			Debug,
			Depth,
			ReflectionBlur,
			ComposeAndApplyReflections2,
			/*
			ResolveHitPoints    ,
			SimpleBlur          ,
		//	ComposeReflections  ,
		//	ApplyReflections    ,
			UpsampleRayHits     ,
			PackNormals         ,
			*/
		}

		override protected void PopulateCommandBuffer(CommandBuffer buffer, Material material, VectorInt2 cameraSize) {
			var isSceneView = postProcessingManager.isSceneView;

			var mipRTSizes = new VectorInt2[8] {
				reflRTSize / 1,
				reflRTSize / 2,
				reflRTSize / 4,
				reflRTSize / 8,
				reflRTSize / 16,
				reflRTSize / 32,
				reflRTSize / 64,
				reflRTSize / 128,
			};
			var mipTempSizes = new VectorInt2[8] {
				reflRTSize / 1,
				new VectorInt2(reflRTSize.x /  1, reflRTSize.y / 2),
				new VectorInt2(reflRTSize.x /  2, reflRTSize.y / 4),
				new VectorInt2(reflRTSize.x /  4, reflRTSize.y / 8),
				new VectorInt2(reflRTSize.x /  8, reflRTSize.y / 16),
				new VectorInt2(reflRTSize.x / 16, reflRTSize.y / 32),
				new VectorInt2(reflRTSize.x / 32, reflRTSize.y / 64),
				new VectorInt2(reflRTSize.x / 64, reflRTSize.y / 128),
			};

		//	#region Depth
		//	GetTemporaryRT(buffer, PropertyIDs.Depth_t, HitTextureSize, RenderTextureFormat.RHalf, FilterMode.Point, RenderTextureReadWrite.Linear);
		//	Blit(buffer, PropertyIDs.Depth_t, material, (int)SSRPass.Depth);
		//	#endregion

			#region RayTrace
			GetTemporaryRT(buffer, PropertyIDs.Hit_t, HitTextureSize, RenderTextureFormat.ARGBHalf, FilterMode.Bilinear, RenderTextureReadWrite.Linear);
			Blit(buffer, PropertyIDs.Hit_t, material, (int)SSRPass.RayTrace);
			#endregion

			#region RetroReflection
			if (isFirstFrame || !settings.useRetroReflections) {
				Blit(buffer, BuiltinRenderTextureType.CameraTarget, lastFrame);
				if (isFirstFrame) {
					isFirstFrame = false;
					setBufferDirty();
				}
			}
			#endregion
	
			#region CameraMipLevels
			var maxCameraMipLvl = settings.useCameraMipMap ? 8 : 1;
			for (int i = 1; i < maxCameraMipLvl; ++i) {
				GetTemporaryRT(buffer, PropertyIDs.tempBuffers_t[i], mipTempSizes[i], RenderTextureFormat.ARGBHalf, FilterMode.Bilinear, RenderTextureReadWrite.Linear);
				var pass = SSRPass.MipMapBlur;//settings.suppressFlickering && i == 1 ? SSRPass.MipMapBlurComressor : SSRPass.MipMapBlurVanilla;
				buffer.SetGlobalFloat(PropertyIDs.MipLevel_f, i-1);
	
				buffer.SetGlobalVector(PropertyIDs.BlurDir_v, new Vector4(0, 2.25f/mipRTSizes[i-1].y, 0, -2.25f/mipRTSizes[i-1].y));
				Blit(buffer, lastFrame, PropertyIDs.tempBuffers_t[i], material, (int)pass);
	
				buffer.SetGlobalVector(PropertyIDs.BlurDir_v, new Vector4(2.25f/mipRTSizes[i-1].x, 0, -2.25f/mipRTSizes[i-1].x, 0));
				buffer.SetGlobalTexture("_MainTex", PropertyIDs.tempBuffers_t[i]);
				buffer.SetRenderTarget(lastFrame, i);
				Blit(buffer, material, (int)pass);
	
				ReleaseTemporaryRT(buffer, PropertyIDs.tempBuffers_t[i]);	// release temporary RT
			}
			#endregion
	
			#region Resolve / CombineTemporal
			var useCombineTemporal = settings.useTemporalSampling && !isSceneView;
		//	GetT|emporaryRT(buffer, PropertyIDs.Refl_t, mipRTSizes[0], RenderTextureFormat.ARGBHalf, FilterMode.Bilinear, RenderTextureReadWrite.Linear);
	
			if (useCombineTemporal) {
				GetTemporaryRT(buffer, PropertyIDs.tempBuffer0_t, reflRTSize, RenderTextureFormat.ARGBHalf, FilterMode.Bilinear, RenderTextureReadWrite.Linear);
				Blit(buffer, lastFrame, PropertyIDs.tempBuffer0_t, material, (int)SSRPass.ResolveAdvanced);
				//	buffer.SetGlobalTexture(PropertyIDs.History_t, history);
				GetTemporaryRT(buffer, PropertyIDs.tempBuffer1_t, reflRTSize, RenderTextureFormat.ARGBHalf, FilterMode.Bilinear, RenderTextureReadWrite.Linear);
				Blit(buffer, PropertyIDs.tempBuffer0_t, PropertyIDs.tempBuffer1_t, material, (int)SSRPass.CombineTemporal);
				ReleaseTemporaryRT(buffer, PropertyIDs.tempBuffer0_t);
				Blit(buffer, PropertyIDs.tempBuffer1_t, history);
				ReleaseTemporaryRT(buffer, PropertyIDs.tempBuffer1_t);
			//	Blit(buffer, PropertyIDs.Refl_t, history);
			} else {
				Blit(buffer, lastFrame, history, material, (int)SSRPass.ResolveAdvanced);
			}
			#endregion
	
		//	#region ReflectionMipLevels
		//	var maxReflectionMipLvl = settings.useReflectionMipMap ? 5 : 1;
		//	for (int i = 1; i < maxReflectionMipLvl; ++i) {
		//		GetT|emporaryRT(buffer, PropertyIDs.tempBuffers_t[i], mipTempSizes[i], RenderTextureFormat.ARGBHalf, FilterMode.Bilinear, RenderTextureReadWrite.Linear);
		//		var pass = SSRPass.ReflectionBlur;//settings.suppressFlickering && i == 1 ? SSRPass.MipMapBlurComressor : SSRPass.MipMapBlurVanilla;
		//		buffer.SetGlobalFloat(PropertyIDs.MipLevel_f, i-1);
		//
		//		buffer.SetGlobalVector(PropertyIDs.BlurDir_v, new Vector4(0, 2.25f/mipRTSizes[i-1].y, 0, -2.25f/mipRTSizes[i-1].y));
		//		Blit(buffer, history, PropertyIDs.tempBuffers_t[i], material, (int)pass);
		//
		//		buffer.SetGlobalVector(PropertyIDs.BlurDir_v, new Vector4(2.25f/mipRTSizes[i-1].x, 0, -2.25f/mipRTSizes[i-1].x, 0));
		//		buffer.SetGlobalTexture("_MainTex", PropertyIDs.tempBuffers_t[i]);
		//		buffer.SetRenderTarget(history, i);
		//		Blit(buffer, material, (int)pass);
		//
		//		ReleaseT|emporaryRT(buffer, PropertyIDs.tempBuffers_t[i]);	// release temporary RT
		//	}
		//	#endregion
	
			#region ComposeAndApplyReflections
			Blit(buffer, BuiltinRenderTextureType.CameraTarget, material, (int)SSRPass.ComposeAndApplyReflections);
			#endregion
	
			#region RetroReflection
			if (settings.useRetroReflections) {
				Blit(buffer, BuiltinRenderTextureType.CameraTarget, lastFrame);
			}                   
			#endregion

			#region Debug
			if (settings.debugOn) {
				Blit(buffer, lastFrame, BuiltinRenderTextureType.CameraTarget, material, (int)SSRPass.Debug);
			}
			#endregion

		//	ReleaseT|emporaryRT(buffer, PropertyIDs.Refl_t);
			ReleaseTemporaryRT(buffer, PropertyIDs.Hit_t);
			//	ReleaseTemporaryRT(buffer, PropertyIDs.Depth_t);

			#if USE_ADVANCED_MATERIAL_SHADING
			buffer.GetT|emporaryRT(PropertyIDs.normalsPacked_t, cameraSize.x, cameraSize.y, 0, FilterMode.Point, RenderTextureFormat.ARGB2101010, RenderTextureReadWrite.Linear, 1);
			#endif
		
			#if USE_ADVANCED_MATERIAL_SHADING
			buffer.ReleaseT|emporaryRT(PropertyIDs.normalsPacked_t);
			#endif

		}
			
		public void OnValidate () {
			setMaterialDirty();
			if (m_Settings.rayTraceResol != lastSettings.rayTraceResol
			|| (m_Settings.reflectionResolution != lastSettings.reflectionResolution)
			|| (m_Settings.useTemporalSampling != lastSettings.useTemporalSampling)
			|| (m_Settings.useCameraMipMap != lastSettings.useCameraMipMap)
			|| (m_Settings.useReflectionMipMap != lastSettings.useReflectionMipMap)) {
				setRenderTextureDirty();
			}
			if (m_Settings.debugOn != lastSettings.debugOn
		//	|| (m_Settings.suppressFlickering != lastSettings.suppressFlickering)
			|| (m_Settings.useTemporalSampling != lastSettings.useTemporalSampling)
			|| (m_Settings.useRetroReflections != lastSettings.useRetroReflections)) {
				setBufferDirty();
			}
			lastSettings = m_Settings;
		}
	}

}
