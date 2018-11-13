
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cat.Common;

namespace Cat.PostProcessing {
	public struct AttributedProperty {
		public object rawValue { get; private set; }

		public Attribute[] attributes { get; private set; }

		public string displayName { get; private set; }

		public AttributedProperty(Attribute[] attributes, object rawValue, string displayName) {
			this.rawValue = rawValue;
			this.attributes = attributes;
			this.displayName = displayName;
		}
	}
	public class CatPostProcessingIngameEditor {
		/*
		//SerializedProperty m_settings;
		//protected SerializedProperty settings { get { return m_settings;}}
		private PostProcessingSettingsBase m_target;
		internal PostProcessingSettingsBase target { get { return m_target;}}

		public static CatPostProcessingIngameEditor Create(Type t, PostProcessingSettingsBase target) {
			var editor = new CatPostProcessingIngameEditor();
			//var editor = (CatPostProcessingEditorBase)CreateEditor(target, t);
			//editor.m_settings = settings;
			//editor.m_serializedObject = new SerializedObject(target);
			editor.m_target = target;
			return editor;
		}

		internal IEnumerable<AttributedProperty> GetAttributedProperties(object target) {
			var properties = from field in target.GetType()
				.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
					where field.Name != "isOverriding"
					where field.Name != "enabled"
					where !field.IsInitOnly
					where (field.IsPublic && field.GetCustomAttributes(typeof(NonSerializedAttribute), false).Length == 0)
				|| (field.GetCustomAttributes(typeof(UnityEngine.SerializeField), false).Length > 0)
				let attributes = field.GetCustomAttributes(false).Cast<Attribute>().ToArray()
				let name = field.Name // TODO: Beatify field Names
				select new AttributedProperty(attributes, field.GetValue(target), name);
			return properties;
		}

		internal void OnInspectorGUIInternal() {
			var properties = GetAttributedProperties(target);
			OnInspectorGUI(properties);
		}

		public abstract void OnInspectorGUI(IEnumerable<AttributedProperty> properties) ;

		protected static void PropertyField(AttributedProperty property) {
			var title = new GUIContent(property.displayName);
			PropertyField(property, title);
		}

		protected static void PropertyField(AttributedProperty property, GUIContent title) {

			// Add tooltip if it's missing and an attribute is available
			if (string.IsNullOrEmpty(title.tooltip)) {
				var tooltipAttr = (TooltipAttribute)property.attributes.FirstOrDefault(x => x is TooltipAttribute);//property.GetAttribute<TooltipAttribute>();
				if (tooltipAttr != null)
					title.tooltip = tooltipAttr.tooltip;
			}
			// EditorGUILayout.PropertyField(property, title, property.isExpanded);

			// Look for a compatible attribute drawer
			foreach (var attr in property.attributes) {
				// Draw unity built-in Decorators (Space, Header)
				if (attr is PropertyAttribute) {
					if (attr is SpaceAttribute) {
						GUILayoutUtility.GetRect(100, (attr as SpaceAttribute).height);
					}
					else if (attr is HeaderAttribute) {
						//CatEditorGUILayout.Splitter();
						var rect = GUILayoutUtility.GetRect(100, 24f);
						rect.y += 8f;
						rect.x += 8f; // rect = GUI.IndentedRect(rect);
						GUI.Label(rect, (attr as HeaderAttribute).header, CatGUILayout.Styles.boldLabel);
					}/*
					else if (attr is Inlined) {
						propertyDrawer = new InlinedAttributeDrawer();
					} else {
						Type drawerType = null;
						if (CatPostProcessingProfileEditor.s_PropertyDrawers.TryGetValue(attr.GetType(), out drawerType)) {
							propertyDrawer = (PropertyDrawer)Activator.CreateInstance(drawerType);
							var field = drawerType.GetField("m_Attribute", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
							field.SetValue(propertyDrawer, attr);
						}
					}*/ /*
				}
			}

			var propertyOverride = property.rawValue as PropertyOverride;

			if (propertyOverride != null) {
				var overrideProperties = GetAttributedProperties(propertyOverride, serializedProperty);
				using (new EditorGUILayout.HorizontalScope()) {
					var isOverriding = overrideProperties.First(p => p.serializedProperty.name == "isOverriding").serializedProperty;
					var rawValue = overrideProperties.First(p => p.serializedProperty.name == "m_RawValue");
					var showAsActive = CatEditorGUILayout.ActivationToggle(isOverriding.boolValue);
					isOverriding.boolValue = showAsActive;
					using (new EditorGUI.DisabledScope(!showAsActive)) {
						PropertyField(rawValue, propertyDrawer, title);
					}
				}

			} else {
				// TODO: Draw field properly:
				PropertyField(property, propertyDrawer, title);
			}

		}

		private static void PropertyField(AttributedProperty property,PropertyDrawer propertyDrawer, GUIContent title) {
			var serializedProperty = property.serializedProperty;
			if (propertyDrawer != null) {
				if (propertyDrawer is InlinedAttributeDrawer) {
					// Draw Inlined Attribute:
					var innerProperties = GetAttributedProperties(property.rawValue, serializedProperty);
					foreach (var innerProperty in innerProperties) {
						PropertyField(innerProperty);
					}
				} else {
					//using (new EditorGUILayout.HorizontalScope()) {
					// all Other Custom Attributes:
					var height = propertyDrawer.GetPropertyHeight(serializedProperty, title);
					var rect = EditorGUILayout.GetControlRect(true, height);
					propertyDrawer.OnGUI(rect, serializedProperty, title);
					//}
				}
			} else if (serializedProperty.hasVisibleChildren
				&& serializedProperty.propertyType != SerializedPropertyType.Vector2
				&& serializedProperty.propertyType != SerializedPropertyType.Vector3) {
				// Default unity field
				GUILayout.Space(12f);
				EditorGUILayout.PropertyField(serializedProperty, title, false);
				var innerProperties = GetAttributedProperties(property.rawValue, serializedProperty);
				foreach (var innerProperty in innerProperties) {
					PropertyField(innerProperty);
				}
			} else {
				// Default unity field
				EditorGUILayout.PropertyField(serializedProperty, title);
			}
		}
		*/
	}
}
