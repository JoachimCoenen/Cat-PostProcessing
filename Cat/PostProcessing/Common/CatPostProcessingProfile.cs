using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cat.PostProcessing {
	
	public sealed class CatPostProcessingProfile : ScriptableObject {
		[Tooltip("A list of all settings & overrides.")]
		[SerializeField] 
		private List<PostProcessingSettingsBase> m_settings = new List<PostProcessingSettingsBase>();

		public List<PostProcessingSettingsBase> settings { get { return m_settings; } }

		void OnEnable() {
			// Make sure every setting is valid:
			settings.RemoveAll(x => x == null);
		}
	}
}