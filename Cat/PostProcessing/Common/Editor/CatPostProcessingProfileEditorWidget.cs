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
	
	//[CanEditMultipleObjects]
	public class CatPostProcessingProfileEditorWidget/* : Editor*/ {
		
		private CatPostProcessingProfile m_profile;
		private SerializedProperty m_ProfileProperty;

		SerializedProperty m_SettingsProperty;
		Dictionary<Type, Type> m_EditorTypes; // SettingsType => EditorType
		List<CatPostProcessingEditorBase> m_Editors = new List<CatPostProcessingEditorBase>();

		internal static Dictionary<Type, Type> s_PropertyDrawers { get; private set; } // PropertyAttribute => PropertyDrawer

		public CatPostProcessingProfile target { get; private set; }
		private SerializedObject serializedObject;

		public CatPostProcessingProfileEditorWidget(CatPostProcessingProfile aTarget, SerializedObject aSerializedObject = null, SerializedProperty aProfileProperty = null) {
			target = aTarget;
			serializedObject = aSerializedObject;
			m_ProfileProperty = aProfileProperty;
		}


		public void OnEnable() {
			if (serializedObject == null /*|| serializedObject.targetObject != target*/) {
				serializedObject = new SerializedObject(target);
			}
			m_profile = target;
			if (m_SettingsProperty == null) {
				if (m_ProfileProperty == null) {
					m_SettingsProperty = serializedObject.FindProperty("m_settings");
				} else {
					m_SettingsProperty = m_ProfileProperty.FindPropertyRelative("m_settings");
				}
			}
			Assert.IsNotNull(m_SettingsProperty);

			FindAllPropertyDrawers();
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

		private void FindAllPropertyDrawers() {
			s_PropertyDrawers = (from t in GetAllAssemblyTypes()
			                     where t.IsSubclassOf(typeof(PropertyDrawer))
			                     where t.IsDefined(typeof(CustomPropertyDrawer), false)
			                     where !t.IsAbstract
			                     let drawerType = t
			                     let attributes = drawerType.GetCustomAttributes(typeof(CustomPropertyDrawer), false)
			                     from a in attributes
			                     let attributeType = a.GetType().GetField("m_Type", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(a) as Type
			                     select new KeyValuePair<Type, Type>(attributeType, drawerType)
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
				m_Editors.Add(CreateNewEditor(m_SettingsProperty.GetArrayElementAtIndex(i),  m_profile.settings[i]));
			}
			m_Editors.Sort( (x, y) => x.target.queueingPosition.CompareTo(y.target.queueingPosition) );
		}

		private CatPostProcessingEditorBase CreateNewEditor(SerializedProperty settings, PostProcessingSettingsBase target) {
			CatPostProcessingEditorBase editor = null;
			Type settingsType = target.GetType();
			Type editorType;
			if (!m_EditorTypes.TryGetValue(settingsType, out editorType)) {
				editorType = typeof(DefaultPostProcessingEditor);
			}
			editor = CatPostProcessingEditorBase.Create(editorType, settings, target);
			return editor;
		}

		public void OnInspectorGUI() {
			// EditorGUILayout.PropertyField(serializedObject.FindProperty("m_settings"), true);
			// EditorGUILayout.Space();
			// CatEditorGUILayout.Splitter();
			// EditorGUILayout.Space();

			foreach (var editor in m_Editors) {
				CatEditorGUILayout.BeginBox();

				var isEditable = true;
				if(DrawEffectHeader(editor, out isEditable)) {
					using (new EditorGUI.DisabledScope(!isEditable)) {
						editor.OnInspectorGUIInternal();
					}
				}
				CatEditorGUILayout.EndBox();
				EditorGUILayout.Space();
			}

			if (m_Editors.Count > 0) {
			} else {
				EditorGUILayout.HelpBox("No Post-Processing effects in this profile", MessageType.Info);
			}


			DrawFooter();
			//CatEditorGUILayout.Splitter();
			EditorGUILayout.Space();
		}

		private bool DrawEffectHeader(CatPostProcessingEditorBase editor, out bool isEditable) {
			var target = editor.target;
			var index = m_profile.settings.IndexOf(target);
			var serializedProperty = m_SettingsProperty.GetArrayElementAtIndex(index);

			using (new EditorGUILayout.HorizontalScope()) {
				//serializedProperty.isExpanded = CatEditorGUILayout.Foldout(serializedProperty.isExpanded, target.effectName);
				//serializedProperty.isExpanded = CatEditorGUILayout.ToggledFoldout(serializedProperty.isExpanded, target.isActive, b => target.isActive = b, target.effectName);

				serializedProperty.isExpanded = CatEditorGUILayout.FoldoutToggle(serializedProperty.isExpanded);

				var isOverridingProperty = editor.serializedObject.FindProperty("isOverriding");

				editor.serializedObject.Update();
				var isOverriding = CatEditorGUILayout.ActivationToggle(isOverridingProperty.boolValue, false);
				isOverridingProperty.boolValue = isOverriding;
				isEditable = isOverriding;

				var rect = GUILayoutUtility.GetRect(new GUIContent(target.effectName), EditorStyles.boldLabel);
				var labelRect = new Rect(
					rect.x, 
					rect.y - 5, 
					rect.width, 
					rect.height
				);
				using (new EditorGUI.DisabledScope(!isOverriding)) {
					EditorGUI.LabelField(labelRect, target.effectName, EditorStyles.boldLabel);
				}
				var e = Event.current;
				if (e.type == EventType.MouseDown && labelRect.Contains(e.mousePosition)) {   
					e.Use();
					serializedProperty.isExpanded = !serializedProperty.isExpanded;
				}

				editor.serializedObject.ApplyModifiedProperties();

				if (CatEditorGUILayout.ContextButton()) {
					var menu = new GenericMenu();
					var resetTitle = new GUIContent("Reset");
					var removeTitle = new GUIContent("Remove");
					menu.AddItem(resetTitle, false, () => ResetEffectOverride(target));
					menu.AddItem(removeTitle, false, () => AskRemoveEffectOverride(target/*, ref deferredAction*/));
					menu.ShowAsContext();
				}
			}
			return serializedProperty.isExpanded;
		}

		private void DrawFooter() {
			//CatEditorGUILayout.BeginBox();
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
			//CatEditorGUILayout.EndBox();
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

			UpdateAllEditors();
		}

		private void AskRemoveEffectOverride(PostProcessingSettingsBase target/*, ref Action deferredAction*/) {
			if (EditorUtility.DisplayDialog("", String.Format("Do you really want to remove {0}", target.effectName), "Remove", "Cancel")) {
				//deferredAction = () => RemoveEffectOverride(editor);
				RemoveEffectOverride(target);
			}
		}

		void RemoveEffectOverride(PostProcessingSettingsBase target) {
			// Huh. Hack to keep foldout state on the next element...
			// bool nextFoldoutState = false;
			// if (id < m_Editors.Count - 1)
			// 	nextFoldoutState = m_Editors[id + 1].baseProperty.isExpanded;

			// Remove from the cached editors list
			//editor.OnDisable();
			var index = m_profile.settings.IndexOf(target);
			// m_Editors.RemoveAt(id);

			serializedObject.Update();

			//var property = m_SettingsProperty.GetArrayElementAtIndex(id);

			// Unassign it (should be null already but serialization does funky things
			m_SettingsProperty.GetArrayElementAtIndex(index).objectReferenceValue = null;

			// ...and remove the array index itself from the list
			m_SettingsProperty.DeleteArrayElementAtIndex(index);
			m_profile.settings.RemoveAt(index);

			// Finally refresh editor reference to the serialized settings list
			// for (int i = 0; i < m_Editors.Count; i++)
			// 	m_Editors[i].baseProperty = m_SettingsProperty.GetArrayElementAtIndex(i).Copy();

			// if (id < m_Editors.Count)
			// 	m_Editors[id].baseProperty.isExpanded = nextFoldoutState;

			// Destroy the setting object after ApplyModifiedProperties(). If we do it before, redo
			// actions will be in the wrong order and the reference to the setting object in the
			// list will be lost.
			Undo.DestroyObjectImmediate(target);

			// Force save / refresh
			EditorUtility.SetDirty(m_profile);
			AssetDatabase.SaveAssets();
			serializedObject.ApplyModifiedProperties();

			UpdateAllEditors();
		}

		void ResetEffectOverride(PostProcessingSettingsBase target) {

			var index = m_profile.settings.IndexOf(target);

			serializedObject.Update();
			target.Reset();
			/*
			var serializedProperty = m_SettingsProperty.GetArrayElementAtIndex(index);
			serializedProperty.objectReferenceValue = null;

			// Create a new object
			var newEffect = CreateEffect(target.GetType());
			Undo.RegisterCreatedObjectUndo(newEffect, "Reset Effect Override");
			AssetDatabase.AddObjectToAsset(newEffect, m_profile);
			serializedProperty.objectReferenceValue = newEffect;
			*/
			serializedObject.ApplyModifiedProperties();
			/*
			// Same as RemoveEffectOverride, destroy at the end so it's recreated first on Undo to
			// make sure the GUID exists before undoing the list state
			Undo.DestroyObjectImmediate(target);

			// Force save / refresh
			EditorUtility.SetDirty(m_profile);
			AssetDatabase.SaveAssets();

			UpdateAllEditors();
			*/
		}

		PostProcessingSettingsBase CreateEffect(Type type) {
			var effect = (PostProcessingSettingsBase)ScriptableObject.CreateInstance(type);
			effect.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
			effect.name = type.Name;
			effect.isOverriding = true;
			effect.Reset();
			return effect;
		}

	}
}
