using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoubleJump : PlayerMovementEffector
{
    [SerializeField]
    float doubleJumpPower = 15;
    [SerializeField]
    float doubleJumpFromSingleJumpLockoutTimer = .1f;

    private int jumpInputGraceFrames = 10;
    private int currJumpGraceFrame = 0;
    bool doubleJumpInputted;

    bool allowDoubleJump = true;
    bool doubleJumpExpended = false;

    bool prevJumpState = false;
    // Start is called before the first frame update
    void Start()
    {
       
    }

    // Update is called once per frame
    public void checkIfDoubleJump(ref PlayerMovementContext moveContext)
    {
        if (Input.GetButtonDown("Jump"))
        {
            doubleJumpInputted = true;
        } // sometimes input is overlooked. allow a brief grace period to compensate
        else if (currJumpGraceFrame > jumpInputGraceFrames && doubleJumpInputted)
        {
            currJumpGraceFrame = 0;
            doubleJumpInputted = false;
        }
        else if (doubleJumpInputted)
        {
            currJumpGraceFrame++;
        }
    }

    void jumpLockoutReferenceCheck(ref PlayerMovementContext moveContext)
    {
        if (!prevJumpState && moveContext.isJumping)
            allowDoubleJump = false;
        else if(!doubleJumpExpended)
            allowDoubleJump = true;

    }

    public void HandleDoubleJump(ref PlayerMovementContext moveContext)
    {
        if(doubleJumpExpended)
            jumpLockoutReferenceCheck(ref moveContext);
        if (doubleJumpInputted && !moveContext.isGrounded && allowDoubleJump && (moveContext.isFalling || moveContext.isJumping) &&
            !RailDetect.isOnSmoothRail && !AttachToRail.isAttachedToRail && !AttachToWall.isAttachedToWall)
        {
            Debug.Log("Double Jumped");
            moveContext.playerVerticalVelocity.y = doubleJumpPower;
            moveContext.isDoubleJumping = true;
            allowDoubleJump = false;
            doubleJumpInputted = false;
            doubleJumpExpended = true;
        }

        if(moveContext.isGrounded || RailDetect.isOnSmoothRail || AttachToRail.isAttachedToRail || AttachToWall.isAttachedToWall)
        {
            doubleJumpExpended = false;
            moveContext.isDoubleJumping = false;
        }

        allowDoubleJump = !doubleJumpExpended;
        prevJumpState = moveContext.isJumping;
        // catch hook from update function and set false
        if (doubleJumpInputted)
            doubleJumpInputted = false;
    }


}
