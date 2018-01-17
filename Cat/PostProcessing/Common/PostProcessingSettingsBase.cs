using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cat.Common;
namespace Cat.PostProcessing {
	
	[Serializable]
	public abstract class PostProcessingSettingsBase : ScriptableObject {

		public bool isActive = true;

		public abstract string effectName { get; }



/*
		public static PostProcessingSettingsBase defaultSettings { 
			get {
				return new PostProcessingSettingsBase();//This();
			}
		}
*/

/*
		public PostProcessingSettingsBase Copy() {
			var original = this;
			var copy = defaultSettings;


			var fields = original.GetType().GetFields();
			foreach (var field in fields) {
				//field.DeclaringType.IsArray;
				object fieldValue = field.GetValue(original);

				if (fieldValue is Array) {
					CopyArrayMembers(fieldValue, field);
				}

			}

			copy.MemberwiseClone();
		}

		private void CopyField(FieldInfo field, object original, object copy) {

			//field.DeclaringType.IsArray;
			object originalFieldValue = field.GetValue(original);

			object copiedFieldValue = null;

			if (originalFieldValue is Array) {
				CopyArrayMembers(originalFieldValue, ref copiedFieldValue);
			}

			field.SetValue(copy, fieldValue);


		}

		private void CopyArrayMembers(object originalFieldValue, ref object copiedFieldValue) {
			Debug.Assert(originalFieldValue is Array);

			var originalArray = originalFieldValue as Array;
			var copiedArray = originalFieldValue.GetType().GetConstructor()

			foreach (var member in originalArray) {
				member;
			}

			copiedFieldValue = copiedArray;
		}
*/
	}
}