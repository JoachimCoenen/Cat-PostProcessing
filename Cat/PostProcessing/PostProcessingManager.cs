using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Cat.Common;

namespace Cat.PostProcessing {

	[RequireComponent(typeof(Camera))]
	[ExecuteInEditMode]
	[ImageEffectAllowedInSceneView]
	[DisallowMultipleComponent]
	public class PostProcessingManager : MonoBehaviour {
		private Camera m_camera = null;
		internal new Camera camera {
			get {
				if (m_camera == null) {
					m_camera = GetComponent<Camera>();
					if (m_camera == null) {
						this.enabled = false;
						throw new ArgumentException(String.Format("PostProcessingManager requires a Camera component attached"));
					}
				}
				return m_camera;
			} 
		}

		private VectorInt2 m_cameraSize = new VectorInt2(1, 1);
		internal VectorInt2 cameraSize {
			get { return m_cameraSize; }
			private set {
				if (m_cameraSize != value) {
					m_cameraSize = value;
					// set all RenderTextures dirty:
					//UpdateDepthTexture();
					//var effects = GetEffects(camera);
					//if (effects != null) {
					//	foreach (var effect in effects.Values) {
					//		if (effect.enabled) {
					//			effect.setRenderTextureDirty();
					//		}
					//	}
					// }
				}
			}
		}

		private VectorInt2 m_lastCameraSize = new VectorInt2(1, 1); 

		internal bool isSceneView { 
			get {
				#if UNITY_EDITOR 
				return UnityEditor.SceneView.currentDrawingSceneView != null && UnityEditor.SceneView.currentDrawingSceneView.camera == camera;
				#else
				return false;
				#endif 
			}
		}

		protected void OnDestroy() {
			if (m_DepthTexture != null) {
				m_DepthTexture.Release();
			}
			camera.RemoveCommandBuffer(m_DepthCommandBufferCameraEvent, depthCommandBuffer);
		}


		internal void RegisterCommandBuffer(PostProcessingBase effect, CameraEvent cameraEvent, CommandBuffer cb) {
			var cbs = GetCommandBuffers(camera);
			if (cbs == null) {
				cbs = new Dictionary<Tuple<Type, CameraEvent>, CommandBuffer>();
				m_CameraCommandBuffers.Add(camera, cbs);
			}

			var key = new Tuple<Type, CameraEvent>(effect.GetType(), cameraEvent);
			if (cbs.ContainsKey(key)) {
				Debug.LogErrorFormat("Camera '{0}' already contains CommandBuffer for ({1}, {2}). Removing old buffer and adding new one", camera, effect.GetType().Name, cameraEvent);
				RemoveCommandBuffer(effect, cameraEvent, cb);
			}
			cbs[key] = cb;
			UpdateCameraCommandBuffers();
		}

		internal void RemoveCommandBuffer(PostProcessingBase effect, CameraEvent cameraEvent, CommandBuffer cb) {
			camera.RemoveCommandBuffer(cameraEvent, cb);
			var cbs = GetCommandBuffers(camera);
			if (cbs == null) {
				Debug.LogWarningFormat("Camera '{0}' was not registered while trying to remove CommandBuffer for ({1}, {2})", camera, effect.GetType().Name, cameraEvent);
				return;
			}
			var key = new Tuple<Type, CameraEvent>(effect.GetType(), cameraEvent);
			if (!cbs.ContainsKey(key)) {
				Debug.LogWarningFormat("CommandBuffer for ({1}, {2}) was not registered while trying to remove CommandBuffer from Camera '{0}'", camera, effect.GetType().Name, cameraEvent);
				return;
			}

			cbs.Remove(key);
			if (cbs.Count == 0) {
				m_CameraCommandBuffers.Remove(camera);
			}
		}

		internal void RemoveAllCommandBuffers(PostProcessingBase effect) {
			var cbs = GetCommandBuffers(camera);
			if (cbs == null) {
				#if UNITY_DEBUG && DEBUG_OUTPUT_VERBIOUS
				Debug.LogFormat("Camera '{0}' was not registered while trying to remove all CommandBuffers for {1}", camera, effect.GetType().Name);
				#endif
				return;
			}

			foreach ( var s in cbs.Where(kv => kv.Key.item1 == effect.GetType()).ToList() ) {
				camera.RemoveCommandBuffer(s.Key.item2, s.Value);
				cbs.Remove(s.Key);
			}

			if (cbs.Count == 0) {
				m_CameraCommandBuffers.Remove(camera);
			}
		}


