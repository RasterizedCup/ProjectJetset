using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseMovement : PlayerMovementEffector
{
    // velocity controls
    [SerializeField]
    private float maxVelocity = 6f;
    [SerializeField]
    private float maxTransitionVelocity = 22f;
    [SerializeField]
    private float bonusVelocityDecayRate = 1f;
    [SerializeField]
    private float bonusVelocityRailDismountDecayRate = 1f;
    [SerializeField]
    private float sprintBonusVelocity = 5f;
    [SerializeField]
    private float controllerVelocityDeadzoneThreshold = .7f;

    // angular controls
    [SerializeField]
    private float turnSmoothingCoefficient = .1f;

    // jump controls
    [SerializeField]
    private float jumpPower = 1;
    [SerializeField]
    private float jumpTimeLockout = .05f;
    [SerializeField]
    private float railMountJumpTimeLockout = .2f;
    [SerializeField]
    private float gravityJumpCoefficient = 40;
    [SerializeField]
    private float gravityBasicCoefficient = 9.8f;
    [SerializeField]
    private float groundCheckDistance = .15f;
    [SerializeField]
    private LayerMask groundLayers;
    [SerializeField]
    private int jumpInputGraceFrames = 3;

    // private vars
    private float turnSmoothVelocity;

    // angular momentum variables
    [SerializeField]
    private float baseAccelChangeRate = 70f;
    [SerializeField]
    private float airVelocityChangeRate = 30f;
    [SerializeField]
    private float airVelocityActiveChangeRate = 70f;
    [SerializeField]
    private float railTransitionVelocity = 21f;
    [SerializeField]
    private float groundTransitionVelocity = 16f;
    private float currAccelChangeRate;

    private float currentAcceptedMaxVelocity;
    private float adjustedAcceptedMaxVelocity;
    private float currentAcceptedTransitionVelocity;

    bool reducingX = false;
    bool reducingZ = false;

    bool isSprinting = false;
    bool JumpInputted = false;

    float currTime = 0;
    float currRailJumpLockoutTime = 0;
    int currJumpGraceFrame = 0;

    private void Start()
    {
        currAccelChangeRate = baseAccelChangeRate;
    }

    Vector3 CameraRelativeFlatten(Vector3 input, Vector3 localUp, bool camIsRight = true)
    {
        // pass horiz and vertical to set input ratios

        Transform camera = cam.transform; // You can cache this to save a search.
        Quaternion flatten = Quaternion.LookRotation(-localUp, (camIsRight) ? camera.forward : camera.right) * Quaternion.Euler(-90f, 0, 0);

        return flatten * input;
    }

    public void handleQuadrantAcceleration(ref PlayerMovementContext moveContext)
    {
        // decay bonus velocity (caused by rail exit) down to max transition speed
        if (currentAcceptedMaxVelocity > currentAcceptedTransitionVelocity)
        {
            if (SmoothRailGrinding.isDismountTransition || AttachToWall.isInTransition || AttachToRail.isInStraightRailTransition)
            {
                currentAcceptedMaxVelocity -= (bonusVelocityRailDismountDecayRate * Time.fixedDeltaTime);
            }
            else
            {
                currentAcceptedMaxVelocity -= (bonusVelocityDecayRate * Time.fixedDeltaTime);
            }
            if (currentAcceptedMaxVelocity < currentAcceptedTransitionVelocity)
            {
                currentAcceptedMaxVelocity = currentAcceptedTransitionVelocity;
            }
        }
        adjustedAcceptedMaxVelocity = currentAcceptedMaxVelocity + (isSprinting ? sprintBonusVelocity : 0);
        float rawAcceptedMaxVelocity = currentAcceptedMaxVelocity + (isSprinting ? sprintBonusVelocity : 0);
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;
        // allow for input smoothing (only applies to controller)
        Vector3 directionalMag = direction;
        directionalMag.x *= Mathf.Abs(horizontal);
        directionalMag.z *= Mathf.Abs(vertical);
        // for controller, allow a max threshold to exist for controller movement
        float goalMaxVelocity;
        // need to find way to gradually accelerate while jumping as well, but ALSO not reset velocity when
        // no input and on rail
        if (directionalMag.magnitude < controllerVelocityDeadzoneThreshold && moveContext.isGrounded)
        {
            // gradually transition to the adjusted velocity
            goalMaxVelocity = rawAcceptedMaxVelocity * directionalMag.magnitude;
        }
        else
        {
            goalMaxVelocity = rawAcceptedMaxVelocity;
        }
        if (adjustedAcceptedMaxVelocity != goalMaxVelocity)
        {
            if (goalMaxVelocity > adjustedAcceptedMaxVelocity)
            {
                adjustedAcceptedMaxVelocity += (moveContext.currAccelChangeRate * Time.fixedDeltaTime);
                //Debug.Log($"raised velocity to: {adjustedAcceptedMaxVelocity}");
            }
            else if (goalMaxVelocity < adjustedAcceptedMaxVelocity)
            {
                adjustedAcceptedMaxVelocity -= (moveContext.currAccelChangeRate * Time.fixedDeltaTime);
                if(adjustedAcceptedMaxVelocity < 0)
                    adjustedAcceptedMaxVelocity = 0;
            }

        }

        direction = CameraRelativeFlatten(direction, Vector3.up);

        moveContext.currAccelMatrix = new Vector3(
            moveContext.currAccelMatrix.x + (direction.x * moveContext.currAccelChangeRate * Time.fixedDeltaTime),
            moveContext.currAccelMatrix.y,
            moveContext.currAccelMatrix.z + (direction.z * moveContext.currAccelChangeRate * Time.fixedDeltaTime));

        // soft directional ratio offsetting
        if (direction.z == 0f && direction.x == 0f) // need to track when reducing what
        {
            if (!reducingX && !reducingZ)
            {
                if (Mathf.Abs(moveContext.currAccelMatrix.x) < Mathf.Abs(moveContext.currAccelMatrix.z))
                {
                    reducingZ = true;
                }
                if (Mathf.Abs(moveContext.currAccelMatrix.x) > Mathf.Abs(moveContext.currAccelMatrix.z))
                {
                    reducingX = true;
                }
            }
            if (reducingX)
            {
                if (moveContext.currAccelMatrix.x > 0)
                {
                    float reductionResult = moveContext.currAccelMatrix.x - moveContext.currAccelChangeRate * Time.fixedDeltaTime;
                    if (reductionResult <= 0)
                    {
                        moveContext.currAccelMatrix.x = 0;
                        moveContext.currAccelMatrix.z = 0;
                    }
                    else
                    {
                        float reductionRate = moveContext.currAccelMatrix.x / reductionResult;

                        moveContext.currAccelMatrix.x = reductionResult;
                        moveContext.currAccelMatrix.z = moveContext.currAccelMatrix.z / reductionRate;

                    }
                }
                else if (moveContext.currAccelMatrix.x < 0)
                {
                    float reductionResult = moveContext.currAccelMatrix.x + moveContext.currAccelChangeRate * Time.fixedDeltaTime;
                    if (reductionResult >= 0)
                    {
                        moveContext.currAccelMatrix.x = 0;
                        moveContext.currAccelMatrix.z = 0;
                    }
                    else
                    {
                        float reductionRate = moveContext.currAccelMatrix.x / reductionResult;

                        moveContext.currAccelMatrix.x = reductionResult;
                        moveContext.currAccelMatrix.z = moveContext.currAccelMatrix.z / Mathf.Abs(reductionRate);
                    }
                }
            }
            else if (reducingZ)
            {
                if (moveContext.currAccelMatrix.z > 0)
                {
                    float reductionResult = moveContext.currAccelMatrix.z - moveContext.currAccelChangeRate * Time.fixedDeltaTime;
                    if (reductionResult <= 0)
                    {
                        moveContext.currAccelMatrix.z = 0;
                        moveContext.currAccelMatrix.x = 0;
                    }
                    else
                    {
                        float reductionRate = moveContext.currAccelMatrix.z / reductionResult;

                        moveContext.currAccelMatrix.z = reductionResult;
                        moveContext.currAccelMatrix.x = moveContext.currAccelMatrix.x / reductionRate;
                    }
                }
                else if (moveContext.currAccelMatrix.z < 0)
                {
                    float reductionResult = moveContext.currAccelMatrix.z + moveContext.currAccelChangeRate * Time.fixedDeltaTime;
                    if (reductionResult >= 0)
                    {
                        moveContext.currAccelMatrix.z = 0;
                        moveContext.currAccelMatrix.x = 0;
                    }
                    else
                    {
                        //Debug.Log($"Quadrant 4 reduct Result: {reductionResult}");
                        float reductionRate = moveContext.currAccelMatrix.z / reductionResult;

                        moveContext.currAccelMatrix.z = reductionResult;
                        moveContext.currAccelMatrix.x = moveContext.currAccelMatrix.x / Mathf.Abs(reductionRate);
                    }
                }
            }

        }
        // recel handling (make var in place of 2. recel in diff direction faster than base accel change rate)
        if (direction.x > 0f && moveContext.currAccelMatrix.x < 0f)
        {
            moveContext.currAccelMatrix.x += moveContext.currAccelChangeRate * 2 * Time.fixedDeltaTime;
        }
        if (direction.z > 0f && moveContext.currAccelMatrix.z < 0f)
        {
            moveContext.currAccelMatrix.z += moveContext.currAccelChangeRate * 2 * Time.fixedDeltaTime;
        }
        if (direction.x < 0f && moveContext.currAccelMatrix.x > 0f)
        {
            moveContext.currAccelMatrix.x -= moveContext.currAccelChangeRate * 2 * Time.fixedDeltaTime;
        }
        if (direction.z < 0f && moveContext.currAccelMatrix.z > 0f)
        {
            moveContext.currAccelMatrix.z -= moveContext.currAccelChangeRate * 2 * Time.fixedDeltaTime;
        }
        if (Input.GetKey(KeyCode.P))
        {
            Debug.Log($"prevAccelMatrix: {moveContext.currAccelMatrix} || prevDirection: {direction}");
        }
        // ratio clamp (need to make it so if velocity clamp is superceded by dash/slide, it is reduced slowly, not instantly)
        if (!AttachToWall.isInTransition && !moveContext.isDashing && !moveContext.isSliding)
        {
            if (moveContext.currAccelMatrix.x > adjustedAcceptedMaxVelocity)
            {
                float commonDivisor = moveContext.currAccelMatrix.x / adjustedAcceptedMaxVelocity;
                moveContext.currAccelMatrix.x = adjustedAcceptedMaxVelocity;
                moveContext.currAccelMatrix.z = moveContext.currAccelMatrix.z / commonDivisor;
            }
            if (moveContext.currAccelMatrix.z > adjustedAcceptedMaxVelocity)
            {
                float commonDivisor = moveContext.currAccelMatrix.z / adjustedAcceptedMaxVelocity;
                moveContext.currAccelMatrix.z = adjustedAcceptedMaxVelocity;
                moveContext.currAccelMatrix.x = moveContext.currAccelMatrix.x / commonDivisor;
            }
            if (moveContext.currAccelMatrix.x < -adjustedAcceptedMaxVelocity)
            {
                float commonDivisor = moveContext.currAccelMatrix.x / -adjustedAcceptedMaxVelocity;
                moveContext.currAccelMatrix.x = -adjustedAcceptedMaxVelocity;
                moveContext.currAccelMatrix.z = moveContext.currAccelMatrix.z / commonDivisor;
            }
            if (moveContext.currAccelMatrix.z < -adjustedAcceptedMaxVelocity)
            {
                float commonDivisor = moveContext.currAccelMatrix.z / -adjustedAcceptedMaxVelocity;
                moveContext.currAccelMatrix.z = -adjustedAcceptedMaxVelocity;
                moveContext.currAccelMatrix.x = moveContext.currAccelMatrix.x / commonDivisor;
            }
        }
        // if our angular momentum supercedes our maximum velocity, ratioatically reduce to max vel
        if(moveContext.currAccelMatrix.magnitude > adjustedAcceptedMaxVelocity && !moveContext.isDashing && !moveContext.isSliding)
        {
            moveContext.currAccelMatrix /= (moveContext.currAccelMatrix.magnitude / adjustedAcceptedMaxVelocity);
        }

        if (Input.GetKey(KeyCode.P))
        {
            Debug.Log($"moveContext.currAccelMatrix: {moveContext.currAccelMatrix} || direction: {direction}");
        }
    }

    public void handleQuadrantBasedMovement(ref PlayerMovementContext moveContext)
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;
        Vector3 rawMoveDir = moveContext.currAccelMatrix;
        if (direction.magnitude >= 0.1f) // don't rotate if aiming
        {
            reducingX = false;
            reducingZ = false;

            moveContext.currIdleTime = Time.time;
            if (!moveContext.isAiming) // only rotate if no aiming
            {
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
                float smoothedAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothingCoefficient);

                transform.rotation = Quaternion.Euler(0f, smoothedAngle, 0f);
            }
        }    
    }

    public void handleWallRunDismountCase(ref PlayerMovementContext moveContext)
    {
        currentAcceptedMaxVelocity = Mathf.Max(Mathf.Abs(moveContext.currAccelMatrix.magnitude), maxTransitionVelocity); // reduce over time if > maxVel
        adjustedAcceptedMaxVelocity = currentAcceptedMaxVelocity; // map given adjust velocity on dismount
        currentAcceptedTransitionVelocity = railTransitionVelocity;
        moveContext.currAccelChangeRate = airVelocityChangeRate;
        moveContext.isJumping = true;
        moveContext.isFalling = false;
        isSprinting = false;
    }

    public void handleSmoothRailJumpcase(ref PlayerMovementContext moveContext)
    {
        RailDetect.isOnSmoothRail = false;
        moveContext.playerVerticalVelocity.y = jumpPower;
        currentAcceptedMaxVelocity = Mathf.Max(Mathf.Abs(moveContext.railVelocity), maxTransitionVelocity); // reduce over time if > maxVel
        adjustedAcceptedMaxVelocity = currentAcceptedMaxVelocity; // map given adjust velocity on dismount
        currentAcceptedTransitionVelocity = railTransitionVelocity;
        moveContext.currAccelChangeRate = airVelocityChangeRate;
        moveContext.isJumping = true;
        moveContext.isFalling = false;
    }

    public void handleSmoothRailDismountCase(ref PlayerMovementContext moveContext)
    {
        RailDetect.isOnSmoothRail = false;
        moveContext.playerVerticalVelocity.y = 0;
        currentAcceptedMaxVelocity = Mathf.Max(Mathf.Abs(moveContext.railVelocity), maxTransitionVelocity); // reduce over time if > maxVel
        adjustedAcceptedMaxVelocity = currentAcceptedMaxVelocity; // map given adjust velocity on dismount
        currentAcceptedTransitionVelocity = railTransitionVelocity;
        moveContext.currAccelChangeRate = airVelocityChangeRate;
        moveContext.isJumping = true;
        moveContext.isFalling = false;
    }

    // update to bool for resetting idle time
    public void handleJumping(GameObject currRail, ref PlayerMovementContext moveContext)
    {
        if (AttachToWall.isInTransition)
        {
            moveContext.currAccelChangeRate = airVelocityChangeRate;
            currentAcceptedMaxVelocity = currentAcceptedMaxVelocity = Mathf.Max(Mathf.Abs(moveContext.currAccelMatrix.magnitude), maxTransitionVelocity); // reduce over time if > maxVel
        }

        moveContext.isGrounded = isGrounded();
        if (moveContext.isGrounded)
        {
            SmoothRailGrinding.isDismountTransition = false;
            AttachToRail.isInStraightRailTransition = false;
            AttachToWall.isInTransition = false;
            AttachToRail.isAttachedToRail = false;
            moveContext.isJumping = false;
        }
        // as is this statement causes a slight jolt upon landing
        if ((AttachToRail.isInitialDismount) && !moveContext.isJumping && !moveContext.isGrounded) // extra condition, is initial dismount (set currRail to null? check?)
        {
            gfx.transform.rotation =  Quaternion.Euler(0, gfx.transform.rotation.eulerAngles.y, gfx.transform.rotation.eulerAngles.z);
            HandleMomentumTransferToAccelMatrix(currRail.transform.up, currRail.transform.up * -1, ref moveContext);
            moveContext.isFalling = true;
            AttachToRail.isInitialDismount = false;
            moveContext.currAccelChangeRate = airVelocityChangeRate;
            if(AttachToRail.isInitialDismount)
                currentAcceptedMaxVelocity = Mathf.Max(Mathf.Abs(moveContext.railVelocity), maxTransitionVelocity); // reduce over time if > maxVel
           // if(AttachToWall.isNewDetach)
           //     currentAcceptedMaxVelocity = Mathf.Max(Mathf.Abs(moveContext.currAccelMatrix.magnitude), maxTransitionVelocity); // reduce over time if > maxVel

            currentAcceptedTransitionVelocity = railTransitionVelocity;
        }
        else // always set initial dismount to false after first physics check
        {
            AttachToRail.isInitialDismount = false;
        }

        if (!AttachToRail.isAttachedToRail && !RailDetect.isOnSmoothRail && moveContext.isGrounded)
        {
            currentAcceptedMaxVelocity = maxVelocity;
        }
        if (!moveContext.isGrounded)
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            Vector3 dir = new Vector3(horizontal, 0, vertical);
            if (dir.magnitude > 0)
            {
                moveContext.currAccelChangeRate = airVelocityActiveChangeRate;
            }
            else
            {
                moveContext.currAccelChangeRate = airVelocityChangeRate;
            }
        }

        if ((moveContext.isGrounded || AttachToRail.isAttachedToRail || AttachToWall.isAttachedToWall) && JumpInputted && Time.time > currTime)
        {
            gfx.transform.rotation = Quaternion.Euler(0, gfx.transform.rotation.eulerAngles.y, gfx.transform.rotation.eulerAngles.z);

            currTime = Time.time + jumpTimeLockout; // needs to be handled upon first becoming grounded/railed
            moveContext.currAccelChangeRate = airVelocityChangeRate;

            // we aren't falling if we're jumping
            if ((AttachToRail.isAttachedToRail || RailDetect.isOnSmoothRail) && Time.time > currRailJumpLockoutTime)
            {
                AttachToWall.isInTransition = false;
                //currRailJumpLockoutTime = Time.time + railMountJumpTimeLockout; // needs to be handled in rail mount function
                GameObject.Find("RailDetector").GetComponent<AttachToRail>().UpdateDismountLockoutTimer();
                currentAcceptedMaxVelocity = Mathf.Max(Mathf.Abs(moveContext.railVelocity), maxTransitionVelocity); // reduce over time if > maxVel
                currentAcceptedTransitionVelocity = railTransitionVelocity;
                // add conditional to run this whenever rail is dismounted
                HandleMomentumTransferToAccelMatrix(currRail.transform.up, currRail.transform.up * -1, ref moveContext);
                AttachToRail.isAttachedToRail = false;
                RailDetect.isOnSmoothRail = false;
                moveContext.playerVerticalVelocity.y = jumpPower;
                moveContext.isJumping = true;
                moveContext.isFalling = false;
                moveContext.currIdleTime = Time.time;
            }
            else if (moveContext.isGrounded && (!AttachToRail.isAttachedToRail && !RailDetect.isOnSmoothRail))
            {
                currentAcceptedMaxVelocity = groundTransitionVelocity;
                currentAcceptedTransitionVelocity = maxVelocity;
                moveContext.playerVerticalVelocity.y = jumpPower;
                moveContext.isJumping = true;
                moveContext.isFalling = false;
                moveContext.currIdleTime = Time.time;
            }
            else if (AttachToWall.isAttachedToWall)
            {
                currentAcceptedMaxVelocity = Mathf.Max(Mathf.Abs(moveContext.currAccelMatrix.magnitude), maxTransitionVelocity); // reduce over time if > maxVel
                currentAcceptedTransitionVelocity = railTransitionVelocity;
            }
        }
        else if (moveContext.isGrounded && !moveContext.isSliding)
        {
            moveContext.isFalling = false;
            currentAcceptedMaxVelocity = maxVelocity;
            moveContext.currAccelChangeRate = baseAccelChangeRate;
            moveContext.isJumping = false;
            gfx.transform.localEulerAngles = Vector3.zero;
        }
        if (!AttachToRail.isAttachedToRail && !AttachToWall.isAttachedToWall && !RailDetect.isOnSmoothRail)
        {
            moveContext.isInitialMount = true;
            moveContext.playerVerticalVelocity.y -= (moveContext.isJumping || moveContext.isFalling ? gravityJumpCoefficient : gravityBasicCoefficient) * Time.fixedDeltaTime;
            controller.Move(moveContext.playerVerticalVelocity * Time.fixedDeltaTime);
        }
    }

    // function designed to circle euler angles back around to 0 if they pass 360
    // handles boundaries for camera based momentum around a zero point defined by the camera angle
    // when aligned straight on a zero angle rail
    // also considers inclusive and exclusive bounding for angles
    public void HandleMomentumTransferToAccelMatrix(Vector3 prevRailForwardDir, Vector3 prevRailBackwardsDir, ref PlayerMovementContext moveContext)
    {
        moveContext.currAccelChangeRate = airVelocityChangeRate;

        Vector3 relPosition = AttachToRail.dismountReferenceRail.transform.position - moveContext.lastFramePos;
        if (moveContext.railVelocity < 0)
        {
            moveContext.railVelocity *= -1;
        }
        // if currAccel is negative, use the inverse of the provided prevRailForwardDir
        float currPosDistance = Mathf.Sqrt(Mathf.Pow(AttachToRail.dismountReferenceRail.transform.position.x - moveContext.lastFramePos.x, 2) + Mathf.Pow(AttachToRail.dismountReferenceRail.transform.position.z - moveContext.lastFramePos.z, 2));
        float prevPosDistance = Mathf.Sqrt(Mathf.Pow(AttachToRail.dismountReferenceRail.transform.position.x - moveContext.secondPrevFramePos.x, 2) + Mathf.Pow(AttachToRail.dismountReferenceRail.transform.position.z - moveContext.secondPrevFramePos.z, 2));
        Vector3 direction = Vector3.zero;
        // if we are before half rail
        if (Vector3.Dot(AttachToRail.dismountReferenceRail.transform.up, relPosition) > 0 && currPosDistance < prevPosDistance)
        {
            // if we're moving closer to half way rail (currRail.transform.position)
            direction = new Vector3(prevRailForwardDir.x, 0f, prevRailForwardDir.z).normalized;
            moveContext.currAccelMatrix = new Vector3((direction.x * moveContext.railVelocity), 0, (direction.z * moveContext.railVelocity));
        }
        if (Vector3.Dot(AttachToRail.dismountReferenceRail.transform.up, relPosition) > 0 && currPosDistance > prevPosDistance)
        {
            // if we're moving closer to half way rail (currRail.transform.position)
            direction = new Vector3(prevRailBackwardsDir.x, 0f, prevRailBackwardsDir.z).normalized;

            moveContext.currAccelMatrix = new Vector3((direction.x * moveContext.railVelocity), 0, (direction.z * moveContext.railVelocity));
        }
        // if we are past half rail
        if (Vector3.Dot(AttachToRail.dismountReferenceRail.transform.up, relPosition) < 0 && currPosDistance > prevPosDistance)
        {
            // if we're moving further from half way rail (currRail.transform.position)
            direction = new Vector3(prevRailForwardDir.x, 0f, prevRailForwardDir.z).normalized;
            moveContext.currAccelMatrix = new Vector3((direction.x * moveContext.railVelocity), 0, (direction.z * moveContext.railVelocity));

        }
        if (Vector3.Dot(AttachToRail.dismountReferenceRail.transform.up, relPosition) < 0 && currPosDistance < prevPosDistance)
        {
            // if we're moving closer to half way rail (currRail.transform.position)
            //  Debug.Log("calcing direction");
            direction = new Vector3(prevRailBackwardsDir.x, 0f, prevRailBackwardsDir.z).normalized;
            moveContext.currAccelMatrix = new Vector3((direction.x * moveContext.railVelocity), 0, (direction.z * moveContext.railVelocity));
        }

        moveContext.lastFramePos = Vector3.zero;
        moveContext.secondPrevFramePos = Vector3.zero;
    }

    bool isGrounded()
    {
        return Physics.Raycast(transform.position + controller.center,
            Vector3.down,
            controller.height * .5f + groundCheckDistance,
            groundLayers) && !AttachToRail.isAttachedToRail;
    }

    public void CheckIfToggleSprint(ref PlayerMovementContext moveContext)
    {
        // base toggle (do not allow sprint toggle if jumping)
        if (Input.GetKeyDown(KeyCode.Joystick1Button8) || Input.GetKeyDown(KeyCode.LeftShift) && moveContext.isGrounded)
        {
            isSprinting = !isSprinting;
        }

        // disable sprint if we're grinding
        if (AttachToRail.isAttachedToRail || RailDetect.isOnSmoothRail)
            isSprinting = false;
    }

    public void CheckIfJump()
    {
        if (Input.GetButtonDown("Jump"))
        {
            JumpInputted = true;
        } // sometimes input is overlooked. allow a brief grace period to compensate
        else if(currJumpGraceFrame > jumpInputGraceFrames && JumpInputted)
        {
            currJumpGraceFrame = 0;
            JumpInputted = false;
        }
        else if(JumpInputted)
        {
            currJumpGraceFrame++;
        }
    }

    public void HandleRailJumpLockout()
    {
        currRailJumpLockoutTime = Time.time + railMountJumpTimeLockout;
    }

    // debug options
    public float getcurrAccelChangeRate()
    {
        return currAccelChangeRate;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawLine(transform.position + controller.center, (transform.position + controller.center) + (Vector3.down * (controller.height * .5f + (AttachToRail.isAttachedToRail ? 0 : groundCheckDistance))));
    }

}

