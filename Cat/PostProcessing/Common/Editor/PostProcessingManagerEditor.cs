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

	[CustomEditor(typeof(PostProcessingManager))]
	//[CanEditMultipleObjects]
	public class PostProcessingManagerEditor : Editor {
		
		private PostProcessingManager m_Manager;
		private VirtualPostProcessingProfile m_VirtualProfile;

		private SerializedProperty m_ProfileProperty;

		private CatPostProcessingProfileEditorWidget widget;
		private CatPostProcessingProfileEditorWidget virtualWidget;

		public void OnEnable() {
			m_Manager = target as PostProcessingManager;
			m_VirtualProfile = m_Manager.virtualProfile;
			m_ProfileProperty = serializedObject.FindProperty("profile");
		}

		public override void OnInspectorGUI() {
			// EditorGUILayout.PropertyField(serializedObject.FindProperty("m_settings"), true);
			// EditorGUILayout.Space();
			serializedObject.Update();
			EditorGUILayout.PropertyField(m_ProfileProperty);
			CatEditorGUILayout.Splitter();

			UpdatePPEditorWidget(ref widget, m_ProfileProperty.objectReferenceValue as CatPostProcessingProfile);
			if (widget != null) {
				widget.OnInspectorGUI();
			}
			
			EditorGUILayout.Space();

			/* //for Debugging purposes only:
			CatEditorGUILayout.Header("Tracked Settings:");
			foreach (var pair in m_VirtualProfile.settings) {
				var effect = pair.Value;
				using (new EditorGUI.DisabledScope(!effect.enabled)) {
					EditorGUILayout.LabelField(effect.effectName);
				}
			}
			*/

			CatEditorGUILayout.Splitter();
			CatEditorGUILayout.Header("Active Effects:");
			foreach (var effect in m_Manager.m_ActiveEffects) {
				using (new EditorGUI.DisabledScope(!effect.enabled)) {
					EditorGUILayout.LabelField(effect.effectName);
				}
			}

			/* //for Debugging purposes only:
			CatEditorGUILayout.Splitter();
			CatEditorGUILayout.Header("OldEffects_helper:");
			foreach (var pair in m_Manager.m_OldEffects_helper) {
				var effect = pair.Value;
				using (new EditorGUI.DisabledScope(!effect.enabled)) {
					EditorGUILayout.LabelField(effect.effectName);
				}
			}
			*/
			if (m_VirtualProfile.settings.Count == 0) {
				EditorGUILayout.HelpBox("No Post-Processing effects", MessageType.Info);
			}
			CatEditorGUILayout.Splitter();
			EditorGUILayout.Space();
			serializedObject.ApplyModifiedProperties();
		}

		void UpdatePPEditorWidget(ref CatPostProcessingProfileEditorWidget widget, CatPostProcessingProfile profileTarget) {
			Debug.Log($"{profileTarget}, UpdatePPEditorWidget");
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
