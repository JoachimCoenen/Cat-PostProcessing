using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Cat.Common;

namespace Cat.CommonEditor
{
    [CustomPropertyDrawer(typeof(CustomLabel))]
    public class CustomLabelAttributeDrawer : PropertyDrawer {

    	// Draw the property inside the given rect
    	public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
    		label.text = (attribute as CustomLabel).labelText;
    		EditorGUI.PropertyField(position, property, label);
    	}
    }

    [CustomPropertyDrawer(typeof(CustomLabelRange))]
    public class CustomLabelRangeAttributeDrawer : PropertyDrawer {

    	// Draw the property inside the given rect
    	public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
    		CustomLabelRange range = (attribute as CustomLabelRange);
    		label.text = range.labelText;
		
    		if (property.propertyType == SerializedPropertyType.Float) {
    			EditorGUI.Slider(position, property, range.min, range.max, label);
    		} else if (property.propertyType == SerializedPropertyType.Integer) {
    			EditorGUI.IntSlider(position, property, (int)range.min, (int)range.max, label);
    		} else {
    			EditorGUI.LabelField(position, label.text, "Use Range with float or int.");
    		}
    	}
    }

    [CustomPropertyDrawer(typeof(ReadOnly))]
    public class ReadOnlyAttributeDrawer : PropertyDrawer {
	
    	// Draw the property inside the given rect
    	public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
			var wasEnabled = GUI.enabled;
    		GUI.enabled = false;
    		EditorGUI.PropertyField(position, property, label);
			GUI.enabled = wasEnabled;
    	}
    }

	[CustomPropertyDrawer(typeof(VectorInt2))]
	public class VectorInt2AttributeDrawer : PropertyDrawer {

		// Draw the property inside the given rect
		public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
			var x = property.FindPropertyRelative("x");
			var y = property.FindPropertyRelative("y");
			var xy = new int[] { x.intValue, y.intValue };
			position = EditorGUI.PrefixLabel(position, 0, label);
			position.height = 16f;
			EditorGUI.BeginChangeCheck();
			MultiIntegerField(position, new string[] { "X", "Y" }, xy, 13f);
			if (EditorGUI.EndChangeCheck()) {
				x.intValue = xy[0];
				y.intValue = xy[1];
			}
		}

		// Shamefully stolen from Unitys own EditorGUI.MultiFloatField() method
		private static void MultiIntegerField(Rect position, string[] subLabels, int[] values, float labelWidth) {
			int num = values.Length;
			float num2 = (position.width - (float)(num - 1) * 2f) / (float)num;
			Rect position2 = new Rect(position);
			position2.width = num2;
			float labelWidth2 = EditorGUIUtility.labelWidth;
			int indentLevel = EditorGUI.indentLevel;
			EditorGUIUtility.labelWidth = labelWidth;
			EditorGUI.indentLevel = 0;
			for (int i = 0; i < values.Length; i++) {
				values[i] = EditorGUI.IntField(position2, subLabels[i], values[i]);
				position2.x += num2 + 2f;
			}
			EditorGUIUtility.labelWidth = labelWidth2;
			EditorGUI.indentLevel = indentLevel;
		}
	}

/*	[CustomPropertyDrawer(typeof(TextureResolution))]
	public class TextureResolutionAttributeDrawer : PropertyDrawer {

		// Draw the property inside the given rect
		public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
			var m_value = property.FindPropertyRelative("m_value");

			var resolutionNames = Array.ConvertAll(TextureResolution.GetNames(), name => new GUIContent(name));
			var resolution = m_value.intValue;
			EditorGUI.BeginChangeCheck(); {
				resolution = EditorGUI.Popup(position, label, resolution, resolutionNames);
			}
			if (EditorGUI.EndChangeCheck()) {
				//m_MaterialEditor.RegisterPropertyChangeUndo(label);
				m_value.intValue = resolution;
			}
		}
	}*/

	[CustomPropertyDrawer(typeof(Inlined))]
	public class InlinedAttributeDrawer : PropertyDrawer {

		// Draw the property inside the given rect
		public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
			SerializedObject so = property.serializedObject;
			SerializedProperty iterator = so.FindProperty(property.name);
			var rect = position;
			var isFirst = true;
			while (iterator.Next(isFirst)) {
				rect.height = EditorGUI.GetPropertyHeight(iterator);
				if (isFirst && rect.height >= 40) {
					rect.y -= 9;
				}
				EditorGUI.PropertyField(rect, iterator);
				rect.y += rect.height;
				isFirst = false;
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
			SerializedObject so = property.serializedObject;
			SerializedProperty iterator = so.FindProperty(property.name);
			var result = 0f;
			var isFirst = true;
			while (iterator.Next(isFirst)) {
				var height = EditorGUI.GetPropertyHeight(iterator);
				if (isFirst && height >= 40) {
					result -= 9;
				}
				result += height;
				isFirst = false;
			}
			return result;
		}
	}


}