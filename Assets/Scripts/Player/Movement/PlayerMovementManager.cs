using BezierSolution;
using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

/* TRICK SYSTEM 
    - While in the air, upon holding LB (on controller) or some key on keyboard
    - toggle the button controls (A,B,X,Y for xbox) to be for tricks instead
    - tricks will be auto performed with input, but if they aren't completed in time 
    - (i.e.) a front flip isn't completed before a rail or the ground is touched, 
    - it breaks a combo

    - When holding LB, after the first input, there is a short period of time where another input
    - is awaited, if none is detected, the player will perform the trick inputted 
    - if LB is released, the input grace period is removed, and the inputted trick is performed immediately
 */

public class PlayerMovementManager : MonoBehaviour
{
    [SerializeField]
    CharacterController controller;

    [SerializeField]
    BaseMovement baseMovement;

    [SerializeField]
    RailGrinding railGrinding;

    [SerializeField]
    SmoothRailGrinding smoothRailGrinding;

    [SerializeField]
    WallRiding wallRiding;

    [SerializeField]
    PlayerDash playerDash;

    [SerializeField]
    PlayerSlide playerSlide;

    [SerializeField]
    DoubleJump doubleJump;

    [SerializeField]
    Animator playerAnim;
    [SerializeField]
    float doubleJumpAnimDurationOffset;

    [SerializeField]
    float fastThreshold;
    [SerializeField] 
    float extendedIdleTime;
    float currIdleTime;

    [SerializeField]
    VisualEffect currVisualEffect; // map this out into a manager (we will have multiple effects)
    [SerializeField]
    VisualEffect railGrindVisualEffect;
    // shader rimlight variables AND vfx graph vars
    [SerializeField]
    float maxIntensity = 1;
    [SerializeField]
    float minIntensity = .5f;
    [SerializeField]
    float oscillationRate = 20f;
    bool isAscending = false;

    bool isInitVfx;
    bool isInitRailGrindVfx;
    [SerializeField]
    Vector3 baseXYZrailgrindVelocityA = new Vector3(-5, -10, -15);
    [SerializeField]
    Vector3 baseXYZrailgrindVelocityB = new Vector3(5, 5, 2);
    Vector3 currXYZrailgrindVelocityA;
    Vector3 currXYZrailgrindVelocityB;
    float currAnimTime = 0;
    bool animResetProcced = false;

    public Renderer render;
    Material mat;
    Material glowMat; // purple glow mat

    public GameObject currRail;

    PlayerMovementContext moveContext;

    bool debugMagnitude;

    float currHistoryFrame = 0; // one time iterator to (playerContext framePos history size)
    // Start is called before the first frame update
    void Start()
    {
        railGrindVisualEffect.resetSeedOnPlay = true;
        currVisualEffect.resetSeedOnPlay = true;
        isInitVfx = true;
        isInitRailGrindVfx = true;
        mat = render.material;
        glowMat = render.materials[1];
        mat.SetFloat("_RimLight", 0);
        mat.SetFloat("_RimLight_Power", .8f);
        glowMat.SetFloat("_Alpha", 0);


        moveContext = new PlayerMovementContext();
        Cursor.lockState = CursorLockMode.Locked;
        Application.targetFrameRate = 240;
    }

    // Update is called once per frame
    void Update()
    {
        // handle single-time inputs in update
        baseMovement.CheckIfToggleSprint(ref moveContext);
        baseMovement.CheckIfJump();
        doubleJump.checkIfDoubleJump(ref moveContext);
        playerSlide.checkIfSlide();
        wallRiding.CheckIfJump(); // distinct from base movement cause its handled so differently
        smoothRailGrinding.CheckIfJump();
        HandleMovementBasedAnimations();
        HandleTempSensitivityChange();
    }

