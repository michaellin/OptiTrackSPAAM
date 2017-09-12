using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HMDMarkerManager : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
    // Used to update the camera position based on self position
	void Update () {
        Camera.main.transform.position = transform.position;
        Camera.main.transform.rotation = transform.rotation;
	}
}
