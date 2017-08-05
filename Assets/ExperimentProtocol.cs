using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExperimentProtocol : MonoBehaviour {

    // Experiment Protocol States. For Init remember to collect subject information. (Subject #)
    private enum experimentState
    {
        Init = 1,
        FindSquares = 2,
        CalibrateHeadsetAlignRot = 3,
        CalibrateHeadsetSPAAM = 4,
        EvaluateSPAAM = 5,
        EvaluateNeedleTip = 6,
        Training = 7,
        PhantomExperimentRand1 = 8,
        PhantomExperimentRand2 = 9,
        RayExperimentRand1 = 10,
        RayExperimentRand2 = 11
    }

    private experimentState currState;

	// Use this for initialization
	void Start () {

        currState = experimentState.Init;
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
