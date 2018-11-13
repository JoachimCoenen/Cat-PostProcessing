using System;
using UnityEngine;
//using UnityEditor;
using UnityEditor.AnimatedValues;
namespace Cat.Common
{
	public static class CatGUILayout {
		public static class Styles {
			public static bool isProSkin = true;
			private static GUIStyle s_BoxSkin = null;
			private static GUIStyle s_SplitterSkin = null;
			private static GUIStyle s_FoldoutSkin = null;
			private static GUIStyle s_ContextButtonSkin = null;
			private static GUIStyle s_FoldoutButtonSkin = null;
			// private static GUIStyle s_PureToggleSkin = null;


			private static GUIStyle s_BoldLabel = null;
			public static GUIStyle boldLabel {
				get {
					if (s_BoldLabel == null) {
						s_BoldLabel = new GUIStyle(GUI.skin.label);
						s_BoldLabel.fontStyle = FontStyle.Bold;
						s_BoldLabel.overflow.left = 9;
					}
					return s_BoldLabel;
				}
			}

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
			/*
			public static GUIStyle FoldoutSkin {
				get {
					if (s_FoldoutSkin == null) {
						s_FoldoutSkin = new GUIStyle(EditorStyles.foldout);
						s_FoldoutSkin.fontStyle = FontStyle.Bold;
					}
					return s_FoldoutSkin;
				}
			}
			*/
			public static GUIStyle ContextButtonSkin {
				get {
					if (s_ContextButtonSkin == null) {
						s_ContextButtonSkin = new GUIStyle(GUI.skin.label);
						s_ContextButtonSkin.fixedWidth = 21;
						s_ContextButtonSkin.padding = new RectOffset(14, 2, 1, 2);
						s_ContextButtonSkin.padding.left = GUI.skin.label.padding.left;
					}
					return s_ContextButtonSkin;
				}
			}
			public static GUIStyle FoldoutButtonSkin {
				get {
					if (s_FoldoutButtonSkin == null) {
						s_FoldoutButtonSkin = new GUIStyle(GUI.skin.label);
						s_FoldoutButtonSkin.padding = new RectOffset(14, 2, 1, 2);
						s_FoldoutButtonSkin.fixedWidth = 13;
						s_FoldoutButtonSkin.margin.left = -11;
						//s_FoldoutButtonSkin.padding.left = EditorStyles.label.padding.left;
					}
					return s_FoldoutButtonSkin;
				}
			}
			/*
			private static Texture2D s_ContextButtonIcon = null;
			public static Texture2D ContextButtonIcon {
				get {
					if (s_ContextButtonIcon == null) {
						if (EditorGUIUtility.isProSkin) {
							s_ContextButtonIcon = (Texture2D)EditorGUIUtility.Load("Builtin Skins/DarkSkin/Images/pane options.png");
						} else {
							s_ContextButtonIcon = (Texture2D)EditorGUIUtility.Load("Builtin Skins/LightSkin/Images/pane options.png");
							//s_ContextButtonIcon = (Texture2D)EditorGUIUtility.Load("Builtin Skins/LightSkin/Images/listicon.png");
						}
					}
					return s_ContextButtonIcon;
				}
			}

			private static Texture2D s_FoldoutIcon = null;
			public static Texture2D FoldoutIcon {
				get {
					if (s_FoldoutIcon == null) {
						if (EditorGUIUtility.isProSkin) {
							s_FoldoutIcon = (Texture2D)EditorGUIUtility.Load("Builtin Skins/DarkSkin/Images/in foldout.png");
						} else {
							s_FoldoutIcon = (Texture2D)EditorGUIUtility.Load("Builtin Skins/LightSkin/Images/in foldout.png");
						}
					}
					return s_FoldoutIcon;
				}
			}
			private static Texture2D s_FoldoutActIcon = null;
			public static Texture2D FoldoutActIcon {
				get {
					if (s_FoldoutActIcon == null) {
						if (EditorGUIUtility.isProSkin) {
							s_FoldoutActIcon = (Texture2D)EditorGUIUtility.Load("Builtin Skins/DarkSkin/Images/in foldout on.png");
						} else {
							s_FoldoutActIcon = (Texture2D)EditorGUIUtility.Load("Builtin Skins/LightSkin/Images/in foldout on.png");
						}
					}
					return s_FoldoutActIcon;
				}
			}

			private const string s_ToggleOffIconName = "shurikencheckmarknormal";
			private const string s_ToggleOnIconName = s_ToggleOffIconName + "on";
			// "shurikencheckmarknormalon"
			// "shurikentogglenormalon"
			// "ol toggle on"
			private static Texture2D s_ToggleOnIcon = null;
			public static Texture2D ToggleOnIcon {
				get {
					if (s_ToggleOnIcon == null) {
						if (EditorGUIUtility.isProSkin) {
							s_ToggleOnIcon = (Texture2D)EditorGUIUtility.Load("Builtin Skins/DarkSkin/Images/" + s_ToggleOnIconName + ".png");
						} else {
							s_ToggleOnIcon = (Texture2D)EditorGUIUtility.Load("Builtin Skins/LightSkin/Images/" + s_ToggleOnIconName + ".png");
						}
					}
					return s_ToggleOnIcon;
				}
			}
			private static Texture2D s_ToggleOffIcon = null;
			public static Texture2D ToggleOffIcon {
				get {
					if (s_ToggleOffIcon == null) {
						if (EditorGUIUtility.isProSkin) {
							s_ToggleOffIcon = (Texture2D)EditorGUIUtility.Load("Builtin Skins/DarkSkin/Images/" + s_ToggleOffIconName + ".png");
						} else {
							s_ToggleOffIcon = (Texture2D)EditorGUIUtility.Load("Builtin Skins/LightSkin/Images/" + s_ToggleOffIconName + ".png");
						}
					}
					return s_ToggleOffIcon;
				}
			}
			*/
			//public static Color BoxColor				= new Color(0.83f, 0.83f, 0.83f, 1.0f);
			//public static Color boxColor				= new Color(0.867f, 0.867f, 0.867f, 1.0f);
			public static Color BoxColor { get{ return new Color(0.816f, 0.816f, 0.816f, 1.0f); } }
			public static Color boxProColor { get{ return new Color(0.243f, 0.243f, 0.243f, 1.0f); } }

