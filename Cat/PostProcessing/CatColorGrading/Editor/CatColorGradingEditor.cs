using System;
using UnityEngine;
using UnityEditor;
using Cat.Common;
using Cat.PostProcessing;
using Cat.CommonEditor;

namespace Cat.PostProcessingEditor {

	[CustomEditor(typeof(CatColorGrading))]
	//[CanEditMultipleObjects]
	public class CatColorGradingEditor : Editor {
		public SerializedProperty settings;

		public void OnEnable() {
			settings = serializedObject.FindProperty("m_Settings");
		}

		public override void OnInspectorGUI() {
			serializedObject.Update();
			EditorGUILayout.PropertyField(settings);


			serializedObject.ApplyModifiedProperties();
			EditorGUILayout.Space();
			DrawResponseFunction(128, 128);
			EditorGUILayout.Space();
		}

		void DrawResponseFunction(float width, float height) {
			var blackPoint = settings.FindPropertyRelative("blackPoint").floatValue;
			var grayPoint  = settings.FindPropertyRelative("midPoint").floatValue;
			var whitePoint = settings.FindPropertyRelative("whitePoint").floatValue;

			var black = 0.0f + blackPoint * 0.25f;
			var gray  = 0.5f + grayPoint  * 0.125f;
			var white = 1.0f + whitePoint * 0.25f;

			var minX = Mathf.Min(0f, black);
			var maxX = Mathf.Max(1f, white);
			var range = new Rect(
				minX, 0f,
				maxX - minX, 1f
			);

			var rect = GUILayoutUtility.GetRect(width, height);
			var graph = new Graph(rect, range);

			// Background
			graph.DrawRect(graph.m_Range, 0.1f, 0.4f);

			// Grid
			graph.DrawGrid(new Vector2(4, 4), 0.4f, 2);
			graph.DrawGrid(new Vector2(2, 2), 0.4f, 3);
			// Label
			//Handles.Label(
			//	PointInRect(0, m_RangeY) + Vector3.right,
			//	"Brightness Response (linear)", EditorStyles.miniLabel
			//);

			// Threshold Range
			var thresholdRect = new Rect(black, graph.m_Range.y, white - black, graph.m_Range.height);
			graph.DrawRect(thresholdRect, 0.25f, -1f);
			// Threshold line
			graph.DrawLine(new Vector2(black, graph.m_Range.yMin), new Vector2(black, graph.m_Range.yMax), 0.85f);
			graph.DrawLine(new Vector2(white, graph.m_Range.yMin), new Vector2(white, graph.m_Range.yMax), 0.85f);
			var grayLerp = Mathf.Lerp(black, white, gray);
			graph.DrawLine(new Vector2(grayLerp,  graph.m_Range.yMin), new Vector2(grayLerp,  graph.m_Range.yMax), 0.85f);

			// Graph
			//graph.DrawFunction(x => x * Mathf.Pow(Mathf.Max(0, x - minLuminance) / (x + 1e-1f), kneeStrength + 1), 0.1f);
			var curveParams = (serializedObject.targetObject as CatColorGrading).settings.GetCurveParams();
			graph.DrawFunction(
				x => {
					x = (x - black) / (white - black);
					x = Mathf.Clamp01(x);
					return (curveParams.w + (curveParams.z + (curveParams.y + curveParams.x*x)*x)*x)*x;
				}
				, 0.90f);
		}
	}
}
