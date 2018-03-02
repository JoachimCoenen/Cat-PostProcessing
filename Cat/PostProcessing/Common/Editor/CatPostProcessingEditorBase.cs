using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Cat.Common;
using Cat.CommonEditor;
using Cat.PostProcessing;

namespace Cat.PostProcessingEditor {
	public struct AttributedProperty {

		public SerializedProperty serializedProperty { get; private set; }
		public object rawValue { get; private set; }

		public Attribute[] attributes { get; private set; }

		public AttributedProperty(SerializedProperty serializedProperty, Attribute[] attributes, object rawValue) {
			this.serializedProperty = serializedProperty;
			this.rawValue = rawValue;
			this.attributes = attributes;
		}
	}

	public abstract class CatPostProcessingEditorBase {

		//SerializedProperty m_settings;
		//protected SerializedProperty settings { get { return m_settings;}}
		private SerializedObject m_serializedObject;
		protected SerializedObject serializedObject { get { return m_serializedObject;}}
		private PostProcessingSettingsBase m_target;
		protected PostProcessingSettingsBase target { get { return m_target;}}


		public static CatPostProcessingEditorBase Create(Type t, SerializedProperty settings, PostProcessingSettingsBase target) {
			var editor = (CatPostProcessingEditorBase)Activator.CreateInstance(t);
			//var editor = (CatPostProcessingEditorBase)CreateEditor(target, t);
			//editor.m_settings = settings;
			//editor.m_serializedObject = new SerializedObject(target);
			editor.m_target = target;
			editor.m_serializedObject = new SerializedObject(target);
			return editor;
		}

		internal static IEnumerable<AttributedProperty> GetAttributedProperties(object target, SerializedProperty serializedProperty) {
			var properties = from field in target.GetType()
				.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
					where field.Name != "enabled"
					where (field.IsPublic && field.GetCustomAttributes(typeof(NonSerializedAttribute), false).Length == 0)
				|| (field.GetCustomAttributes(typeof(UnityEngine.SerializeField), false).Length > 0)
				let property = serializedProperty.FindPropertyRelative(field.Name)
				let attributes = field.GetCustomAttributes(false).Cast<Attribute>().ToArray()
				select new AttributedProperty(property, attributes, field.GetValue(target));
			return properties;
		}
		internal IEnumerable<AttributedProperty> GetAttributedProperties(object target) {
			var properties = from field in target.GetType()
				.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
					where field.Name != "enabled"
					where (field.IsPublic && field.GetCustomAttributes(typeof(NonSerializedAttribute), false).Length == 0)
				|| (field.GetCustomAttributes(typeof(UnityEngine.SerializeField), false).Length > 0)
				let property = serializedObject.FindProperty(field.Name)
				let attributes = field.GetCustomAttributes(false).Cast<Attribute>().ToArray()
				select new AttributedProperty(property, attributes, field.GetValue(target));
			return properties;
		}

		internal void OnInspectorGUIInternal() {
			var properties = GetAttributedProperties(target);
			OnInspectorGUI(properties);
		}

		public abstract void OnInspectorGUI(IEnumerable<AttributedProperty> properties) ;

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

		protected static void PropertyField(AttributedProperty property)
		{
			var title = new GUIContent(property.serializedProperty.displayName);
			PropertyField(property, title);
		}

		protected static void PropertyField(AttributedProperty property, GUIContent title)
		{
			// Check for DisplayNameAttribute first
		//	var displayNameAttr = (DisplayNameAttribute)attributes.FirstOrDefault(x => x is DisplayNameAttribute);//property.GetAttribute<DisplayNameAttribute>();
		//	if (displayNameAttr != null)
		//		title.text = displayNameAttr.displayName;

			// Add tooltip if it's missing and an attribute is available
			if (string.IsNullOrEmpty(title.tooltip))
			{
				var tooltipAttr = (TooltipAttribute)property.attributes.FirstOrDefault(x => x is TooltipAttribute);//property.GetAttribute<TooltipAttribute>();
				if (tooltipAttr != null)
					title.tooltip = tooltipAttr.tooltip;
			}

			// Look for a compatible attribute drawer
			PropertyDrawer propertyDrawer = null;
			foreach (var attr in property.attributes) {
				// Draw unity built-in Decorators (Space, Header)
				if (attr is PropertyAttribute) {
					if (attr is SpaceAttribute) {
						EditorGUILayout.GetControlRect(false, (attr as SpaceAttribute).height);
					}
					else if (attr is HeaderAttribute) {
						var rect = EditorGUILayout.GetControlRect(false, 24f);
						rect.y += 8f;
						rect = EditorGUI.IndentedRect(rect);
						EditorGUI.LabelField(rect, (attr as HeaderAttribute).header, EditorStyles.boldLabel);
					}
					else if (attr is Inlined) {
						propertyDrawer = new InlinedAttributeDrawer();
					} else {
						Type drawerType = null;
						if (CatPostProcessingProfileEditor.s_PropertyDrawers.TryGetValue(attr.GetType(), out drawerType)) {
							propertyDrawer = (PropertyDrawer)Activator.CreateInstance(drawerType);
							var field = drawerType.GetField("m_Attribute", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
							field.SetValue(propertyDrawer, attr);
						}
					}
				}
			}

			var serializedProperty = property.serializedProperty;
			if (propertyDrawer != null) {
				if (propertyDrawer is InlinedAttributeDrawer) {
					// Draw Inlined Attribute:
					var innerProperties = GetAttributedProperties(property.rawValue, property.serializedProperty);
					foreach (var innerProperty in innerProperties) {
						PropertyField(innerProperty);
					}
				} else {
					using (new EditorGUILayout.HorizontalScope()) {
						// all Other Custom Attributes:
						var height = propertyDrawer.GetPropertyHeight(serializedProperty, title);
						var rect = EditorGUILayout.GetControlRect(true, height);
						propertyDrawer.OnGUI(rect, serializedProperty, title);
					}
				}
			} else if (serializedProperty.hasVisibleChildren
					&& serializedProperty.propertyType != SerializedPropertyType.Vector2
					&& serializedProperty.propertyType != SerializedPropertyType.Vector3) {
				// Default unity field
				GUILayout.Space(12f);
				EditorGUILayout.PropertyField(serializedProperty, title, false);
				var innerProperties = GetAttributedProperties(property.rawValue, property.serializedProperty);
				foreach (var innerProperty in innerProperties) {
					PropertyField(innerProperty);
				}
			} else {
				// Default unity field
				using (new EditorGUILayout.HorizontalScope()) {
					EditorGUILayout.PropertyField(serializedProperty, title);
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