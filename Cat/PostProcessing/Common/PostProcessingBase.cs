//#define DEBUG_OUTPUT_VERBIOUS
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Cat.Common;
namespace Cat.PostProcessing {

	public sealed class RenderTextureContainer {
		private RenderTexture rt;

		public RenderTextureContainer() {
			this.rt = null;
		}

		public RenderTextureContainer(RenderTexture aRt) {
			this.rt = aRt;
		}

		~RenderTextureContainer() {
			if (this.rt != null) {
				this.rt.Release();
			}
			this.rt = null;
		}

		internal void setRT(RenderTexture aRt) {
			if (this.rt != null) {
				this.rt.Release();
			}
			this.rt = aRt;
		}

		public override string ToString() {
			return String.Format("({0})", rt);
		}

		public static implicit operator RenderTexture(RenderTextureContainer rtc) {
			return rtc.rt;
		}
		public static implicit operator RenderTargetIdentifier(RenderTextureContainer rtc) {
			return rtc.rt;
		}
	}


	[RequireComponent(typeof(Camera), typeof(PostProcessingManager))]
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
		internal void setMaterialDirty() {
			isMaterialDirty = true;
		}
		protected int isRenderTextureDirty = 2;
		internal void setRenderTextureDirty() {
			isRenderTextureDirty = 2;
		}

		protected bool isFirstFrame { get; set; }

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

	//	private Camera m_camera = null;
	//	protected new Camera camera {
	//		get {
	//			if (m_camera == null) {
	//				m_camera = GetComponent<Camera>();
	//				if (m_camera == null) {
	//					this.enabled = false;
	//					throw new ArgumentException(String.Format("{0} requires a Camera component attached", effectName));
	//				}
	//				setRenderTextureDirty();
	//			}
	//			return m_camera;
	//		} 
	//	}

		private PostProcessingManager m_postProcessingManager = null;
		protected PostProcessingManager postProcessingManager {
			get {
				if (m_postProcessingManager == null) {
					m_postProcessingManager = GetComponent<PostProcessingManager>();
					if (m_postProcessingManager == null) {
						this.enabled = false;
						throw new ArgumentException(String.Format("{0} requires a PostProcessingManager component attached", effectName));
					}
				}
				return m_postProcessingManager;
			} 
		}


		private readonly HashSet<RenderTextureContainer> m_OldRenderTextures = new HashSet<RenderTextureContainer>();
		private readonly HashSet<RenderTextureContainer> m_RenderTextures = new HashSet<RenderTextureContainer>();

		protected void CreateRT(RenderTextureContainer rtc, VectorInt2 rtSize, int rtDepth, bool rtUseMipMap, RenderTextureFormat rtFormat, FilterMode rtFilterMode = FilterMode.Point, RenderTextureReadWrite rtReadWrite = RenderTextureReadWrite.Default, TextureWrapMode rtWrapMode = TextureWrapMode.Clamp, string rtName = "RenderTexture") {
			RenderTexture rt = rtc;
			if (rt == null // if at least one difference, release old texture and create a new one:
				|| rtSize       != new VectorInt2(rt.width, rt.height)
				|| rtDepth      != rt.depth
				|| rtUseMipMap  != rt.useMipMap
				|| rtFormat     != rt.format
				|| rtFilterMode != rt.filterMode
				|| rtReadWrite  != (rt.sRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear)
				|| rtWrapMode   != rt.wrapMode
				|| rtName       != rt.name
				|| false
			)  { 
				// if at least one difference, release old texture and create a new one:
			//	if (rt != null && rt.IsCreated()) {
			//		rt.Release();
			//	}



				rtc.setRT(new RenderTexture(Math.Max(1, rtSize.x), Math.Max(1, rtSize.y), rtDepth, rtFormat, rtReadWrite) {
					filterMode = rtFilterMode,
					wrapMode = rtWrapMode,
					useMipMap = rtUseMipMap,
					autoGenerateMips = false,
					antiAliasing = 1,
					name = rtName
				});
			}
			m_OldRenderTextures.Remove(rtc);
			m_RenderTextures.Add(rtc);



		//	m_OldRenderTextures.Remove(rt); // remove rt from OLD rts list, so it doesn't get released accidentally.
			//rt.Create();
		}

