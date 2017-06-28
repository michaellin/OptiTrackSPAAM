using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.WSA.Input;
using UnityEngine.UI;

using MathNet.Numerics.LinearAlgebra;


public class SPAAMCalibration : MonoBehaviour
{
    public float speed = 0.3f;             // speed for moving the camera
    public float distFwd = 0.4f;           // 0.4 meters in front of the user

    // *** User Interface *** //
    public Text textStatus;

    public GameObject CalibrationObject;   // This is the virtual object that will be moved around
    public GameObject AlignmentObject;     // This object will be the one attached to the alignment object
    public GameObject AlignmentObjectHMD;  // This object will be the one attached to the HoloLens

    private bool initialAlignment = false;

    // *** Calibration Parameters *** //
    private int numData = 12;
    private float[,] AlignmentPoints;
    private Vector3[] CalibrationPoints =
        { new Vector3( 0, 0, 0 ),
          new Vector3( 0, 0, 0.5f),
          new Vector3( 0, 0, 0.2f),
          new Vector3( 0.6f, 0, 0.1f),
          new Vector3( -0.5f, 0, 0.5f),
          new Vector3( -0.3f, 0, 0.5f),
          new Vector3( -0.8f, 0, 0.5f),
          new Vector3( -0.1f, 0, 0.5f),
          new Vector3( -0.3f, 0, -0.5f),
          new Vector3( -0.8f, 0, -0.5f),
          new Vector3( 0.7f, 0, 0.5f),
          new Vector3( -0.05f, 0, 0.5f)
        };
    private int currStep = 0;
    private Transform CalibrationObjectTransform;
    private Transform AlignmentObjectTransform;
    private Transform AlignmentObjectHMDTransform;

    // Use this for initialization
    void Start()
    {
        AlignmentPoints = new float[numData, 7];                     // Initialize 2D array of numData and 7 which is 3 for position and 4 for orientation
        AlignmentObjectTransform = AlignmentObject.transform;
        AlignmentObjectHMDTransform = AlignmentObjectHMD.transform;

        CalibrationObjectTransform = CalibrationObject.transform;
        CalibrationObjectTransform.position = CalibrationPoints[0];  // Set the first position
        textStatus.text = "Step " + (currStep + 1);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Input.GetKeyDown( KeyCode.Space ) && (currStep < numData))
         {
            if (!initialAlignment)
            {
                initialAlignment = true;
            }
            Vector3 relativePos = AlignmentObjectTransform.position - AlignmentObjectHMDTransform.position;
            Quaternion relativeRot = Quaternion.Inverse( AlignmentObjectHMDTransform.rotation) * AlignmentObjectTransform.rotation; // rotation from HMD to object

            AlignmentPoints[currStep, 0] = relativePos.x;
            AlignmentPoints[currStep, 1] = relativePos.y;
            AlignmentPoints[currStep, 2] = relativePos.z;
            AlignmentPoints[currStep, 3] = relativeRot.w;
            AlignmentPoints[currStep, 4] = relativeRot.x;
            AlignmentPoints[currStep, 5] = relativeRot.y;
            AlignmentPoints[currStep, 6] = relativeRot.z;

            if (currStep == numData - 1)
            {
                textStatus.text = "Done.";
            }
            else
            {
                currStep++;
                CalibrationObjectTransform.position = CalibrationPoints[currStep];  // Set next position
                textStatus.text = "Step " + (currStep + 1);
            }
        } else if (Input.GetKeyDown( KeyCode.P ))
        {
            for (int i = 0; i < currStep; i ++)
            {
                Debug.Log( "posx " + AlignmentPoints[i, 0].ToString( "F4" ) +
                            " posy " + AlignmentPoints[i, 1].ToString( "F4" ) +
                            " posz " + AlignmentPoints[i, 2].ToString( "F4" ) +
                            " rotw " + AlignmentPoints[i, 3].ToString( "F4" ) +
                            " rotx " + AlignmentPoints[i, 4].ToString( "F4" ) +
                            " roty " + AlignmentPoints[i, 5].ToString( "F4" ) +
                            " rotz " + AlignmentPoints[i, 6].ToString( "F4" ) );
            }
        }

        float xAxisValue = speed*Input.GetAxis( "Horizontal" );
        float zAxisValue = speed*Input.GetAxis( "Vertical" );
        if (Camera.current != null)
        {
            Camera.current.transform.Translate( new Vector3( xAxisValue, 0.0f, zAxisValue ) );
        }

        transform.position = Camera.main.transform.position + Camera.main.transform.forward * distFwd;
        if (!initialAlignment)
        {
            transform.rotation = Quaternion.AngleAxis( Camera.main.transform.rotation.eulerAngles.y, Vector3.up );
        }
    }

    private Matrix4x4 GetCalibration(float[,] measured, float[,] displayed)
    {
        // TODO
        return new Matrix4x4();
    }

}
