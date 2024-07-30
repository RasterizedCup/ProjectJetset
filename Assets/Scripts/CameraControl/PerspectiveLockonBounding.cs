using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerspectiveLockonBounding : MonoBehaviour
{
    [SerializeField]
    Camera mainCam;

    float frustumHeight;
    float frustumWidth;
    // Start is called before the first frame update
    void Start()
    {
        // lock on target receives two sphere triggers.
        // min-trigger: minimum distance cam threshold and the base bounds for all softlock, hardlock targets, and player
        // max-trigger: maximum outer radius of cam threshold, targets or player beyond this threshold no longer have locking apply
        // between min and max: calc distance (N) from min-trigger to target/player furthest from min trigger that is within max trigger
        // expand camera rig fulcrums by N distance to compensate for this (keeping everything in view)
        // potentially add buffer to expand more, damping to prevent instant expansion
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
