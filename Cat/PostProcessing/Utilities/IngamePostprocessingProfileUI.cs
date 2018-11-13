/*==== DebugSceneUI.cs =====================================================
 * Class that shows via IMGUI at runtime a Hierarchy and a Inspector like 
 * the built-in Editor. Can be useful when testing game on Android or doing 
 * some reverse-engineering ;)
 * 
 * Author: Victor Le aka "Coac"
 * Repository : https://github.com/Coac/ingame-editor-ui.git
 * =========================================================================*/

using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cat.Common;

namespace Cat.PostProcessing {
	public class IngamePostprocessingProfileUI : MonoBehaviour {
		/*
	    public KeyCode toggleGuiKey = KeyCode.End;
	    private bool toggleUI = true;

		public CatPostProcessingProfile profile;

	    void Start()
		{
			this.hierachyRect = new Rect(0, 0, 500, Screen.height);
	    }

	    void Update()
	    {
	        if(Input.GetKeyDown(toggleGuiKey))
	        {
	            toggleUI = !toggleUI;
	        }
	    }

	    void OnGUI()
	    {
	        if (!toggleUI) return;

	        this.draw();
		}

		public void draw()
		{
			hierachyRect = GUI.Window(0, hierachyRect, InspectorFunction, "Hierarchy");
		}

		private Rect hierachyRect;
		private Vector2 scrollViewVector = Vector2.zero;
		void InspectorFunction(int windowID) {
			GUILayout.Label("CatPostProcessingProfile");

			using (var scrollViewScope = new GUILayout.ScrollViewScope(scrollViewVector)) {
				scrollViewVector = scrollViewScope.scrollPosition;

				foreach (var setting in profile.settings) {
					drawSetting(setting);
				}
			}
		}

		void drawSetting(PostProcessingSettingsBase setting) {

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
			// EditorGUILayout.PropertyField(serializedObject.FindProperty("m_settings"), true);
			// EditorGUILayout.Space();
			// CatEditorGUILayout.Splitter();
			// EditorGUILayout.Space();

			foreach (var setting in profile.settings) {
				CatGUILayout.BeginBox();

				var isEditable = true;
				if(DrawEffectHeader(setting, out isEditable)) {
					using (new EditorGUI.DisabledScope(!isEditable)) {
						drawSetting(setting);
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
			CatEditorGUILayout.Splitter();
			EditorGUILayout.Space();
		}

		private bool DrawEffectHeader(PostProcessingSettingsBase setting, out bool isEditable) {
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
					menu.AddItem(removeTitle, false, () => AskRemoveEffectOverride(target));
					menu.ShowAsContext();
				}
			}
			return serializedProperty.isExpanded;
		}

		private void DrawFooter() {
			CatEditorGUILayout.BeginBox();
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
			CatEditorGUILayout.EndBox();
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

		private void AskRemoveEffectOverride(PostProcessingSettingsBase target) {
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

			serializedObject.ApplyModifiedProperties();

		}

		PostProcessingSettingsBase CreateEffect(Type type) {
			var effect = (PostProcessingSettingsBase)ScriptableObject.CreateInstance(type);
			effect.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
			effect.name = type.Name;
			effect.isOverriding = true;
			effect.Reset();
			return effect;
		}
		*/
	}
	
}
