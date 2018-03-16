using System;
using UnityEngine;
using Cat.Common;

// Inspired By: Kino/Bloom v2 - Bloom filter for Unity:
// https://github.com/keijiro/KinoBloom

namespace Cat.PostProcessing {
	[RequireComponent(typeof(Camera))]
	[ExecuteInEditMode]
	[ImageEffectAllowedInSceneView]
	[AddComponentMenu("Cat/PostProcessing/Bloom")]
	public class CatBloomRenderer : PostProcessingBaseImageEffect<CatBloom> {

		override protected string shaderName { 
			get { return "Hidden/Cat Bloom"; } 
		}
		override public string effectName { 
			get { return "Bloom"; } 
		}
		override internal DepthTextureMode requiredDepthTextureMode { 
			get { return DepthTextureMode.None; } 
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
		}
			
		override protected void UpdateMaterialPerFrame(Material material, Camera camera, VectorInt2 cameraSize) {
			setMaterialDirty();
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

		internal override void RenderImage(RenderTexture source, RenderTexture destination) {
			const int maxMipLvl = 7;

			const int maxUpsample = 1;
			var mipLevelFloat = Mathf.Clamp(Mathf.Log(Mathf.Max(source.width, source.height) / 32.0f + 1, 2), maxUpsample, maxMipLvl);
			material.SetFloat(PropertyIDs.MipLevel_f, mipLevelFloat);
			var mipLevel = (int)mipLevelFloat;
			var tempBuffers = new RenderTexture[mipLevel+1];

			#region Downsample
			RenderTexture last = source;
			var size = new VectorInt2(last.width, last.height);
			for (int i = 0; i <= mipLevel; i++) {
				var pass = i == 0 ? BloomPass.BloomIntensity : BloomPass.Downsample;
				var current = GetTemporaryRT(PropertyIDs.tempBuffers_t[i], size, RenderTextureFormat.ARGBHalf, FilterMode.Bilinear, RenderTextureReadWrite.Linear);
				Blit(last, current, material, (int)pass);
				tempBuffers[i] = current;
				last = current;
				size /= 2;
			}
			#endregion

			#region Upsample
			for (int i = mipLevel; i > maxUpsample; i--) {
				var current = tempBuffers[i-1];
				material.SetFloat(PropertyIDs.Weight_f, Mathf.Clamp01(mipLevelFloat - i));
				Blit(last, current, material, (int)BloomPass.Upsample);
				ReleaseTemporaryRT(last);	// release temporary RT
				last = current;
			}
			#endregion

			#region Apply
			Blit(source, destination);
			Blit(tempBuffers[maxUpsample], destination, material, (int)BloomPass.ApplyBloom);
			#endregion

			#region Debug
			if (settings.debugOn) {
				Blit(tempBuffers[maxUpsample], destination, material, (int)BloomPass.Debug);
			}
			#endregion

			for (int i = 0; i <= maxUpsample; i++) {
				ReleaseTemporaryRT(tempBuffers[i]);	// release temporary RT
			}

		}

	
		public void OnValidate () {
			setMaterialDirty();
		}
	}

	[Serializable]
	[SettingsForPostProcessingEffect(typeof(CatBloomRenderer))]
	public class CatBloom : PostProcessingSettingsBase {

		override public string effectName { 
			get { return "Bloom"; } 
		}
		override public int queueingPosition {
			get { return 2950; } 
		}

		[Header("Primary Settings")]
		[Range(0, 1)]
		public FloatProperty intensity = new FloatProperty();

		[Range(0, 1)]
		public FloatProperty dirtIntensity = new FloatProperty();

		public TextureProperty dirtTexture = new TextureProperty();


		[Header("Secondary Settings")]
		[Range(0, 1)]
		public FloatProperty minLuminance = new FloatProperty();

		[Range(0, 4)]
		public FloatProperty kneeStrength = new FloatProperty();


		[Header("Debugging")]
		public BoolProperty debugOn = new BoolProperty();

		public CatBloom() {
			intensity.rawValue		= 0.25f;
			dirtIntensity.rawValue	= 0.5f;
			dirtTexture.rawValue	= null;

			minLuminance.rawValue	= 0.5f;
			kneeStrength.rawValue	= 1;

			debugOn.rawValue		= false;
		}

		public static CatBloom defaultSettings { 
			get {
				return new CatBloom();
			}
		}

	}

}
