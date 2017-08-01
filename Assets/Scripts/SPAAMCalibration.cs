using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.WSA.Input;
using UnityEngine.UI;

using Matrix = MathNet.Numerics.LinearAlgebra.Matrix<double>;
using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;


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
    private float[,] CalibrationPoints =
        { { 0, 0.5f, 0 },
          { 0, 0, 0.5f },
          { 0, 0, 0.2f },
          { 0.6f, 0, 0.1f },
          { -0.5f, 0, 0.5f },
          { -0.3f, 0.2f, 0.5f },
          { -0.8f, 0, 0.5f },
          { -0.1f, 0, 0.5f },
          { -0.3f, 0, -0.5f },
          { -0.8f, 0, -0.5f },
          { 0.7f, 0, 0.5f },
          { -0.05f, 0, 0.5f }
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

        CalibrationObjectTransform = CalibrationObject.transform;    // Use to set the pre-defined calibration positions
        CalibrationObjectTransform.position = Camera.main.transform.position + new Vector3( CalibrationPoints[0,0], CalibrationPoints[0, 1], CalibrationPoints[0, 2] );  // Set the first position
        textStatus.text = "Step " + (currStep + 1);

        // Testing space
        // GetCalibration( CalibrationPoints, CalibrationPoints );
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
            Quaternion relativeRot = Quaternion.Inverse( AlignmentObjectHMDTransform.rotation ) * AlignmentObjectTransform.rotation; // rotation from HMD to object

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
                CalibrationObjectTransform.position = Camera.main.transform.position + new Vector3( CalibrationPoints[currStep, 0], CalibrationPoints[currStep, 1], CalibrationPoints[currStep, 2] );  // Set next position
                textStatus.text = "Step " + (currStep + 1);
            }
        }
        else if (Input.GetKeyDown( KeyCode.P ))
        {
            for (int i = 0; i < currStep; i++)
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

        float xAxisValue = speed * Input.GetAxis( "Horizontal" );
        float zAxisValue = speed * Input.GetAxis( "Vertical" );
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

    /// <summary>
    /// Get Calibration calculates the projection matrix from points in displayed to points in measured. The problem is defined
    /// as qi = T*pi. If we rework the matrix we can get A*t = 0. The reworked matrix is of the following form:
    /// A = [-q1 -q2 -q3 -1   0   0   0  0   0   0   0  0 p1q1 p1q2 p1q3;
    ///        0   0   0  0 -q1 -q2 -q3 -1   0   0   0  0 p2q1 p2q2 p2q3;
    ///        0   0   0  0   0   0   0  0 -q1 -q2 -q3 -1 p3q1 p3q2 p3q3;
    ///        ...
    ///        ]
    /// and t is of the form:
    /// t = [t11; t12; t13; t14; t21; t22; t23; t24; t31; t32; t33; t34; t41; t42; t43]
    /// We solve for t using SVD decomposition.
    /// </summary>
    /// <param name="pi"></param>
    /// <param name="qi"></param>
    /// <returns></returns>
    private Matrix4x4 GetCalibration( float[,] pi, float[,] qi )
    {
        Matrix A = Matrix.Build.Dense( 12 * 3, 15, 0 ); // Initialized a matrix of 36x15 of zeros
        for (int p = 0; p < 12; p++)
        {
            Vector currRow1 = Vector.Build.DenseOfArray( new double[] { -qi[p, 0], -qi[p, 1], -qi[p, 2], -1, 0, 0, 0, 0, 0, 0, 0, 0, pi[p, 0] * qi[p, 0], pi[p, 0] * qi[p, 1], pi[p, 0] * qi[p, 2] } );
            Vector currRow2 = Vector.Build.DenseOfArray( new double[] { 0, 0, 0, 0, -qi[p, 0], -qi[p, 1], -qi[p, 2], -1, 0, 0, 0, 0, pi[p, 1] * qi[p, 0], pi[p, 1] * qi[p, 1], pi[p, 1] * qi[p, 2] } );
            Vector currRow3 = Vector.Build.DenseOfArray( new double[] { 0, 0, 0, 0, 0, 0, 0, 0, -qi[p, 0], -qi[p, 1], -qi[p, 2], -1, pi[p, 2] * qi[p, 0], pi[p, 2] * qi[p, 1], pi[p, 2] * qi[p, 2] } );
            A.SetRow( 3 * p, currRow1 );
            A.SetRow( 3 * p + 1, currRow2 );
            A.SetRow( 3 * p + 2, currRow3 );
        }

        var svd = A.Svd(true);

        //Debug.Log( svd.VT );

        //var diff = A - svd.U * svd.W * svd.VT;

        Debug.Log( "SVD" );
        Debug.Log( svd.VT );

        return new Matrix4x4();
    }

}