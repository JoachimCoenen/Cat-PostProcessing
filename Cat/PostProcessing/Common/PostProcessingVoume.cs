using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Cat.PostProcessing {
	[ExecuteInEditMode]
	public class PostProcessingVoume : MonoBehaviour {
		static List<PostProcessingVoume> s_Volumes;
		static List<PostProcessingVoume> Volumes {
			get {
				return s_Volumes ?? (s_Volumes = new List<PostProcessingVoume>()); 
			}
		}

		static void AddVolume(PostProcessingVoume volume) {
			Volumes.Add(volume);
		}

		static void RemoveVolume(PostProcessingVoume volume) {
			Volumes.Remove(volume);
		}

		// public static void GetActiveProfiles(Transform transform, IOrderedEnumerable<IGrouping<int, object>> profiles) {
		// 	var wsRay = new Ray(transform.position, transform.TransformDirection(Vector3.forward));
		// 	var wsPos = transform.position;
		// 
		// 	var filteredVolumes = 
		// 		from v in Volumes
		// 		let allColliders = v.GetColliders()
		// 			where allColliders.Count() > 0
		// 		let closestCollision = allColliders.Aggregate(
		// 			new { closestPoint = Vector3.zero, sqrDistance = float.PositiveInfinity },
		// 			(a, c) => {
		// 				var closestPointC = c.ClosestPoint(wsPos);
		// 				var sqrDistanceC = (closestPointC - wsPos).sqrMagnitude;
		// 				if (sqrDistanceC < a.sqrDistance) {
		// 					a.closestPoint = closestPointC;
		// 					a.sqrDistance = sqrDistanceC;
		// 				}
		// 				return a;
		// 			}
		// 		)
		// 			where closestCollision.sqrDistance < v.BlendDistance
		// 		group new { volume = v, collision = closestCollision } by v.Importance into volumeGroup
		// 		orderby volumeGroup.Key ascending
		// 		select volumeGroup;
		// 
		// 	var filteredVolumes = 
		// 		from v in Volumes
		// 		let allColliders = v.GetColliders()
		// 			where allColliders.Count() > 0
		// 			where allColliders.Any(c => 0 == (c.ClosestPoint(wsPos) - wsPos).sqrMagnitude)
		// 		orderby v.Importance ascending
		// 		select v.m_sharedProfile;
		// 	return filteredVolumes.LastOrDefault();
		// }

		public static void GetActiveProfile(Transform transform, VirtualPostProcessingProfile virtualProfile) {
			Assert.IsNotNull(virtualProfile);

			var wsRay = new Ray(transform.position, transform.TransformDirection(Vector3.forward));
			var wsPos = transform.position;

			// Filter all volumes, that have an effect on the camera and group them by their Importance:
			var filteredVolumes = 
				from v in Volumes
				let allColliders = v.GetColliders()
				where allColliders.Any()
				let closestCollision = allColliders.Aggregate(
					new { closestPoint = Vector3.zero, sqrDistance = float.PositiveInfinity },
					(a, c) => {
						var closestPointC = c.ClosestPoint(wsPos);
						var sqrDistanceC = (closestPointC - wsPos).sqrMagnitude;
						return (sqrDistanceC >= a.sqrDistance) ? a : new { closestPoint = closestPointC, sqrDistance = sqrDistanceC };
					}
				)
				where closestCollision.sqrDistance < v.BlendDistance
				group new { volume = v, collision = closestCollision } by v.Importance into volumeGroup
				orderby volumeGroup.Key ascending
				select volumeGroup;

			foreach (var volumeGroup in filteredVolumes) {
				foreach (var volume in volumeGroup) {
					if (volume.volume.m_sharedProfile == null)
						continue;
					var t1 = 1f - Mathf.Clamp01(volume.collision.sqrDistance / volume.volume.BlendDistance);
					if (t1 <= Mathf.Epsilon)
						continue;
					virtualProfile.InterpolateTo(volume.volume.m_sharedProfile, t1);
					
				}
			}


		}

		[SerializeField]
		private CatPostProcessingProfile m_sharedProfile;

		[SerializeField]
		public float BlendDistance = 1;

		[SerializeField]
		public int Importance = 1;


		internal IEnumerable<Collider> GetColliders() {
			// TODO: Optimize !!!
			var colliders = new List<Collider>();
			GetComponents<Collider>(colliders);
			return colliders;
		}

		void OnEnable() {
			AddVolume(this);
		}

		void OnDisable() {
			RemoveVolume(this); 
		}

		// Use this for initialization
		void Start () {
			
		}
		
		// Update is called once per frame
		void Update () {
			
		}

		void OnDrawGizmos() {
			Gizmos.color = new Color(0.1f, 0.2f, 0.8f, 0.5f);
			Gizmos.matrix = transform.localToWorldMatrix;
			var colliders = GetColliders();
			foreach (var collider in colliders) {
				var type = collider.GetType();
				if (collider is BoxCollider) {
					var boxCollider = collider as BoxCollider;
					Gizmos.DrawCube(boxCollider.center, boxCollider.size);
				//	Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
				} else if (collider is SphereCollider ) {
					var sphereCollider = collider as SphereCollider;
					Gizmos.DrawSphere(sphereCollider.center, sphereCollider.radius);
				} else if(collider is MeshCollider) {
					var meshCollider = collider as MeshCollider;
					Gizmos.DrawMesh(meshCollider.sharedMesh);
				//	Gizmos.DrawWireMesh(meshCollider.sharedMesh);
				}
			}

		}

		// TODO: Look into a better volume previsualization system

	}

}
