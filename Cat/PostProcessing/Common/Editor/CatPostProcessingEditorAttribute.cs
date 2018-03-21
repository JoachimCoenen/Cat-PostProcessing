using System;
using UnityEngine;

namespace Cat.PostProcessing {
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public sealed class CatPostProcessingEditorAttribute : Attribute {
		public readonly Type settingsType;

		public CatPostProcessingEditorAttribute(Type settingsType) {
			this.settingsType = settingsType;
		}
	}
}
