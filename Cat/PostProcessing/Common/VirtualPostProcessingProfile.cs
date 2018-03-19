using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cat.PostProcessing {
	public class VirtualPostProcessingProfile {
		
		[Tooltip("A list of all settings & overrides.")]
		private Dictionary<Type, PostProcessingSettingsBase> m_Settings = new Dictionary<Type, PostProcessingSettingsBase>();

		private Dictionary<Type, PostProcessingSettingsBase> m_SettingsOnHold = new Dictionary<Type, PostProcessingSettingsBase>();

		public Dictionary<Type, PostProcessingSettingsBase> settings { get { return m_Settings; } }

		public void Reset() {
			m_SettingsOnHold.Clear();
			foreach (var oldSetting in m_Settings) {
				m_SettingsOnHold[oldSetting.Key] = oldSetting.Value;
				oldSetting.Value.isActive = false;
			}
			m_Settings.Clear();
		}

		public void InterpolateTo(VirtualPostProcessingProfile other, float otherFactor) {
			InterpolateTo(other.settings.Values, otherFactor);
		}

		public void InterpolateTo(CatPostProcessingProfile other, float otherFactor) {
			InterpolateTo(other.settings, otherFactor);
		}

		private void InterpolateTo(IEnumerable<PostProcessingSettingsBase> other, float otherFactor) {
			foreach (var otherSetting in other) {
				if (!otherSetting.isActive) {
					continue;
				}
				var type = otherSetting.GetType();
				PostProcessingSettingsBase setting;
				if (!m_Settings.TryGetValue(type, out setting)) {
					if (!m_SettingsOnHold.TryGetValue(type, out setting)) {
						setting = (PostProcessingSettingsBase)Activator.CreateInstance(type);
					} else {
						setting.isActive = true;
					}
					m_Settings[type] = setting;
					setting.TurnAllOverridesOff();
				}
				setting.InterpolateTo(otherSetting, otherFactor);
			}
		}

	}
}
