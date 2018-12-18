using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using Cat.Common;
using Cat.PostProcessing;
using Cat.CommonEditor;
using Cat.PostProcessingEditor;

namespace Cat.PostProcessingEditor {

	[CustomEditor(typeof(CatPostProcessingProfile))]
	//[CanEditMultipleObjects]
	public class CatPostProcessingProfileEditor : Editor {

		private CatPostProcessingProfileEditorWidget widget;

		public void OnEnable() {
			widget = new CatPostProcessingProfileEditorWidget(target as CatPostProcessingProfile, serializedObject);
			widget.OnEnable();
		}

		public override void OnInspectorGUI() {
			if (widget != null) {
				widget.OnInspectorGUI();
			}
		}

	}
}
