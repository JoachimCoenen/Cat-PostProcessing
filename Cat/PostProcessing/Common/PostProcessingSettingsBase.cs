using System;
using UnityEngine;
using Cat.Common;
namespace Cat.PostProcessing {
	
	[Serializable]
	public class PostProcessingSettingsBase : ScriptableObject {

		public bool isActive = true;

		public readonly string effectName = "Default Name: 'Post Processing Settings Base'";
			
	}
}