using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using UnityEngine;

public class CameraRecenterLogic : MonoBehaviour
{
    [SerializeField]
    CinemachineFreeLook camControls;
    [SerializeField]
    CinemachineCollider camCollider;
    [SerializeField]
    GameObject CamObj;
    [SerializeField]
    GameObject PlayerCamObj;
    [SerializeField]
    PlayerMovementManager PlayerObj;
    [SerializeField]
    float wallRideInstantRecenterRate;
    [SerializeField]
    float railInstantRecenterRate;
    [SerializeField]
    float railRecenterRate;
    [SerializeField]
    float wallRideRecenterRate;
    [SerializeField]
    float baseRailWaitTime;
    [SerializeField]
    float baseWallWaitTime;
    bool isFirstMount; // instant recenter when wallriding
    bool isUpdateMount;

    [SerializeField]
    float initialMountThreshold; // time where instant recenter is enabled
    [SerializeField]
    float InstantRecenterBreakThreshold;
    [SerializeField]
    float camOffsetBiasWallRide;
    [SerializeField]
    float camOffsetBiasWallRideChangeRate;
    [SerializeField]
    float wallRideInstantPOFdamper;
    [SerializeField]
    float wallRideInstantPOFtransferRate;
    [SerializeField]
    float wallCamBiasDetectionRadius;
    [SerializeField]
    float wallCamBiasOffsetMultiplier;
    [SerializeField]
    float wallRideReducedCamApproachSpeed = 1.2f;
    [SerializeField]
    float pointOfFocusOffsetClamp;
    [SerializeField]
    LayerMask wallLayers;
    [SerializeField]
    float xSens;
    [SerializeField]
    float ySens;
    [SerializeField]
    float wallCamBiasOffsetMultiplierY;
    [SerializeField]
    float wallDismountFocusPointRecenterRate;

    float currBias;
    float currTime;

    float initialCamSpeedX, initialCamSpeedY;

    float baseYPosition;

    float currWallCamBias;
    float currWallCamBiasX;
    float currWallCamBiasY;
    bool camTouchingWall;
    bool isWallLock;
    bool overrideDisableRecenter;
    bool isInitWallDismountRecenter = false;
    bool isInitialWallAttach = true;
   
    public static bool specialCaseRecenter_quaternionMath = false;

    float dampValXBase, dampValYBase, dampValZBase;
    float collideDampBase, collideDampOccludeBase;
    // Start is called before the first frame update
    void Start()
    {
        initialCamSpeedX = GetComponent<CinemachineFreeLook>().m_XAxis.m_MaxSpeed;
        initialCamSpeedY = GetComponent<CinemachineFreeLook>().m_YAxis.m_MaxSpeed;
        currBias = 0;
        currWallCamBias = 0;
        isFirstMount = true;

        baseYPosition = CamObj.transform.localPosition.y;

        dampValXBase = camControls.GetRig(0).GetCinemachineComponent<CinemachineOrbitalTransposer>().m_XDamping;
        dampValYBase = camControls.GetRig(1).GetCinemachineComponent<CinemachineOrbitalTransposer>().m_YDamping;
        dampValZBase = camControls.GetRig(2).GetCinemachineComponent<CinemachineOrbitalTransposer>().m_ZDamping;

        collideDampBase = camCollider.m_Damping;
        collideDampOccludeBase = camCollider.m_DampingWhenOccluded;
    }

// Update is called once per frame
void Update()
    {
        DetectWalls();
        if(AttachToWall.isRoundWall)
            HandleWallRideFocusPointShift();
        else
            forceWallrideFocusPointShift();
        HandleWallrideBiasOffset();
        if(!overrideDisableRecenter)
            HandleRecenter();
    }

    void DetectWalls()
    {
        Collider[] wallTouches = Physics.OverlapSphere(transform.position, wallCamBiasDetectionRadius, wallLayers);
        bool isHit = Physics.SphereCast(transform.position, wallCamBiasDetectionRadius, Vector3.forward, out RaycastHit hitInfo, 0, wallLayers);
        // only link cam to currently attached wall?
        camTouchingWall = wallTouches.Length > 0 || isHit;
    }

