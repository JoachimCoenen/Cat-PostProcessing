using System;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Cat.Common;
using Cat.CommonEditor;
using Cat.PostProcessing;
using UnityEditor.Cat.PostProcessingEditor;

namespace Cat.PostProcessingEditor {
	public class DefaultPostProcessingEditor : CatPostProcessingEditorBase {
		public override void OnInspectorGUI() {
			SerializedProperty propertyIterator = serializedObject.FindProperty("m_Settings");
			serializedObject.Update();
			//EditorGUILayout.PropertyField(propertyIterator, true);

			Type type = target.GetType();
			var field = type.GetField("m_Settings", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			var attrs = field.GetCustomAttributes(false);
			var attributes = attrs.Cast<Attribute>().ToArray();
			//Attribute[] attributes = 
			PropertyField(propertyIterator, attributes);
			serializedObject.ApplyModifiedProperties();
			return;

			
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
