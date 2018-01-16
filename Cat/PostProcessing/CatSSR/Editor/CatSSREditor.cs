using System;
using UnityEngine;
using UnityEditor;
using System.Linq;
using Cat.Common;
using Cat.PostProcessing;
using Cat.CommonEditor;

namespace Cat.PostProcessingEditor {


	using Settings = CatSSR.Settings;

	[CustomEditor(typeof(CatSSR))]
	//[CanEditMultipleObjects]
	public class CatSSREditor : Editor {
		public SerializedProperty settings;

		public void OnEnable() {
			settings = serializedObject.FindProperty("m_Settings");
		}

		public override void OnInspectorGUI() {

			bool hasMixedValues = settings.hasMultipleDifferentValues;



			//int num = Array.IndexOf<Enum>(nonObsoleteEnumData.values, selected);

			var presetType = typeof(Settings.Preset);
			var allowedPresets = from name in Enum.GetNames(presetType)
			                      where presetType.GetField(name).GetCustomAttributes(typeof(ObsoleteAttribute), false).Length == 0
			                      select name;

			Settings.Preset[] allowedValues = (from name in allowedPresets
				select (Settings.Preset)Enum.Parse(presetType, name)).ToArray<Settings.Preset>();

			string[] displayedOptions = {};

			displayedOptions = displayedOptions.Concat(from name in allowedPresets
				select ObjectNames.NicifyVariableName(name)).ToArray<string>();
			
			var catSSR = (target as CatSSR);

			var selectedIndex = -1; // allowedValues.Length-1;
			// while (selectedIndex >= 0 && Settings.GetPreset(allowedValues[selectedIndex]) == catSSR.settings) {
			// 	selectedIndex--;
			// }

			bool oldShowMixedValue = EditorGUI.showMixedValue;
			EditorGUI.showMixedValue = hasMixedValues;
			EditorGUI.BeginChangeCheck(); {
				selectedIndex = EditorGUILayout.Popup("Preset", selectedIndex, displayedOptions);
			}
			if (EditorGUI.EndChangeCheck()) {
				if (selectedIndex >= 0) {
					(target as CatSSR).settings = Settings.GetPreset(allowedValues[selectedIndex]);
				}
			}
			EditorGUI.showMixedValue = oldShowMixedValue;


			serializedObject.Update();
			EditorGUILayout.PropertyField(settings);
			serializedObject.ApplyModifiedProperties();
		}
			
	}
}
