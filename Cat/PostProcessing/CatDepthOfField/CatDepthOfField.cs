using System;
using UnityEngine;
using UnityEngine.Rendering;
using Cat.Common;

// Inspired By: Kino/Bloom v2 - Bloom filter for Unity:
// https://github.com/keijiro/KinoBloom

namespace Cat.PostProcessing {
	[RequireComponent(typeof (Camera))]
	[ExecuteInEditMode]
	[ImageEffectAllowedInSceneView]
	[AddComponentMenu("Cat/PostProcessing/DepthOfField")]
	public class CatDepthOfField : PostProcessingBaseImageEffect {

		[Serializable]
		public struct Settings {
			[CustomLabelRange(0.1f, 22, "f-Stop f/n")]
			public float			fStop;

			[Range(0, 50)]
			public float			focusDistance;

			[Range(1, 7)]
			public float			radius;

			[Header("Debugging")]
			public bool					debugOn;

			public static Settings defaultSettings { 
				get {
					return new Settings {
						fStop					= 2f,
						focusDistance			= 1.6f,

						radius					= 3f,
						
						debugOn					= false,
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

		override protected string shaderName { 
			get { return "Hidden/Cat DepthOfField"; } 
		}
		override public string effectName { 
			get { return "Depth Of Field"; } 
		}
		override internal DepthTextureMode requiredDepthTextureMode { 
			get { return DepthTextureMode.Depth; } 
		}
		override public bool isActive { 
			get { return true; } 
		}


		private readonly RenderTextureContainer blurTex = new RenderTextureContainer();

		static class PropertyIDs {
			internal static readonly int fStop_f			= Shader.PropertyToID("_fStop");
			internal static readonly int FocusDistance_f	= Shader.PropertyToID("_FocusDistance");
			internal static readonly int Radius_f			= Shader.PropertyToID("_Radius");

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
				Shader.PropertyToID("_TempTexture_This_texture_is_never_going_to_be_directly_referenced9"),
			};
		}

		override protected void UpdateRenderTextures(VectorInt2 cameraSize) {
			CreateRT(blurTex, cameraSize, 0, true, RenderTextureFormat.DefaultHDR, FilterMode.Bilinear, RenderTextureReadWrite.Default, TextureWrapMode.Clamp, "blurTex");
		}

		override protected void UpdateMaterialPerFrame(Material material) {
			setMaterialDirty();
		}

		override protected void UpdateMaterial(Material material) {
			var settings = this.settings;
			material.SetFloat(PropertyIDs.fStop_f, settings.fStop);
			material.SetFloat(PropertyIDs.FocusDistance_f, settings.focusDistance);
			material.SetFloat(PropertyIDs.Radius_f, settings.radius);

			// debugOn
		}

		private enum DOFPass {
			PreFilter = 0,
			Blur,
			Apply,
			Debug,
			Blit,
		}

		void OnRenderImage(RenderTexture source, RenderTexture destination) {
			const int maxMipLvl = 7;
			var camSize = postProcessingManager.cameraSize;

			//var mipLevelFloat = Mathf.Clamp(Mathf.Log(Mathf.Max(source.width, source.height) / 32.0f + 1, 2), maxUpsample, maxMipLvl);
			material.SetFloat(PropertyIDs.MipLevel_f, 0);

			var mipLevels = 3+1;
			var tempBuffers = new RenderTexture[mipLevels];


			var size = new VectorInt2(source.width, source.height);
			tempBuffers[0] = GetTemporaryRT(PropertyIDs.tempBuffers_t[0], size, RenderTextureFormat.ARGBHalf, FilterMode.Bilinear, RenderTextureReadWrite.Linear);
			Blit(source, tempBuffers[0], material, (int)DOFPass.PreFilter);
			Blit(tempBuffers[0], blurTex, material, (int)DOFPass.Blur);
			#region CameraMipLevels
			for (int i = 1; i < mipLevels; i++) {
				tempBuffers[i] = GetTemporaryRT(PropertyIDs.tempBuffers_t[i], size, RenderTextureFormat.ARGBHalf, FilterMode.Bilinear, RenderTextureReadWrite.Linear);
				material.SetFloat(PropertyIDs.MipLevel_f, i);
				Blit(blurTex, tempBuffers[i], material, (int)DOFPass.Blur);

				Shader.SetGlobalTexture("_MainTex", tempBuffers[i]);
				Graphics.SetRenderTarget(blurTex, i);
				Graphics.Blit(tempBuffers[i], material, (int)DOFPass.Blit);
				size.Set((int)(size.x / 2), (int)(size.y / 2));

			//	ReleaseTemporaryRT(buffer, PropertyIDs.tempBuffers_t[i]);	// release temporary RT
			}
			#endregion


		//	#region Downsample
		//	RenderTexture last = source;
		//	var size = new VectorInt2(last.width, last.height);
		//	for (int i = 0; i < mipLevels; i++) {
		//		var current = GetTemporaryRT(PropertyIDs.tempBuffers_t[i], size, RenderTextureFormat.ARGBHalf, FilterMode.Bilinear, RenderTextureReadWrite.Linear);
		//		var pass = i == 0 ? DOFPass.PreFilter : DOFPass.Blur;
		//		Blit(last, current, material, (int)pass);
		//		material.SetFloat(PropertyIDs.MipLevel_f, i);
		//		tempBuffers[i] = current;
		//		last = current;
		//		size /= i > 1 ? 2 : 1;
		//	}
		//	#endregion

			Blit(source, destination);
			Blit(blurTex, destination, material, (int)DOFPass.Apply);

			#region Debug
			if (settings.debugOn) {
				//material.SetFloat(PropertyIDs.MipLevel_f, 3-1);
				Blit(source, destination, material, (int)DOFPass.Debug);
			}
			#endregion

			#region free
			for (int i = 0; i < mipLevels; i++) {
				ReleaseTemporaryRT(tempBuffers[i]);	// release temporary RT
			}
			#endregion

		}

	
		public void OnValidate () {
			setMaterialDirty();
		}
	}

}
