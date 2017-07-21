using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Cat.Common;

namespace Cat.PostProcessing {

	[RequireComponent(typeof(Camera))]
	[ExecuteInEditMode]
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
					UpdateDepthTexture();
					Dictionary<Type, PostProcessingBase> effectsList;
					if (m_CameraPostProcessingEffects.TryGetValue(camera, out effectsList)) {
						foreach (var effect in effectsList.Values) {
							if (effect.enabled) {
								effect.setRenderTextureDirty();
							}
						}
					}
				}
			}
		}

		internal bool isSceneView { 
			get {
				#if UNITY_EDITOR 
				return UnityEditor.SceneView.currentDrawingSceneView != null && UnityEditor.SceneView.currentDrawingSceneView.camera == camera;
				#else
				return false;
				#endif 
			}
		}

		protected void OnPostRender() {
			// This is here inorder to overcome a bug, where Camera.pixelWidth & Camera.pixelHeight 
			// return a slightly wrong value in the editor scene view.
			cameraSize = new VectorInt2(camera.activeTexture.width, camera.activeTexture.height);
			//Debug.LogFormat("aspect: {0}, size: {1}", camera.aspect, cameraSize);

		}

		protected void OnDestroy() {
			if (m_DepthTexture != null) {
				m_DepthTexture.Release();
			}
			camera.RemoveCommandBuffer(CameraEvent.AfterDepthTexture, depthCommandBuffer);
		}


		internal void RegisterCommandBuffer(PostProcessingBase effect, CameraEvent cameraEvent, CommandBuffer cb) {
			Dictionary<Tuple<Type, CameraEvent>, CommandBuffer> cbs;
			if (!m_CameraCommandBuffers.TryGetValue(camera, out cbs)) {
				cbs = new Dictionary<Tuple<Type, CameraEvent>, CommandBuffer>();
				m_CameraCommandBuffers.Add(camera, cbs);
			}

			var key = new Tuple<Type, CameraEvent>(effect.GetType(), cameraEvent);
			if (cbs.ContainsKey(key)) {
				Debug.LogErrorFormat("Camera '{0}' already contains CommandBuffer for ({1}, {2}). Removing old buffer and adding new one", camera, effect.GetType().Name, cameraEvent);
				RemoveCommandBuffer(effect, cameraEvent, cb);
			}
			cbs[key] = cb;
			camera.AddCommandBuffer(cameraEvent, cb);
		}

		internal void RemoveCommandBuffer(PostProcessingBase effect, CameraEvent cameraEvent, CommandBuffer cb) {
			camera.RemoveCommandBuffer(cameraEvent, cb);
			Dictionary<Tuple<Type, CameraEvent>, CommandBuffer> cbs;
			if (!m_CameraCommandBuffers.TryGetValue(camera, out cbs)) {
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
			Dictionary<Tuple<Type, CameraEvent>, CommandBuffer> cbs;
			if (!m_CameraCommandBuffers.TryGetValue(camera, out cbs)) {
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
		
		internal void UpdateCameraDepthTextureMode() {
			var depthTextureMode = DepthTextureMode.None;
			Dictionary<Type, PostProcessingBase> effectsList = null;
			if (m_CameraPostProcessingEffects.TryGetValue(camera, out effectsList)) {
				foreach (var effect in effectsList) {
					depthTextureMode |= effect.Value.requiredDepthTextureMode;
				}
			}

			camera.depthTextureMode = depthTextureMode;
			camera.RemoveCommandBuffer(CameraEvent.BeforeLighting, depthCommandBuffer);
			if ((depthTextureMode & DepthTextureMode.Depth) == DepthTextureMode.Depth) {
				camera.AddCommandBuffer(CameraEvent.BeforeLighting, depthCommandBuffer);
				
			}

		}


		internal void AddEffect(PostProcessingBase effect) {
			// Get Effects of camera
			// if camera doesn't have this effectType: add it. else throw exception?
			//
			Dictionary<Type, PostProcessingBase> effectsList;
			if (!m_CameraPostProcessingEffects.TryGetValue(camera, out effectsList)) {
				effectsList = new Dictionary<Type, PostProcessingBase>();
				m_CameraPostProcessingEffects.Add(camera, effectsList);
			}
			if (!effectsList.ContainsKey(effect.GetType())) {
				effectsList.Add(effect.GetType(), effect);
				UpdateCameraDepthTextureMode();
			}
			effect.InitializeEffect();
		}

		internal bool TryRemoveEffect(PostProcessingBase effect) {
				RemoveAllCommandBuffers(effect);
				Dictionary<Type, PostProcessingBase> effectsList;
				if (m_CameraPostProcessingEffects.TryGetValue(camera, out effectsList)) {
					if (effectsList.ContainsKey(effect.GetType())) {
						var wasSuccessfull = effectsList.Remove(effect.GetType());
						UpdateCameraDepthTextureMode();
					}
					if (effectsList.Count == 0) {
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

			cb.Blit(BuiltinRenderTextureType.ResolvedDepth, depthTexture);
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

		//static readonly Dictionary<Light, Dictionary<Type, PostProcessingBase>> m_LightPostProcessingEffects = 
		//	new Dictionary<Light, Dictionary<Type, PostProcessingBase>>();
		//static readonly Dictionary<Light, Dictionary<Tuple<Type, LightEvent>, CommandBuffer>> m_LightCommandBuffers = 
		//	new Dictionary<Light, Dictionary<Tuple<Type, LightEvent>, CommandBuffer>>();

	}
}
