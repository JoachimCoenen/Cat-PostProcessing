using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
namespace Cat.CommonEditor
{
	public static class CatEditorGUILayout {
		private static class Styles {
			private static GUIStyle s_BoxSkin = null;
			private static GUIStyle s_SplitterSkin = null;
			private static GUIStyle s_FoldoutSkin = null;
			private static GUIStyle s_ContextButtonSkin = null;

			private static Texture2D s_ContextButtonIcon = null;

			public static GUIStyle BoxSkin {
				get {
					if (s_BoxSkin == null) {
						s_BoxSkin = new GUIStyle(GUI.skin.box);
						s_BoxSkin.normal.background = Texture2D.whiteTexture;
						//s_BoxSkin.padding.top = Styles.boxSkin.padding.bottom+2;
						s_BoxSkin.overflow.left = 9;
					}
					return s_BoxSkin;
				}
			}
			public static GUIStyle SplitterSkin {
				get {
					if (s_SplitterSkin == null) {
						s_SplitterSkin = new GUIStyle(GUI.skin.box);
						s_SplitterSkin.normal.background = Texture2D.whiteTexture;
						s_SplitterSkin.margin.bottom = 6;
						s_SplitterSkin.overflow.left = 0;
						s_SplitterSkin.overflow.right = 0;
					}
					return s_SplitterSkin;
				}
			}
			public static GUIStyle FoldoutSkin {
				get {
					if (s_FoldoutSkin == null) {
						s_FoldoutSkin = new GUIStyle(EditorStyles.foldout);
						s_FoldoutSkin.fontStyle = FontStyle.Bold;
					}
					return s_FoldoutSkin;
				}
			}
			public static GUIStyle ContextButtonSkin {
				get {
					if (s_ContextButtonSkin == null) {
						s_ContextButtonSkin = new GUIStyle(EditorStyles.label);
						s_ContextButtonSkin.fixedWidth = 21;
						s_ContextButtonSkin.padding = EditorStyles.foldout.padding;
						s_ContextButtonSkin.padding.left = EditorStyles.label.padding.left;
					}
					return s_ContextButtonSkin;
				}
			}

			public static Texture2D ContextButtonIcon {
				get {
					if (s_ContextButtonIcon == null) {
						if (EditorGUIUtility.isProSkin) {
							s_ContextButtonIcon = (Texture2D)EditorGUIUtility.Load("Builtin Skins/DarkSkin/Images/pane options.png");
						} else {
							s_ContextButtonIcon = (Texture2D)EditorGUIUtility.Load("Builtin Skins/LightSkin/Images/pane options.png");
						}
					}
					return s_ContextButtonIcon;
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
			
		public static bool ContextButton() {
			var contextIcon = Styles.ContextButtonIcon;
			var rect = GUILayoutUtility.GetRect(new GUIContent(contextIcon), Styles.ContextButtonSkin);
			var contextRect = new Rect(rect.x, rect.y + 3, contextIcon.width, contextIcon.height);

			GUI.DrawTexture(contextRect, contextIcon);

			var e = Event.current;
			if (e.type == EventType.MouseDown && contextRect.Contains(e.mousePosition)) {   
				e.Use();
				return true;
			}
			return false;
			//return GUILayout.Button(Styles.ContextButtonIcon, Styles.ContextButtonSkin);
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