		protected void CreateRT(RenderTextureContainer rtc, VectorInt2 rtSize, int rtDepth, RenderTextureFormat rtFormat, FilterMode rtFilterMode = FilterMode.Point, RenderTextureReadWrite rtReadWrite = RenderTextureReadWrite.Default, TextureWrapMode rtWrapMode = TextureWrapMode.Clamp, string rtName = "RenderTexture") {
			CreateRT(rtc, rtSize, rtDepth, false, rtFormat, rtFilterMode, rtReadWrite, rtWrapMode, rtName);
		}

		protected void CreateRT(RenderTextureContainer rtc, VectorInt2 rtSize, RenderTextureFormat rtFormat, FilterMode rtFilterMode = FilterMode.Point, RenderTextureReadWrite rtReadWrite = RenderTextureReadWrite.Default, TextureWrapMode rtWrapMode = TextureWrapMode.Clamp, string rtName = "RenderTexture") {
			CreateRT(rtc, rtSize, 0, rtFormat, rtFilterMode, rtReadWrite, rtWrapMode, rtName);
		}
			
		protected void CreateCopyRT(RenderTextureContainer rtc, VectorInt2 rtSize, int rtDepth, bool rtUseMipMap, RenderTextureFormat rtFormat, FilterMode rtFilterMode = FilterMode.Point, RenderTextureReadWrite rtReadWrite = RenderTextureReadWrite.Default, TextureWrapMode rtWrapMode = TextureWrapMode.Clamp, string rtName = "RenderTexture") {
			RenderTexture tempRT = null;
			try {
				RenderTexture rt = rtc;
				if (rt != null && rt.IsCreated()) {
					var tempRTReadWrite = rt.sRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear;
					tempRT = RenderTexture.GetTemporary(rt.width, rt.height, rt.depth, rt.format, tempRTReadWrite, rt.antiAliasing);
					tempRT.filterMode = rt.filterMode;
					Graphics.Blit(rtc, tempRT);
					isFirstFrame = false;
				} else {
					isFirstFrame = true;
				}

				CreateRT(rtc, rtSize, rtDepth, rtUseMipMap, rtFormat, rtFilterMode, rtReadWrite, rtWrapMode, rtName);

				if (!isFirstFrame) {
					Graphics.Blit(tempRT, rtc);
				}
			} finally {
				if (tempRT != null) {
					RenderTexture.ReleaseTemporary(tempRT);
				}
			}
		}

		protected void CreateCopyRT(RenderTextureContainer rtc, VectorInt2 rtSize, int rtDepth, RenderTextureFormat rtFormat, FilterMode rtFilterMode = FilterMode.Point, RenderTextureReadWrite rtReadWrite = RenderTextureReadWrite.Default, TextureWrapMode rtWrapMode = TextureWrapMode.Clamp, string rtName = "RenderTexture") {
			CreateCopyRT(rtc, rtSize, rtDepth, false, rtFormat, rtFilterMode, rtReadWrite, rtWrapMode, rtName);
		}

		protected void CreateCopyRT(RenderTextureContainer rtc, VectorInt2 rtSize, RenderTextureFormat rtFormat, FilterMode rtFilterMode = FilterMode.Point, RenderTextureReadWrite rtReadWrite = RenderTextureReadWrite.Default, TextureWrapMode rtWrapMode = TextureWrapMode.Clamp, string rtName = "RenderTexture") {
			CreateCopyRT(rtc, rtSize, 0, rtFormat, rtFilterMode, rtReadWrite, rtWrapMode, rtName);
		}


		protected void ReleaseAllOldRTs() {
			foreach (var rtc in m_OldRenderTextures) {
				if (rtc != null) {
					rtc.setRT(null);
				}
			}
			m_OldRenderTextures.Clear(); // IMPORTANT for m_*Old*RenderTextures!!!, but not necessary for regular m_RenderTextures
		}

		protected void ReleaseAllRTs() {
			foreach (var rtc in m_RenderTextures) {
				if (rtc != null) {
					rtc.setRT(null);
				}
			}
			// m_RenderTextures.Clear(); not necessary here, but important for m_*Old*RenderTextures!
			ReleaseAllOldRTs(); // ... ,too.
		}

