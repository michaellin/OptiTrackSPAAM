using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.WSA.Input;
using UnityEngine.UI;

//using Matrix = MathNet.Numerics.LinearAlgebra.Matrix<double>;
//using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;



public class SPAAMCalibration : MonoBehaviour
{
    public float speed = 0.3f;             // speed for moving the camera
    public float distFwd = 0.4f;           // 0.4 meters in front of the user

    // *** User Interface *** //
    public Text textStatus;

    //public GameObject HoloLensMarker;      // This is the virtual object that will be moved around
    public GameObject NeedleMarker;                // This marker will be the one attached to the needle
    public GameObject HoloLensMarker;              // This marker will be the one attached to the HoloLens
    public GameObject CalibrationObject;
    public GameObject TestObject;
    private Transform CalibrationObjectTransform;  // This is the object that will move around w/r to the HoloLens for calibration

    private bool initialAlignmentDone = false;
    public bool completedAlignment = false;
    Matrix4x4 T_H;

    // *** Calibration Parameters *** //
    public const int SPAAMPoints = 12;
    private double[,] AlignmentPoints;
    private double[,] CalibrationPoints =
        { { 0, 0, 0.5 },
          { 0, 0.04, 0.5 },
          { 0.025, 0, 0.5 },
          { 0.025, -0.04, 0.5 },
          { -0.04, 0, 0.5 },
          { -0.025, 0.025, 0.5 },
          { -0.04, 0.04, 0.5 },
          { -0.025, 0, 0.5 },
          { -0.025, -0.04, 0.5 },
          { 0.04, 0, 0.5 },
          { 0.045, 0.02, 0.5 },
          { -0.025, 0, 0.5 }
        };
    private double[,] MeasuredCalibrationPoints;
    private int currStep = 0;
    private bool debounceCalib = false;
    //private Transform CalibrationObjectTransform;
    //private Transform AlignmentObjectTransform;
    //private Transform HoloLensMarkerTransform;

