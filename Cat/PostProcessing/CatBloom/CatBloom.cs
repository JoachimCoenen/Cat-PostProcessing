using System;
using UnityEngine;
using UnityEngine.Rendering;
using Cat.Common;

// Inspired By: Kino/Bloom v2 - Bloom filter for Unity:
// https://github.com/keijiro/KinoBloom

namespace Cat.PostProcessing {
	[RequireComponent(typeof(Camera))]
	[ExecuteInEditMode]
	[ImageEffectAllowedInSceneView]
	[AddComponentMenu("Cat/PostProcessing/Bloom")]
	public class CatBloomRenderer : PostProcessingBaseCommandBuffer<CatBloom> {

		override protected string shaderName { 
			get { return "Hidden/Cat Bloom"; } 
		}
		override public string effectName { 
			get { return "Bloom"; } 
		}
		override internal DepthTextureMode requiredDepthTextureMode { 
			get { return DepthTextureMode.None; } 
		}
		override protected CameraEvent cameraEvent { 
			get { return CameraEvent.BeforeImageEffects; }
		}


		static class PropertyIDs {
			internal static readonly int Intensity_f		= Shader.PropertyToID("_Intensity");
			internal static readonly int DirtIntensity_f	= Shader.PropertyToID("_DirtIntensity");
			internal static readonly int DirtTexture_t		= Shader.PropertyToID("_DirtTexture");

			internal static readonly int MinLuminance_f		= Shader.PropertyToID("_MinLuminance");
			internal static readonly int KneeStrength_f		= Shader.PropertyToID("_KneeStrength");

			// debugOn

			internal static readonly int BlurDir_v			= Shader.PropertyToID("_BlurDir");
			internal static readonly int MipLevel_f			= Shader.PropertyToID("_MipLevel");
			internal static readonly int Weight_f			= Shader.PropertyToID("_Weight");

			internal static readonly int BaseTex_t			= Shader.PropertyToID("_BaseTex");
		

			internal static readonly int tempBuffer0_t		= Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced0x");
			internal static readonly int tempBuffer1_t		= Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced1x");
			internal static readonly int[] tempBuffers_t	= new int[] {
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced1"),
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced2"),
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced3"),
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced4"),
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced5"),
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced6"),
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced7"),
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced8"),
			};
			internal static readonly int[] tempBuffers2_t	= new int[] {
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced1x1"),
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced1x2"),
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced1x3"),
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced1x4"),
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced1x5"),
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced1x6"),
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced1x7"),
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced1x8"),
			};
		}
			
		override protected void UpdateMaterialPerFrame(Material material, Camera camera, VectorInt2 cameraSize) {
			setMaterialDirty();
			setBufferDirty();
		}

		override protected void UpdateMaterial(Material material, Camera camera, VectorInt2 cameraSize) {
			var settings = this.settings;
			material.SetFloat(PropertyIDs.MinLuminance_f, settings.minLuminance);
			material.SetFloat(PropertyIDs.KneeStrength_f, settings.kneeStrength);
			material.SetFloat(PropertyIDs.Intensity_f, settings.intensity);
			material.SetTexture(PropertyIDs.DirtTexture_t, settings.dirtTexture);
			material.SetFloat(PropertyIDs.DirtIntensity_f, settings.dirtIntensity);
			// debugOn
		}

