using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Cat.Common;

namespace Cat.PostProcessing {

	public static class PostProcessingManager {
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
		
		static readonly Dictionary<Light, Dictionary<Type, PostProcessingBase>> m_LightPostProcessingEffects = 
			new Dictionary<Light, Dictionary<Type, PostProcessingBase>>();
		static readonly Dictionary<Light, Dictionary<Tuple<Type, LightEvent>, CommandBuffer>> m_LightCommandBuffers = 
			new Dictionary<Light, Dictionary<Tuple<Type, LightEvent>, CommandBuffer>>();


		internal static void RegisterCommandBuffer(PostProcessingBase effect, Camera camera, CameraEvent cameraEvent, CommandBuffer cb) {
			Dictionary<Tuple<Type, CameraEvent>, CommandBuffer> cbs;
			if (!m_CameraCommandBuffers.TryGetValue(camera, out cbs)) {
				cbs = new Dictionary<Tuple<Type, CameraEvent>, CommandBuffer>();
				m_CameraCommandBuffers.Add(camera, cbs);
			}

			var key = new Tuple<Type, CameraEvent>(effect.GetType(), cameraEvent);
			if (cbs.ContainsKey(key)) {
				Debug.LogErrorFormat("Camera '{0}' already contains CommandBuffer for ({1}, {2}). Removing old buffer and adding new one", camera, effect.GetType().Name, cameraEvent);
				RemoveCommandBuffer(effect, camera, cameraEvent, cb);
			}
			cbs[key] = cb;
			camera.AddCommandBuffer(cameraEvent, cb);
		}

		internal static void RemoveCommandBuffer(PostProcessingBase effect, Camera camera, CameraEvent cameraEvent, CommandBuffer cb) {
			camera.RemoveCommandBuffer(cameraEvent, cb);
			Dictionary<Tuple<Type, CameraEvent>, CommandBuffer> cbs;
			if (!m_CameraCommandBuffers.TryGetValue(camera, out cbs)) {
				Debug.LogWarningFormat("Camera '{0}' was not registered while trying to remove CommandBuffer for ({1}, {2})", camera, effect.GetType().Name, cameraEvent);
				return;
			}
			var key = new Tuple<Type, CameraEvent>(effect.GetType(), cameraEvent);
			if (!cbs.ContainsKey(key)) {
				Debug.LogErrorFormat("CommandBuffer for ({1}, {2}) was not registered while trying to remove CommandBuffer from Camera '{0}'", camera, effect.GetType().Name, cameraEvent);
				return;
			}

			cbs.Remove(key);
			if (cbs.Count == 0) {
				m_CameraCommandBuffers.Remove(camera);
			}
		}

		internal static void RemoveAllCommandBuffers(PostProcessingBase effect, Camera camera) {
			Dictionary<Tuple<Type, CameraEvent>, CommandBuffer> cbs;
			if (!m_CameraCommandBuffers.TryGetValue(camera, out cbs)) {
				Debug.LogWarningFormat("Camera '{0}' was not registered while trying to remove all CommandBuffers for {1}", camera, effect.GetType().Name);
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
	
		internal static void UpdateCameraDepthTextureMode(Camera aCamera) {
			var depthTextureMode = DepthTextureMode.None;
			Dictionary<Type, PostProcessingBase> effectsList;
			if (m_CameraPostProcessingEffects.TryGetValue(aCamera, out effectsList)) {
				foreach (var effect in effectsList) {
					depthTextureMode |= effect.Value.requiredDepthTextureMode;
				}
			}

			aCamera.depthTextureMode = depthTextureMode;
		}

		internal static void AddEffect(PostProcessingBase effect, Camera camera) {
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
				UpdateCameraDepthTextureMode(camera);
			}
			effect.InitializeEffect();
		}

		internal static bool TryRemoveEffect(PostProcessingBase effect, Camera camera) {
			if (camera != null) {
				RemoveAllCommandBuffers(effect, camera);
				Dictionary<Type, PostProcessingBase> effectsList;
				if (m_CameraPostProcessingEffects.TryGetValue(camera, out effectsList)) {
					if (effectsList.ContainsKey(effect.GetType())) {
						var wasSuccessfull = effectsList.Remove(effect.GetType());
						UpdateCameraDepthTextureMode(camera);
					}
					if (effectsList.Count == 0) {
						m_CameraPostProcessingEffects.Remove(camera);
					}
				}
			}
			return true; // JCO@@@ TODO: TryRemoveEffectFromCamera return value!!! TUT
		}

		internal static void RemoveEffect(PostProcessingBase effect, Camera camera) {
			if (!TryRemoveEffect(effect, camera)) {
				Debug.LogErrorFormat("PostProcessingBase.TryRemoveEffectFromCamera({0}, {1}) failed", camera.name, effect.effectName);
			}
		}

	
	}
}