			const float c = 0.07f;
			public static Color splitterColor { get{ return new Color(0.76f-c, 0.76f-c, 0.76f-c, 1); } }
			//public static Color splitterColor { get{ return new Color(0.6f, 0.6f, 0.6f, 1); } }
			public static Color splitterProColor { get{ return new Color(0.18f-c, 0.18f-c, 0.18f-c, 1); } }

		}

		public static void BeginBox() {
			Color guibackgroundColor = GUI.backgroundColor;

			GUI.backgroundColor *= Styles.isProSkin ? Styles.boxProColor : Styles.BoxColor;				
			GUILayout.BeginVertical(Styles.BoxSkin);
			GUI.backgroundColor = guibackgroundColor;
		}

		public static void EndBox() {
			GUILayout.EndVertical();
		}

		public static bool Foldout(bool isExpanded, string text) {
			isExpanded = GUILayout.Toggle(isExpanded, text);
			return isExpanded;
		}

		public static bool Foldout(bool isExpanded, GUIContent text) {
			isExpanded = GUILayout.Toggle(isExpanded, text);
			return isExpanded;
		}
		/*
		public static bool ToggledFoldout(bool isExpanded, bool toggleState, Action<bool> setToggleState, string text) {
			setToggleState(EditorGUILayout.Toggle(toggleState));
			isExpanded = EditorGUILayout.Foldout(isExpanded, text, true, Styles.FoldoutSkin);
			return isExpanded;
		}
		*/
		public static void Header(string text) {
			GUILayout.Label(text, Styles.boldLabel);
		}

		public static void Header(GUIContent text) {
			GUILayout.Label(text, Styles.boldLabel);
		}
		/*
		public static bool ContextButton() {
			var contextIcon = Styles.ContextButtonIcon;
			var rect = GUILayoutUtility.GetRect(new GUIContent(contextIcon), Styles.ContextButtonSkin);
			var contextRect = new Rect(rect.x, rect.y + 4, contextIcon.width, contextIcon.height);

			GUI.DrawTexture(contextRect, contextIcon);

			var e = Event.current;
			if (e.type == EventType.MouseDown && contextRect.Contains(e.mousePosition)) {   
				e.Use();
				return true;
			}
			return false;
			//return GUILayout.Button(Styles.ContextButtonIcon, Styles.ContextButtonSkin);
		}
		*/
		/*
		public static bool FoldoutToggle(bool value) {
			var foldOutIcon = value ? Styles.FoldoutActIcon : Styles.FoldoutIcon;
			var rect = GUILayoutUtility.GetRect(foldOutIcon.width - 12, foldOutIcon.height, Styles.FoldoutButtonSkin);
			var contextRect = new Rect(rect.x - 11, rect.y + 1, foldOutIcon.width, foldOutIcon.height);
			GUI.DrawTexture(contextRect, foldOutIcon);

			var e = Event.current;
			if (e.type == EventType.MouseDown && contextRect.Contains(e.mousePosition)) {   
				e.Use();
				value = !value;
			}
			return value;
			//return GUILayout.Button(Styles.ContextButtonIcon, Styles.ContextButtonSkin);
		}
		*/
		public static bool ActivationToggle(bool value, bool isLeftMost = true) { // TODO: find better name for 'bool ActivationToggle(bool)'
			var toggleIcon = new GUIContent(value ? "X" : "O");
			var toggleIconSize = GUI.skin.label.CalcSize(toggleIcon);
			var rect = GUILayoutUtility.GetRect(
				toggleIconSize.x - (isLeftMost ? 12 : 0), 
				toggleIconSize.y, Styles.FoldoutButtonSkin
			);
			var toggleRect = new Rect(
				rect.x - (isLeftMost ? 11 : 0), 
				rect.y + 1, 
				toggleIconSize.x, 
				toggleIconSize.y
			);
			GUI.Label(toggleRect, toggleIcon);

			var e = Event.current;
			if (e.type == EventType.MouseDown && toggleRect.Contains(e.mousePosition)) {   
				e.Use();
				value = !value;
			}
			return value;
			//return GUILayout.Button(Styles.ContextButtonIcon, Styles.ContextButtonSkin);
		}

		/*
		public static bool PureToggle(bool value) {
			value = EditorGUILayout.ToggleLeft("", value, Styles.PureToggleSkin);
			return value;
		}
		*/

		public static void Splitter(float thickness = 2) {
			Color guibackgroundColor = GUI.backgroundColor;

			GUI.backgroundColor *= Styles.isProSkin ? Styles.splitterProColor : Styles.splitterColor;
			GUILayout.Box("", Styles.SplitterSkin, new GUILayoutOption[]{GUILayout.ExpandWidth(true), GUILayout.Height(thickness)});
			GUI.backgroundColor = guibackgroundColor;
		}

		private static bool IsFadeGroupSupported() {
			return true;
		}
		/*
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
		*/

	} // class CatEditorGUILayout

}
