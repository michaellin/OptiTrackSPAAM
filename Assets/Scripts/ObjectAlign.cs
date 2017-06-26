using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.WSA.Input;

public class ObjectAlign : MonoBehaviour
{

    private bool anchored = false;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Input.GetKeyDown( KeyCode.A ))
        {
            anchored ^= true;
        }
        else if (Input.GetKeyDown( KeyCode.UpArrow ))
        {
            transform.position += Vector3.up * 0.002f;
        }
        else if (Input.GetKeyDown( KeyCode.DownArrow ))
        {
            transform.position -= Vector3.up * 0.002f;
        }
        else if (Input.GetKeyDown( KeyCode.RightArrow ))
        {
            transform.position += (Camera.main.transform.TransformPoint( Vector3.right * 0.002f ) - Camera.main.transform.position);
        }
        else if (Input.GetKeyDown( KeyCode.LeftArrow ))
        {
            transform.position -= (Camera.main.transform.TransformPoint( Vector3.right * 0.002f ) - Camera.main.transform.position);
        }

        transform.position = Camera.main.transform.position + Camera.main.transform.forward * 0.6f;
        if (!anchored)
        {
            transform.rotation = Quaternion.AngleAxis( Camera.main.transform.rotation.eulerAngles.y, Vector3.up );
        }
    }

}
