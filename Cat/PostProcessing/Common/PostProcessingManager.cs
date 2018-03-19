using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Cat.Common;

namespace Cat.PostProcessing {

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class SettingsForPostProcessingEffect : Attribute {
		//
		// Fields
		//
		public Type m_EffectType;

		//
		// Constructors
		//
		public SettingsForPostProcessingEffect(Type postProcessingEffect) {
			m_EffectType = postProcessingEffect;
		}
	}


	[RequireComponent(typeof(Camera))]
	[ExecuteInEditMode]
	[ImageEffectAllowedInSceneView]
	[DisallowMultipleComponent]
	[AddComponentMenu("Cat/PostProcessing/Post Processing Manager")]
	public class PostProcessingManager : MonoBehaviour {
	/*
		static Dictionary<Type, Type> s_EffectsForSettings;

		private static void FindAllEffectsForSettings() {
			s_EffectsForSettings = (from t in GetAllAssemblyTypes()
				where t.IsSubclassOf(typeof(PropertyDrawer))
				where t.IsDefined(typeof(CustomPropertyDrawer), false)
				where !t.IsAbstract
				let drawerType = t
				let attributes = drawerType.GetCustomAttributes(typeof(CustomPropertyDrawer), false)
				from a in attributes
				let attributeType = a.GetType().GetField("m_Type", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(a) as Type
				select new KeyValuePair<Type, Type>(attributeType, drawerType)
			).ToDictionary(x => x.Key, x => x.Value);
		}
	*/
		//public CatPostProcessingProfile m_profile;
		public CatPostProcessingProfile profile;

		private VirtualPostProcessingProfile virtualProfile = new VirtualPostProcessingProfile();

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

		internal void RegisterCommandBuffer(PostProcessingBase effect, CameraEvent cameraEvent, CommandBuffer cb) {
			var key = new Tuple<Type, CameraEvent>(effect.GetType(), cameraEvent);
			if (m_CommandBuffers.ContainsKey(key)) {
				Debug.LogErrorFormat("Camera '{0}' already contains CommandBuffer for ({1}, {2}). Removing old buffer and adding new one", camera, effect.GetType().Name, cameraEvent);
				RemoveCommandBuffer(effect, cameraEvent, cb);
			}
			m_CommandBuffers[key] = cb;
			// UpdateCameraCommandBuffers();
		}

		internal void RemoveCommandBuffer(PostProcessingBase effect, CameraEvent cameraEvent, CommandBuffer cb) {
			camera.RemoveCommandBuffer(cameraEvent, cb);
			var key = new Tuple<Type, CameraEvent>(effect.GetType(), cameraEvent);
			if (!m_CommandBuffers.ContainsKey(key)) {
				Debug.LogWarningFormat("CommandBuffer for ({1}, {2}) was not registered while trying to remove CommandBuffer from Camera '{0}'", camera, effect.GetType().Name, cameraEvent);
				return;
			}

			m_CommandBuffers.Remove(key);
		}

		internal void RemoveAllCommandBuffers(PostProcessingBase effect) {

			foreach ( var s in m_CommandBuffers.Where(kv => kv.Key.item1 == effect.GetType()).ToList() ) {
				camera.RemoveCommandBuffer(s.Key.item2, s.Value);
				m_CommandBuffers.Remove(s.Key);
			}
		}
			
		private readonly Dictionary<Type, PostProcessingBase> m_OldEffects_helper = 
			new Dictionary<Type, PostProcessingBase>();
		
		internal void UpdateEffectsSetup() {

			virtualProfile.Reset();
			if (profile != null) {
				virtualProfile.InterpolateTo(profile, 1);
			}
			PostProcessingVoume.GetActiveProfile(this.transform, virtualProfile);


			// Detect changes in effects setup, then add new effects:
			var newEffects = (from pair in virtualProfile.settings
				let setting = pair.Value
				let settingsType = pair.Key
				where !m_OldEffects_helper.ContainsKey(setting.GetType())
				where settingsType.IsDefined(typeof(SettingsForPostProcessingEffect), false)
				//orderby setting.queueingPosition ascending
				let attributes = settingsType.GetCustomAttributes(typeof(SettingsForPostProcessingEffect), false)
				let attribute = attributes[0] as SettingsForPostProcessingEffect
				select new { type = attribute.m_EffectType, setting = setting}
			);

			foreach (var effect in newEffects) {
				var effectInstance = (PostProcessingBase)Activator.CreateInstance(effect.type);
				effectInstance.m_Settings = effect.setting;
				AddEffect(effectInstance);
			}
			if (newEffects.Any()) {
				m_Effects.Sort((x, y) => x.queueingPosition.CompareTo(y.queueingPosition));

			}

			UpdateCameraDepthTextureMode();
			//UpdateCameraCommandBuffers();
		}

		private bool m_requiresDepthTexture = false;
		internal void UpdateCameraDepthTextureMode() {
			var depthTextureMode = DepthTextureMode.None;
			if (enabled) {
				//depthTextureMode = (from pair in effects where pair.Value.enabled select pair.Value.requiredDepthTextureMode).Aggregate((l, r) => l | r);
				depthTextureMode = m_Effects
					.Where(effect => effect.enabled)
					.Select(effect => effect.requiredDepthTextureMode)
					.Aggregate(depthTextureMode, (l, r) => l | r);
				
			}
			camera.depthTextureMode = depthTextureMode;
			m_requiresDepthTexture = (depthTextureMode & DepthTextureMode.Depth) == DepthTextureMode.Depth;
		}

