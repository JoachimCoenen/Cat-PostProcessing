using System;
using UnityEngine;
using UnityEngine.Rendering;
using Cat.Common;

//using UnityEngineInternal;
//using UnityEditor;
//using UnityEditorInternal;

namespace Cat.PostProcessing {
	public class CatAORenderer : PostProcessingBaseCommandBuffer<CatAO> {

		private RenderingPath m_currentRenderingPath;
		private CameraEvent GetAppropriateCameraEvent(bool isDebugModeOn, RenderingPath renderingPath) {
			return isDebugModeOn  || renderingPath != RenderingPath.DeferredShading
				? CameraEvent.BeforeImageEffectsOpaque 
				: CameraEvent.BeforeReflections;
		}
		private CameraEvent m_CameraEvent = CameraEvent.BeforeReflections;

		override protected string shaderName { 
			get { return "Hidden/Cat Ambient Occlusion"; } 
		}
		override public string effectName { 
			get { return "Ambient Occlusion"; } 
		}
		override internal DepthTextureMode requiredDepthTextureMode { 
			get { return DepthTextureMode.Depth | DepthTextureMode.MotionVectors; } 
		}
		override protected CameraEvent cameraEvent { 
			get { return m_CameraEvent; }
		}

		static class PropertyIDs {
			internal static readonly int OcclusionNormals1_t	= Shader.PropertyToID("_OcclusionNormals1");
			internal static readonly int OcclusionNormals2_t	= Shader.PropertyToID("_OcclusionNormals2");
			internal static readonly int OcclusionNormals3_t	= Shader.PropertyToID("_OcclusionNormals3");
			internal static readonly int Intensity_f			= Shader.PropertyToID("_Intensity");
			internal static readonly int SampleCount_i			= Shader.PropertyToID("_SampleCount");
			internal static readonly int Radius_f				= Shader.PropertyToID("_Radius");
			internal static readonly int BlurDir_v				= Shader.PropertyToID("_BlurDir");
		}

		override protected void UpdateRenderTextures(Camera camera, VectorInt2 cameraSize) {
			setBufferDirty();
		}

		override protected void UpdateMaterialPerFrame(Material material, Camera camera, VectorInt2 cameraSize) {
			m_currentRenderingPath = camera.actualRenderingPath;
			setMaterialDirty();
		}

		override protected void UpdateMaterial(Material material, Camera camera, VectorInt2 cameraSize) {
			var settings = this.settings;
			material.SetFloat(PropertyIDs.Intensity_f, settings.intensity);
			material.SetInt(PropertyIDs.SampleCount_i, settings.sampleCount);
			material.SetFloat(PropertyIDs.Radius_f, settings.radius);
			setBufferDirty();
		}

		private enum SSRPass {
			SampleProximity  = 0,
			Blur			    ,
			Resolve				,
			ResolveDebug		,
			MultiplyAlpha		,
		}

		override protected void PopulateCommandBuffer(CommandBuffer buffer, Material material, VectorInt2 cameraSize) {
			GetTemporaryRT(buffer, PropertyIDs.OcclusionNormals1_t, cameraSize, RenderTextureFormat.ARGBHalf, FilterMode.Bilinear, RenderTextureReadWrite.Linear);
			GetTemporaryRT(buffer, PropertyIDs.OcclusionNormals2_t, cameraSize, RenderTextureFormat.ARGBHalf, FilterMode.Bilinear, RenderTextureReadWrite.Linear);
		//	GetTemporaryRT(buffer, PropertyIDs.OcclusionNormals3_t, cameraSize, RenderTextureFormat.ARGBHalf, FilterMode.Bilinear, RenderTextureReadWrite.Linear);

			Blit(buffer, PropertyIDs.OcclusionNormals1_t, material, (int)SSRPass.SampleProximity);
			buffer.SetGlobalVector(PropertyIDs.BlurDir_v, new Vector2(1, 0));
			Blit(buffer, PropertyIDs.OcclusionNormals1_t, PropertyIDs.OcclusionNormals2_t, material, (int)SSRPass.Blur);
			buffer.SetGlobalVector(PropertyIDs.BlurDir_v, new Vector2(0, 1));
		//	Blit(buffer, PropertyIDs.OcclusionNormals2_t, PropertyIDs.OcclusionNormals3_t, material, (int)SSRPass.Blur);
			if (settings.debugOn) {
				Blit(buffer, PropertyIDs.OcclusionNormals2_t, BuiltinRenderTextureType.CameraTarget, material, (int)SSRPass.ResolveDebug);
			} else  {
				Blit(buffer, PropertyIDs.OcclusionNormals2_t, BuiltinRenderTextureType.CameraTarget, material, (int)SSRPass.Resolve);
			}
		//	ReleaseTemporaryRT(buffer, PropertyIDs.OcclusionNormals3_t);
			ReleaseTemporaryRT(buffer, PropertyIDs.OcclusionNormals2_t);
			ReleaseTemporaryRT(buffer, PropertyIDs.OcclusionNormals1_t);
			if (m_currentRenderingPath == RenderingPath.DeferredShading) {
				Blit(buffer, BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.GBuffer0, material, (int)SSRPass.MultiplyAlpha);
			}

			var appropriateCameraEvent = GetAppropriateCameraEvent(settings.debugOn, m_currentRenderingPath);
			if (appropriateCameraEvent != m_CameraEvent) {
				postProcessingManager.RemoveCommandBuffer(this, cameraEvent, buffer);
				m_CameraEvent = appropriateCameraEvent;
				postProcessingManager.RegisterCommandBuffer(this, cameraEvent, buffer);
			}
		}
	
		public void OnValidate () {
			setMaterialDirty();
		}
	}

	[Serializable]
	[SettingsForPostProcessingEffect(typeof(CatAORenderer))]
	public class CatAO : PostProcessingSettingsBase {
		override public bool enabled { get { return intensity > 0; } }

		override public string effectName { 
			get { return "Ambient Occlusion"; } 
		}
		override public int queueingPosition {
			get { return 1050; } 
		}

		[Range(0, 2)]
		public FloatProperty intensity = new FloatProperty();

		[Range(3, 16)]
		public IntProperty sampleCount = new IntProperty();

		[Range(1e-4f, 2)]
		public FloatProperty radius = new FloatProperty();

		//[Space(15)]
		public BoolProperty debugOn = new BoolProperty();

		public override void Reset() {
			intensity.rawValue = 0f;
			sampleCount.rawValue = 10;
			radius.rawValue = 0.3f;
			debugOn.rawValue = false;
		}
	}

}