    void forceWallrideFocusPointShift()
    {
        if (AttachToWall.isAttachedToWall)
        {
            camControls.m_XAxis.m_Wrap = false;

            // float rotValIndex = PlayerObj.transform.rotation.eulerAngles.y; // use wall rotation maybe?
            float rotValIndex;
            if (AttachToWall.isLeftWallHit)
                rotValIndex = Quaternion.FromToRotation(Vector3.right, AttachToWall.wallCurrNormal).eulerAngles.y;
            else
                rotValIndex = Quaternion.FromToRotation(Vector3.right * -1, AttachToWall.wallCurrNormal).eulerAngles.y;
            if (isInitialWallAttach)
            {
                // reduce all damping values
                camCollider.m_Damping = 0;
                camCollider.m_DampingWhenOccluded = 0;

                for (var i = 0; i <= 2; i++)
                {
                    camControls.GetRig(i).GetCinemachineComponent<CinemachineOrbitalTransposer>().m_XDamping = 0;
                    camControls.GetRig(i).GetCinemachineComponent<CinemachineOrbitalTransposer>().m_YDamping = 0;
                    camControls.GetRig(i).GetCinemachineComponent<CinemachineOrbitalTransposer>().m_ZDamping = 0;
                }

                camControls.m_XAxis.m_MinValue = rotValIndex - 90;
                camControls.m_XAxis.m_MaxValue = rotValIndex + 90;
                // clamp rot values, quaternion stuff

                 if(camControls.m_XAxis.Value < camControls.m_XAxis.m_MinValue)
                 {
                     camControls.m_XAxis.m_MinValue -= 360;
                     camControls.m_XAxis.m_MaxValue -= 360;
                 }

                if (camControls.m_XAxis.m_MinValue > 180 || camControls.m_XAxis.Value < 0)
                {
                    specialCaseRecenter_quaternionMath = true;     
                    
                }
                else
                {
                    specialCaseRecenter_quaternionMath = false;
                }
                // float currRotationYoffsetFromCenterClamp = PlayerObj.transform.rotation.eulerAngles.y - ;
                // camControls.m_XAxis.Value = //clamp value point between new vals
                // get pos/neg offset of cam axis value from player orientation, add that to center of clamp for wall mount
                isInitialWallAttach = false;

                // get proportional value of x axis between min and max value relative to min and max point of focus clamp
                float camAngleNormalizedX = (camControls.m_XAxis.Value - camControls.m_XAxis.m_MinValue) /
                    (camControls.m_XAxis.m_MaxValue - camControls.m_XAxis.m_MinValue);
                camAngleNormalizedX = Mathf.Clamp(camAngleNormalizedX, 0, 1);

                // index clamp value from negative clamp
                float netFocusPointOffset = (pointOfFocusOffsetClamp * 2) * camAngleNormalizedX;
                currWallCamBiasX = ((pointOfFocusOffsetClamp * -1) + netFocusPointOffset) * wallRideInstantPOFdamper;

               // Debug.Log(camControls.m_XAxis.m_MinValue + " " + camControls.m_XAxis.m_MaxValue + " " + camControls.m_XAxis.Value);
               // Debug.Log($"normalized cam angle: {camAngleNormalizedX}");
               // Debug.Log($"PoF position: {CamObj.transform.localPosition}");

                // initialize Y cam offset values as well here
                float camAngleNormalizedY = camControls.m_YAxis.Value;
                float absoluteFocusPointOffsetY = (pointOfFocusOffsetClamp * 2) * camAngleNormalizedY;
                currWallCamBiasY = ((pointOfFocusOffsetClamp * -1) + absoluteFocusPointOffsetY) * wallRideInstantPOFdamper;
            }

            GetComponent<CinemachineCollider>().m_CollideAgainst = LayerMask.GetMask("ground", "wallride", "wallRideCurved");

            camControls.m_RecenterToTargetHeading.m_enabled = false;

            //float mouseMotionX = Input.GetAxisRaw("Mouse X") * xSens;
            //float mouseMotionY = Input.GetAxisRaw("Mouse Y") * ySens;

            GetComponent<CinemachineFreeLook>().m_XAxis.m_MaxSpeed = initialCamSpeedX * wallRideReducedCamApproachSpeed;
            GetComponent<CinemachineFreeLook>().m_YAxis.m_MaxSpeed = initialCamSpeedY * wallRideReducedCamApproachSpeed;

            // directly map camera direction proportionally to the value on the camera between its clamps
            // (can be modified in intensity with wallRideInstantPOFdamper
            float camAngleNormalizedContX = (camControls.m_XAxis.Value - camControls.m_XAxis.m_MinValue) /
                    (camControls.m_XAxis.m_MaxValue - camControls.m_XAxis.m_MinValue);
            camAngleNormalizedContX = Mathf.Clamp(camAngleNormalizedContX, 0, 1);

            // index clamp value from negative clamp
            float absoluteFocusPointOffsetContX = (pointOfFocusOffsetClamp * 2) * camAngleNormalizedContX;
            currWallCamBiasX = ((pointOfFocusOffsetClamp * -1) + absoluteFocusPointOffsetContX) * wallRideInstantPOFdamper;

            // float additiveXOffset = GetComponent<CinemachineFreeLook>().m_XAxis.m_InputAxisValue * initialCamSpeedX * wallCamBiasOffsetMultiplier;
            // currWallCamBiasX += additiveXOffset;
            // currWallCamBiasX = Mathf.Clamp(currWallCamBiasX, pointOfFocusOffsetClamp * -1, pointOfFocusOffsetClamp);

            // y normalized between 0-1
            float camAngleNormalizedContY = camControls.m_YAxis.Value;
            float absoluteFocusPointOffsetContY = (pointOfFocusOffsetClamp * 2) * camAngleNormalizedContY;
            currWallCamBiasY = ((pointOfFocusOffsetClamp * -1) + absoluteFocusPointOffsetContY) * wallRideInstantPOFdamper * -1;

            // float additiveYOffset = GetComponent<CinemachineFreeLook>().m_YAxis.m_InputAxisValue * initialCamSpeedY * wallCamBiasOffsetMultiplierY;
            // currWallCamBiasY += additiveYOffset;
            // currWallCamBiasY = Mathf.Clamp(currWallCamBiasY, pointOfFocusOffsetClamp * -1, pointOfFocusOffsetClamp);
            
            //CamObj.transform.localPosition = new Vector3(currWallCamBiasX, baseYPosition + currWallCamBiasY, CamObj.transform.localPosition.z);
            Vector3 goalPosition = new Vector3(currWallCamBiasX, baseYPosition + currWallCamBiasY, CamObj.transform.localPosition.z);

            CamObj.transform.localPosition = Vector3.MoveTowards(CamObj.transform.localPosition, goalPosition, wallRideInstantPOFtransferRate);
            if (AttachToWall.isLeftWallHit)
            {
                if(CamObj.transform.localPosition.x < 0)
                {
                    GetComponent<CinemachineCollider>().m_CollideAgainst = LayerMask.GetMask("ground");
                }
            }
            else
            {
                if (CamObj.transform.localPosition.x > 0)
                {
                    GetComponent<CinemachineCollider>().m_CollideAgainst = LayerMask.GetMask("ground");
                }
            }
        }
        else
        {
            isInitialWallAttach = true;
            camControls.m_XAxis.m_Wrap = true;
            camControls.m_XAxis.m_MinValue = -180;
            camControls.m_XAxis.m_MaxValue = 180;

            if(CamObj.transform.localPosition == Vector3.zero) // only renable the layers when pointOfFocus is recentered to prevent clipping
                GetComponent<CinemachineCollider>().m_CollideAgainst = LayerMask.GetMask("ground", "wallride", "wallRideCurved");
            if (DebugLockOn.isBaseLockon)
            {
                GetComponent<CinemachineFreeLook>().m_XAxis.m_MaxSpeed = initialCamSpeedX;
                GetComponent<CinemachineFreeLook>().m_YAxis.m_MaxSpeed = initialCamSpeedY;
            }
            //CamObj.transform.localPosition = Vector3.zero;
            CamObj.transform.localPosition = Vector3.MoveTowards(
                CamObj.transform.localPosition,
                Vector3.zero,
                wallDismountFocusPointRecenterRate * Time.deltaTime);
            isWallLock = false;
            overrideDisableRecenter = false;
            currWallCamBiasX = 0;
            currWallCamBiasY = 0;

            camCollider.m_Damping = collideDampBase;
            camCollider.m_DampingWhenOccluded = collideDampOccludeBase;

            if (DebugLockOn.isBaseLockon)
            {
                for (var i = 0; i <= 2; i++)
                {
                    camControls.GetRig(i).GetCinemachineComponent<CinemachineOrbitalTransposer>().m_XDamping = dampValXBase;
                    camControls.GetRig(i).GetCinemachineComponent<CinemachineOrbitalTransposer>().m_YDamping = dampValYBase;
                    camControls.GetRig(i).GetCinemachineComponent<CinemachineOrbitalTransposer>().m_ZDamping = dampValZBase;
                }
            }
        }
    }

