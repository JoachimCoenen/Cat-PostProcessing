using System;
using UnityEngine;
using UnityEngine.Rendering;
using Cat.Common;

namespace Cat.PostProcessing {
	[RequireComponent(typeof (Camera))]
	[ExecuteInEditMode]
	[ImageEffectAllowedInSceneView]
	[AddComponentMenu("Cat/PostProcessing/Tempral Anti-Alialising")]
	public class CatAA : PostProcessingBase {
		public enum JitterMatrixType {
			ps0, ps, psy, HaltonSequence, psx, ps4
		}

		[Range(0.0f, 2.0f)]
		[SerializeField]
		float jitterStrength = 1f;

		[Range(0.0f, 2.0f)]
		[SerializeField]
		float sharpness = 0.3f;
        
		private const bool enableVelocityPrediction = true;
        
        [Range(0.0f, 80.0f)]
		[SerializeField]
        float velocityWeightScale = 20;
        
		[SerializeField]
        JitterMatrixType jitterMatrix = JitterMatrixType.ps;

		[Range(2, 16)]
		[SerializeField]
		int haltonSequenceLength = 8;

		private RenderTexture lastFrame1;
		private RenderTexture lastFrame2;
		private RenderTexture lastFrame3;

		int IDHistory1_t;
		int IDHistory2_t;
		int IDHistory3_t;
		int IDTemp_t;
		int IDJitterVelocity_v;
		int IDDirectionality_v;

		override protected string shaderName {
			get { return "Hidden/CatAA"; }
		}
		override public string effectName {
			get { return "Cat Temporal Antialialising"; }
		}
		override internal DepthTextureMode requiredDepthTextureMode {
			get { return DepthTextureMode.MotionVectors | DepthTextureMode.Depth; }
		}
		override public bool isActive { get { return true; }
		}

		override protected void UpdateRenderTextures(VectorInt2 cameraSize) {
			CreateRT(ref lastFrame1, cameraSize, RenderTextureFormat.DefaultHDR, FilterMode.Bilinear);
		//	CreateRT(ref lastFrame2, cameraSize, RenderTextureFormat.DefaultHDR, FilterMode.Bilinear);
		//	CreateRT(ref lastFrame3, cameraSize, RenderTextureFormat.DefaultHDR, FilterMode.Bilinear);
			Shader.SetGlobalTexture(IDHistory1_t, lastFrame1);
		//	Shader.SetGlobalTexture(IDHistory2_t, lastFrame2);
		//	Shader.SetGlobalTexture(IDHistory3_t, lastFrame3);
		}

		int TMSAACounter = 0;

		//[ImageEffectTransformsToLDR]
		void OnRenderImage(RenderTexture source, RenderTexture destination) {
			//	Graphics.Blit(source, lastFrame1);
			#if UNITY_EDITOR
			var isSceneView = UnityEditor.SceneView.currentDrawingSceneView != null && UnityEditor.SceneView.currentDrawingSceneView.camera == camera;
			if (isSceneView) {
				BlitNow(source, destination);
				return;
			}
			#endif

			var tempTex = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);
			BlitNow(source, tempTex, material, 0);
		//	Debug.LogFormat("{0}", destination != null);
			BlitNow(tempTex, lastFrame1);
			BlitNow(lastFrame1, destination);

			//Graphics.Blit(source, tempTex, material, 3);
			//Graphics.Blit(tempTex, lastFrame1, material, 4);
			//Graphics.Blit(tempTex, destination, material, 4);
	
			RenderTexture.ReleaseTemporary(tempTex);
		}