		internal void OnEffectsChanged() {
			UpdateCameraDepthTextureMode();
			UpdateEffectsSetup();
		}



		delegate RenderFunc MakeRenderDelegate(PostProcessingBaseImageEffect e);
		delegate SupportFunc MakeSupportDelegate(PostProcessingBase e);

		delegate void RenderFunc(RenderTexture source, RenderTexture destination);
		delegate void SupportFunc(Camera camera, VectorInt2 cameraSize); // JCO@@@ TODO: find better name
		// delegate void PreCullFunc();
		// delegate void PreRenderFunc();
		// delegate void PostRenderFunc();


		private IEnumerable<RenderFunc> m_imageEffectsRenderChain = new List<RenderFunc>();
		private IEnumerable<SupportFunc> m_preCullChain = new List<SupportFunc>();
		private IEnumerable<SupportFunc> m_preRenderChain = new List<SupportFunc>();
//		private IEnumerable<SupportFunc> m_postRenderChain = new List<SupportFunc>();

		internal void UpdateEffectsSetup() {
			var effects = new List<PostProcessingBase>();
			GetComponents<PostProcessingBase>(effects);
			var activeEffects = from effect in effects
				where effect.enabled && effect.isActive
				select effect;

			MakeRenderDelegate makeRenderDelegate = e => ((s, d) => e.RenderImage(s, d));
			m_imageEffectsRenderChain = from effect in activeEffects
				where effect is PostProcessingBaseImageEffect
				let imgEffect = effect as PostProcessingBaseImageEffect
				select makeRenderDelegate(imgEffect);

			MakeSupportDelegate makePreCullDelegate = e => ((cam, size) => e.PreCull(cam, size));
			m_preCullChain = from effect in activeEffects
				select makePreCullDelegate(effect);
			
			MakeSupportDelegate makePreRenderDelegate = e => ((cam, size) => e.PreRender(cam, size));
			m_preRenderChain = from effect in activeEffects
				select makePreRenderDelegate(effect);
			
			//MakeSupportDelegate makePostRenderDelegate = e => ((cam, size) => e.PostRender());
			//m_postRenderChain = from effect in activeEffects
			//	select makePostRenderDelegate(effect);
		}

		private bool m_requiresDepthTexture = false;
		internal void UpdateCameraDepthTextureMode() {
			var depthTextureMode = DepthTextureMode.None;
			var effects = GetEffects(camera);
			if (effects != null && enabled) {
				//depthTextureMode = (from pair in effects where pair.Value.enabled select pair.Value.requiredDepthTextureMode).Aggregate((l, r) => l | r);
				depthTextureMode = effects
					.Where(pair => pair.Value.enabled)
					.Select(pair => pair.Value.requiredDepthTextureMode)
					.Aggregate(depthTextureMode, (l, r) => l | r);

			}

			camera.depthTextureMode = depthTextureMode;

			m_requiresDepthTexture = (depthTextureMode & DepthTextureMode.Depth) == DepthTextureMode.Depth;
		}

		internal void UpdateCameraCommandBuffers() {
			var effects = new List<PostProcessingBase>();
			GetComponents<PostProcessingBase>(effects);
			var activeEffects = from effect in effects
					where effect.enabled && effect.isActive
				select effect;
			
			var commandBuffers = GetCommandBuffers(camera);
			if (commandBuffers != null) {
				
				var buffers = from effect in activeEffects
					join pair in commandBuffers on effect.GetType() equals pair.Key.item1
					select new { cameraEvent = pair.Key.item2, buffer = pair.Value }; //produces flat sequence (hopefully)

				foreach (var cb in buffers) {
					camera.RemoveCommandBuffer(cb.cameraEvent, cb.buffer);
				}
				if (this.enabled) {
					foreach (var cb in buffers) {
						camera.AddCommandBuffer(cb.cameraEvent, cb.buffer);
					}
				}

			}
		}


		internal void AddEffect(PostProcessingBase effect) {
			// Get Effects of camera
			// if camera doesn't have this effectType: add it. else throw exception?
			//
			var effects = GetEffects(camera);
			if (effects == null) {
				effects = new Dictionary<Type, PostProcessingBase>();
				m_CameraPostProcessingEffects.Add(camera, effects);
			}
			if (!effects.ContainsKey(effect.GetType())) {
				effects.Add(effect.GetType(), effect);
				OnEffectsChanged();
			}
			effect.InitializeEffect();
		}

