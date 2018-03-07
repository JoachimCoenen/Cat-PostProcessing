using System;
using System.Collections.Generic;
using UnityEngine;
using Cat.Common;

namespace Cat.PostProcessing {
	
	public sealed class CatPostProcessingProfile : ScriptableObject {
		[Tooltip("A list of all settings & overrides.")]
		public PostProcessingSettingsBase[] m_settings = new PostProcessingSettingsBase[]{} ;

		void OnEnable() {
			// Make sure every setting is valid:
			// m_settings.RemoveAll(x => x == null);
		}
	}
}