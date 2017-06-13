using UnityEngine;

namespace Cat.Common
{
    public class CustomLabel : PropertyAttribute {
    	public string labelText;
    	public CustomLabel (string aLabelText) {
    		labelText = aLabelText;
    	}
    }
    public class CustomLabelRange : PropertyAttribute {
    	public string labelText;
    	public float min;
    	public float max;
	
    	public CustomLabelRange(float min, float max, string labeltext) {
    		this.min = min;
    		this.max = max;
    		this.labelText = labeltext;
    	}
    }

    public class ReadOnly : PropertyAttribute {
    	public ReadOnly () {
    	}
    }
}
