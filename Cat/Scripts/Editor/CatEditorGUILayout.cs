using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
namespace Cat.CommonEditor
{
	public static class CatEditorGUILayout {
		private static class Styles {
			private static GUIStyle boxSkin = null;
			private static GUIStyle splitterSkin = null;
			private static GUIStyle foldoutSkin = null;

			public static GUIStyle BoxSkin {
				get {
					if (boxSkin == null) {
						boxSkin = new GUIStyle(GUI.skin.box);
						boxSkin.normal.background = Texture2D.whiteTexture;
						//boxSkin.padding.top = Styles.boxSkin.padding.bottom+2;
						boxSkin.overflow.left = 9;
					}
					return boxSkin;
				}
			}
			public static GUIStyle SplitterSkin {
				get {
					if (splitterSkin == null) {
						splitterSkin = new GUIStyle(GUI.skin.box);
						splitterSkin.normal.background = Texture2D.whiteTexture;
						splitterSkin.margin.bottom = 6;
						splitterSkin.overflow.left = 0;
						splitterSkin.overflow.right = 0;
					}
					return splitterSkin;
				}
			}
			public static GUIStyle FoldoutSkin {
				get {
					if (foldoutSkin == null) {
						foldoutSkin = new GUIStyle(EditorStyles.foldout);
						foldoutSkin.fontStyle = FontStyle.Bold;
					}
					return foldoutSkin;
				}
			}

			//public static Color BoxColor				= new Color(0.83f, 0.83f, 0.83f, 1.0f);
			//public static Color boxColor				= new Color(0.867f, 0.867f, 0.867f, 1.0f);
			public static Color BoxColor { get{ return new Color(0.816f, 0.816f, 0.816f, 1.0f); } }
			public static Color boxProColor { get{ return new Color(0.243f, 0.243f, 0.243f, 1.0f); } }

			const float c = 0.07f;
			public static Color splitterColor { get{ return new Color(0.76f-c, 0.76f-c, 0.76f-c, 1); } }
			public static Color splitterProColor { get{ return new Color(0.18f-c, 0.18f-c, 0.18f-c, 1); } }

		}

		public static void BeginBox() {
			Color guibackgroundColor = GUI.backgroundColor;

			GUI.backgroundColor *= EditorGUIUtility.isProSkin ? Styles.boxProColor : Styles.BoxColor;				
			EditorGUILayout.BeginVertical(Styles.BoxSkin);
			GUI.backgroundColor = guibackgroundColor;
		}

		public static void EndBox() {
			EditorGUILayout.EndVertical();
		}

		public static bool Foldout(bool isExpanded, string text) {
			isExpanded = EditorGUILayout.Foldout(isExpanded, text, true, Styles.FoldoutSkin);
			return isExpanded;
		}

		public static bool Foldout(bool isExpanded, GUIContent text) {
			isExpanded = EditorGUILayout.Foldout(isExpanded, text, true, Styles.FoldoutSkin);
			return isExpanded;
		}

		public static void Header(string text) {
			GUILayout.Label(text, EditorStyles.boldLabel);
		}

		public static void Header(GUIContent text) {
			GUILayout.Label(text, EditorStyles.boldLabel);
		}


		public static void Splitter(float thickness = 2) {
			Color guibackgroundColor = GUI.backgroundColor;

			GUI.backgroundColor *= EditorGUIUtility.isProSkin ? Styles.splitterProColor : Styles.splitterColor;
			GUILayout.Box("", Styles.SplitterSkin, new GUILayoutOption[]{GUILayout.ExpandWidth(true), GUILayout.Height(thickness)});
			GUI.backgroundColor = guibackgroundColor;
		}

		private static bool IsFadeGroupSupported() {
			return true;
		}

		public static bool BeginAnimatedFadeGroup(this Editor self, AnimBool isVisible) {
			if (IsFadeGroupSupported()) {
				isVisible.valueChanged.RemoveListener(self.Repaint);
				isVisible.valueChanged.AddListener(self.Repaint);

				return EditorGUILayout.BeginFadeGroup(isVisible.faded);
			} else {
				//GUILayout.BeginVertical();
				return isVisible.value;
			}
		}

		public static bool MidAnimatedFadeGroup(this Editor self, AnimBool isVisible) {
			if (IsFadeGroupSupported()) {
				EditorGUILayout.EndFadeGroup();
				return EditorGUILayout.BeginFadeGroup(1f - isVisible.faded);
			} else {
				//GUILayout.BeginVertical();
				return !isVisible.value;
			}
		}

		public static void EndAnimatedFadeGroup(this Editor self) {
			if (IsFadeGroupSupported()) {
				EditorGUILayout.EndFadeGroup();
			} else {
				//GUILayout.EndVertical();
			}
		}
	} // class CatEditorGUILayout
}