		internal bool TryRemoveEffect(PostProcessingBase effect) {
			RemoveAllCommandBuffers(effect);
			var effects = GetEffects(camera);
			if (effects != null) {
				if (effects.ContainsKey(effect.GetType())) {
					var wasSuccessfull = effects.Remove(effect.GetType());
					OnEffectsChanged();
				}
				if (effects.Count == 0) {
					m_CameraPostProcessingEffects.Remove(camera);
				}
			}
			return true; // JCO@@@ TODO: TryRemoveEffectFromCamera return value!!! TUT
		}

		internal void RemoveEffect(PostProcessingBase effect) {
			if (!TryRemoveEffect(effect)) {
				Debug.LogErrorFormat("PostProcessingBase.TryRemoveEffectFromCamera({0}, {1}) failed", camera.name, effect.effectName);
			}
		}

		//[ImageEffectTransformsToLDR]
		void OnRenderImage(RenderTexture source, RenderTexture destination) {
			if (!enabled) {
				Debug.LogFormat("WTF Unity? {0}.OnRenderImage() on {1} called, even though {0} is NOT enabled.", GetType().Name, name);
				Graphics.Blit(source, destination);
			}
			var last = source;
			foreach (RenderFunc renderFunc in m_imageEffectsRenderChain) {
				var current = RenderTexture.GetTemporary(last.width, last.height, 0, last.format);
				renderFunc(last, current);
				if (last != source) { RenderTexture.ReleaseTemporary(last); }
				last = current;
			}

			Graphics.Blit(last, destination);

			if (last != source) {
				RenderTexture.ReleaseTemporary(last);
			}
		}

		private void OnPreCull(){
			var cam = this.camera;
			var size = this.cameraSize;

			if (m_lastCameraSize != cameraSize) {
				UpdateDepthTexture();
				m_lastCameraSize = cameraSize;
			}
			UpdateCameraDepthBufferCameraEvent(cam.actualRenderingPath);

			foreach (var preCullFunc in m_preCullChain) {
				preCullFunc(cam, size);
			}
		}

		private void OnPreRender(){
			var cam = this.camera;
			var size = this.cameraSize;
			foreach (var preRenderFunc in m_preRenderChain) {
				preRenderFunc(cam, size);
			}
		}

		private void OnPostRender(){
			// This is here inorder to overcome a bug, where Camera.pixelWidth & Camera.pixelHeight 
			// return a slightly wrong value in the editor scene view.
			cameraSize = new VectorInt2(camera.activeTexture.width, camera.activeTexture.height);
			//Debug.LogFormat("aspect: {0}, size: {1}", camera.aspect, cameraSize);

			//var cam = this.camera;
			//var size = this.cameraSize;
			//foreach (var postRenderFunc in m_postRenderChain) {
			//	postRenderFunc(cam, size);
			// }
		}

		private void OnEnable(){
			UpdateCameraCommandBuffers();
			UpdateCameraDepthTextureMode();
		}

		private void OnDisable(){
			UpdateCameraCommandBuffers();
			UpdateCameraDepthTextureMode();
		}



		private static Texture2D s_BlueNoiseTexture = null;
		internal static Texture2D blueNoiseTexture {
			get {
				if (s_BlueNoiseTexture == null) {
					s_BlueNoiseTexture = Resources.Load("tex_BlueNoise_256x256_UNI") as Texture2D;
				}
				return s_BlueNoiseTexture;
			} 
		} 

		private RenderTexture m_DepthTexture = null;
		private RenderTexture depthTexture {
			get {
				if (m_DepthTexture == null) {
					UpdateDepthTexture();
				}
				return m_DepthTexture;
			} 
		} 

		private Material m_DepthMaterial = null;
		protected Material depthMaterial {
			get {
				const string shaderName = "Hidden/Cat Depth Shader";
				if (m_DepthMaterial == null) {
					var shader = Shader.Find(shaderName);
					if (shader == null) {
						this.enabled = false;
						throw new ArgumentException(String.Format("Shader not found: '{0}'", shaderName));
					}
					m_DepthMaterial = new Material(shader);
					m_DepthMaterial.hideFlags = HideFlags.DontSave;
				}
				return m_DepthMaterial;
			} 
		}

		private CameraEvent GetAppropriateDepthBufferCameraEvent(RenderingPath renderingPath) {
			return renderingPath != RenderingPath.DeferredShading
			 	? CameraEvent.AfterForwardOpaque 
			 	: CameraEvent.BeforeLighting;
		}

