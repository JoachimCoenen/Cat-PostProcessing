using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using Cat.Common;
using System.Text;

namespace Cat.PostProcessing {
	
	[Serializable]
	public abstract class PostProcessingSettingsBase : ScriptableObject {
		abstract public bool enabled { get; }
		public bool isOverriding = true;

		public abstract string effectName { get; }
		public abstract int queueingPosition { get; } 

		public abstract void Reset();

		internal ReadOnlyCollection<PropertyOverride> properties;
		void OnEnable() {
			// Automatically grab all fields of type ParameterOverride for this instance
			properties = GetType()
				.GetFields(BindingFlags.Public | BindingFlags.Instance)
				.Where(t => t.FieldType.IsSubclassOf(typeof(PropertyOverride)))
				.OrderBy(t => t.MetadataToken) // Guaranteed order
				.Select(t => (PropertyOverride)t.GetValue(this))
				.ToList()
				.AsReadOnly();

		}

		internal void TurnAllOverridesOff() {
			foreach (var property in properties) {
				property.isOverriding = false;
			}
		}

		internal void TurnAllOverridesOn() {
			foreach (var property in properties) {
				property.isOverriding = true;
			}
		}

		public void InterpolateTo(PostProcessingSettingsBase other, float otherFactor) {
			for (int i = 0; i < properties.Count; i++) {
				if (other.properties[i].isOverriding) {
					this.properties[i].InterpolateTo(other.properties[i], otherFactor);
				}
			}
		}
			
	}

}