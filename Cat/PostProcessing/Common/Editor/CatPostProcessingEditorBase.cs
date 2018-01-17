using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Cat.Common;
using Cat.CommonEditor;
using Cat.PostProcessing;

namespace UnityEditor.Cat.PostProcessingEditor {
	public abstract class CatPostProcessingEditorBase {

		SerializedProperty m_settings;
		protected SerializedProperty settings { get { return m_settings;}}
		SerializedObject m_serializedObject;
		protected SerializedObject serializedObject { get { return m_serializedObject;}}
		PostProcessingSettingsBase m_target;
		protected PostProcessingSettingsBase target { get { return m_target;}}

		public static CatPostProcessingEditorBase Create(Type t, SerializedProperty settings, PostProcessingSettingsBase target) {
			var editor = (CatPostProcessingEditorBase)Activator.CreateInstance(t);
			editor.m_settings = settings;
			editor.m_serializedObject = new SerializedObject(target);
			editor.m_target = target;
			return editor;
		}

		public abstract void OnInspectorGUI() ;

		/*
		private static Dictionary<Type, Type> s_allPropertyDrawers;
		private static Dictionary<Type, Type> allPropertyDrawers {
			get {
				if (s_allPropertyDrawers != null) {
					return s_allPropertyDrawers;
				}
				s_allPropertyDrawers = (from t in GetAllAssemblyTypes()
					where t.IsSubclassOf(typeof(PropertyDrawer))
					where t.IsDefined(typeof(CustomPropertyDrawer), false)
				                        where !t.IsAbstract
					let a = t.GetCustomAttributes(typeof(CustomPropertyDrawer), false)[0]
					let attribute = a as CustomPropertyDrawer
					select new KeyValuePair<Type, Type>(attribute.m_Type, t)
				).ToDictionary(x => x.Key, x => x.Value);
			}
		}
		*/
		/*
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
*/

		protected void PropertyField(SerializedProperty property, Attribute[] attributes)
		{
			var title = new GUIContent(property.displayName);
			PropertyField(property, attributes, title);
		}

		protected void PropertyField(SerializedProperty property, Attribute[] attributes, GUIContent title)
		{
			// Check for DisplayNameAttribute first
		//	var displayNameAttr = (DisplayNameAttribute)attributes.FirstOrDefault(x => x is DisplayNameAttribute);//property.GetAttribute<DisplayNameAttribute>();
		//	if (displayNameAttr != null)
		//		title.text = displayNameAttr.displayName;

			// Add tooltip if it's missing and an attribute is available
			if (string.IsNullOrEmpty(title.tooltip))
			{
				var tooltipAttr = (TooltipAttribute)attributes.FirstOrDefault(x => x is TooltipAttribute);//property.GetAttribute<TooltipAttribute>();
				if (tooltipAttr != null)
					title.tooltip = tooltipAttr.tooltip;
			}

			// Look for a compatible attribute decorator
		//	AttributeDecorator decorator = null;


			PropertyDrawer propertyDrawer = null;
			foreach (var attr in attributes)
			{
				/*
				// Use the first decorator we found
				if (decorator == null)
				{
					decorator = EditorUtilities.GetDecorator(attr.GetType());
					attribute = attr;
				}
				*/
				// Draw unity built-in Decorators (Space, Header)
				if (attr is PropertyAttribute)
				{
					EditorGUILayout.HelpBox(attr.ToString(), MessageType.Info);
					if (attr is SpaceAttribute)
					{
						EditorGUILayout.GetControlRect(false, (attr as SpaceAttribute).height);
					}
					else if (attr is HeaderAttribute)
					{
						var rect = EditorGUILayout.GetControlRect(false, 24f);
						rect.y += 8f;
						rect = EditorGUI.IndentedRect(rect);
						EditorGUI.LabelField(rect, (attr as HeaderAttribute).header, EditorStyles.boldLabel);
					}
					else if (attr is Inlined) {
						propertyDrawer = new InlinedAttributeDrawer();
					}
				}
			}

			bool invalidProp = false;
			/*
			if (decorator != null && !decorator.IsAutoProperty())
			{
				if (decorator.OnGUI(property.value, property.overrideState, title, attribute))
					return;

				// Attribute is invalid for the specified property; use default unity field instead
				invalidProp = true;
			}
			*/
			using (new EditorGUILayout.HorizontalScope())
			{
				// Property
			/*
				if (decorator != null && !invalidProp)
				{
					if (decorator.OnGUI(property.value, property.overrideState, title, attribute))
						return;
				}
			*/

				if (propertyDrawer != null) {
					var height = propertyDrawer.GetPropertyHeight(property, title);
					var rect = EditorGUILayout.GetControlRect(true, height);
					propertyDrawer.OnGUI(rect, property, title);
				} else if (property.hasVisibleChildren
					&& property.propertyType != SerializedPropertyType.Vector2
					&& property.propertyType != SerializedPropertyType.Vector3)
				{
					// Default unity field
					GUILayout.Space(12f);
					EditorGUILayout.PropertyField(property, title, true);
				}
				else
				{
					// Default unity field
					EditorGUILayout.PropertyField(property, title);
				}
			}
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


	}
}