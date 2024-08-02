using BezierSolution;
using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class SmoothRailGrinding : PlayerMovementEffector
{
    [SerializeField]
    float maxGrindSpeed;
    [SerializeField]
    float maxOverDriveGrindSpeed;
    [SerializeField]
    float grindSpeedChangeRate;
    [SerializeField]
    float grindOverDriveSpeedChangeRate;
    [SerializeField]
    float dismountAllowedTimeLockout = .1f;
    [SerializeField]
    RailDetect railDetect;
    [SerializeField]
    BezierWalkerWithSpeed bezierWalker;
    [SerializeField]
    CinemachineFreeLook camRotTracker;
    [SerializeField]
    private float zeroPoint = 270;
    [SerializeField]
    private float splineCenterPoint = .5f;
    [SerializeField]
    private float splineVerticalRecenterRate = 1f;
    [SerializeField]
    private float recenterDelay = 2f;    

    public static bool dismountJumpVelocityTransition;
    public static bool dismountVelocityTransition;
    public static bool isDismountTransition;
    public static bool handleCamYOffset;

    float currRecenterDelay = 0;
    float currTime = 0;
    float currLockoutTime = 0;
    bool firstRun;
    bool jumpInputted = false;
    float currY, prevY;
    float currVelocity = 0;
    bool isLerpingToCamTarget = true;
    float currJumpGraceFrame = 0;
    float jumpInputGraceFrames = 3;
    float prevCurrentVelocity;
    bool isAccelerating;
    // improve this mess
    void HandleSmoothRailYOffset()
    {
        if (firstRun)
        {
            currTime = Time.time + currRecenterDelay;
            firstRun = false;
        }

        if (Mathf.Abs(camRotTracker.m_YAxis.m_InputAxisValue) + Mathf.Abs(camRotTracker.m_XAxis.m_InputAxisValue) > 0)
            currTime = Time.time + 1;

        if (RailDetect.isOnSmoothRail && Time.time >= currTime)
        {
            // create a lerp time to go to the current axis so we dont just snap
            // also create a delay

            // Debug.Log($"{currY / 360}");
            if (isLerpingToCamTarget)
            {
                if(camRotTracker.m_YAxis.Value > splineCenterPoint)
                {
                    camRotTracker.m_YAxis.Value -= (splineVerticalRecenterRate * Time.fixedDeltaTime);
                    if (camRotTracker.m_YAxis.Value < splineCenterPoint)
                    {
                        camRotTracker.m_YAxis.Value = splineCenterPoint;
                        isLerpingToCamTarget = false;
                    }
                }
                else if(camRotTracker.m_YAxis.Value < splineCenterPoint)
                {
                    camRotTracker.m_YAxis.Value += (splineVerticalRecenterRate * Time.fixedDeltaTime);
                    if (camRotTracker.m_YAxis.Value > splineCenterPoint)
                    {
                        camRotTracker.m_YAxis.Value = splineCenterPoint;
                        isLerpingToCamTarget = false;
                    }
                }
            }
            else
            {
                float standardizedRot = currY / 360;
                if (standardizedRot > .5f)
                {
                    if (currVelocity < 0)
                    {
                        camRotTracker.m_YAxis.Value = splineCenterPoint + (1 - standardizedRot);
                        //Debug.Log("velocity NEGATIVE rot > .5");
                    }
                    else
                    {
                        camRotTracker.m_YAxis.Value = splineCenterPoint - (1 - standardizedRot);
                        //Debug.Log("velocity POSITIVE rot > .5");
                    }
                }
                else
                {
                    if (currVelocity < 0)
                    {
                        camRotTracker.m_YAxis.Value = splineCenterPoint - standardizedRot;
                        //Debug.Log("velocity NEGATIVE rot < .5");
                    }
                    else
                    {
                        camRotTracker.m_YAxis.Value = splineCenterPoint + standardizedRot;
                        //Debug.Log("velocity POSITIVE rot < .5");
                    }
                }
            }
        }
        else if(!handleCamYOffset)
        {
            isLerpingToCamTarget = true;
            firstRun = true;
        }
    }

    public void CheckIfJump()
    {
        if(Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Jump"))
        {
            jumpInputted = true;
        }
        else if (currJumpGraceFrame > jumpInputGraceFrames && jumpInputted)
        {
            currJumpGraceFrame = 0;
            jumpInputted = false;
        }
        else if (jumpInputted)
        {
            currJumpGraceFrame++;
        }
    }

    public void HandleRailSmoothMovement(ref PlayerMovementContext moveContext)
    {
        float normalT, prevNormalT;
        if (RailDetect.isInitialMount)
        {
            currRecenterDelay = 0;
            cam.GetComponent<CinemachineBrain>().m_UpdateMethod = CinemachineBrain.UpdateMethod.SmartUpdate;
            cam.GetComponent<CinemachineBrain>().m_BlendUpdateMethod = CinemachineBrain.BrainUpdateMethod.LateUpdate;
            // set normalizedT to be equal to our location on spline
            currVelocity = moveContext.currAccelMatrix.magnitude; // only update on initial impact
            RailDetect.isInitialMount = false;
            bezierWalker.enabled = true;
            
            bezierWalker.spline = RailDetect.currSplineRail;
            
            bezierWalker.spline.FindNearestPointTo(transform.position, out normalT); // check prev turn normal?
            //bezierWalker.spline.FindNearestPointTo(moveContext.prevFramePos, out prevNormalT); // check prev turn normal?
            bezierWalker.spline.FindNearestPointTo(moveContext.positionHistory.Peek(), out prevNormalT); // check prev turn normal?
            Vector3 mountingNormal = bezierWalker.spline.GetNormal(normalT);

            bezierWalker.NormalizedT = normalT;
            if (prevNormalT > normalT)
                currVelocity *= -1;
            Debug.Log("init mount rail spline");
            currLockoutTime = dismountAllowedTimeLockout + Time.time;

            bezierWalker.travelMode = bezierWalker.spline.loop ? TravelMode.Loop : TravelMode.Once;
        }

        if (Mathf.Abs(camRotTracker.m_YAxis.m_InputAxisValue) + Mathf.Abs(camRotTracker.m_XAxis.m_InputAxisValue) > 0)
        {
            currRecenterDelay = recenterDelay;
        }

        bezierWalker.speed = currVelocity;
        moveContext.railVelocity = currVelocity;

        if (jumpInputted && Time.time > currLockoutTime)
        {
            cam.GetComponent<CinemachineBrain>().m_UpdateMethod = CinemachineBrain.UpdateMethod.FixedUpdate;
            cam.GetComponent<CinemachineBrain>().m_BlendUpdateMethod = CinemachineBrain.BrainUpdateMethod.FixedUpdate;

            bezierWalker.enabled = false;
            RailDetect.isOnSmoothRail = false;
            railDetect.setReattachLockout();
            moveContext.currAccelMatrix = transform.forward * Mathf.Abs(currVelocity);
            //moveContext.playerVerticalVelocity = new Vector3(0, 25, 0);

            dismountJumpVelocityTransition = true;
            isDismountTransition = true;
        }

        // make the dismount dynamic, both as a variable, and mapped to consider length in the function
        //Debug.Log(bezierWalker.spline.length);
        // if less than certain length, use NormalizedT, if greater than, use length begin and end offset
        // use:
        // if bezierWalker.NormalizedT > 50
        //  (1 - bezierWalker.NormalizedT) * bezierWalker.spline.length < Dismount threshold

        // goal is to reduce fixed timestep (and take some stuff out of it!!)
        if (!bezierWalker.spline.loop && (bezierWalker.NormalizedT == 1 || bezierWalker.NormalizedT == 0))
        {
            cam.GetComponent<CinemachineBrain>().m_UpdateMethod = CinemachineBrain.UpdateMethod.FixedUpdate;
            cam.GetComponent<CinemachineBrain>().m_BlendUpdateMethod = CinemachineBrain.BrainUpdateMethod.FixedUpdate;

            bezierWalker.enabled = false;
            RailDetect.isOnSmoothRail = false;
            moveContext.currAccelMatrix = transform.forward * Mathf.Abs(currVelocity);
            dismountVelocityTransition = true;
            isDismountTransition = true;
            railDetect.setReattachLockout();
        }

        (float, float, bool) offsetVals;

        BezierSpline.Segment segment = bezierWalker.spline.GetSegmentAt(bezierWalker.NormalizedT);

        float currAngle = Quaternion.LookRotation(segment.GetTangent(), segment.GetNormal()).eulerAngles.y;
        currY = Quaternion.LookRotation(segment.GetTangent(), segment.GetNormal()).eulerAngles.x;

        offsetVals = FindZeroPointOffsetBoundariesv2(currAngle);

        float inputDirection = Input.GetAxisRaw("Vertical");

        float accelerationRate = getCurrentSpeedBracket();

        if (Input.GetKey(KeyCode.W) || inputDirection > 0)
        {
            if (!offsetVals.Item3)
            {
                currVelocity = (offsetVals.Item1 >= cam.rotation.eulerAngles.y &&
                        cam.rotation.eulerAngles.y >= offsetVals.Item2) ?
                        currVelocity + accelerationRate * Time.fixedDeltaTime * inputDirection :
                        currVelocity - accelerationRate * Time.fixedDeltaTime * inputDirection;
            }
            else
            {
                currVelocity = (offsetVals.Item1 <= cam.rotation.eulerAngles.y ||
                        cam.rotation.eulerAngles.y <= offsetVals.Item2) ?
                        currVelocity + accelerationRate * Time.fixedDeltaTime * inputDirection :
                        currVelocity - accelerationRate * Time.fixedDeltaTime * inputDirection;
            }
        }
        if (Input.GetKey(KeyCode.S) || inputDirection < 0)
        {
            if (!offsetVals.Item3)
            {
                currVelocity = (offsetVals.Item1 >= cam.rotation.eulerAngles.y &&
                        cam.rotation.eulerAngles.y >= offsetVals.Item2) ?
                        currVelocity - accelerationRate * Time.fixedDeltaTime * Mathf.Abs(inputDirection) :
                        currVelocity + accelerationRate * Time.fixedDeltaTime * Mathf.Abs(inputDirection);
            }
            else
            {
                currVelocity = (offsetVals.Item1 <= cam.rotation.eulerAngles.y ||
                        cam.rotation.eulerAngles.y <= offsetVals.Item2) ?
                        currVelocity - accelerationRate * Time.fixedDeltaTime * Mathf.Abs(inputDirection) :
                        currVelocity + accelerationRate * Time.fixedDeltaTime * Mathf.Abs(inputDirection);
            }
        }
        

        currVelocity = Mathf.Clamp(currVelocity, -maxOverDriveGrindSpeed, maxOverDriveGrindSpeed);
        isAccelerating = Mathf.Abs(prevCurrentVelocity) < Mathf.Abs(currVelocity);
        prevCurrentVelocity = currVelocity;
        HandleSmoothRailYOffset();
        // handle camera recenter and zero-points (camera knows what direction forward is)
        // set appropriate velocity change rates when dismounting
        // check normals per update to see our expected forward orientation to make camera based forward movement
    }



    (float, float, bool) FindZeroPointOffsetBoundariesv2(float railAngle)
    {
        //Debug.Log(cam.eulerAngles.y);
        bool angleInversion = false;

        if(railAngle >= 270 && railAngle <= 359)
        {
            angleInversion = true;
        }

        bool outerBound = false;
        // pass player rotation (y-axis)
        float zeroPointOffset = railAngle;
        // get positive (+90) offset rotation
        float zeroPointOffsetUpperBounds = zeroPointOffset + ((!angleInversion)  ? 90 : -90);
        // if adding positive rotation supercedes 360, we look back around to 0
        // still inner bound, lower rotation is just the higher value now
        if (zeroPointOffsetUpperBounds >= 360)
        {
            zeroPointOffsetUpperBounds = zeroPointOffsetUpperBounds - 360;
        }

        // get negative (-90) offset rotation
        float zeroPointOffsetLowerBounds = zeroPointOffset - ((!angleInversion) ? 90 : -90);
        // if lower rotation is below 0, wrap around to positive
        // becomes outer bound rotation since we are checking boundaries greater than lower, and lesser than higher
        if (zeroPointOffsetLowerBounds < 0)
        {
            zeroPointOffsetLowerBounds = 360 + zeroPointOffsetLowerBounds;
            outerBound = true;
        }
        if (zeroPointOffsetLowerBounds >= 360)
        {
            zeroPointOffsetLowerBounds = zeroPointOffsetLowerBounds - 360;
            outerBound = true;
        }
        // return the highest and lowest of each bound check
        return (Mathf.Max(zeroPointOffsetLowerBounds, zeroPointOffsetUpperBounds), Mathf.Min(zeroPointOffsetLowerBounds, zeroPointOffsetUpperBounds), outerBound);
    }

    float getCurrentSpeedBracket()
    {
        // if we're accelerating and past our normal threshold
        if(isAccelerating && Mathf.Abs(currVelocity) >= maxGrindSpeed)
        {
            return grindOverDriveSpeedChangeRate;
        }
        return grindSpeedChangeRate;
    }
}
