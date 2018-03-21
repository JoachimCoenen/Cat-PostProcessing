using System.Collections.Generic;
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

			foreach (var property in properties) {
				PropertyField(property);
			}

			EditorGUILayout.Space();
			serializedObject.ApplyModifiedProperties();
			
		}

	}
}
