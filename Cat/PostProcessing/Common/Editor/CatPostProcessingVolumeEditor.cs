using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using Cat.Common;
using Cat.PostProcessing;
using Cat.CommonEditor;
using Cat.PostProcessingEditor;

namespace Cat.PostProcessingEditor {

	[CustomEditor(typeof(PostProcessingVoume))]
	//[CanEditMultipleObjects]
	public class CatPostProcessingVolumeEditor : Editor {

		private CatPostProcessingProfileEditorWidget widget;
		private SerializedProperty m_ProfileProperty;

		public void OnEnable() {
			m_ProfileProperty = serializedObject.FindProperty("m_sharedProfile");
		}

		public override void OnInspectorGUI() {
			this.DrawDefaultInspector();


			CatEditorGUILayout.Splitter();
			CatEditorGUILayout.BeginBox();
			m_ProfileProperty.isExpanded = CatEditorGUILayout.Foldout(m_ProfileProperty.isExpanded, "Shared Profile:");
			CatEditorGUILayout.EndBox();

			if (m_ProfileProperty.isExpanded) {
				using (new EditorGUILayout.HorizontalScope()) {
					EditorGUILayout.Space();
					using (new EditorGUILayout.VerticalScope()) {
						UpdatePPEditorWidget(ref widget, m_ProfileProperty.objectReferenceValue as CatPostProcessingProfile);
						if (widget != null) {
							widget.OnInspectorGUI();
						}
					}
				}

				CatEditorGUILayout.BeginBox();
				EditorGUILayout.Space();
				CatEditorGUILayout.EndBox();
			}
		}


		void UpdatePPEditorWidget(ref CatPostProcessingProfileEditorWidget widget, CatPostProcessingProfile profileTarget) {
			if (widget == null || widget.target != profileTarget) {
				if (profileTarget != null) {
					widget = new CatPostProcessingProfileEditorWidget(profileTarget);//, serializedObject, m_ProfileProperty);
					widget.OnEnable();
				} else {
					widget = null;
				}
			}
		}


		bool DrawPropertyField(SerializedProperty prop, ref bool isFirst, bool includeChildren = false) {
			var hasNext = prop.Next(isFirst);
			isFirst = false;
			if (hasNext) {
				EditorGUILayout.PropertyField(prop, includeChildren);
			}
			return hasNext;
		}

		bool SkipPropertyField(SerializedProperty prop, ref bool isFirst) {
			var hasNext = prop.Next(isFirst);
			isFirst = false;
			return hasNext;
		}

	}
}