    // Use this for initialization
    void Start()
    {
        T_H = Matrix4x4.identity;    // Initialize the alignment matrix as identity

        CalibrationObjectTransform = CalibrationObject.transform; // obtain the transform of the container for the calibration object
        if (completedAlignment == false)
        {
            TestObject.SetActive( false );
        } else
        {
            CalibrationObject.SetActive( false );
        }

        AlignmentPoints = new double[SPAAMPoints, 7];             // Initialize 2D array of numData and 7 which is 3 for position and 4 for orientation
        MeasuredCalibrationPoints = new double[SPAAMPoints, 3];   // Array to store the calibration points in world coordinates

        CalibrationObjectTransform.localPosition = new Vector3( (float)CalibrationPoints[0,0], (float)CalibrationPoints[0, 1], (float)CalibrationPoints[0, 2] );  // Set the first position

        textStatus.text = "Step " + (currStep + 1);

        // Testing space
        //GetCalibration( CalibrationPoints, CalibrationPoints );
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (completedAlignment)
        {
            // ** 0.00340	-0.04503	-0.03366	0.00316
            //    0.00268    0.02992    0.01796     0.00146
            //    0.05821    0.51140    0.35615    -0.00126
            //    0.07317    0.63595    0.44050     1.00000
            Vector3 relativePos = Camera.main.transform.InverseTransformVector(NeedleMarker.transform.position - HoloLensMarker.transform.position);
            Quaternion relativeRot = Quaternion.Inverse( HoloLensMarker.transform.rotation ) * NeedleMarker.transform.rotation;
            TestObject.transform.localPosition = T_H.MultiplyPoint(relativePos);
            TestObject.transform.rotation = Camera.main.transform.rotation * relativeRot;
        }

        CalibrationObjectTransform.localPosition = new Vector3( (float)CalibrationPoints[currStep, 0], (float)CalibrationPoints[currStep, 1], (float)CalibrationPoints[currStep, 2] );  // Set next position
        //CalibrationObjectTransform.transform.up = Vector3.up;
        if (Input.GetKeyDown( KeyCode.L ))
        {
            Debug.Log(Camera.main.transform.InverseTransformVector( NeedleMarker.transform.position - HoloLensMarker.transform.position ));
        }
        if (Input.GetKeyDown( KeyCode.Space ) && (currStep < SPAAMPoints) && (debounceCalib == false))
        {
            if (!initialAlignmentDone)
            {
                initialAlignmentDone = true;
            }
            Vector3 relativePos = Camera.main.transform.InverseTransformVector(NeedleMarker.transform.position - HoloLensMarker.transform.position); // in the Camera coordinate system
            Quaternion relativeRot = Quaternion.Inverse( HoloLensMarker.transform.rotation ) * NeedleMarker.transform.rotation;                      // rotation from HMD to object

            AlignmentPoints[currStep, 0] = relativePos.x;
            AlignmentPoints[currStep, 1] = relativePos.y;
            AlignmentPoints[currStep, 2] = relativePos.z;
            AlignmentPoints[currStep, 3] = relativeRot.w;
            AlignmentPoints[currStep, 4] = relativeRot.x;
            AlignmentPoints[currStep, 5] = relativeRot.y;
            AlignmentPoints[currStep, 6] = relativeRot.z;

            MeasuredCalibrationPoints[currStep, 0] = CalibrationObjectTransform.position.x;
            MeasuredCalibrationPoints[currStep, 1] = CalibrationObjectTransform.position.y;
            MeasuredCalibrationPoints[currStep, 2] = CalibrationObjectTransform.position.z;

            if (currStep == SPAAMPoints - 1)
            {
                textStatus.text = "Done.";
                Debug.Log( "Done" );
                completedAlignment = true;
                Matrix4x4 T_H = GetCalibration( CalibrationPoints, AlignmentPoints );
                Debug.Log( T_H );

                Debug.Log( T_H.inverse );

                CalibrationObject.SetActive( false );
                TestObject.SetActive( true );

                // Figure out what to do next
                completedAlignment = true;
            }
            else
            {
                currStep++;
                textStatus.text = "Step " + (currStep + 1);
                Debug.Log( "Step " + (currStep + 1) );
            }
            debounceCalib = true;
            Invoke( "recoverDebounce", 0.5f );
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
        else if (Input.GetKeyDown( KeyCode.R ))
        {
            completedAlignment = false;
            CalibrationObject.SetActive( true );
            TestObject.SetActive( false );
            currStep = 0;
            CalibrationObjectTransform.position = new Vector3( (float)CalibrationPoints[0, 0], (float)CalibrationPoints[0, 1], (float)CalibrationPoints[0, 2] );  // Set the first position
        }

        //float xAxisValue = speed * Input.GetAxis( "Horizontal" );
        //float zAxisValue = speed * Input.GetAxis( "Vertical" );
        //if (Camera.current != null)
        //{
        //    Camera.current.transform.Translate( new Vector3( xAxisValue, 0.0f, zAxisValue ) );
        //}

        //transform.position = Camera.main.transform.position + Camera.main.transform.forward * distFwd;
        //if (!initialAlignment)
        //{
        //    transform.rotation = Quaternion.AngleAxis( Camera.main.transform.rotation.eulerAngles.y, Vector3.up );
        //}
    }

    /// <summary>
    /// Recover from debouncing state for space bar
    /// </summary>
    private void recoverDebounce()
    {
        debounceCalib = false;
    }

    /// <summary>
    /// Get Calibration calculates the projection matrix from points in displayed to points in measured. The problem is defined
    /// as qi = T*pi. Where pi are measured and qi are defined points. If we rework the matrix we can get A*t = 0. The reworked 
    /// matrix is of the following form:
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
    private Matrix4x4 GetCalibration( double[,] pi, double[,] qi )
    {
        const int doubleSize = 8;
        const int matrixWidth = 16;
        const int matrixHeight = SPAAMPoints*3;

        var A = new double[matrixHeight, matrixWidth];
        for (int p = 0; p < SPAAMPoints; p++)
        {
            double[] row1 = { -qi[p, 0], -qi[p, 1], -qi[p, 2], -1, 0, 0, 0, 0, 0, 0, 0, 0, pi[p, 0] * qi[p, 0], pi[p, 0] * qi[p, 1], pi[p, 0] * qi[p, 2], pi[p, 0] };
            Buffer.BlockCopy( row1, 0, A, doubleSize * matrixWidth * ( 3 * p), doubleSize * matrixWidth );
            double[] row2 = { 0, 0, 0, 0, -qi[p, 0], -qi[p, 1], -qi[p, 2], -1, 0, 0, 0, 0, pi[p, 1] * qi[p, 0], pi[p, 1] * qi[p, 1], pi[p, 1] * qi[p, 2], pi[p, 1] };
            Buffer.BlockCopy( row2, 0, A, doubleSize * matrixWidth * (3 * p + 1), doubleSize * matrixWidth );
            double[] row3 = { 0, 0, 0, 0, 0, 0, 0, 0, -qi[p, 0], -qi[p, 1], -qi[p, 2], -1, pi[p, 2] * qi[p, 0], pi[p, 2] * qi[p, 1], pi[p, 2] * qi[p, 2], pi[p, 2] };
            Buffer.BlockCopy( row3, 0, A, doubleSize * matrixWidth * (3 * p + 2), doubleSize * matrixWidth );
        }
        
        // Prepare matrices to get SVD results
        double[] W = new double[matrixWidth];
        double[,] U = new double[matrixHeight, matrixWidth];
        double[,] VT = new double[matrixWidth, matrixWidth];

        alglib.svd.rmatrixsvd( A, matrixHeight, matrixWidth, 0, 1, 2, ref W, ref U, ref VT ); // SVD with alglib

        double[] coeffs = new double[matrixWidth];

        Buffer.BlockCopy( VT, doubleSize * matrixWidth * (matrixWidth - 1), coeffs, 0, doubleSize * matrixWidth ); // Last row of VT contains the parameters of the transform matrix.
        printMatrix( coeffs );

        Matrix4x4 Tresult = new Matrix4x4();
        Tresult.m00 = (float)coeffs[0];
        Tresult.m01 = (float)coeffs[1];
        Tresult.m02 = (float)coeffs[2];
        Tresult.m03 = (float)coeffs[3];
        Tresult.m10 = (float)coeffs[4];
        Tresult.m11 = (float)coeffs[5];
        Tresult.m12 = (float)coeffs[6];
        Tresult.m13 = (float)coeffs[7];
        Tresult.m20 = (float)coeffs[8];
        Tresult.m21 = (float)coeffs[9];
        Tresult.m22 = (float)coeffs[10];
        Tresult.m23 = (float)coeffs[11];
        Tresult.m30 = (float)coeffs[12];
        Tresult.m31 = (float)coeffs[13];
        Tresult.m32 = (float)coeffs[14];
        Tresult.m33 = (float)coeffs[15];

        return Tresult;
    }

    static void printMatrix(double[,] M)
    {
        int height = M.GetLength( 0 );
        int width = M.GetLength( 1 );
        Debug.Log( height );
        Debug.Log( width );
        string ToPrint = "";
        for (int i = 0; i < height; i ++)
        {
            for (int j = 0; j < width; j++)
            {
                ToPrint += M[i, j].ToString();
                ToPrint += "\t";
            }
            ToPrint += "\n";
        }
        Debug.Log( ToPrint );
    }

    static void printMatrix( double[] arr )
    {
        int len = arr.GetLength(0);
        string ToPrint = "";
        ToPrint += "[";
        for (int i = 0; i < len; i++)
        {
            ToPrint += arr[i].ToString();
            ToPrint += "\t";
        }
        ToPrint += "]";
        Debug.Log( ToPrint );
    }
}


//0.01808	-0.07162	-0.04280	0.00015
//0.01320	-0.05461	-0.03111	-0.00093
//0.06696	-0.26426	-0.15963	0.00166
//0.19797	-0.79218	-0.47122	1.00000


//-0.01073	-0.04196	-0.03186	0.00028
//-0.01492	-0.05362	-0.03482	-0.00336
//-0.05420	-0.21257	-0.16917	0.00611
//-0.19471	-0.74994	-0.56186	1.00000

//0.00824	    0.20384	    0.14430	   -0.00116
//0.00768	    0.18966	    0.13457	   -0.00125
//-0.01778	-0.42970	-0.30268	0.00149
//-0.02845	-0.63785	-0.44629	1.00000


//0.43976	0.00045	-0.00536	0.00080
//0.00632	0.44879	-0.00260	0.04021
//-0.00197	0.08185	0.47870	-0.00038
//-0.00248	0.10202	0.59769	1.00000