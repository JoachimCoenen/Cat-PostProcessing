using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cat.Common;
namespace Cat.PostProcessing {
	
	//[Serializable]
	public class PostProcessingSettingsBase : ScriptableObject {

		public static PostProcessingSettingsBase defaultSettings { 
			get {
				return new PostProcessingSettingsBase();//This();
			}
		}

		public PostProcessingSettingsBase Copy() {
			var original = this;
			var copy = defaultSettings;


			var fields = original.GetType().GetFields();
			foreach (var field in fields) {
				field.DeclaringType.IsArray;
				object fieldValue = field.GetValue(original, null);

				if (fieldValue is Array) {
					CopyArrayMembers(fieldValue, field);
				}

			}

			copy.MemberwiseClone();
		}

		private void CopyArrayMembers(object fieldValue, FieldInfo field) {
			Debug.Assert(fieldValue is Array);

			var array = fieldValue as Array;
			foreach (var member in array) {
				member;
			}
			
		}
			
	}
}