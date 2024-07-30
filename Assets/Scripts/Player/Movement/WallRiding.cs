using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallRiding : PlayerMovementEffector // change this class in inherit basemovement?
{
    [SerializeField]
    private float wallrunMaxSpeed;
    [SerializeField]
    private float wallRunHeightChangeSpeed;
    [SerializeField]
    private float wallJumpDismountForce;
    [SerializeField]
    private float wallJumpVerticalForce;
    [SerializeField]
    private float minSpeedUntilWallDismount;
    [SerializeField]
    private float wallRunAccelerationRate;
    [SerializeField]
    private float controllerDeadzoneThreshold;
    [SerializeField]
    float gfxWallRideOffset;
    [SerializeField]
    float camOffsetStartTimeThreshold = .1f;
    [SerializeField]
    CinemachineFreeLook camRotTracker;

    float currTime = 0;
    float currMagnitude = 0;
    bool JumpInputted;
    int currJumpGraceFrame = 0;
    int jumpInputGraceFrames = 3;

    float prevX, currX;
    bool handleCamRot = false;

    private void handleCurvedWallCameraRotation()
    {
        camRotTracker.m_XAxis.Value += (currX - prevX);
        //camRotTracker.m_XAxis.Value = 0;
    }

    public void handleWallRunMovement(ref PlayerMovementContext moveContext)
    {
        // translate matrix momentum upon initial attach
        if (AttachToWall.isInitAttach)
        {
            currTime = Time.time + camOffsetStartTimeThreshold;
            PlayerMovementContext.isWallRunning = true;
            currMagnitude = moveContext.currAccelMatrix.magnitude;
            moveContext.currAccelMatrix = new Vector3(moveContext.currAccelMatrix.x, 0, moveContext.currAccelMatrix.z);
            moveContext.playerVerticalVelocity = Vector3.zero;
            AttachToWall.isInitAttach = false;

            // offset for wallrun
            if(AttachToWall.isRightWallHit)
                gfx.transform.localPosition = new Vector3(gfx.transform.localPosition.x - gfxWallRideOffset, gfx.transform.localPosition.y, gfx.transform.localPosition.z);
            else
                gfx.transform.localPosition = new Vector3(gfx.transform.localPosition.x + gfxWallRideOffset, gfx.transform.localPosition.y, gfx.transform.localPosition.z);
        }
        handlemagnitudeChanges();
        //moveContext.currAccelMatrix = new Vector3 (moveContext.currAccelMatrix.x, 0, moveContext.currAccelMatrix.z);
        Vector3 wallNormal = AttachToWall.wallCurrNormal;
        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        prevX = controller.transform.rotation.eulerAngles.y;

        if (AttachToWall.isRightWallHit)
        {          
            wallForward *= -1;
            controller.transform.rotation = Quaternion.FromToRotation(Vector3.right * -1, wallNormal);
            currX = Quaternion.FromToRotation(Vector3.right * -1, wallNormal).eulerAngles.y;
            handleCamRot = camRotTracker.m_XAxis.m_InputAxisValue >= 0;
        }
        else
        {
            controller.transform.rotation = Quaternion.FromToRotation(Vector3.right, wallNormal);
            currX = Quaternion.FromToRotation(Vector3.right, wallNormal).eulerAngles.y;
            handleCamRot = camRotTracker.m_XAxis.m_InputAxisValue <= 0;
        }
        moveContext.currAccelMatrix = wallForward * currMagnitude;
        moveContext.currAccelMatrix += (-wallNormal);

        if(Time.time > currTime && handleCamRot)
            handleCurvedWallCameraRotation();
    }

    public void handleWallRideJumpDismount(ref PlayerMovementContext moveContext)
    {
        if (!JumpInputted)
            return;
        Debug.Log("Launch Attempt");
        AttachToWall.isNewDetach = true;
        Vector3 wallLaunchOff = Vector3.zero;
        Vector3 wallNormal = AttachToWall.wallCurrNormal;

        if (AttachToWall.isRightWallHit)
        {
            wallLaunchOff = Vector3.Cross(moveContext.currAccelMatrix.normalized, transform.up);
        }
        else // find how to fix left wall
        {
            wallLaunchOff = Vector3.Cross(moveContext.currAccelMatrix.normalized, transform.up) * -1;
        }
        // figure out why this is updated instantly, follow flow (debug from the below line)
        moveContext.currAccelMatrix = wallLaunchOff * wallJumpDismountForce + moveContext.currAccelMatrix;
        moveContext.playerVerticalVelocity.y += wallJumpVerticalForce;

        // reset player position on dismount
        gfx.transform.localPosition = new Vector3(0, gfx.transform.localPosition.y, gfx.transform.localPosition.z);

        AttachToWall.isInTransition = true;
    }

    public void resetLocalCharPosition()
    {
        gfx.transform.localPosition = new Vector3(0, gfx.transform.localPosition.y, gfx.transform.localPosition.z);
    }

    public void CheckIfJump()
    {
        if (Input.GetButtonDown("Jump"))
        {
            JumpInputted = true;
        } // sometimes input is overlooked. allow a brief grace period to compensate
        else if (currJumpGraceFrame > jumpInputGraceFrames && JumpInputted)
        {
            currJumpGraceFrame = 0;
            JumpInputted = false;
        }
        else if (JumpInputted)
        {
            currJumpGraceFrame++;
        }
    }

    private void handlemagnitudeChanges()
    {
        float horizontal = Input.GetAxisRaw("Horizontal"); // use for traversing up and down wall
        float vertical = Input.GetAxisRaw("Vertical");

        currMagnitude += (vertical * wallRunAccelerationRate * Time.fixedDeltaTime);
        currMagnitude = Mathf.Clamp(currMagnitude, -wallrunMaxSpeed, wallrunMaxSpeed);
    }
}
