using UnityEngine;
using UnityEditor.ProjectWindowCallback;
using System.IO;
using UnityEditor;
using Cat.PostProcessing;

namespace Cat.PostProcessingEditor
{
    public static class CatPostProcessingFactory
    {
        [MenuItem("Assets/Create/Cat Post-Processing Profile", priority = 201)]
        static void MenuCreatePostProcessingProfile()
        {
            var icon = EditorGUIUtility.FindTexture("ScriptableObject Icon");
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateCatPostProcessingProfile>(), "New Cat Post-Processing Profile.asset", icon, null);
        }

		internal static CatPostProcessingProfile CreateCatPostProcessingProfileAtPath(string path)
        {
			var profile = ScriptableObject.CreateInstance<CatPostProcessingProfile>();
            profile.name = Path.GetFileName(path);
            AssetDatabase.CreateAsset(profile, path);
            return profile;
        }
    }

	class DoCreateCatPostProcessingProfile : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
			CatPostProcessingProfile profile = CatPostProcessingFactory.CreateCatPostProcessingProfileAtPath(pathName);
            ProjectWindowUtil.ShowCreatedAsset(profile);
        }
    }
}