		override protected void UpdateCameraMatricesPerFrame(Camera camera) {
			
			Vector2[] jitterVectors = {
				Vector2.zero, Vector2.zero,
				Vector2.zero, Vector2.zero
			};
			switch (jitterMatrix) {
			case JitterMatrixType.ps0:
				jitterVectors = ps0; break;
			case JitterMatrixType.ps:
				jitterVectors = ps; break;
			case JitterMatrixType.psy:
				jitterVectors = psy; break;
			case JitterMatrixType.HaltonSequence:
				jitterVectors = HaltonSequence; break;
			case JitterMatrixType.psx:
				jitterVectors = psx; break;
			case JitterMatrixType.ps4:
				jitterVectors = ps4; break;
			}
		//	camera.ResetProjectionMatrix();
			if (jitterMatrix == JitterMatrixType.HaltonSequence) {
				TMSAACounter = (TMSAACounter + 1) % haltonSequenceLength ;
			} else {
				TMSAACounter = (TMSAACounter + 1) % 4 ;
			}

			var newP = jitterVectors[TMSAACounter] * jitterStrength;

			newP.x /= Mathf.Max(1, (float)cameraSize.x);
			newP.y /= Mathf.Max(1, (float)cameraSize.y);
		//	camera.projectionMatrix = Matrix4x4.TRS(newP, Quaternion.identity, Vector3.one) * camera.nonJitteredProjectionMatrix;
		//	camera.projectionMatrix = getOffsetMatrix(newP) * camera.nonJitteredProjectionMatrix;
			var pMat = camera.nonJitteredProjectionMatrix;
			var x = newP.x;
			pMat[0, 0] += x * pMat[2, 0];
			pMat[0, 1] += x * pMat[2, 1];
			pMat[0, 2] += x * pMat[2, 2];
			pMat[0, 3] += x * pMat[2, 3];
			var y = newP.y;
			pMat[1, 0] += y * pMat[2, 0];
			pMat[1, 1] += y * pMat[2, 1];
			pMat[1, 2] += y * pMat[2, 2];
			pMat[1, 3] += y * pMat[2, 3];
			#if UNITY_EDITOR
			var isSceneView = UnityEditor.SceneView.currentDrawingSceneView != null && UnityEditor.SceneView.currentDrawingSceneView.camera == camera;
			if (isSceneView) {
				return;
			}
			#endif
			camera.projectionMatrix = pMat;
			Shader.SetGlobalVector(IDJitterVelocity_v, 0.5f*newP);

		}

		override protected void UpdateMaterialPerFrame(Material material) {
			material.SetVector(IDDirectionality_v, new Vector2(TMSAACounter % 2 == 0 ? 1 : -1, 1));
			// NOPE!
		}

		override protected void UpdateMaterial(Material material) {
			var isSceneView = false;
		#if UNITY_EDITOR
			isSceneView = UnityEditor.SceneView.currentDrawingSceneView != null && UnityEditor.SceneView.currentDrawingSceneView.camera == camera;
		#endif
			var allowVelocityPrediction = enableVelocityPrediction && !isSceneView;
			material.SetInt("_EnableVelocityPrediction", allowVelocityPrediction ? 1 : 0);
			material.SetFloat("_VelocityWeightScale", velocityWeightScale);
			material.SetFloat("_Sharpness", sharpness);
		}

		void getShaderPropertyIDs() {
			IDHistory1_t = Shader.PropertyToID("_HistoryTex1");
			IDHistory2_t = Shader.PropertyToID("_HistoryTex2");
			IDHistory3_t = Shader.PropertyToID("_HistoryTex3");
			IDTemp_t = Shader.PropertyToID("__IDTemp_t__");

			IDJitterVelocity_v = Shader.PropertyToID("_JitterVelocity");
			IDDirectionality_v = Shader.PropertyToID("_Directionality");
		}

		override protected void OnPostRender() {
			camera.ResetProjectionMatrix();
			base.OnPostRender();
		}

		override protected void OnDisable() {
			//camera.ResetWorldToCameraMatrix();
			camera.ResetProjectionMatrix();
			base.OnDisable();
		}

		void Awake() {
			getShaderPropertyIDs();
		}

		void OnValidate () {
			setMaterialDirty();
		//	setBufferDirty();
		}

