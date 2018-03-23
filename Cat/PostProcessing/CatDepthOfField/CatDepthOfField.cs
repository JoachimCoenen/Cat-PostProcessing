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
	[AddComponentMenu("Cat/PostProcessing/DepthOfField")]
	public class CatDepthOfFieldRenderer : PostProcessingBaseCommandBuffer<CatDepthOfField> {

		override protected string shaderName { 
			get { return "Hidden/Cat DepthOfField"; } 
		}
		override public string effectName { 
			get { return "Depth Of Field"; } 
		}
		override internal DepthTextureMode requiredDepthTextureMode { 
			get { return DepthTextureMode.Depth; } 
		}
		override protected CameraEvent cameraEvent { 
			get { return CameraEvent.BeforeImageEffects; }
		}

		static class PropertyIDs {
			internal static readonly int Intensity_f		= Shader.PropertyToID("_Intensity");
			internal static readonly int fStop_f			= Shader.PropertyToID("_fStop");
			internal static readonly int FocusDistance_f	= Shader.PropertyToID("_FocusDistance");
			internal static readonly int Radius_f			= Shader.PropertyToID("_Radius");

			// debugOn

			internal static readonly int BlurDir_v			= Shader.PropertyToID("_BlurDir");
			internal static readonly int MipLevel_f			= Shader.PropertyToID("_MipLevel");
			internal static readonly int Weight_f			= Shader.PropertyToID("_Weight");

			internal static readonly int BlurTex_t			= Shader.PropertyToID("_BlurTex");

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

		override protected void UpdateMaterialPerFrame(Material material, Camera camera, VectorInt2 cameraSize) {
			setMaterialDirty();
		}

		override protected void UpdateMaterial(Material material, Camera camera, VectorInt2 cameraSize) {
			var settings = this.settings;
			material.SetFloat(PropertyIDs.Intensity_f, settings.intensity);
			material.SetFloat(PropertyIDs.fStop_f, settings.fStop);
			material.SetFloat(PropertyIDs.FocusDistance_f, settings.focusDistance);
			material.SetFloat(PropertyIDs.Radius_f, settings.radius);
			setBufferDirty();
		}

		private enum DOFPass {
			PreFilter = 0,
			Blur,
			Apply,
			Debug,
		}

		override protected void PopulateCommandBuffer(CommandBuffer buffer, Material material, VectorInt2 cameraSize) {
			GetTemporaryRT(buffer, PropertyIDs.tempBuffers_t[0], cameraSize, RenderTextureFormat.ARGBHalf, FilterMode.Bilinear, RenderTextureReadWrite.Linear);

			Blit(buffer, BuiltinRenderTextureType.CameraTarget, PropertyIDs.tempBuffers_t[0], material, (int)DOFPass.PreFilter);

			GetTemporaryRT(buffer, PropertyIDs.BlurTex_t, cameraSize / 2, RenderTextureFormat.ARGBHalf, FilterMode.Bilinear, RenderTextureReadWrite.Linear);
			Blit(buffer, PropertyIDs.tempBuffers_t[0], PropertyIDs.BlurTex_t, material, (int)DOFPass.Blur);

			ReleaseTemporaryRT(buffer, PropertyIDs.tempBuffers_t[0]);

			GetTemporaryRT(buffer, PropertyIDs.tempBuffers_t[1], cameraSize, RenderTextureFormat.ARGBHalf, FilterMode.Bilinear, RenderTextureReadWrite.Linear);
			Blit(buffer, BuiltinRenderTextureType.CameraTarget, PropertyIDs.tempBuffers_t[1]);
			Blit(buffer, PropertyIDs.tempBuffers_t[1], BuiltinRenderTextureType.CameraTarget, material, (int)DOFPass.Apply);
			ReleaseTemporaryRT(buffer, PropertyIDs.BlurTex_t);

			#region Debug
			if (settings.debugOn) {
				//material.SetFloat(PropertyIDs.MipLevel_f, 3-1);
				Blit(buffer, PropertyIDs.tempBuffers_t[1], BuiltinRenderTextureType.CameraTarget, material, (int)DOFPass.Debug);
			}
			#endregion

			ReleaseTemporaryRT(buffer, PropertyIDs.tempBuffers_t[1]);

		}
	
		public void OnValidate () {
			setMaterialDirty();
		}
	}

	[Serializable]
	[SettingsForPostProcessingEffect(typeof(CatDepthOfFieldRenderer))]
	public class CatDepthOfField : PostProcessingSettingsBase {
		override public bool enabled { get { return intensity > 0; } }

		override public string effectName { 
			get { return "Depth Of Field"; } 
		}
		override public int queueingPosition {
			get { return 2850; } 
		}

		[Range(0, 1)]
		public FloatProperty intensity = new FloatProperty();

		[CustomLabelRange(0.1f, 22, "f-Stop f/n")]
		public FloatProperty fStop = new FloatProperty();

		[Range(0.185f, 100f)]
		public FloatProperty focusDistance = new FloatProperty();

		[Range(1, 15)]
		public FloatProperty radius = new FloatProperty();

		[Header("Debugging")]
		public BoolProperty debugOn = new BoolProperty();

		public override void Reset() {
			intensity.rawValue		= 0f;
			fStop.rawValue			= 2f;
			focusDistance.rawValue	= 1.6f;
			radius.rawValue			= 3f;
			debugOn.rawValue		= false;
		}

	}

}
