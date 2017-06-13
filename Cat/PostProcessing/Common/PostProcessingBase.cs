using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Cat.Common;

namespace Cat.PostProcessing {

	[RequireComponent(typeof (Camera))]
	[ExecuteInEditMode]
	[ImageEffectAllowedInSceneView]
	public abstract class PostProcessingBase : MonoBehaviour {
		abstract protected string shaderName { get; }
		abstract public string effectName { get; }
		abstract internal DepthTextureMode requiredDepthTextureMode { get; }
		abstract public bool isActive { get; }

		abstract protected void UpdateMaterial(Material material);
		virtual internal void InitializeEffect() {}
		virtual protected void UpdateRenderTextures(VectorInt2 cameraSize) {}
		virtual protected void UpdateCameraMatricesPerFrame(Camera camera) {}
		virtual protected void UpdateMaterialPerFrame(Material material) {}

		private bool isMaterialDirty = false;
		protected void setMaterialDirty() {
			isMaterialDirty = true;
		}
		private bool isRenderTextureDirty  = true;
		protected void setRenderTextureDirty() {
			isRenderTextureDirty = true;
		}

		private Material m_Material = null;
		protected Material material {
			get {
				if (m_Material == null) {
					var shader = Shader.Find(shaderName);
					if (shader == null) {
						this.enabled = false;
						throw new ArgumentException(String.Format("Shader not found: '{0}'", shaderName));
					}
					m_Material = new Material(shader);
					m_Material.hideFlags = HideFlags.DontSave;
					setMaterialDirty();
				}
				return m_Material;
			} 
		}

		private Camera m_camera = null;
		protected new Camera camera {
			get {
				if (m_camera == null) {
					m_camera = GetComponent<Camera>();
					if (m_camera == null) {
						this.enabled = false;
						throw new ArgumentException(String.Format("{0} requires a Camera component attached", effectName));
					}
					setRenderTextureDirty();
				}
				return m_camera;
			} 
		}

		protected VectorInt2 cameraSize { get; private set; }

		private HashSet<RenderTexture> m_RenderTextures = new HashSet<RenderTexture>();

		protected void CreateRT(ref RenderTexture rt, VectorInt2 rtSize, int rtDepth, RenderTextureFormat rtFormat, FilterMode rtFilterMode = FilterMode.Point, RenderTextureReadWrite rtReadWrite = RenderTextureReadWrite.Default, TextureWrapMode rtWrapMode = TextureWrapMode.Clamp, string rtName = "RenderTexture") {
			if (rt != null && rt.IsCreated()) {
				rt.Release();
			}
			m_RenderTextures.Remove(rt);

			rt = new RenderTexture(Math.Max(1, rtSize.x), Math.Max(1, rtSize.y), rtDepth, rtFormat, rtReadWrite) {
				filterMode = rtFilterMode,
				wrapMode = rtWrapMode,
				useMipMap = false,
				autoGenerateMips = false,
				antiAliasing = 1,
				name = rtName
			};
			m_RenderTextures.Add(rt);
			rt.Create();
		}

		protected void CreateRT(ref RenderTexture rt, VectorInt2 rtSize, RenderTextureFormat rtFormat, FilterMode rtFilterMode = FilterMode.Point, RenderTextureReadWrite rtReadWrite = RenderTextureReadWrite.Default, TextureWrapMode rtWrapMode = TextureWrapMode.Clamp, string rtName = "RenderTexture") {
			CreateRT(ref rt, rtSize, 0, rtFormat, rtFilterMode, rtReadWrite, rtWrapMode, rtName);
		}

		protected void ReleaseAllRTs() {
			foreach (var rt in m_RenderTextures) {
				rt.Release();
			}
		}

		protected void GetTemporaryRT(CommandBuffer cb, int ID_t, VectorInt2 rtSize, int rtDepth, RenderTextureFormat rtFormat, FilterMode rtFilterMode = FilterMode.Point, RenderTextureReadWrite rtReadWrite = RenderTextureReadWrite.Default) {
			cb.GetTemporaryRT(
				ID_t, rtSize.x, rtSize.y, rtDepth, 
				rtFilterMode, 
				rtFormat, 
				rtReadWrite, 
				1, 
				false
			);
		}

		protected void GetTemporaryRT(CommandBuffer cb, int ID_t, VectorInt2 rtSize, RenderTextureFormat rtFormat, FilterMode rtFilterMode = FilterMode.Point, RenderTextureReadWrite rtReadWrite = RenderTextureReadWrite.Default) {
			GetTemporaryRT(cb, ID_t, rtSize, 0, rtFormat, rtFilterMode, rtReadWrite);
		}

