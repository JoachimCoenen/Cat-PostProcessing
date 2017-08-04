using System;
using UnityEngine;
using UnityEditor;
using Cat.Common;
using Cat.PostProcessing;
using Cat.CommonEditor;

namespace Cat.PostProcessingEditor {
	using Settings = CatBloom.Settings;

	[CustomEditor(typeof(CatBloom))]
	//[CanEditMultipleObjects]
	public class CatBloomEditor : Editor {
		public SerializedProperty settings;

		public void OnEnable() {
			settings = serializedObject.FindProperty("m_Settings");
		}

		public override void OnInspectorGUI() {
			serializedObject.Update();
			EditorGUILayout.PropertyField(settings);
			serializedObject.ApplyModifiedProperties();
			EditorGUILayout.Space();
			DrawResponseFunction();
			EditorGUILayout.Space();
		}

		class Graph {
			public readonly Rect m_ViewPort;
			public readonly Rect m_Range;

			readonly Vector2 m_ParamA;
			readonly Vector2 m_ParamB;

			public Graph(Rect viewPort, Rect range) {
				m_ViewPort = viewPort;
				m_Range = range;
				m_ParamA = new Vector2(m_ViewPort.width / m_Range.width, -m_ViewPort.height / m_Range.height);
				//m_ParamB = m_ViewPort.position - Vector2.Scale(m_Range.min, m_ParamA);
				m_ParamB = m_ViewPort.position - Vector2.Scale(new Vector2(m_Range.xMin, m_Range.yMax), m_ParamA);
			}

			// Transform a point into the graph rect
			Vector2 PointToScreen(Vector2 p) {
				var x01 = (p.x - m_Range.xMin) / m_Range.width;
				var xvb = x01 * m_ViewPort.width + m_ViewPort.xMin;
				return Vector2.Scale(p, m_ParamA) + m_ParamB;
			}

			Vector3[] lineVec = new Vector3[2];
			// Draw a line in the graph rect
			public void DrawLine(Vector2 p1, Vector2 p2, float grayscale) {
				Handles.color = Color.white * grayscale;
				lineVec[0] = PointToScreen(p1);
				lineVec[1] = PointToScreen(p2);
				Handles.DrawAAPolyLine(2, lineVec);
			}

			// Draw a rect in the graph rect
			public void DrawRect(Rect rect, float fill, float line) {
				var colorFill = fill < 0 ? Color.clear : Color.white * fill;
				var colorLine = line < 0 ? Color.clear : Color.white * line;

				var min = PointToScreen(rect.min);
				var max = PointToScreen(rect.max);
				Handles.DrawSolidRectangleWithOutline(new Rect(min, max - min), colorFill, colorLine);
			}

			// Draw a grid in the graph rect
			public void DrawGrid(float grayscale) {
				// Horizontal lines
				Vector2 p0 = m_Range.min;
				Vector2 p1 = m_Range.max;
				for (var y = Mathf.Ceil(m_Range.yMin); y < m_Range.yMax; y++) {
					p0.y = y; p1.y = y;
					DrawLine(p0, p1, grayscale);
				}

				// Vertical lines
				p0 = m_Range.min;
				p1 = m_Range.max;
				for (var x = Mathf.Ceil(m_Range.xMin); x < m_Range.xMax; x++) {
					p0.x = x; p1.x =x;
					DrawLine(p0, p1, grayscale);
				}
			}

			// Draw a line in the graph rect
			public void DrawFunction(Func<float, float> f, float grayscale, int resolution = 48) {
				var curveVertices = new Vector3[resolution];
				// Response curve
				var vcount = 0;
				while (vcount < resolution) {
					var p = new Vector2();
					p.x = m_Range.x + m_Range.width * vcount / (resolution - 1f);
					p.y = f(p.x);

					if (m_Range.Contains(p)) {
						curveVertices[vcount++] = PointToScreen(p);
					} else {
						if (vcount > 1) {
							// Extend the last segment to the top edge of the rect.
							var v1 = curveVertices[vcount - 2];
							var v2 = curveVertices[vcount - 1];
							var clip = (m_ViewPort.y - v1.y) / (v2.y - v1.y);
							curveVertices[vcount - 1] = v1 + (v2 - v1) * clip;
						}
						break;
					}
				}

				if (vcount > 1) {
					Handles.color = Color.white * grayscale;
					Handles.DrawAAPolyLine(2.0f, vcount, curveVertices);
				}
			}
		}

		void DrawResponseFunction() {
			var minLuminance = settings.FindPropertyRelative("minLuminance").floatValue;
			var kneeStrength = settings.FindPropertyRelative("kneeStrength").floatValue;
			var range = new Vector2(5f, 2f);

			var rect = GUILayoutUtility.GetRect(128, 80);
			var graph = new Graph(rect, new Rect(Vector2.zero, range));

			// Background
			graph.DrawRect(graph.m_Range, 0.1f, 0.4f);
			// Grid
			graph.DrawGrid(0.4f);
			// Label
			//Handles.Label(
			//	PointInRect(0, m_RangeY) + Vector3.right,
			//	"Brightness Response (linear)", EditorStyles.miniLabel
			//);
			// Threshold line
			graph.DrawLine(new Vector2(minLuminance, graph.m_Range.yMin), new Vector2(minLuminance, graph.m_Range.yMax), 0.85f);
			// Graph
			//graph.DrawFunction(x => x * Mathf.Pow(Mathf.Max(0, x - minLuminance) / (x + 1e-1f), kneeStrength + 1), 0.1f);
			graph.DrawFunction(x => Mathf.Pow(Mathf.Max(0, x-minLuminance), kneeStrength*4 + 1) / Mathf.Pow(Mathf.Max(0, x-minLuminance) + 1e-1f, kneeStrength*4), 0.90f);
		}
	}
}
