using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using Cat.Common;
using Cat.PostProcessing;
using Cat.CommonEditor;
using UnityEditor.Cat.PostProcessingEditor;

namespace Cat.PostProcessingEditor {

	[CustomEditor(typeof(CatPostProcessingProfile))]
	//[CanEditMultipleObjects]
	public class CatPostProcessingProfileEditor : Editor {
		
		private CatPostProcessingProfile m_profile;

		SerializedProperty m_SettingsProperty;
		Dictionary<Type, Type> m_EditorTypes; // SettingsType => EditorType
		List<CatPostProcessingEditorBase> m_Editors = new List<CatPostProcessingEditorBase>();

		public void OnEnable() {
			m_profile = target as CatPostProcessingProfile;

			m_SettingsProperty = serializedObject.FindProperty("m_settings");
			Assert.IsNotNull(m_SettingsProperty);

			FindAllEditors();
			UpdateAllEditors();
		}

		private void FindAllEditors() {
			m_EditorTypes = (from t in GetAllAssemblyTypes()
			                   where t.IsSubclassOf(typeof(CatPostProcessingEditorBase))
			                   where t.IsDefined(typeof(CatPostProcessingEditorAttribute), false)
			                   where !t.IsAbstract
			                   let editorType = t
			                   let attributes = editorType.GetCustomAttributes(typeof(CatPostProcessingEditorAttribute), false)
			                   from a in attributes
			                   let attribute = a as CatPostProcessingEditorAttribute
			                   select new KeyValuePair<Type, Type>(attribute.settingsType, editorType)
			).ToDictionary(x => x.Key, x => x.Value);
		}

		public static IEnumerable<Type> GetAllAssemblyTypes() {
			return AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(t => {
						// Ugly hack to handle mis-versioned dlls
						var innerTypes = new Type[0];
						try {
							innerTypes = t.GetTypes();
						}
						catch {}
						return innerTypes;
					});
		}

		private void UpdateAllEditors() {
			m_Editors.Clear();

			for (int i = 0; i < m_profile.settings.Count; i++) {
				m_Editors.Add(GetEditor(m_SettingsProperty.GetArrayElementAtIndex(i),  m_profile.settings[i]));
			}
		}

		private CatPostProcessingEditorBase GetEditor(SerializedProperty settings, PostProcessingSettingsBase target) {

			Type settingsType = target.GetType();
			Type editorType;
			if (!m_EditorTypes.TryGetValue(settingsType, out editorType))
				editorType = typeof(DefaultPostProcessingEditor);
			
			var editor = CatPostProcessingEditorBase.Create(editorType, settings, target);

			return editor;
		}

		public override void OnInspectorGUI() {
			foreach (var editor in m_Editors) {
				EditorGUILayout.HelpBox(editor.ToString(), MessageType.Info);
				editor.OnInspectorGUI();
				EditorGUILayout.Space();
				EditorGUILayout.Space();
			}


			if (m_Editors.Count > 0) {
				DrawLine();
				EditorGUILayout.Space();
			} else {
				EditorGUILayout.HelpBox("No Post-Processing effects in this profile", MessageType.Info);
			}



			if (GUILayout.Button("Add effect...", EditorStyles.miniButton))
			{
				var menu = new GenericMenu();

				var settingsTypes = from t in GetAllAssemblyTypes()
					where t.IsSubclassOf(typeof(PostProcessingSettingsBase))
					//where t.IsDefined(typeof(CatPostProcessingEditorAttribute), false)
					where !t.IsAbstract
					select t;

				foreach (var type in settingsTypes) {
					var title = new GUIContent(type.Name);
					bool exists = m_profile.settings.Any(x => x.GetType() == type);

					if (!exists)
						menu.AddItem(title, false, () => AddEffectOverride(type));
					else
						menu.AddDisabledItem(title);
				}

				menu.ShowAsContext();
			}

			EditorGUILayout.Space();

		}

		private void DrawLine() {
			var rect = GUILayoutUtility.GetRect(1f, 1f);
			// Splitter rect should be full-width
			rect.xMin = 0f;
			rect.width += 4f;
			if (Event.current.type != EventType.Repaint)
				return;
			EditorGUI.DrawRect(rect, !EditorGUIUtility.isProSkin
				? new Color(0.6f, 0.6f, 0.6f, 1.333f)
				: new Color(0.12f, 0.12f, 0.12f, 1.333f));
		}

		void AddEffectOverride(Type type) {
			serializedObject.Update();

			var effect = CreateEffect(type);
			Undo.RegisterCreatedObjectUndo(effect, "Add Effect Override");

			// Store this new effect as a subasset so we can reference it safely afterwards
			AssetDatabase.AddObjectToAsset(effect, m_profile);

			// Grow the list first, then add - that's how serialized lists work in Unity
			m_SettingsProperty.arraySize++;
			var effectProp = m_SettingsProperty.GetArrayElementAtIndex(m_SettingsProperty.arraySize - 1);
			effectProp.objectReferenceValue = effect;

			// Force save / refresh
			EditorUtility.SetDirty(m_profile);
			AssetDatabase.SaveAssets();

			// Create & store the internal editor object for this effect
			UpdateAllEditors();

			serializedObject.ApplyModifiedProperties();
		}

		PostProcessingSettingsBase CreateEffect(Type type) {
			var effect = (PostProcessingSettingsBase)ScriptableObject.CreateInstance(type);
			effect.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
			effect.name = type.Name;
			effect.isActive = true;
			return effect;
		}

		bool DrawPropertyField(SerializedProperty prop, ref bool isFirst) {
			var hasNext = prop.Next(isFirst);
			isFirst = false;
			if (hasNext) {
				EditorGUILayout.PropertyField(prop);
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