		internal void UpdateCameraDepthBufferCameraEvent(RenderingPath renderingPath) {
			camera.RemoveCommandBuffer(m_DepthCommandBufferCameraEvent, depthCommandBuffer);

			if (m_requiresDepthTexture ) {
				m_DepthCommandBufferCameraEvent = GetAppropriateDepthBufferCameraEvent(renderingPath);
				camera.AddCommandBuffer(m_DepthCommandBufferCameraEvent, depthCommandBuffer);
			}
		}


		private CameraEvent m_DepthCommandBufferCameraEvent;
		private CommandBuffer m_DepthCommandBuffer;
		private CommandBuffer depthCommandBuffer {
			get {
				if (m_DepthCommandBuffer == null) {
					m_DepthCommandBuffer = new CommandBuffer();
					m_DepthCommandBuffer.name = "Aggregate Depth Buffer";
					UpdateDepthCommandBuffer();
				}
				return m_DepthCommandBuffer;

			}
		}

		void UpdateDepthTexture() {
			if (m_DepthTexture != null) {
				m_DepthTexture.Release();
			}
			m_DepthTexture = new RenderTexture(Math.Max(1, cameraSize.x), Math.Max(1, cameraSize.y), 0, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear) {
				filterMode = FilterMode.Point,
				wrapMode = TextureWrapMode.Clamp,
				useMipMap = false,
				autoGenerateMips = false,
				antiAliasing = 1,
				name = "_DepthTexture"
			};
			Shader.SetGlobalTexture(PropertyIDs.Depth_t, m_DepthTexture);
			UpdateDepthCommandBuffer();
		}

		void UpdateDepthCommandBuffer() {
			var cb = depthCommandBuffer;
			cb.Clear();

			//cb.Blit(BuiltinRenderTextureType.Depth, depthTexture);
			cb.Blit(BuiltinRenderTextureType.None, depthTexture, depthMaterial, 0);
			cb.SetGlobalTexture(PropertyIDs.Depth_t, depthTexture);
		}

		static class PropertyIDs {
			internal static readonly int normalsPacked_t				= Shader.PropertyToID("_NormalsPacked");
			internal static readonly int Depth_t						= Shader.PropertyToID("_DepthTexture");
			internal static readonly int blueNoise_t					= Shader.PropertyToID("_BlueNoise");
		}
			
		/*
		 static PostProcessingManager _instance;

		static public PostProcessingManager instance {
			get {
				if (_instance == null) {
					_instance = new PostProcessingManager();
				}
				return _instance;
			}
		}
		*/
		private struct Tuple<T1, T2> {
			public T1 item1;
			public T2 item2;
			public Tuple(T1 i1, T2 i2) {
				item1 = i1;
				item2 = i2;
			}
		}
		private struct Tuple<T1, T2, T3> {
			public T1 item1;
			public T2 item2;
			public T3 item3;
			public Tuple(T1 i1, T2 i2, T3 i3) {
				item1 = i1;
				item2 = i2;
				item3 = i3;
			}
		}
		static readonly Dictionary<Camera, Dictionary<Type, PostProcessingBase>> m_CameraPostProcessingEffects = 
			new Dictionary<Camera, Dictionary<Type, PostProcessingBase>>();
		static readonly Dictionary<Camera, Dictionary<Tuple<Type, CameraEvent>, CommandBuffer>> m_CameraCommandBuffers = 
			new Dictionary<Camera, Dictionary<Tuple<Type, CameraEvent>, CommandBuffer>>();

		static Dictionary<Type, PostProcessingBase> GetEffects(Camera aCamera) {
			// Get Effects of camera
			// if camera doesn't have this effectType: return null;
			//
			Dictionary<Type, PostProcessingBase> effects = null;
			m_CameraPostProcessingEffects.TryGetValue(aCamera, out effects);
			return effects;
		}

		static Dictionary<Tuple<Type, CameraEvent>, CommandBuffer> GetCommandBuffers(Camera aCamera) {
			Dictionary<Tuple<Type, CameraEvent>, CommandBuffer> cbs = null;
			m_CameraCommandBuffers.TryGetValue(aCamera, out cbs);
			return cbs;
		}





		//static readonly Dictionary<Light, Dictionary<Type, PostProcessingBase>> m_LightPostProcessingEffects = 
		//	new Dictionary<Light, Dictionary<Type, PostProcessingBase>>();
		//static readonly Dictionary<Light, Dictionary<Tuple<Type, LightEvent>, CommandBuffer>> m_LightCommandBuffers = 
		//	new Dictionary<Light, Dictionary<Tuple<Type, LightEvent>, CommandBuffer>>();

}


}
