using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cat.PostProcessing {
	public class VirtualPostProcessingProfile {
		
		[Tooltip("A list of all settings & overrides.")]
		private readonly Dictionary<Type, PostProcessingSettingsBase> m_Settings = new Dictionary<Type, PostProcessingSettingsBase>();

		public Dictionary<Type, PostProcessingSettingsBase> settings { get { return m_Settings; } }

		public void Reset() {
			foreach (var oldSetting in m_Settings) {
				oldSetting.Value.isOverriding = false;
				oldSetting.Value.Reset();
			}
		}

		public void InterpolateTo(VirtualPostProcessingProfile other, float otherFactor) {
			InterpolateTo(other.settings.Values, otherFactor);
		}

		public void InterpolateTo(CatPostProcessingProfile other, float otherFactor) {
			InterpolateTo(other.settings, otherFactor);
		}

		private void InterpolateTo(IEnumerable<PostProcessingSettingsBase> other, float otherFactor) {
			foreach (var otherSetting in other) {
				if (!otherSetting.isOverriding) {
					continue;
				}
				var type = otherSetting.GetType();
				PostProcessingSettingsBase setting;
				if (!m_Settings.TryGetValue(type, out setting)) {
					setting = (PostProcessingSettingsBase)Activator.CreateInstance(type);
					m_Settings[type] = setting;
				}
				setting.InterpolateTo(otherSetting, otherFactor);
			}
		}

	}
}