		protected void ReleaseTemporaryRT(CommandBuffer cb, int ID_t) {
			cb.ReleaseTemporaryRT(ID_t);
		}

		protected void BlitNow(Texture mainTex, RenderTexture renderTarget) {
			Graphics.Blit(mainTex, renderTarget);
		}

		protected void BlitNow(Material material, int pass = -1) {
			//	GL.PushMatrix();
			//	GL.LoadOrtho();

			material.SetPass(pass);

			GL.Begin(GL.QUADS); {
				GL.TexCoord2(0, 0); GL.Vertex3(-1, -1, 0); // left top
				GL.TexCoord2(1, 0); GL.Vertex3( 1, -1, 0); // right top
				GL.TexCoord2(1, 1); GL.Vertex3( 1,  1, 0); // right bottom
				GL.TexCoord2(0, 1); GL.Vertex3(-1,  1, 0); // left bottom
			} GL.End();

			//	GL.PopMatrix();
		}

		protected void BlitNow(Texture mainTex, Material material, int pass = -1) {
			if (mainTex != null) {
				material.SetTexture("_MainTex", mainTex);
			}
			BlitNow(material, pass);
		}

		protected void BlitNow(Texture mainTex, RenderBuffer[] renderTargets, RenderBuffer depthTarget, Material material, int pass = -1) {
			BlitNow(mainTex, new RenderTargetSetup(renderTargets, depthTarget), material, pass);
		}

		protected void BlitNow(Texture mainTex, RenderTargetSetup renderTargetSetup, Material material, int pass) {
			Graphics.SetRenderTarget(renderTargetSetup);
			BlitNow(mainTex, material, pass);
		}

		protected void BlitNow(Texture mainTex, RenderTexture renderTarget, Material material, int pass = -1) {
			Graphics.SetRenderTarget(renderTarget);
			BlitNow(mainTex, material, pass);
		}

		protected void BlitNow(RenderTargetSetup renderTargetSetup, Material material, int pass = -1) {
			BlitNow(null, renderTargetSetup, material, pass);
		}

		protected void BlitNow(RenderBuffer[] renderTargets, RenderBuffer depthTarget, Material material, int pass = -1) {
			BlitNow(null, renderTargets, depthTarget, material, pass);
		}

		protected void BlitNow(RenderTexture renderTarget, Material material, int pass = -1) {
			BlitNow(null, renderTarget, material, pass);
		}
			
		virtual protected void OnPreCull() {
			UpdateCameraMatricesPerFrame(camera);
		}

		virtual protected void OnPreRender() {
			if (isRenderTextureDirty) {
				ReleaseAllRTs();
				UpdateRenderTextures(cameraSize);
				#if UNITY_DEBUG
				Debug.LogFormat("{0}.{1}.OnPreRender(): No. of RenderTextures (created RTs / all RTs) = {2} / {3};", 
					camera.name, effectName, m_RenderTextures.Count(rt => rt.IsCreated()), m_RenderTextures.Count());
				#endif
			}
			if (isMaterialDirty) {
				UpdateMaterial(material);
			}
			isRenderTextureDirty = false;
			isMaterialDirty = false;
			UpdateMaterialPerFrame(material);
		}

		virtual protected void OnPostRender() {
			// This is to overcome a bug, where Camera.pixelWidth & Camera.pixelHeight 
			// return a slightly wrong value in the editor scene view.
			var newCameraSize = new VectorInt2(camera.activeTexture.width, camera.activeTexture.height);
			if (newCameraSize != cameraSize) {
				cameraSize = newCameraSize;
				setRenderTextureDirty();
			}
		}

		virtual protected void OnEnable() {
			PostProcessingManager.AddEffect(this, camera);
			setRenderTextureDirty();
		}

		virtual protected void OnDisable() {
			PostProcessingManager.RemoveEffect(this, camera);
			ReleaseAllRTs();
			#if UNITY_DEBUG
			Debug.LogFormat("{0}.{1}.OnDisable(): No. of RenderTextures (created RTs / all RTs) = {2} / {3};", 
				camera.name, effectName, m_RenderTextures.Count(rt => rt.IsCreated()), m_RenderTextures.Count());
			#endif
		}

		virtual protected void OnDestroy() {
			enabled = false;
			// becuase the 'DontSave' flag is set, we have to destroy the Material explicitly:
			DestroyImmediate(m_Material);
			m_Material = null;
			ReleaseAllRTs();
		}

