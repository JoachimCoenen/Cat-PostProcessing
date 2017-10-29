using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Cat.Common;

namespace Cat.CommonEditor
{
	
	public class Graph {
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
			return Vector2.Scale(p, m_ParamA) + m_ParamB;
		}

		Vector3[] lineVec = new Vector3[2];

		// Draw a line in the graph rect
		public void DrawLine(Vector2 p1, Vector2 p2, Color color, float width = 2) {
			Handles.color = color;
			lineVec[0] = PointToScreen(p1);
			lineVec[1] = PointToScreen(p2);
			Handles.DrawAAPolyLine(width, lineVec);
		}

		public void DrawLine(Vector2 p1, Vector2 p2, float grayscale, float width = 2) {
			DrawLine(p1, p2, Color.white * grayscale, width);
		}

		// Draw a rect in the graph rect
		public void DrawRect(Rect rect, Color fillColor, Color lineColor) {

			var min = PointToScreen(rect.min);
			var max = PointToScreen(rect.max);
			Handles.DrawSolidRectangleWithOutline(new Rect(min, max - min), fillColor, lineColor);
		}

		public void DrawRect(Rect rect, float fillGrayscale, float lineGrayscale) {
			var fillColor = fillGrayscale <= 0 ? Color.clear : Color.white * fillGrayscale;
			var lineColor = lineGrayscale <= 0 ? Color.clear : Color.white * lineGrayscale;
			DrawRect(rect, fillColor, lineColor);
		}

		// Draw a grid in the graph rect
		public void DrawGrid(Vector2 resolution, Color color, float width = 2) {
			// Horizontal lines
			Vector2 p0 = m_Range.min;
			Vector2 p1 = m_Range.max;
			var xStep = 1f / Mathf.Max(resolution.x, float.Epsilon);
			var yStep = 1f / Mathf.Max(resolution.y, float.Epsilon);

			for (var y = Mathf.Ceil(m_Range.yMin); y < m_Range.yMax; y += yStep) {
				p0.y = y; p1.y = y;
				DrawLine(p0, p1, color, width);
			}

			// Vertical lines
			p0 = m_Range.min;
			p1 = m_Range.max;
			for (var x = Mathf.Ceil(m_Range.xMin); x < m_Range.xMax; x += xStep) {
				p0.x = x; p1.x =x;
				DrawLine(p0, p1, color, width);
			}
		}

		public void DrawGrid(Vector2 resolution, float grayscale, float width = 2) {
			DrawGrid(resolution, Color.white * grayscale, width);
		}

		public void DrawGrid(Color color) {
			DrawGrid(new Vector2(1, 1), color);
		}

		public void DrawGrid(float grayscale) {
			DrawGrid(new Vector2(1, 1), Color.white * grayscale);
		}

		private Vector2 ClampVector(Vector2 origin, Vector2 terminal) {
			var o = origin;
			var p = terminal;

			var op = p - o;
			var corner = new Vector2(op.x > 0 ? m_Range.xMax : m_Range.xMin, op.y > 0 ? m_Range.yMax : m_Range.yMin);
			var oc = corner - o;
			var mult = Mathf.Min(Mathf.Abs(oc.x / op.x), Mathf.Abs(oc.y / op.y));

			return op * mult + o;
		}

		// Draw a line in the graph rect
		public void DrawFunction(Func<float, float> f, Color color, int resolution = 48) {
			var curveVertices = new Vector3[resolution];
			// Response curve
			var vcount = 0;
			bool wasOutside = false;
			var o = new Vector2();
			for (int i = 0; i < resolution && vcount < resolution; i++) {
				var p = new Vector2();
				p.x = m_Range.x + m_Range.width * i / (resolution - 1f);
				p.y = f(p.x);

				if (m_Range.Contains(p)) {
					if (wasOutside) {
						curveVertices[vcount++] = PointToScreen(ClampVector(p, o));
					}
					curveVertices[vcount++] = PointToScreen(p);
					wasOutside = false;
				} else {
					if (!wasOutside) {
						// Draw the segment to the edge of the rect.
						curveVertices[vcount++] = PointToScreen(ClampVector(o, p));

						// // Extend the last segment to the top edge of the rect.
						// var v1 = curveVertices[vcount - 2];
						// var v2 = curveVertices[vcount - 1];
						// var clip = (m_ViewPort.y - v1.y) / (v2.y - v1.y);
						// curveVertices[vcount - 1] = v1 + (v2 - v1) * clip;
					//	break;
					}
					wasOutside = true;
				}
				o = p;
			}

			if (vcount > 1) {
				Handles.color = color;
				Handles.DrawAAPolyLine(2.0f, vcount, curveVertices);
			}
		}

		public void DrawFunction(Func<float, float> f, float grayscale, int resolution = 48) {
			DrawFunction(f, Color.white * grayscale, resolution);
		}

	}

}