		private enum BloomPass {
			BloomIntensity  = 0,
			Downsample,
			Upsample,
			ApplyBloom,
			Debug,
			//BloomBlur,
		}
		override protected void PopulateCommandBuffer(CommandBuffer buffer, Material material, VectorInt2 cameraSize) {
			const int maxMipLvl = 7;

			const int maxUpsample = 0;
			var mipLevelFloat = Mathf.Clamp(Mathf.Log(Mathf.Max(cameraSize.x, cameraSize.y) / 48.0f + 1, 2), maxUpsample, maxMipLvl);
			material.SetFloat(PropertyIDs.MipLevel_f, mipLevelFloat);
			var mipLevel = (int)mipLevelFloat;
			var pyramidDownSize = mipLevel + 1;
			var pyramidUpSize = mipLevel;


			// Negative anamorphic ratio values distort vertically - positive is horizontal
			//float ratio = 0.5f * Mathf.Clamp(settings.anisotropicRatio, -1, 1);
			//float rw = ratio > 0 ? 1-ratio : 1f;
			//float rh = ratio > 0 ? 1+ratio : 1f;

			// keeps the area equal:
			float ratio = Mathf.Sqrt(1-Mathf.Abs(settings.anisotropicRatio)*0.75f);
			float rw = settings.anisotropicRatio > 0 ? 1*ratio : 1/ratio;
			float rh = settings.anisotropicRatio > 0 ? 1/ratio : 1*ratio;
			var size = new VectorInt2(Mathf.FloorToInt(cameraSize.x*rw*0.5f), Mathf.FloorToInt(cameraSize.y*rh*0.5f));

			var tempBuffersDown = new int[pyramidDownSize];
			var tempBuffersUp = new int[pyramidUpSize];

			for (var i = 0; i < pyramidDownSize; i++) {
				tempBuffersDown[i] = PropertyIDs.tempBuffers_t[i];
			}
			for (var i = 0; i < pyramidUpSize; i++) {
				tempBuffersUp[i] = PropertyIDs.tempBuffers2_t[i];
			}

			#region Downsample
			RenderTargetIdentifier last = BuiltinRenderTextureType.CameraTarget;

			for (int i = 0; i < pyramidDownSize; i++) {
				var pass = i == 0 ? BloomPass.BloomIntensity : BloomPass.Downsample;

				var current = tempBuffersDown[i];
				GetTemporaryRT(buffer, current, size / (1 << i), RenderTextureFormat.ARGBHalf, FilterMode.Bilinear, RenderTextureReadWrite.Linear);
				Blit(buffer, last, current, material, (int)pass);
				last = current;
				//size /= 2;
			}
			#endregion

			#region Upsample
			for (int i = pyramidUpSize-1; i >= maxUpsample; i--) {
				buffer.SetGlobalFloat(PropertyIDs.Weight_f, Mathf.Clamp01(mipLevelFloat - i - 1));
				buffer.SetGlobalTexture(PropertyIDs.BaseTex_t, tempBuffersDown[i]);

				var current = tempBuffersUp[i];
				GetTemporaryRT(buffer, current, size / (1 << i), RenderTextureFormat.ARGBHalf, FilterMode.Bilinear, RenderTextureReadWrite.Linear);

				Blit(buffer, last, current, material, (int)BloomPass.Upsample); 
				last = current;
			}
			#endregion

			#region Apply
			GetTemporaryRT(buffer, PropertyIDs.BaseTex_t, cameraSize, RenderTextureFormat.ARGBHalf, FilterMode.Point, RenderTextureReadWrite.Linear);
			Blit(buffer, BuiltinRenderTextureType.CameraTarget, PropertyIDs.BaseTex_t);
			//buffer.SetGlobalTexture(PropertyIDs.BaseTex_t, BuiltinRenderTextureType.CameraTarget);
			Blit(buffer, last, BuiltinRenderTextureType.CameraTarget, material, (int)BloomPass.ApplyBloom);
			ReleaseTemporaryRT(buffer, PropertyIDs.BaseTex_t);	// release temporary RT
			#endregion

			#region Debug
			if (settings.debugOn) {
				Blit(buffer, last, BuiltinRenderTextureType.CameraTarget, material, (int)BloomPass.Debug);
				//Blit(buffer, tempBuffersDown[0], BuiltinRenderTextureType.CameraTarget);
			}
			#endregion

			for (int i = 0; i < pyramidDownSize; i++) {
				ReleaseTemporaryRT(buffer, tempBuffersDown[i]);	// release temporary RT
			}
			for (int i = maxUpsample; i < pyramidUpSize; i++) {
				ReleaseTemporaryRT(buffer, tempBuffersUp[i]);	// release temporary RT
			}
		}

		public void OnValidate () {
			setMaterialDirty();
		}
	}

	[Serializable]
	[SettingsForPostProcessingEffect(typeof(CatBloomRenderer))]
	public class CatBloom : PostProcessingSettingsBase {
		override public bool enabled { get { return intensity > 0 || (dirtIntensity > 0 && dirtTexture.rawValue != null); } }

		override public string effectName { 
			get { return "Bloom"; } 
		}
		override public int queueingPosition {
			get { return 2950; } 
		}

		[Header("Primary Settings")]
		[Range(0, 4)]
		public FloatProperty intensity = new FloatProperty();

		[Range(0, 2)]
		public FloatProperty dirtIntensity = new FloatProperty();

		public TextureProperty dirtTexture = new TextureProperty();

		[Range(-1, 1)]
		public FloatProperty anisotropicRatio = new FloatProperty();

		[Header("Secondary Settings")]
		[Range(0, 1)]
		public FloatProperty minLuminance = new FloatProperty();

		[Range(0, 4)]
		public FloatProperty kneeStrength = new FloatProperty();


		[Header("Debugging")]
		public BoolProperty debugOn = new BoolProperty();

		public override void Reset() {
			intensity.rawValue			= 0.0f;
			dirtIntensity.rawValue		= 0.0f;
			dirtTexture.rawValue		= null;
			anisotropicRatio.rawValue	= 0.0f;

			minLuminance.rawValue		= 0.5f;
			kneeStrength.rawValue		= 1;

			debugOn.rawValue			= false;
		}

	}

}