		private void RenewAllRenderTextures() { // find better name...
			m_OldRenderTextures.UnionWith(m_RenderTextures);
			//m_RenderTextures.Clear();
			UpdateRenderTextures(postProcessingManager.cameraSize);

			ReleaseAllOldRTs();

			#if UNITY_DEBUG && DEBUG_OUTPUT_VERBIOUS
			Debug.LogFormat("{0}.{1}.RenewAllRenderTextures(): No. of RenderTextures (created RTs / all RTs) = {2} / {3};", 
			postProcessingManager.camera.name, effectName, m_RenderTextures.Count(rtc => ((RenderTexture)rtc).IsCreated()), m_RenderTextures.Count());
			#endif
		}

		virtual protected void OnPreCull() {
			UpdateCameraMatricesPerFrame(postProcessingManager.camera);
			if (isRenderTextureDirty > 0) {
				isRenderTextureDirty--;
				if (isRenderTextureDirty >= 0) {
					RenewAllRenderTextures();
				}
			}
			if (isMaterialDirty) {
				UpdateMaterial(material);
				isMaterialDirty = false;
			}
		}

		virtual protected void OnPreRender() {
			UpdateMaterialPerFrame(material);
		}

		virtual protected void OnPostRender() {
			#if UNITY_DEBUG
			//Debug.LogFormat("RenderTextureContainer: No. of RenderTextures (created RTs / RTs not null / all RTs) = {0} / {1} / {2};\n{3};", 
			//	RenderTextureContainer.allRTs.Count(rt => (rt != null && rt.IsCreated())), 
			//	RenderTextureContainer.allRTs.Count(rt => rt != null), 
			//	RenderTextureContainer.allRTs.Count(), 
			//	String.Join(" | ", (from rtc in RenderTextureContainer.allRTs let rt = (RenderTexture)rtc where (rt != null && rt.IsCreated()) select rt.name).ToArray())
			//);
			#endif
			isFirstFrame = false;

		}

		virtual protected void OnEnable() {
			isFirstFrame = true;
			postProcessingManager.AddEffect(this);
			setMaterialDirty();
			setRenderTextureDirty();
		}