		static Mesh s_BlitQuad;
		public static Mesh blitQuad
		{
			get
			{
				if (s_BlitQuad == null) {
					s_BlitQuad = new Mesh {
						vertices = new[] {
							new Vector3(-1f, -1f, 0f),
							new Vector3( 1f,  1f, 0f),
							new Vector3( 1f, -1f, 0f),
							new Vector3(-1f,  1f, 0f)
						},
						//vertices = new[] {
						//	new Vector3( 0,  0, 0.1f),
						//	new Vector3( 1,  1, 0.1f),
						//	new Vector3( 1,  0, 0.1f),
						//	new Vector3( 0,  1, 0.1f)
						//},
						uv = new[] {
							new Vector2(0, 0),
							new Vector2(1, 1),
							new Vector2(1, 0),
							new Vector2(0, 1)
						},
						triangles = new[] { 0, 1, 2, 1, 0, 3 }
					};
					s_BlitQuad.RecalculateNormals();
					s_BlitQuad.RecalculateBounds();
				}
				return s_BlitQuad;
			}
		}
	}


	[RequireComponent(typeof (Camera))]
	[ExecuteInEditMode]
	[ImageEffectAllowedInSceneView]
	public abstract class PostProcessingBaseCommandBuffer : PostProcessingBase {

		abstract protected CameraEvent cameraEvent { get; }
		abstract protected void PopulateCommandBuffer(CommandBuffer buffer, Material material);


		override internal void InitializeEffect() {
			base.InitializeEffect();
			PostProcessingManager.RegisterCommandBuffer(this, camera, cameraEvent, buffer);
		}
			
		private bool isBufferDirty  = false;
		protected void setBufferDirty() {
			isBufferDirty = true;
		}

		private CommandBuffer m_Buffer;
		private CommandBuffer buffer {
			get {
				if (m_Buffer == null) {
					m_Buffer = new CommandBuffer();
					m_Buffer.name = effectName;
					setBufferDirty();
				}
				return m_Buffer;

			} 
		}
			
		override protected void OnPreRender() {
			base.OnPreRender();
			if (isBufferDirty) {
				buffer.Clear();
				PopulateCommandBuffer(buffer, material);
			}
			isBufferDirty = false;
			UpdateMaterialPerFrame(material);
		}

		protected void Blit(CommandBuffer cb, RenderTargetIdentifier mainTex, RenderTargetIdentifier renderTarget) {
			cb.Blit(mainTex, renderTarget);
		}

		protected void Blit(CommandBuffer cb, Material material, int pass = -1) {
			cb.DrawMesh(blitQuad, Matrix4x4.identity, material, 0, pass);
		}

		protected void Blit(CommandBuffer cb, RenderTargetIdentifier mainTex, RenderTargetIdentifier[] renderTargets, RenderTargetIdentifier depthTarget, Material material, int pass = -1) {
			if (mainTex != BuiltinRenderTextureType.None) {
				cb.SetGlobalTexture("_MainTex", mainTex);
			}
			if (depthTarget != BuiltinRenderTextureType.None || renderTargets.Any(rt => rt != BuiltinRenderTextureType.None)) {
				cb.SetRenderTarget(renderTargets, depthTarget);
			}
			Blit(cb, material, pass);
		}

		protected void Blit(CommandBuffer cb, RenderTargetIdentifier mainTex, RenderTargetIdentifier[] renderTargets, Material material, int pass = -1) {
			Blit(cb, mainTex, renderTargets, BuiltinRenderTextureType.None, material, pass);
		}

		protected void Blit(CommandBuffer cb, RenderTargetIdentifier mainTex, RenderTargetIdentifier renderTarget, Material material, int pass = -1) {
			Blit(cb, mainTex, new [] { renderTarget }, renderTarget, material, pass);
		}

		protected void Blit(CommandBuffer cb, RenderTargetIdentifier[] renderTargets, RenderTargetIdentifier depthTarget, Material material, int pass = -1) {
			Blit(cb, BuiltinRenderTextureType.None, renderTargets, depthTarget, material, pass);
		}

		protected void Blit(CommandBuffer cb, RenderTargetIdentifier[] renderTargets, Material material, int pass = -1) {
			Blit(cb, BuiltinRenderTextureType.None, renderTargets, material, pass);
		}

		protected void Blit(CommandBuffer cb, RenderTargetIdentifier renderTarget, Material material, int pass = -1) {
			Blit(cb, BuiltinRenderTextureType.None, renderTarget, material, pass);
		}

	}
}