		internal void UpdateCameraCommandBuffers() {
			//var effects = new List<PostProcessingBase>();
			//GetComponents<PostProcessingBase>(effects);
			/*
			var activeEffects = from pair in m_Effects
				let effect = pair.Value
					where effect.enabled && effect.isActive
				select effect;
			*/
			//Debug.Log(activeEffects.Count());
			var buffers = from effect in m_Effects
				join pair in m_CommandBuffers on effect.GetType() equals pair.Key.item1
				select new { cameraEvent = pair.Key.item2, buffer = pair.Value, isActiveAndEnabled = effect.enabled }; //produces flat sequence (hopefully)
			

			//Debug.Log("Buffers: " + activeEffects.Count());
			foreach (var cb in buffers) {
				camera.RemoveCommandBuffer(cb.cameraEvent, cb.buffer);
				if (cb.isActiveAndEnabled && this.enabled) {
					camera.AddCommandBuffer(cb.cameraEvent, cb.buffer);
				}
			}

		}

		private void AddEffect(PostProcessingBase effect) {
			// Get Effects of camera
			// if camera doesn't have this effectType: add it. else throw exception?
			//
			//if (!m_Effects.ContainsKey(effect.GetType())) {
				m_OldEffects_helper.Add(effect.m_Settings.GetType(), effect);
				m_Effects.Add(effect);
			//}
			effect.InitializeEffectInternal(this);
		}

		private bool TryRemoveEffect(PostProcessingBase effect) {
			RemoveAllCommandBuffers(effect);
			m_OldEffects_helper.Remove(effect.m_Settings.GetType());
			effect.OnDestroy();
			return m_Effects.Remove(effect);
		}

		private void RemoveEffect(PostProcessingBase effect) {
			if (!TryRemoveEffect(effect)) {
				Debug.LogErrorFormat("PostProcessingBase.TryRemoveEffectFromCamera({0}, {1}) failed", camera.name, effect.effectName);
			}
		}

		private void RemoveAllEffects() {
			var tempEffects = (from effect in m_Effects select effect).ToList();
			foreach (var effect in tempEffects) {
				RemoveEffect(effect);
			}
			m_Effects.Clear();
		}

		//[ImageEffectTransformsToLDR]
		void OnRenderImage(RenderTexture source, RenderTexture destination) {
			if (!enabled) {
				Debug.LogFormat("WTF Unity?! {0}.OnRenderImage() on {1} called, even though {0} is NOT enabled.", GetType().Name, name);
				Graphics.Blit(source, destination);
			}
			var last = source;

			foreach (var effect in m_Effects.OfType<PostProcessingBaseImageEffectBasis>().Where(x => x.enabled)) {
				var current = RenderTexture.GetTemporary(last.width, last.height, 0, last.format);
				effect.RenderImage(last, current);
				if (last != source) { RenderTexture.ReleaseTemporary(last); }
				last = current;
			}

			Graphics.Blit(last, destination);

			if (last != source) {
				RenderTexture.ReleaseTemporary(last);
			}
		}

		private void OnPreCull() {
			UpdateEffectsSetup(); // TODO: This is a potential performance killer!! remove it
			var cam = this.camera;

			this.cameraSize = new VectorInt2(camera.pixelWidth, camera.pixelHeight);

			var size = this.cameraSize;

			if (m_lastCameraSize != cameraSize) {
				UpdateDepthTexture();
				m_lastCameraSize = cameraSize;
			}
			UpdateCameraDepthBufferCameraEvent(cam.actualRenderingPath);

			foreach (var effect in m_Effects.Where(x => x.enabled )) {
				effect.PreCull(cam, size);
			}
		}

		private void OnPreRender(){
			var cam = this.camera;
			var size = this.cameraSize;
			foreach (var effect in m_Effects.Where(x => x.enabled)) {
				effect.PreRender(cam, size);
			}
		}

		private void OnEnable(){
			UpdateEffectsSetup();
			UpdateCameraCommandBuffers();
			UpdateCameraDepthTextureMode();
		}

		private void OnDisable(){
			RemoveAllEffects();
			UpdateCameraDepthTextureMode();
		}

		private void OnDestroy() {
			RemoveAllEffects();

			if (m_DepthTexture != null) {
				m_DepthTexture.Release();
			}
			camera.RemoveCommandBuffer(m_DepthCommandBufferCameraEvent, depthCommandBuffer);
		}

		void OnValidate() {
			// UpdateEffectsSetup();
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


		// private readonly Dictionary<Type, PostProcessingBase> m_Effects = 
		// 	new Dictionary<Type, PostProcessingBase>();
		private readonly List<PostProcessingBase> m_Effects = 
			new List<PostProcessingBase>();
		private readonly Dictionary<Tuple<Type, CameraEvent>, CommandBuffer> m_CommandBuffers = 
			new Dictionary<Tuple<Type, CameraEvent>, CommandBuffer>();

	}


}