    void HandleTempSensitivityChange()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            GameObject.Find("ThirdPersonCamera").GetComponent<CinemachineFreeLook>().m_XAxis.m_MaxSpeed *= 1.4f;
            GameObject.Find("ThirdPersonCamera").GetComponent<CinemachineFreeLook>().m_YAxis.m_MaxSpeed *= 1.4f;
        }
        if (Input.GetKeyDown(KeyCode.N))
        {
            GameObject.Find("ThirdPersonCamera").GetComponent<CinemachineFreeLook>().m_XAxis.m_MaxSpeed /= 1.4f;
            GameObject.Find("ThirdPersonCamera").GetComponent<CinemachineFreeLook>().m_YAxis.m_MaxSpeed /= 1.4f;
        }
    }


    // reconsider what we have in fixedUpdate and update
    void FixedUpdate()
    {
        handleEarlyFixedUpdate();

        // WHEN ON SPLINE RAIL
        // convert camera to smartupdate/lateupdate for update and blend method
        // FOR EVERYTHING ELSE
        // switch to fixed update

        // make a switch to discern what our movement type is, and just use that
        // convert rail/wall attaches to be in move context, make static?
        Vector3 currentDashPower = playerDash.HandlePlayerDash(ref moveContext);
        doubleJump.HandleDoubleJump(ref moveContext);
        playerSlide.handleSlide(ref moveContext);

        if (RailDetect.isOnSmoothRail)
        {
            smoothRailGrinding.HandleRailSmoothMovement(ref moveContext);
            if (SmoothRailGrinding.dismountJumpVelocityTransition)
            {
                baseMovement.handleSmoothRailJumpcase(ref moveContext);
                SmoothRailGrinding.dismountJumpVelocityTransition = false;
            }
            if (SmoothRailGrinding.dismountVelocityTransition)
            {
                baseMovement.handleSmoothRailDismountCase(ref moveContext);
                SmoothRailGrinding.dismountVelocityTransition = false;
            }
        }

        if (!AttachToRail.isAttachedToRail && !AttachToWall.isAttachedToWall && !RailDetect.isOnSmoothRail)
        {
            baseMovement.handleQuadrantAcceleration(ref moveContext);
            baseMovement.handleQuadrantBasedMovement(ref moveContext);
            controller.Move((moveContext.currAccelMatrix + currentDashPower) * Time.fixedDeltaTime);
        }
        ;
        if (AttachToRail.isAttachedToRail && AttachToRail.lockoutInitialized)
        {
            baseMovement.HandleRailJumpLockout(); 
            AttachToRail.lockoutInitialized = false;
        }

        baseMovement.handleJumping(currRail, ref moveContext);
        railGrinding.handleRailGrinding(currRail, ref moveContext);

        if (AttachToWall.isAttachedToWall)
        {
            wallRiding.handleWallRunMovement(ref moveContext);
            wallRiding.handleWallRideJumpDismount(ref moveContext);
            controller.Move(moveContext.currAccelMatrix * Time.fixedDeltaTime);

            if (AttachToWall.isInTransition)
            {
                baseMovement.handleWallRunDismountCase(ref moveContext);
            }
        }
        else
        {
            wallRiding.resetLocalCharPosition();
        }
        handleLateFixedUpdate();
    }
    // -.5f if wallride right
    void HandleMovementBasedAnimations()
    {
        playerAnim.SetBool("isIdle", moveContext.currAccelMatrix.magnitude == 0);
        playerAnim.SetBool("isRunning", moveContext.currAccelMatrix.magnitude > 0 && !AttachToRail.isAttachedToRail && !moveContext.isJumping);
        playerAnim.SetBool("isJumping", moveContext.isJumping);
        playerAnim.SetBool("isDoubleJumping", moveContext.isDoubleJumping);
        playerAnim.SetBool("isFalling", moveContext.isFalling);
        playerAnim.SetBool("isDashing", moveContext.isDashing);
        playerAnim.SetBool("isSliding", moveContext.isSliding);
        // will need custom grinding anim
        playerAnim.SetBool("isGrinding", AttachToRail.isAttachedToRail || RailDetect.isOnSmoothRail);
        playerAnim.SetBool("isWallRunningRight", AttachToWall.isAttachedToWall && AttachToWall.isRightWallHit);
        playerAnim.SetBool("isWallRunningLeft", AttachToWall.isAttachedToWall && AttachToWall.isLeftWallHit);
        playerAnim.SetBool("isGrindingFast", (AttachToRail.isAttachedToRail || RailDetect.isOnSmoothRail) && Mathf.Abs(moveContext.railVelocity) > fastThreshold);
        playerAnim.SetBool("isExtendedIdle", Time.time > extendedIdleTime + moveContext.currIdleTime && moveContext.railVelocity == 0);
        playerAnim.SetFloat("currVelocity", moveContext.currAccelMatrix.magnitude);

        // prep to unset double jump animation
        if (moveContext.isDoubleJumping && !animResetProcced && 
            playerAnim.GetCurrentAnimatorStateInfo(0).IsName("DoubleJump"))
        {
            currAnimTime = Time.time + playerAnim.GetCurrentAnimatorStateInfo(0).length - doubleJumpAnimDurationOffset;
            animResetProcced = true;
        }
        if(animResetProcced && Time.time > currAnimTime)
        {
            moveContext.isDoubleJumping = false;
            animResetProcced = false;
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            Debug.Log("break statement");
        }

        if ((AttachToRail.isAttachedToRail || RailDetect.isOnSmoothRail) && 
           Mathf.Abs(moveContext.railVelocity) > fastThreshold)
        {
            if (isInitRailGrindVfx)
            {
                railGrindVisualEffect.Reinit();
                railGrindVisualEffect.Play();
                isInitRailGrindVfx = false;
            }
        }
        else if(Mathf.Abs(moveContext.railVelocity) <= fastThreshold || 
            (!AttachToRail.isAttachedToRail && !RailDetect.isOnSmoothRail))
        {
            railGrindVisualEffect.Stop();
            isInitRailGrindVfx = true;
        }

        // set VisualEffect here for now
        if ((AttachToRail.isAttachedToRail || RailDetect.isOnSmoothRail) && 
            Mathf.Abs(moveContext.railVelocity) > fastThreshold + 100 /* NEED NEW CONDITION FOR GLOW */)
        {
            if (isInitVfx)
            {
                currVisualEffect.Reinit();
                isInitVfx = false;
                currVisualEffect.Play();
                mat.SetFloat("_RimLight", 1);
                glowMat.SetFloat("_Alpha", 1);
                Debug.Log("reinit vfx");
            }
            float currRimLightPower = mat.GetFloat("_RimLight_Power");
            if (isAscending)
            {
                currRimLightPower += (oscillationRate * Time.deltaTime);
                if (currRimLightPower >= maxIntensity)
                    isAscending = false;
            }
            else
            {
                currRimLightPower -= (oscillationRate * Time.deltaTime);
                if (currRimLightPower <= minIntensity)
                    isAscending = true;
            }
            mat.SetFloat("_RimLight_Power", currRimLightPower);
        }
        else if (moveContext.isGrounded || ((AttachToRail.isAttachedToRail || RailDetect.isOnSmoothRail) && Mathf.Abs(moveContext.railVelocity) < fastThreshold)) // only stop effect when we touch the ground
        {
            currVisualEffect.Stop();
            isInitVfx = true;
            mat.SetFloat("_RimLight", 0);
            mat.SetFloat("_RimLight_Power", .8f);
            glowMat.SetFloat("_Alpha", 0);
        }
    }

    void handleEarlyFixedUpdate()
    {
        moveContext.currFramePos = transform.position;

        currRail = AttachToRail.railattachedTo;
        if (currRail == null)
            currRail = AttachToRail.dismountReferenceRail;
    }

    void handleLateFixedUpdate()
    {
        debugMagnitude = moveContext.currFramePos.magnitude < moveContext.lastFramePos.magnitude;
        moveContext.prevFramePos = moveContext.currFramePos;

        if (moveContext.lastFramePos == Vector3.zero)
            moveContext.lastFramePos = transform.position;
        else
        {
            // sometimes after a rail dismount, secondPrev and prev will become same. prevents that
            Vector3 prevPrevOffset = moveContext.secondPrevFramePos;
            moveContext.secondPrevFramePos = moveContext.lastFramePos;
            moveContext.lastFramePos = transform.position;
            if ((moveContext.secondPrevFramePos.x == moveContext.lastFramePos.x && moveContext.secondPrevFramePos.z == moveContext.lastFramePos.z) 
                && moveContext.railVelocity > 0) // only set if we're moving
                moveContext.secondPrevFramePos = prevPrevOffset;
        }

        moveContext.positionHistory.Enqueue(moveContext.lastFramePos);
        if(currHistoryFrame >= moveContext.maxPosHistoryTrack)
        {
            moveContext.positionHistory.Dequeue();
        }
        else
        {
            currHistoryFrame++;
        }

        if (AttachToRail.isAttachedToRail && !AttachToRail.movementCorrected)
        {
            controller.enabled = false;
            transform.position = AttachToRail.mountPoint;
            AttachToRail.movementCorrected = true;
            controller.enabled = true;
        }
    }

    // for places outside of standard movement manager that may need to reference and modify moveContext
    public ref PlayerMovementContext getMoveContextReference()
    {
        return ref moveContext;
    }

    // debug functions
    public int getRailSpeed()
    {
        return (int)Mathf.Abs(moveContext.railVelocity);
    }

    public bool getRailSpeedDirection()
    {
        return moveContext.railVelocity >= 0;
    }

    public int getSpeed()
    {
        return (int)Mathf.Abs(moveContext.currAccelMatrix.magnitude);
    }

    public bool isUnsignedVelocityGloballyPositive()
    {
        return moveContext.secondPrevFramePos.magnitude > moveContext.lastFramePos.magnitude;
    }

    public int getCurrentAccelRate()
    {
        return (int)moveContext.currAccelChangeRate;
    }

    public PlayerMovementContext getMoveContext()
    {
        return moveContext;
    }
}
