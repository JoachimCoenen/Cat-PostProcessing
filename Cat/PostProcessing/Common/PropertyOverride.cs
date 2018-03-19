using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cat.Common;

namespace Cat.PostProcessing {
	[Serializable]
	public abstract class PropertyOverride {
		[SerializeField]
		public bool isOverriding = true;

		protected PropertyOverride(bool isOverriding){
			this.isOverriding = isOverriding;
		}

		public T rawValue<T>() {
			return ((PropertyOverride<T>)this).rawValue;
		}

		public abstract void InterpolateTo(PropertyOverride other, float otherFactor);
	}

	[Serializable]
	public class PropertyOverride<T> : PropertyOverride {

		[SerializeField]
		internal T m_RawValue;

		public T rawValue {
			get { return m_RawValue; }
			set { m_RawValue = value; }
		}

		public PropertyOverride() : this(default(T), true) {}

		public PropertyOverride(T value) : this(value, true) {}

		public PropertyOverride(T value, bool isActive) : base(isActive) {
			m_RawValue = value;
		}

		public override void InterpolateTo(PropertyOverride other, float otherFactor) {
			InterpolateTo(other as PropertyOverride<T>, otherFactor);
		}

		public virtual void InterpolateTo(PropertyOverride<T> other, float otherFactor) {
			this.rawValue = otherFactor >= 0.5f ? other.rawValue : this.rawValue;
		}

		public static implicit operator T(PropertyOverride<T> prop) {
			return prop.rawValue;
		}

		public static explicit operator PropertyOverride<T>(T value) {
			return new PropertyOverride<T>(value);
		}
	}

	[Serializable]
	public class FloatProperty : PropertyOverride<float> {
		public override void InterpolateTo(PropertyOverride<float> other, float otherFactor) {
			this.rawValue = Mathf.Lerp(this.rawValue, other.rawValue, otherFactor);
		}
	}

	[Serializable]
	public class IntProperty : PropertyOverride<int> {
		public override void InterpolateTo(PropertyOverride<int> other, float otherFactor) {
			this.rawValue = Mathf.RoundToInt(Mathf.Lerp(this.rawValue, other.rawValue, otherFactor));
		}
	}

	[Serializable]
	public class BoolProperty : PropertyOverride<bool> {}

	[Serializable]
	public class ColorProperty : PropertyOverride<Color> {
		public override void InterpolateTo(PropertyOverride<Color> other, float otherFactor) {
			this.rawValue = Color.Lerp(this.rawValue, other.rawValue, otherFactor);
		}

		public static implicit operator Vector4(ColorProperty prop) {
			return prop.rawValue;
		}
	}

	[Serializable]
	public class TextureResolutionProperty : PropertyOverride<TextureResolution> {}

	[Serializable]
	public class TextureProperty : PropertyOverride<Texture> {
		public override void InterpolateTo(PropertyOverride<Texture> other, float otherFactor) {
			this.rawValue = (this.rawValue != null && otherFactor < 1) || otherFactor <= 0 ? this.rawValue : other.rawValue;
		}
	}
}