    // another solution:
    // make camera lerp to its standing position when we get on a wall, immediately replace camera lateral with mouse turning movement

    // also set this to ONLY map to the wall we are currently on

    void HandleWallRideFocusPointShift()
    {
        // if cam is touching collider of wall
        // if wall right, map mouse movement to x-pos turn left
        // if wall left, map mouse movement to x-pos turn right
        // keep internal offset that returns cam lateral control when == 0
        if (AttachToWall.isAttachedToWall)
        {
            if (AttachToWall.isLeftWallHit && (camTouchingWall || isWallLock) && currWallCamBias >= 0)
            {
                overrideDisableRecenter = true;
                camControls.m_RecenterToTargetHeading.m_enabled = false;
                isWallLock = true;
                //Debug.Log($"{GetComponent<CinemachineFreeLook>().m_XAxis.m_InputAxisValue} {initialCamSpeedX}{}");
                GetComponent<CinemachineFreeLook>().m_XAxis.m_MaxSpeed = wallRideReducedCamApproachSpeed;
                float additiveXOffset = GetComponent<CinemachineFreeLook>().m_XAxis.m_InputAxisValue * initialCamSpeedX * wallCamBiasOffsetMultiplier;
                currWallCamBias += additiveXOffset;
                currWallCamBias = Mathf.Clamp(currWallCamBias, pointOfFocusOffsetClamp * -1, pointOfFocusOffsetClamp);
                CamObj.transform.localPosition = new Vector3(currWallCamBias, CamObj.transform.localPosition.y, CamObj.transform.localPosition.z);

                if (currWallCamBias < 0)
                {
                    currWallCamBias = -.01f;
                    isWallLock = false;
                }
                // if wall hit && currWallCamBias > 0?
            }
            else if (AttachToWall.isLeftWallHit && currWallCamBias == -.01f && !isWallLock)
            {
                currWallCamBias = 0;
                GetComponent<CinemachineFreeLook>().m_XAxis.m_MaxSpeed = initialCamSpeedX;
                CamObj.transform.localPosition = Vector3.zero;
                overrideDisableRecenter = false;
            }
            if (AttachToWall.isRightWallHit && (camTouchingWall || isWallLock) && currWallCamBias <= 0)
            {
                overrideDisableRecenter = true;
                camControls.m_RecenterToTargetHeading.m_enabled = false;
                isWallLock = true;
                GetComponent<CinemachineFreeLook>().m_XAxis.m_MaxSpeed = wallRideReducedCamApproachSpeed;
                float additiveXOffset = GetComponent<CinemachineFreeLook>().m_XAxis.m_InputAxisValue * initialCamSpeedX * wallCamBiasOffsetMultiplier;
                currWallCamBias += additiveXOffset;
                currWallCamBias = Mathf.Clamp(currWallCamBias, pointOfFocusOffsetClamp * -1, pointOfFocusOffsetClamp);
                CamObj.transform.localPosition = new Vector3(currWallCamBias, CamObj.transform.localPosition.y, CamObj.transform.localPosition.z);

                if (currWallCamBias > 0)
                {
                    currWallCamBias = .01f;
                    isWallLock = false;
                }
            }
            else if (AttachToWall.isRightWallHit && currWallCamBias == .01f && !isWallLock)
            {
                currWallCamBias = 0;
                GetComponent<CinemachineFreeLook>().m_XAxis.m_MaxSpeed = initialCamSpeedX;
                CamObj.transform.localPosition = Vector3.zero;
                overrideDisableRecenter = false;
            }
        }
        else
        {
            if(DebugLockOn.isBaseLockon)
                GetComponent<CinemachineFreeLook>().m_XAxis.m_MaxSpeed = initialCamSpeedX;
            CamObj.transform.localPosition = Vector3.zero;
            isWallLock = false;
            overrideDisableRecenter = false;
            currWallCamBias = 0;
        }
    }

