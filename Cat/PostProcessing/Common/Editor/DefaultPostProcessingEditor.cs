using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Cat.Common;
using Cat.CommonEditor;
using Cat.PostProcessing;
using Cat.PostProcessingEditor;

namespace Cat.PostProcessingEditor {
	public class DefaultPostProcessingEditor : CatPostProcessingEditorBase {
		public override void OnInspectorGUI(IEnumerable<AttributedProperty> properties) {
			serializedObject.Update();
			//SerializedProperty propertyIterator = serializedObject.FindProperty("m_Settings");
			//EditorGUILayout.PropertyField(propertyIterator, true);
			// TopRowFields();

			foreach (var property in properties) {
				PropertyField(property);
			}

			// Type type = target.GetType();
			// var field = type.GetField("m_Settings", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			// var attrs = field.GetCustomAttributes(false);
			// var attributes = attrs.Cast<Attribute>().ToArray();
			//Attribute[] attributes = 
			//EditorGUILayout.PropertyField(propertyIterator, true);


			var settingsProp = serializedObject.FindProperty("m_Settings");
			//var settingsProp = propertyIterator.FindPropertyRelative("m_Settings");

			bool isFirst = true;
			//while (DrawPropertyField(settingsProp, ref isFirst)) {}
					
			//PropertyField(propertyIterator, attributes);




			EditorGUILayout.Space();
			serializedObject.ApplyModifiedProperties();
			
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
