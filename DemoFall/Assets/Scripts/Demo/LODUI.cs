using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LODUI : MonoBehaviour {

    public Slider LOD_Slider;
    public Text LOD_Value;

	public void ToggleSlider(bool value) {
        LOD_Slider.gameObject.SetActive(!value);
    }

    public void LODValueChange(float value) {
        LOD_Value.text = value.ToString();
    }
}
