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
	[AddComponentMenu("Cat/PostProcessing/Bloom")]
	public class CatBloom : PostProcessingBaseCommandBuffer {
		public enum DebugMode {
			BloomTextureCompressed = 0,
			BloomTexture = 1,
		//	MipLevel = 2,
		}

		[Serializable]
		public struct Settings {
			[Range(0, 1)]
			public float				minLuminance;
			[Range(0, 4)]
			public float				kneeStrength;

			[Range(0, 1)]
			public float				intensity;

			[Range(2, 32)]
			public int					radius;

			[Header("Debugging")]
			public bool					debugOn;

			public DebugMode			debugMode;

			[Range(0, 4)]
			public float				mipLevelForDebug;


			public static Settings defaultSettings { 
				get {
					return new Settings {
						minLuminance			= 0.5f,
						kneeStrength			= 1,
						intensity				= 1,
						radius					= 16,
						
						debugOn					= false,
						debugMode				= DebugMode.BloomTexture,
						mipLevelForDebug		= 0,
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

		private readonly RenderTextureContainer bloomTexture = new RenderTextureContainer();

		private bool isSecondFrame = false;

		override protected CameraEvent cameraEvent { 
			get { return CameraEvent.BeforeImageEffects; }
		}
		override protected string shaderName { 
			get { return "Hidden/Cat Bloom"; } 
		}
		override public string effectName { 
			get { return "Bloom"; } 
		}
		override internal DepthTextureMode requiredDepthTextureMode { 
			get { return DepthTextureMode.None; } 
		}
		override public bool isActive { 
			get { return true; } 
		}

		static class PropertyIDs {
			internal static readonly int MinLuminance_f			= Shader.PropertyToID("_MinLuminance");
			internal static readonly int KneeStrength_f			= Shader.PropertyToID("_KneeStrength");
			internal static readonly int Intensity_f			= Shader.PropertyToID("_Intensity");
			internal static readonly int MipLevelForRadius_f	= Shader.PropertyToID("_MipLevelForRadius");

			// debugOn
			internal static readonly int DebugMode_i					= Shader.PropertyToID("_DebugMode");
			internal static readonly int MipLevelForDebug_i				= Shader.PropertyToID("_MipLevelForDebug");


			internal static readonly int BlurDir_v						= Shader.PropertyToID("_BlurDir");
			internal static readonly int MipLevel_f						= Shader.PropertyToID("_MipLevel");
		

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

		override protected void UpdateRenderTextures(VectorInt2 cameraSize) {
			CreateRT(bloomTexture, cameraSize, 0, true, RenderTextureFormat.DefaultHDR, FilterMode.Bilinear, RenderTextureReadWrite.Linear, TextureWrapMode.Clamp, "bloomTexture");
			setBufferDirty();
		}

		override protected void UpdateMaterialPerFrame(Material material) {
			setMaterialDirty();
		}

		override protected void UpdateMaterial(Material material) {
			var settings = this.settings;
			material.SetFloat(PropertyIDs.MinLuminance_f, settings.minLuminance);
			material.SetFloat(PropertyIDs.KneeStrength_f, settings.kneeStrength);
			material.SetFloat(PropertyIDs.Intensity_f, settings.intensity);
			material.SetFloat(PropertyIDs.MipLevelForRadius_f, Mathf.Log(settings.radius / 4f, 2f) + 1f);
			// debugOn
			material.SetInt(PropertyIDs.DebugMode_i, (int)settings.debugMode);
			material.SetFloat	(PropertyIDs.MipLevelForDebug_i, settings.mipLevelForDebug);
		}

		private enum BloomPass {
			BloomIntensity  = 0,
			BloomBlur,
			ApplyBloom,
			Debug,
		}

		override protected void PopulateCommandBuffer(CommandBuffer buffer, Material material, bool isFirstFrame) {
			var camSize = postProcessingManager.cameraSize;
			var mipRTSizes = new VectorInt2[8] {
				camSize / 1,
				camSize / 2,
				camSize / 4,
				camSize / 8,
				camSize / 16,
				camSize / 32,
				camSize / 64,
				camSize / 128,
			};
			var mipTempSizes = new VectorInt2[8] {
				camSize / 1,
				new VectorInt2(camSize.x /  1, camSize.y / 2),
				new VectorInt2(camSize.x /  2, camSize.y / 4),
				new VectorInt2(camSize.x /  4, camSize.y / 8),
				new VectorInt2(camSize.x /  8, camSize.y / 16),
				new VectorInt2(camSize.x / 16, camSize.y / 32),
				new VectorInt2(camSize.x / 32, camSize.y / 64),
				new VectorInt2(camSize.x / 64, camSize.y / 128),
			};

			//GetTemporaryRT(buffer, PropertyIDs.tempBuffer0_t, postProcessingManager.cameraSize, RenderTextureFormat.ARGBHalf, FilterMode.Point, RenderTextureReadWrite.Linear);
			Blit(buffer, BuiltinRenderTextureType.CameraTarget, bloomTexture, material, (int)BloomPass.BloomIntensity);
			//Blit(buffer, bloomTexture, BuiltinRenderTextureType.CameraTarget);
			//ReleaseTemporaryRT(buffer, PropertyIDs.tempBuffer0_t);

			#region MipLevels
			var maxReflectionMipLvl = 5;
			for (int i = 1; i < maxReflectionMipLvl; ++i) {
				GetTemporaryRT(buffer, PropertyIDs.tempBuffers_t[i], mipTempSizes[i], RenderTextureFormat.ARGBHalf, FilterMode.Bilinear, RenderTextureReadWrite.Linear);
				var pass = BloomPass.BloomBlur;//settings.suppressFlickering && i == 1 ? SSRPass.MipMapBlurComressor : SSRPass.MipMapBlurVanilla;
				buffer.SetGlobalFloat(PropertyIDs.MipLevel_f, i-1);
		
				buffer.SetGlobalVector(PropertyIDs.BlurDir_v, new Vector4(0, 2.0f/mipRTSizes[i-1].y, 0, -2.0f/mipRTSizes[i-1].y));
				Blit(buffer, bloomTexture, PropertyIDs.tempBuffers_t[i], material, (int)pass);
		
				buffer.SetGlobalVector(PropertyIDs.BlurDir_v, new Vector4(2.0f/mipRTSizes[i-1].x, 0, -2.0f/mipRTSizes[i-1].x, 0));
				buffer.SetGlobalTexture("_MainTex", PropertyIDs.tempBuffers_t[i]);
				buffer.SetRenderTarget(bloomTexture, i);
				Blit(buffer, material, (int)pass);
		
				ReleaseTemporaryRT(buffer, PropertyIDs.tempBuffers_t[i]);	// release temporary RT
			}
			#endregion

			Blit(buffer, bloomTexture, BuiltinRenderTextureType.CameraTarget, material, (int)BloomPass.ApplyBloom);

			#region Debug
			if (settings.debugOn) {
				Blit(buffer, bloomTexture, BuiltinRenderTextureType.CameraTarget, material, (int)BloomPass.Debug);
			}
			#endregion

		}

		void Awake() {
		//	getShaderPropertyIDs();
		}
	
		public void OnValidate () {
			setMaterialDirty();
			if (m_Settings.debugOn != lastSettings.debugOn) {
				setBufferDirty();
			}
			lastSettings = m_Settings;
		}
	}

}