    void HandleWallrideBiasOffset()
    {
        if (AttachToWall.isAttachedToWall)
        {
            currBias = (AttachToWall.isRightWallHit) ? currBias + camOffsetBiasWallRideChangeRate * Time.deltaTime : currBias - camOffsetBiasWallRideChangeRate * Time.deltaTime;
            currBias = Mathf.Clamp(currBias, camOffsetBiasWallRide * -1, camOffsetBiasWallRide);
            camControls.m_Heading.m_Bias = currBias;
        }
        else
        {
            if(camControls.m_Heading.m_Bias < 0)
            {
                currBias = currBias + camOffsetBiasWallRideChangeRate * Time.deltaTime;
                if (currBias > 0)
                    currBias = 0;

            }
            else if(camControls.m_Heading.m_Bias > 0)
            {
                currBias = currBias - camOffsetBiasWallRideChangeRate * Time.deltaTime;
                if (currBias < 0)
                    currBias = 0;
            }
            camControls.m_Heading.m_Bias = currBias;
        }
    }

    // simplify cases? just make tracking toggleable? on initial mount, it is enabled
    void HandleRecenter()
    {
  /*      if (!DebugLockOn.isBaseLockon)
        {
            camControls.m_RecenterToTargetHeading.m_enabled = false;
            camControls.m_YAxisRecentering.m_enabled = false;
            return;
        }*/

        SetCamInstantResetFromRotMagnitude();
        if (isFirstMount && (AttachToRail.isAttachedToRail || AttachToWall.isAttachedToWall || RailDetect.isOnSmoothRail))
        {
            currTime = Time.time + initialMountThreshold;
        }

        if (currTime > Time.time && (AttachToRail.isAttachedToRail || AttachToWall.isAttachedToWall || RailDetect.isOnSmoothRail))
        {
            //Debug.Log("Reset first mount");
            SmoothRailGrinding.handleCamYOffset = true;
            camControls.m_RecenterToTargetHeading.m_enabled = true;
            camControls.m_RecenterToTargetHeading.m_WaitTime = 0;
            camControls.m_YAxisRecentering.m_WaitTime = 0;
            camControls.m_RecenterToTargetHeading.m_RecenteringTime =
                (AttachToRail.isAttachedToRail || RailDetect.isOnSmoothRail) ? railInstantRecenterRate : wallRideInstantRecenterRate;
            isFirstMount = false;
            isUpdateMount = true;
            if(AttachToWall.isAttachedToWall && !AttachToWall.isRoundWall)
                camControls.m_YAxisRecentering.m_enabled = true;
            else
                camControls.m_YAxisRecentering.m_enabled = false;
        }
        else if (Input.GetButton("CamRecenter"))
        {
            //Debug.Log("forcing recenter");
            if (AttachToWall.isAttachedToWall)
            {
                camControls.m_RecenterToTargetHeading.m_WaitTime = 0;
                camControls.m_RecenterToTargetHeading.m_RecenteringTime = wallRideInstantRecenterRate;
                camControls.m_RecenterToTargetHeading.m_enabled = true;
                camControls.m_YAxisRecentering.m_enabled = true;
            }
            else if (AttachToRail.isAttachedToRail || RailDetect.isOnSmoothRail)
            {
                camControls.m_RecenterToTargetHeading.m_WaitTime = 0;
                camControls.m_RecenterToTargetHeading.m_RecenteringTime = railInstantRecenterRate;
                camControls.m_RecenterToTargetHeading.m_enabled = true;
                SmoothRailGrinding.handleCamYOffset = true;
                camControls.m_YAxisRecentering.m_enabled = false;
            }
            else // base reset case, have own vars?
            {
                camControls.m_RecenterToTargetHeading.m_WaitTime = 0;
                camControls.m_RecenterToTargetHeading.m_RecenteringTime = railInstantRecenterRate;
                camControls.m_RecenterToTargetHeading.m_enabled = true;
                SmoothRailGrinding.handleCamYOffset = true;
                camControls.m_YAxisRecentering.m_enabled = false;
            }
        } // only disable if we move camera       
        else if (AttachToWall.isAttachedToWall && isUpdateMount)
        {
            Debug.Log("wall mount update");
            camControls.m_RecenterToTargetHeading.m_WaitTime = 0;
            camControls.m_RecenterToTargetHeading.m_RecenteringTime = wallRideInstantRecenterRate;
            camControls.m_RecenterToTargetHeading.m_enabled = true;
            isUpdateMount = false;
            if(!AttachToWall.isRoundWall)
                camControls.m_YAxisRecentering.m_enabled = true;
        }
        else if ((AttachToRail.isAttachedToRail || RailDetect.isOnSmoothRail) && isUpdateMount)
        {
            //Debug.Log("rail mount update");
            camControls.m_RecenterToTargetHeading.m_WaitTime = baseRailWaitTime;
            camControls.m_RecenterToTargetHeading.m_RecenteringTime = railRecenterRate;
            camControls.m_RecenterToTargetHeading.m_enabled = true;
            SmoothRailGrinding.handleCamYOffset = true;
            isUpdateMount = false;
            camControls.m_YAxisRecentering.m_enabled = false;
        }

        // reset init mount if no attach
        if(!AttachToWall.isAttachedToWall && !AttachToRail.isAttachedToRail && !RailDetect.isOnSmoothRail)
        {
            //Debug.Log("full reset and disable");
            isFirstMount = true;
            isUpdateMount = true;
            camControls.m_RecenterToTargetHeading.m_enabled = Input.GetButton("CamRecenter");
            SmoothRailGrinding.handleCamYOffset = Input.GetButton("CamRecenter");
            camControls.m_YAxisRecentering.m_enabled = false;
        }
        if((AttachToWall.isAttachedToWall || AttachToRail.isAttachedToRail || RailDetect.isOnSmoothRail) && !Input.GetButton("CamRecenter") && currTime < Time.time)
        {
            camControls.m_RecenterToTargetHeading.m_WaitTime = (AttachToRail.isAttachedToRail || RailDetect.isOnSmoothRail) ? baseRailWaitTime : baseWallWaitTime;
            camControls.m_RecenterToTargetHeading.m_RecenteringTime = (AttachToRail.isAttachedToRail || RailDetect.isOnSmoothRail) ? railRecenterRate : wallRideRecenterRate;
            camControls.m_YAxisRecentering.m_WaitTime = (AttachToRail.isAttachedToRail || RailDetect.isOnSmoothRail) ? baseRailWaitTime : baseWallWaitTime;

            camControls.m_RecenterToTargetHeading.m_enabled = true;
            SmoothRailGrinding.handleCamYOffset = true;
        }
        // disable recenter if we have a stick/mouse input past threshold
        if (Mathf.Abs(camControls.m_YAxis.m_InputAxisValue) + Mathf.Abs(camControls.m_XAxis.m_InputAxisValue) > InstantRecenterBreakThreshold && 
            !Input.GetButton("CamRecenter") &&
            (!AttachToWall.isAttachedToWall && !AttachToWall.isRoundWall)) // always recenter on walls
        {
            camControls.m_RecenterToTargetHeading.m_enabled = false;          
            SmoothRailGrinding.handleCamYOffset = false;
        }
       /* if (AttachToWall.isAttachedToWall && !AttachToWall.isRoundWall) // override rule
        {
            camControls.m_RecenterToTargetHeading.m_WaitTime = 0;
            // these two lines below are disabled while kinks of wallride camera are worked out
            camControls.m_YAxisRecentering.m_enabled = false;
            camControls.m_RecenterToTargetHeading.m_enabled = false;
        }*/
    }

    void SetCamInstantResetFromRotMagnitude()
    {
        if(Mathf.Abs(camControls.m_YAxis.m_InputAxisValue) + Mathf.Abs(camControls.m_XAxis.m_InputAxisValue) > InstantRecenterBreakThreshold)
        {
            currTime = 0;
        }
    }
}
