﻿using System;
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
    private Transform CalibrationObjectTransform;  // This is the object that will move around w/r to the HoloLens for calibration

    private bool initialAlignment = false;

    // *** Calibration Parameters *** //
    public const int SPAAMPoints = 12;
    private double[,] AlignmentPoints;
    private double[,] CalibrationPoints =
        { { 0, 0.5, 0 },
          { 0, 0, 0.5 },
          { 0, 0, 0.2 },
          { 0.6, 0, 0.1 },
          { -0.5, 0, 0.5 },
          { -0.3, 0.2, 0.5 },
          { -0.8, 0, 0.5 },
          { -0.1, 0, 0.5 },
          { -0.3, 0, -0.5 },
          { -0.8, 0, -0.5 },
          { 0.7, 0, 0.5 },
          { -0.05, 0, 0.5 }
        };
    private int currStep = 0;
    //private Transform CalibrationObjectTransform;
    //private Transform AlignmentObjectTransform;
    //private Transform HoloLensMarkerTransform;

    // Use this for initialization
    void Start()
    {
        CalibrationObjectTransform = HoloLensMarker.transform.GetChild( 0 ); // obtain the transform of the container for the calibration object

        AlignmentPoints = new double[SPAAMPoints, 7];                     // Initialize 2D array of numData and 7 which is 3 for position and 4 for orientation


        CalibrationObjectTransform = NeedleMarker.transform;    // Use to set the pre-defined calibration positions
        CalibrationObjectTransform.position = Camera.main.transform.position + new Vector3( (float)CalibrationPoints[0,0], (float)CalibrationPoints[0, 1], (float)CalibrationPoints[0, 2] );  // Set the first position

        textStatus.text = "Step " + (currStep + 1);

        // Testing space
        GetCalibration( CalibrationPoints, CalibrationPoints );
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Input.GetKeyDown( KeyCode.Space ) && (currStep < SPAAMPoints))
        {
            if (!initialAlignment)
            {
                initialAlignment = true;
            }
            Vector3 relativePos = NeedleMarker.transform.position - HoloLensMarker.transform.position;
            Quaternion relativeRot = Quaternion.Inverse( HoloLensMarker.transform.rotation ) * NeedleMarker.transform.rotation; // rotation from HMD to object

            AlignmentPoints[currStep, 0] = relativePos.x;
            AlignmentPoints[currStep, 1] = relativePos.y;
            AlignmentPoints[currStep, 2] = relativePos.z;
            AlignmentPoints[currStep, 3] = relativeRot.w;
            AlignmentPoints[currStep, 4] = relativeRot.x;
            AlignmentPoints[currStep, 5] = relativeRot.y;
            AlignmentPoints[currStep, 6] = relativeRot.z;

            if (currStep == SPAAMPoints - 1)
            {
                textStatus.text = "Done.";
                GetCalibration( CalibrationPoints, AlignmentPoints );
                //Matrix4x4 T_H = GetCalibration( CalibrationPoints, AlignmentPoints );
                //Debug.Log( T_H );
            }
            else
            {
                currStep++;
                CalibrationObjectTransform.position = Camera.main.transform.position + new Vector3( (float)CalibrationPoints[currStep, 0], (float)CalibrationPoints[currStep, 1], (float)CalibrationPoints[currStep, 2] );  // Set next position
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
    /// as qi = T*pi. Where pi are defined points and qi are measured. If we rework the matrix we can get A*t = 0. The reworked 
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
        const int matrixWidth = 15;
        const int matrixHeight = SPAAMPoints*3;

        var A = new double[matrixHeight, matrixWidth];
        for (int p = 0; p < SPAAMPoints; p++)
        {
            double[] row1 = { -qi[p, 0], -qi[p, 1], -qi[p, 2], -1, 0, 0, 0, 0, 0, 0, 0, 0, pi[p, 0] * qi[p, 0], pi[p, 0] * qi[p, 1], pi[p, 0] * qi[p, 2] };
            Buffer.BlockCopy( row1, 0, A, doubleSize * matrixWidth * ( 3 * p), doubleSize * matrixWidth );
            double[] row2 = { 0, 0, 0, 0, -qi[p, 0], -qi[p, 1], -qi[p, 2], -1, 0, 0, 0, 0, pi[p, 1] * qi[p, 0], pi[p, 1] * qi[p, 1], pi[p, 1] * qi[p, 2] };
            Buffer.BlockCopy( row2, 0, A, doubleSize * matrixWidth * (3 * p + 1), doubleSize * matrixWidth );
            double[] row3 = { 0, 0, 0, 0, 0, 0, 0, 0, -qi[p, 0], -qi[p, 1], -qi[p, 2], -1, pi[p, 2] * qi[p, 0], pi[p, 2] * qi[p, 1], pi[p, 2] * qi[p, 2] };
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
        Tresult.m33 = 1.0f;

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