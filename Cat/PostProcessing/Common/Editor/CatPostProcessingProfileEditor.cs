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

	[CustomEditor(typeof(CatPostProcessingProfile))]
	//[CanEditMultipleObjects]
	public class CatPostProcessingProfileEditor : Editor {
		
		private CatPostProcessingProfile m_profile;

		SerializedProperty m_SettingsProperty;


		public void OnEnable() {
			m_profile = target as CatPostProcessingProfile;

			m_SettingsProperty = serializedObject.FindProperty("m_settings");
			Assert.IsNotNull(m_SettingsProperty);

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

		public override void OnInspectorGUI() {
			serializedObject.Update();

			var settingsProp = serializedObject.FindProperty("m_settings");

			EditorGUILayout.PropertyField(settingsProp, true);

			DrawFooter();
			EditorGUILayout.Space();

			serializedObject.ApplyModifiedProperties();
		}

		private void DrawFooter() {
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
					bool exists = m_profile.m_settings.Any(x => x.GetType() == type);

					if (!exists)
						menu.AddItem(title, false, () => AddEffectOverride(type));
					else
						menu.AddDisabledItem(title);
				}

				menu.ShowAsContext();
			}
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

			serializedObject.ApplyModifiedProperties();

			//serializedObject.Update();
		}

		PostProcessingSettingsBase CreateEffect(Type type) {
			var effect = (PostProcessingSettingsBase)ScriptableObject.CreateInstance(type);
			effect.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
			effect.name = type.Name;
			effect.isActive = true;
			return effect;
		}

	}
}
