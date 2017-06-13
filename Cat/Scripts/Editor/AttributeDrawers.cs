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
    		GUI.enabled = false;
    		EditorGUI.PropertyField(position, property, label);
    		GUI.enabled = true;
    	}
    }
}