		Matrix4x4 getOffsetMatrix(Vector2 offset){
			var matrix = Matrix4x4.zero;

			//	matrix[0, 0] = 1;
			//	matrix[0, 1] = 0;
			matrix[0, 2] = offset.x;
			//	matrix[0, 3] = 0;

			//	matrix[1, 0] = 0;
			//	matrix[1, 1] = 1;
			matrix[1, 2] = offset.y;
			//	matrix[1, 3] = 0;

			//	matrix[2, 0] = 0;
			//	matrix[2, 1] = 0;
			//	matrix[2, 2] = 1;
			//	matrix[2, 3] = 0;

			//	matrix[3, 0] = 0;
			//	matrix[3, 1] = 0;
			//	matrix[3, 2] = 0;
			//	matrix[3, 3] = 1;
			return matrix;
		}


		Vector2[] HaltonSequence = {
			new Vector2(0.500000000f, 0.333333333f),
			new Vector2(0.250000000f, 0.666666667f),
			new Vector2(0.750000000f, 0.111111111f),
			new Vector2(0.125000000f, 0.444444444f),
			new Vector2(0.625000000f, 0.777777778f),
			new Vector2(0.375000000f, 0.222222222f),
			new Vector2(0.875000000f, 0.555555556f),
			new Vector2(0.062500000f, 0.888888889f),
			new Vector2(0.562500000f, 0.037037037f),
			new Vector2(0.312500000f, 0.370370370f),
			new Vector2(0.812500000f, 0.703703704f),
			new Vector2(0.187500000f, 0.148148148f),
			new Vector2(0.687500000f, 0.481481481f),
			new Vector2(0.437500000f, 0.814814815f),
			new Vector2(0.937500000f, 0.259259259f),
			new Vector2(0.031250000f, 0.592592593f),
			new Vector2(0.531250000f, 0.925925926f),
			new Vector2(0.281250000f, 0.074074074f),
			new Vector2(0.781250000f, 0.407407407f),
			new Vector2(0.156250000f, 0.740740741f),
			new Vector2(0.656250000f, 0.185185185f),
			new Vector2(0.406250000f, 0.518518519f),
			new Vector2(0.906250000f, 0.851851852f),
			new Vector2(0.093750000f, 0.296296296f),
			new Vector2(0.593750000f, 0.629629630f),
			new Vector2(0.343750000f, 0.962962963f),
			new Vector2(0.843750000f, 0.012345679f),
			new Vector2(0.218750000f, 0.345679012f),
			new Vector2(0.718750000f, 0.679012346f),
			new Vector2(0.468750000f, 0.123456790f),
			new Vector2(0.968750000f, 0.456790123f),
			new Vector2(0.015625000f, 0.790123457f)
		};

		static readonly Vector2[] ps0 = new Vector2[] {
			new Vector2(-0.75f, -0.50f),
			new Vector2( 0.50f, -0.75f ),
			new Vector2( 0.75f,  0.50f),
			new Vector2(-0.50f,  0.75f )
		};
		
		static readonly Vector2[] ps = new Vector2[] {
			new Vector2(-0.5f , -0.25f),
			new Vector2( 0.25f, -0.5f ),
			new Vector2( 0.5f ,  0.25f),
			new Vector2(-0.25f,  0.5f )
		};
		
		static readonly Vector2[] psy = new Vector2[] {
			new Vector2(-0.667f, -0.667f),
			new Vector2( 0.250f, -0.25f ),
			new Vector2( 0.667f,  0.25f),
			new Vector2(-0.250f,  0.667f )
		};
		
		static readonly Vector2[] psx = new Vector2[] {
			new Vector2(-0.166f, -0.166f) * 2f,
			new Vector2( 0.166f, -0.166f) * 2f,
			new Vector2( 0.166f,  0.166f) * 2f,
			new Vector2(-0.166f,  0.166f) * 2f
		};
        
		static readonly Vector2[] ps4 = new Vector2[] {
			(new Vector2(+0.38268f, -0.92388f)) * 0.5f,
			(new Vector2(+0.92388f, +0.38268f)) * 0.5f,
			(new Vector2(-0.38268f, +0.92388f)) * 0.5f,
			(new Vector2(-0.92388f, -0.38268f)) * 0.5f
		};
	}

}