		virtual protected void OnDisable() {
			postProcessingManager.RemoveEffect(this);
			ReleaseAllRTs();
			#if UNITY_DEBUG && DEBUG_OUTPUT_VERBIOUS
			Debug.LogFormat("{0}.{1}.OnDisable(): No. of RenderTextures (created RTs / all RTs) = {2} / {3};", 
			postProcessingManager.camera.name, effectName, m_RenderTextures.Count(rt => rt.IsCreated()), m_RenderTextures.Count());
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
		abstract protected void PopulateCommandBuffer(CommandBuffer buffer, Material material, bool isFirstFrame);


		override internal void InitializeEffect() {
			base.InitializeEffect();
			postProcessingManager.RegisterCommandBuffer(this, cameraEvent, buffer);
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
				PopulateCommandBuffer(buffer, material, isFirstFrame);
				isBufferDirty = false;
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

		override protected void OnPostRender() {
			if (isFirstFrame) {
				setBufferDirty();
			}
			base.OnPostRender();
		}

		override protected void OnEnable() {
			base.OnEnable();
			setBufferDirty();
		}
	}


	[RequireComponent(typeof (Camera))]
	[ExecuteInEditMode]
	[ImageEffectAllowedInSceneView]
	public abstract class PostProcessingBaseImageEffect : PostProcessingBase {

		protected RenderTexture GetTemporaryRT(int ID_t, VectorInt2 rtSize, int rtDepth, RenderTextureFormat rtFormat, FilterMode rtFilterMode = FilterMode.Point, RenderTextureReadWrite rtReadWrite = RenderTextureReadWrite.Default) {
			var rt = RenderTexture.GetTemporary(rtSize.x, rtSize.y, rtDepth, rtFormat, rtReadWrite);
			rt.filterMode = rtFilterMode;
			return rt;
		}

		protected RenderTexture GetTemporaryRT(int ID_t, VectorInt2 rtSize, RenderTextureFormat rtFormat, FilterMode rtFilterMode = FilterMode.Point, RenderTextureReadWrite rtReadWrite = RenderTextureReadWrite.Default) {
			return GetTemporaryRT(ID_t, rtSize, 0, rtFormat, rtFilterMode, rtReadWrite);
		}

		protected RenderTexture GetTemporaryRT(Material material, int ID_t, VectorInt2 rtSize, int rtDepth, RenderTextureFormat rtFormat, FilterMode rtFilterMode = FilterMode.Point, RenderTextureReadWrite rtReadWrite = RenderTextureReadWrite.Default) {
			var rt = GetTemporaryRT(material, ID_t, rtSize, rtDepth, rtFormat, rtFilterMode, rtReadWrite);
			material.SetTexture(ID_t, rt);
			return rt;
		}

		protected RenderTexture GetTemporaryRT(Material material, int ID_t, VectorInt2 rtSize, RenderTextureFormat rtFormat, FilterMode rtFilterMode = FilterMode.Point, RenderTextureReadWrite rtReadWrite = RenderTextureReadWrite.Default) {
			return GetTemporaryRT(material, ID_t, rtSize, 0, rtFormat, rtFilterMode, rtReadWrite);
		}

		protected void ReleaseTemporaryRT(RenderTexture rt) {
			RenderTexture.ReleaseTemporary(rt);
		}

		protected void Blit(Texture mainTex, RenderTexture renderTarget) {
			Graphics.Blit(mainTex, renderTarget);
		}

		protected void Blit(Material material, int pass = -1, bool flipY = false) {
			//	GL.PushMatrix();
			//	GL.LoadOrtho();

			material.SetPass(pass);
			if (flipY) {
				GL.Begin(GL.QUADS); {
					GL.TexCoord2(0, 1); GL.Vertex3(-1, -1, 0); // left top
					GL.TexCoord2(1, 1); GL.Vertex3( 1, -1, 0); // right top
					GL.TexCoord2(1, 0); GL.Vertex3( 1,  1, 0); // right bottom
					GL.TexCoord2(0, 0); GL.Vertex3(-1,  1, 0); // left bottom
				} GL.End();
			} else {
				GL.Begin(GL.QUADS); {
					GL.TexCoord2(0, 0); GL.Vertex3(-1, -1, 0); // left top
					GL.TexCoord2(1, 0); GL.Vertex3( 1, -1, 0); // right top
					GL.TexCoord2(1, 1); GL.Vertex3( 1,  1, 0); // right bottom
					GL.TexCoord2(0, 1); GL.Vertex3(-1,  1, 0); // left bottom
				} GL.End();
			}
			//	GL.PopMatrix();
		}

		protected void Blit(Texture mainTex, Material material, int pass = -1, bool flipY = false) {
			if (mainTex != null) {
				//material.SetTexture("_MainTex", mainTex);
				material.mainTexture = mainTex;
			}
			Blit(material, pass, flipY);
		}

		protected void Blit(Texture mainTex, RenderBuffer[] renderTargets, RenderBuffer depthTarget, Material material, int pass = -1) {
			Blit(mainTex, new RenderTargetSetup(renderTargets, depthTarget), material, pass);
		}

		protected void Blit(Texture mainTex, RenderTargetSetup renderTargetSetup, Material material, int pass = -1) {
			Graphics.SetRenderTarget(renderTargetSetup);
			Blit(mainTex, material, pass);
		}

		protected void Blit(Texture mainTex, RenderTexture renderTarget, Material material, int pass = -1) {
			Graphics.SetRenderTarget(renderTarget);
			Blit(mainTex, material, pass, renderTarget == null);
		}

		protected void Blit(RenderTargetSetup renderTargetSetup, Material material, int pass = -1) {
			Blit(null, renderTargetSetup, material, pass);
		}

		protected void Blit(RenderBuffer[] renderTargets, RenderBuffer depthTarget, Material material, int pass = -1) {
			Blit(null, renderTargets, depthTarget, material, pass);
		}

		protected void Blit(RenderTexture renderTarget, Material material, int pass = -1) {
			Blit(null, renderTarget, material, pass);
		}
			
	}



}
	