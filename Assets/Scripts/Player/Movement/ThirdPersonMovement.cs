using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.VFX;


/*
 *  BUG - when jumping off a rail with a '0' Y rotation, if the player has momentum and uses a lateral input and lets
 *      go before landing, the player will face laterally, but continue accelerating forward until another input
 *      is registered.
 */

/* FEATURE - See to enable "recenter to target heading" on cinemachine cam with recenter time tied to momentum of player
 *  (not moving = no recenter, max speed = quickest recenter), use cinemachine component "recenter to target heading"
 *  - include button to quickly recenter cam
 */

/* FEATURE - allow 'A' and 'D' (by extension, other mappable inputs) to be used when rail navigating
 * depending on camera perspective
 */

/* FEATURE - make a sprint toggle
 */



public class ThirdPersonMovement : MonoBehaviour
{
    public GameObject currRail; // rail to map the directional movement to
    public GameObject gfx;
    public CharacterController controller;
    public Transform cam;
    public VisualEffect currVisualEffect; // map this out into a manager (we will have multiple effects)
    public VisualEffect railGrindVisualEffect;

    [SerializeField]
    private float extendedIdleTime;
    private float currIdleTime;

    [SerializeField]
    private Animator playerAnim;

    [SerializeField]
    private float maxVelocity = 6f;
    [SerializeField]
    private float maxTransitionVelocity = 22f;
    [SerializeField]
    private float maxRailVelocity = 20f;
    [SerializeField]
    private float fastThreshold = 20f;
    [SerializeField]
    private float velocityRailIncreaseRate = 50;
    [SerializeField]
    private float bonusVelocityDecayRate = 1f;

    float currentAcceleration = 0;

    [SerializeField]
    private float turnSmoothingCoefficient = .1f;
    [SerializeField]
    private float gfxVerticalRotationRate = 100f;

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

    private float turnSmoothVelocity;

    private bool isJumping = false;
    private bool isFalling = false;

    private float zeroPoint = 270;
    private bool isInitialMount = true;

    // used to use maxRailTransitionVelocity instead of maxTransitionVelocity

    Vector3 playerVerticalVelocity = Vector3.zero;

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
    private Vector3 currAccelMatrix = Vector3.zero; // handles directional momentum transfer
    private float currentAcceptedMaxVelocity;
    private float currentAcceptedTransitionVelocity;

    // frame track vars
    Vector3 lastFramePos = Vector3.zero;
    Vector3 secondPrevFramePos = Vector3.zero;
    bool reducingX = false;
    bool reducingZ = false;

    float currTime = 0;
    float currRailJumpLockoutTime = 0;

    bool isPositiveAccel = false;

    // shader rimlight variables AND vfx graph vars
    [SerializeField]
    float maxIntensity = 1;
    [SerializeField]
    float minIntensity = .5f;
    [SerializeField]
    float oscillationRate = 20f;
    bool isAscending = false;
    bool isInitVfx;

    public Renderer render;
    Material mat;
    Material glowMat; // purple glow mat
    private void Start()
    {
        railGrindVisualEffect.resetSeedOnPlay = true;
        currVisualEffect.resetSeedOnPlay = true;
        isInitVfx = true;
        mat = render.material;
        glowMat = render.materials[1];
        mat.SetFloat("_RimLight", 0);
        mat.SetFloat("_RimLight_Power", .8f);
        glowMat.SetFloat("_Alpha", 0);

        currentAcceptedMaxVelocity = maxVelocity;
        currAccelChangeRate = baseAccelChangeRate;
        currIdleTime = Time.time;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;        
        Application.targetFrameRate = 165;
    } 

    void FixedUpdate()
    {
        currRail = AttachToRail.railattachedTo;
        if (currRail == null)
            currRail = AttachToRail.dismountReferenceRail;
        if (!AttachToRail.isAttachedToRail)
        {
            handleQuadrantAcceleration();
            handleQuadrantBasedMovement();
        }
        
        if(AttachToRail.isAttachedToRail && AttachToRail.lockoutInitialized)
        {
            Debug.Log("rail lockout updated");
            HandleRailJumpLockout();
            AttachToRail.lockoutInitialized = false;
        }
        
        handleJumping();
        handleRailGrinding();

        if (lastFramePos == Vector3.zero)
            lastFramePos = transform.position;
        else
        {
            // ??? sometimes after a rail dismount, secondPrev and prev will become same. prevents that
            Vector3 prevPrevOffset = secondPrevFramePos;
            secondPrevFramePos = lastFramePos;
            lastFramePos = transform.position;
            if ((secondPrevFramePos.x == lastFramePos.x && secondPrevFramePos.z == lastFramePos.z) && currentAcceleration > 0) // only set if we're moving
                secondPrevFramePos = prevPrevOffset;
        }

        if (AttachToRail.isAttachedToRail && !AttachToRail.movementCorrected)
        {
            controller.enabled = false;
            transform.position = AttachToRail.mountPoint;
            AttachToRail.movementCorrected = true;
            controller.enabled = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        HandleTempSensitivityChange();

        HandleAnimationUpdates();

        if (Input.GetKeyDown(KeyCode.P))
            Debug.Log(cam.rotation.eulerAngles.y);
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

    // make update - gradually update max accpted velocity in x and z directions based on total input from joystick
    void handleQuadrantAcceleration()
    {
        // decay bonus velocity (caused by rail exit) down to max transition speed
        if (currentAcceptedMaxVelocity > currentAcceptedTransitionVelocity)
        {
            currentAcceptedMaxVelocity -= (bonusVelocityDecayRate * Time.fixedDeltaTime);
            if(currentAcceptedMaxVelocity < currentAcceptedTransitionVelocity)
            {
                currentAcceptedMaxVelocity = currentAcceptedTransitionVelocity;
            }
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        // allow for input smoothing (only applies to controller)
        direction.x *= Mathf.Abs(horizontal);
        direction.z *= Mathf.Abs(vertical);

        direction = CameraRelativeFlatten(direction, Vector3.up);

        currAccelMatrix = new Vector3(
            currAccelMatrix.x + (direction.x * currAccelChangeRate * Time.fixedDeltaTime), 
            currAccelMatrix.y, 
            currAccelMatrix.z + (direction.z * currAccelChangeRate * Time.fixedDeltaTime));    

        // soft directional ratio offsetting
        if(direction.z == 0f && direction.x == 0f) // need to track when reducing what
        {
            if (!reducingX && !reducingZ)
            {
                if (Mathf.Abs(currAccelMatrix.x) < Mathf.Abs(currAccelMatrix.z))
                {
                    reducingZ = true;
                }
                if (Mathf.Abs(currAccelMatrix.x) > Mathf.Abs(currAccelMatrix.z))
                {
                    reducingX = true;
                }
            }
            if (reducingX)
            {
                if(currAccelMatrix.x > 0)
                {
                    float reductionResult = currAccelMatrix.x - currAccelChangeRate * Time.fixedDeltaTime;
                    if (reductionResult <= 0)
                    {
                        currAccelMatrix.x = 0;
                        currAccelMatrix.z = 0;
                    }
                    else
                    {
                        float reductionRate = currAccelMatrix.x / reductionResult;
                        currAccelMatrix.x = reductionResult;
                        currAccelMatrix.z = currAccelMatrix.z / reductionRate;
                    }
                }
                else if(currAccelMatrix.x < 0)
                {
                    float reductionResult = currAccelMatrix.x + currAccelChangeRate * Time.fixedDeltaTime;
                    if (reductionResult >= 0)
                    {
                        currAccelMatrix.x = 0;
                        currAccelMatrix.z = 0;
                    }
                    else
                    {
                        float reductionRate = currAccelMatrix.x / reductionResult;
                        currAccelMatrix.x = reductionResult;
                        currAccelMatrix.z = currAccelMatrix.z / Mathf.Abs(reductionRate);
                    }
                }
            }
            else if(reducingZ)
            {
                if (currAccelMatrix.z > 0)
                {
                    float reductionResult = currAccelMatrix.z - currAccelChangeRate * Time.fixedDeltaTime;
                    if (reductionResult <= 0)
                    {
                        currAccelMatrix.z = 0;
                        currAccelMatrix.x = 0;
                    }
                    else
                    {
                        float reductionRate = currAccelMatrix.z / reductionResult;
                        currAccelMatrix.z = reductionResult;
                        currAccelMatrix.x = currAccelMatrix.x / reductionRate;
                    }
                }
                else if(currAccelMatrix.z < 0)
                {
                    float reductionResult = currAccelMatrix.z + currAccelChangeRate * Time.fixedDeltaTime;
                    if (reductionResult >= 0)
                    {
                        currAccelMatrix.z = 0;
                        currAccelMatrix.x = 0;
                    }
                    else
                    {
                        float reductionRate = currAccelMatrix.z / reductionResult;
                        currAccelMatrix.z = reductionResult;
                        currAccelMatrix.x = currAccelMatrix.x / Mathf.Abs(reductionRate);
                    }
                }
            }
        }
        // recel handling (make var in place of 2. recel in diff direction faster than base accel change rate)
        if (direction.x > 0f && currAccelMatrix.x < 0f)
        {
            currAccelMatrix.x += currAccelChangeRate * 2 * Time.fixedDeltaTime;
        }
        if (direction.z > 0f && currAccelMatrix.z < 0f)
        {
            currAccelMatrix.z += currAccelChangeRate * 2 * Time.fixedDeltaTime;
        }
        if (direction.x < 0f && currAccelMatrix.x > 0f)
        {
            currAccelMatrix.x -= currAccelChangeRate * 2 * Time.fixedDeltaTime;
        }
        if (direction.z < 0f && currAccelMatrix.z > 0f)
        {
            currAccelMatrix.z -= currAccelChangeRate * 2 * Time.fixedDeltaTime;
        }
        if (Input.GetKey(KeyCode.P)){
            Debug.Log($"prevAccelMatrix: {currAccelMatrix} || prevDirection: {direction}");
        }
        // ratio clamp
        if (currAccelMatrix.x > currentAcceptedMaxVelocity)
        {
            float commonDivisor = currAccelMatrix.x / currentAcceptedMaxVelocity;
            currAccelMatrix.x = currentAcceptedMaxVelocity;
            currAccelMatrix.z = currAccelMatrix.z / commonDivisor;
        }
        if (currAccelMatrix.z > currentAcceptedMaxVelocity)
        {
            float commonDivisor = currAccelMatrix.z / currentAcceptedMaxVelocity;
            currAccelMatrix.z = currentAcceptedMaxVelocity;
            currAccelMatrix.x = currAccelMatrix.x / commonDivisor;
        }
        if(currAccelMatrix.x < -currentAcceptedMaxVelocity)
        {
            float commonDivisor = currAccelMatrix.x / -currentAcceptedMaxVelocity;
            currAccelMatrix.x = -currentAcceptedMaxVelocity;
            currAccelMatrix.z = currAccelMatrix.z / commonDivisor;
        }
        if (currAccelMatrix.z < -currentAcceptedMaxVelocity)
        {
            float commonDivisor = currAccelMatrix.z / -currentAcceptedMaxVelocity;
            currAccelMatrix.z = -currentAcceptedMaxVelocity;
            currAccelMatrix.x = currAccelMatrix.x / commonDivisor;
        }

        if (Input.GetKey(KeyCode.P))
        {
            Debug.Log($"currAccelMatrix: {currAccelMatrix} || direction: {direction}");
        }
    }

    Vector3 CameraRelativeFlatten(Vector3 input, Vector3 localUp, bool camIsRight = true)
    {
        // pass horiz and vertical to set input ratios

        Transform camera = cam.transform; // You can cache this to save a search.
        Quaternion flatten = Quaternion.LookRotation(-localUp, (camIsRight) ? camera.forward : camera.right) * Quaternion.Euler(-90f, 0, 0);

        return flatten * input;
    }

    void handleQuadrantBasedMovement()
    {
        if (!AttachToRail.isAttachedToRail)
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;
            Vector3 rawMoveDir = currAccelMatrix;
            if (direction.magnitude >= 0.1f)
            {
                reducingX = false;
                reducingZ = false;

                currIdleTime = Time.time;

                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
                float smoothedAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothingCoefficient);

                transform.rotation = Quaternion.Euler(0f, smoothedAngle, 0f);
            }
            controller.Move(rawMoveDir * Time.fixedDeltaTime);
        }
    }

    void handleRailGrinding()
    {
        if (AttachToRail.isAttachedToRail)
        {
            playerVerticalVelocity.y = 0; // perpetually reset jump power whenever mounted on rail
            currIdleTime = Time.time;

            if (isInitialMount)
            {
                isJumping = false;
                currentAcceleration = currAccelMatrix.magnitude;
                currAccelMatrix = Vector3.zero;
            }

            // handle forwards and backwards
            // factor in camera rotation
            // two 180 degree quadrants, based locally on rotation of current rail
            // 359-180 == W is forward, 0-179 == W is backwards, S is inverse
            // base movement choice input on which quadrant camera is rotated into            
            float inputDirection = Input.GetAxisRaw("Vertical");
            if (Input.GetKey(KeyCode.W) || inputDirection > 0)
            {
                (float, float, bool) offsetVals = FindZeroPointOffsetBoundaries(currRail.transform.eulerAngles.y);                
                // inner value checks (ex: no overlap past 0, bound is between 100 and 200)
                if (!offsetVals.Item3)
                {
                    // handle velocity inversion when mounting rail from negative angle
                    if (isInitialMount && (offsetVals.Item1 < cam.rotation.eulerAngles.y ||
                        cam.rotation.eulerAngles.y < offsetVals.Item2))
                    {
                        currentAcceleration *= -1;
                        isInitialMount = false;
                    }

                    currentAcceleration = (
                        offsetVals.Item1 >= cam.rotation.eulerAngles.y &&
                        cam.rotation.eulerAngles.y >= offsetVals.Item2 ?
                        currentAcceleration + (velocityRailIncreaseRate * Time.fixedDeltaTime) :
                        currentAcceleration - (velocityRailIncreaseRate * Time.fixedDeltaTime)); // calculate velocity rate increase
                }
                else
                { // xor bounds, meaning overlap past 0, bound is less than 100, greater than 200

                    // handle velocity inversion when mounting rail from negative angle
                    if (isInitialMount && (offsetVals.Item1 > cam.rotation.eulerAngles.y &&
                        cam.rotation.eulerAngles.y > offsetVals.Item2))
                    {
                        currentAcceleration *= -1;
                        isInitialMount = false;
                    }

                    currentAcceleration = (
                        offsetVals.Item1 <= cam.rotation.eulerAngles.y ||
                        cam.rotation.eulerAngles.y <= offsetVals.Item2 ?
                        currentAcceleration + (velocityRailIncreaseRate * Time.fixedDeltaTime) :
                        currentAcceleration - (velocityRailIncreaseRate * Time.fixedDeltaTime)); // calculate velocity rate increase
                }
                if (currentAcceleration > maxRailVelocity)
                    currentAcceleration = maxRailVelocity;
                if (currentAcceleration < maxRailVelocity * -1)
                    currentAcceleration = maxRailVelocity * -1;
            }
            else if (Input.GetKey(KeyCode.S) || inputDirection < 0)
            {
                (float, float, bool) offsetVals = FindZeroPointOffsetBoundaries(currRail.transform.eulerAngles.y);

                // Debug.Log($"Max = {offsetVals.Item1}, Min = {offsetVals.Item2}, AngleCam = {cam.rotation.eulerAngles.y}, isOuterBound = {offsetVals.Item3}");

                // inner value checks (ex: no, lap over 0, bound is between 100 and 200)
                if (!offsetVals.Item3)
                {
                    // handle velocity inversion when mounting rail from negative angle
                    if (isInitialMount && (offsetVals.Item1 > cam.rotation.eulerAngles.y &&
                        cam.rotation.eulerAngles.y > offsetVals.Item2))
                    {
                        currentAcceleration *= -1;
                        isInitialMount = false;
                    }

                    currentAcceleration = (
                        offsetVals.Item1 >= cam.rotation.eulerAngles.y &&
                        cam.rotation.eulerAngles.y >= offsetVals.Item2 ?
                        currentAcceleration - (velocityRailIncreaseRate * Time.fixedDeltaTime) :
                        currentAcceleration + (velocityRailIncreaseRate * Time.fixedDeltaTime)); // calculate velocity rate increase
                }
                else
                { // xor bounds, meaning overlap past 0, bound is less than 100, greater than 200
                    // handle velocity inversion when mounting rail from negative angle
                    if (isInitialMount && (offsetVals.Item1 < cam.rotation.eulerAngles.y ||
                        cam.rotation.eulerAngles.y < offsetVals.Item2))
                    {
                        currentAcceleration *= -1;
                        isInitialMount = false;
                    }

                    currentAcceleration = (
                        offsetVals.Item1 <= cam.rotation.eulerAngles.y ||
                        cam.rotation.eulerAngles.y <= offsetVals.Item2 ?
                        currentAcceleration - (velocityRailIncreaseRate * Time.fixedDeltaTime) :
                        currentAcceleration + (velocityRailIncreaseRate * Time.fixedDeltaTime)); // calculate velocity rate increase
                }
                if (currentAcceleration > maxRailVelocity)
                    currentAcceleration = maxRailVelocity;
                if (currentAcceleration < maxRailVelocity * -1)
                    currentAcceleration = maxRailVelocity * -1;
            }
            // only check for mount correction on first pass
            else
            {
                // get initial contact point info. if greater than center, handle neg,
                // if less than center, handle pos
                Vector3 relPosition = currRail.transform.position - lastFramePos;

                if (isInitialMount)
                {
                    // need to be handled differently
                    // if we aren't past half rail
                    bool dontToggle = false;
                    float currPosDistance = Mathf.Sqrt(Mathf.Pow(currRail.transform.position.x - lastFramePos.x, 2) + Mathf.Pow(currRail.transform.position.z - lastFramePos.z, 2));
                    float prevPosDistance = Mathf.Sqrt(Mathf.Pow(currRail.transform.position.x - secondPrevFramePos.x, 2) + Mathf.Pow(currRail.transform.position.z - secondPrevFramePos.z, 2));

                    if (Vector3.Dot(currRail.transform.up, relPosition) > 0 && currPosDistance < prevPosDistance)
                    {
                        // if we're moving closer to half way rail (currRail.transform.position)
                        dontToggle = true;
                    }
                    // if we are past half rail
                    if (Vector3.Dot(currRail.transform.up, relPosition) < 0 && currPosDistance > prevPosDistance)
                    {
                        // if we're moving further from half way rail (currRail.transform.position)
                        dontToggle = true;
                    }
                    if (!dontToggle)
                    {
                        currentAcceleration *= -1;
                    }
                    dontToggle = false;
                }
            }

            isInitialMount = false;

            // compare player direction and camera direction against rail direction when hopping on rail, handle momentum based on that
            // camera orientation controls *forwards* and *backwards*
            // currRail.transform.up.normalized or * -1, depending on initial momentum and camera

            controller.Move(currRail.transform.up.normalized * (currentAcceleration) * Time.fixedDeltaTime);

            transform.rotation = Quaternion.Euler(0, (currentAcceleration > 0 ? currRail.transform.eulerAngles.y - 90 : currRail.transform.eulerAngles.y + 90), 0);
            // instant rotation line
            // gfx.transform.localRotation = Quaternion.Euler((currentAcceleration > 0 ? currRail.transform.eulerAngles.z - 90 : (currRail.transform.eulerAngles.z - 90) * -1), 0, 0);

            // gradual vertical rotation
            Vector3 newGfxRotation = Quaternion.RotateTowards(
                gfx.transform.localRotation,
                Quaternion.Euler((currentAcceleration > 0 ? currRail.transform.eulerAngles.z - 90 : (currRail.transform.eulerAngles.z - 90) * -1), 0, 0),
                Mathf.Abs(gfxVerticalRotationRate * (currentAcceleration/2 * .2f) * Time.fixedDeltaTime)).eulerAngles;

            if (currentAcceleration > 0 && !isPositiveAccel)
            {
                newGfxRotation.x *= -1;
                isPositiveAccel = true;
            }
            if (currentAcceleration < 0 && isPositiveAccel)
            {
                newGfxRotation.x *= -1;
                isPositiveAccel = false;
            }
            gfx.transform.localRotation = Quaternion.Euler(newGfxRotation.x, newGfxRotation.y, newGfxRotation.z);
            // end gradual rotation code
        }
    }
   
    void handleJumping() 
    {
        bool isCurrGrounded = isGrounded();
        if (isCurrGrounded)
        {
            AttachToRail.isAttachedToRail = false;
            isJumping = false;
        }
        // as is this statement causes a slight jolt upon landing
        if (AttachToRail.isInitialDismount && !isJumping && !isCurrGrounded) // extra condition, is initial dismount (set currRail to null? check?)
        {
            HandleMomentumTransferToAccelMatrix(currRail.transform.up, currRail.transform.up * -1);
            isFalling = true;
            AttachToRail.isInitialDismount = false;
            currAccelChangeRate = airVelocityChangeRate;
            currentAcceptedMaxVelocity = Mathf.Max(Mathf.Abs(currentAcceleration), maxTransitionVelocity); // reduce over time if > maxVel
            currentAcceptedTransitionVelocity = railTransitionVelocity;
        }
        else // always set initial dismount to false after first physics check
        {
            AttachToRail.isInitialDismount = false;
        }

        if (!AttachToRail.isAttachedToRail && isCurrGrounded)
        {
            currentAcceptedMaxVelocity = maxVelocity;
        }
        if (!isCurrGrounded)
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            Vector3 dir = new Vector3 (horizontal, 0, vertical);
            if (dir.magnitude > 0)
            {
                currAccelChangeRate = airVelocityActiveChangeRate;
            }
            else
            {
                currAccelChangeRate = airVelocityChangeRate;
            }
        }
        
        if ((isCurrGrounded || AttachToRail.isAttachedToRail) && Input.GetButton("Jump") && Time.time > currTime)
        {
            currTime = Time.time + jumpTimeLockout; // needs to be handled upon first becoming grounded/railed
            currAccelChangeRate = airVelocityChangeRate; 

            // we aren't falling if we're jumping
            if (AttachToRail.isAttachedToRail && Time.time > currRailJumpLockoutTime)
            {
                Debug.Log("Dismount lockout passed");
                //currRailJumpLockoutTime = Time.time + railMountJumpTimeLockout; // needs to be handled in rail mount function
                GameObject.Find("RailDetector").GetComponent<AttachToRail>().UpdateDismountLockoutTimer();
                currentAcceptedMaxVelocity = Mathf.Max(Mathf.Abs(currentAcceleration), maxTransitionVelocity); // reduce over time if > maxVel
                currentAcceptedTransitionVelocity = railTransitionVelocity;
                // add conditional to run this whenever rail is dismounted
                HandleMomentumTransferToAccelMatrix(currRail.transform.up, currRail.transform.up * -1);
                AttachToRail.isAttachedToRail = false;
                playerVerticalVelocity.y = jumpPower;
                isJumping = true;
                isFalling = false;
                currIdleTime = Time.time;
            }
            else if(isCurrGrounded && !AttachToRail.isAttachedToRail)
            {
                currentAcceptedMaxVelocity = groundTransitionVelocity;
                currentAcceptedTransitionVelocity = maxVelocity;                
                playerVerticalVelocity.y = jumpPower;
                isJumping = true;
                isFalling = false;
                currIdleTime = Time.time;
            }

        }
        else if (isCurrGrounded)
        {
            isFalling = false;
            currentAcceptedMaxVelocity = maxVelocity;
            currAccelChangeRate = baseAccelChangeRate;
            isJumping = false;
            gfx.transform.localEulerAngles = Vector3.zero;
        }
        if (!AttachToRail.isAttachedToRail)
        {
            isInitialMount = true;
            playerVerticalVelocity.y -= (isJumping || isFalling ? gravityJumpCoefficient : gravityBasicCoefficient) * Time.fixedDeltaTime;
            controller.Move(playerVerticalVelocity * Time.fixedDeltaTime);
        }
    }

    // function designed to circle euler angles back around to 0 if they pass 360
    // handles boundaries for camera based momentum around a zero point defined by the camera angle
    // when aligned straight on a zero angle rail
    // also considers inclusive and exclusive bounding for angles
    (float, float, bool) FindZeroPointOffsetBoundaries(float railAngle)
    {
        bool outerBound = false;

        float zeroPointOffset = zeroPoint - (360 - railAngle);
        float zeroPointOffsetUpperBounds = zeroPointOffset + 90;
        if(zeroPointOffsetUpperBounds >= 360)
        {
            zeroPointOffsetUpperBounds = zeroPointOffsetUpperBounds - 360;           
        }

        float zeroPointOffsetLowerBounds = zeroPointOffset - 90;
        if (zeroPointOffsetLowerBounds < 0)
        {
            zeroPointOffsetLowerBounds = 360 + zeroPointOffsetLowerBounds;
            outerBound = true;
        }
        return (Mathf.Max(zeroPointOffsetLowerBounds, zeroPointOffsetUpperBounds), Mathf.Min(zeroPointOffsetLowerBounds, zeroPointOffsetUpperBounds), outerBound);
    }

    void HandleAnimationUpdates()
    {
        playerAnim.SetBool("isIdle", currAccelMatrix.magnitude == 0);
        playerAnim.SetBool("isRunning", currAccelMatrix.magnitude > 0 && !AttachToRail.isAttachedToRail && !isJumping);
        playerAnim.SetBool("isJumping", isJumping);
        playerAnim.SetBool("isFalling", isFalling);
        // will need custom grinding anim
        playerAnim.SetBool("isGrinding", AttachToRail.isAttachedToRail);
        playerAnim.SetBool("isGrindingFast", AttachToRail.isAttachedToRail && Mathf.Abs(currentAcceleration) > fastThreshold);
        playerAnim.SetBool("isExtendedIdle", Time.time > extendedIdleTime + currIdleTime && currentAcceleration == 0);

        if (AttachToRail.isAttachedToRail)
        {
            railGrindVisualEffect.Play();
        }
        else
        {
            railGrindVisualEffect.Stop();
        }

        // set VisualEffect here for now
        if (AttachToRail.isAttachedToRail && Mathf.Abs(currentAcceleration) > fastThreshold)
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
        else if(isGrounded() || (AttachToRail.isAttachedToRail && Mathf.Abs(currentAcceleration) < fastThreshold && !isGrounded())) // only stop effect when we touch the ground
        {
            currVisualEffect.Stop();
            isInitVfx = true;
            mat.SetFloat("_RimLight", 0);
            mat.SetFloat("_RimLight_Power", .8f);
            glowMat.SetFloat("_Alpha", 0);
        }
       
    }

    // match direction of the transform.right of the rail
    public void HandleMomentumTransferToAccelMatrix(Vector3 prevRailForwardDir, Vector3 prevRailBackwardsDir)
    {
        currAccelChangeRate = airVelocityChangeRate;

        Vector3 relPosition = AttachToRail.dismountReferenceRail.transform.position - lastFramePos;
        if (currentAcceleration < 0)
        {
            currentAcceleration *= -1;
        }
        // if currAccel is negative, use the inverse of the provided prevRailForwardDir
        float currPosDistance = Mathf.Sqrt(Mathf.Pow(AttachToRail.dismountReferenceRail.transform.position.x - lastFramePos.x, 2) + Mathf.Pow(AttachToRail.dismountReferenceRail.transform.position.z - lastFramePos.z, 2));
        float prevPosDistance = Mathf.Sqrt(Mathf.Pow(AttachToRail.dismountReferenceRail.transform.position.x - secondPrevFramePos.x, 2) + Mathf.Pow(AttachToRail.dismountReferenceRail.transform.position.z - secondPrevFramePos.z, 2));
        Vector3 direction = Vector3.zero;
        // if we are before half rail
        if (Vector3.Dot(AttachToRail.dismountReferenceRail.transform.up, relPosition) > 0 && currPosDistance < prevPosDistance)
        {
            // if we're moving closer to half way rail (currRail.transform.position)
            direction = new Vector3(prevRailForwardDir.x, 0f, prevRailForwardDir.z).normalized;
            currAccelMatrix = new Vector3((direction.x * currentAcceleration), 0, (direction.z * currentAcceleration));
        }
        if (Vector3.Dot(AttachToRail.dismountReferenceRail.transform.up, relPosition) > 0 && currPosDistance > prevPosDistance)
        {
            // if we're moving closer to half way rail (currRail.transform.position)
            direction = new Vector3(prevRailBackwardsDir.x, 0f, prevRailBackwardsDir.z).normalized;

            currAccelMatrix = new Vector3((direction.x * currentAcceleration), 0, (direction.z * currentAcceleration));
        }
        // if we are past half rail
        if (Vector3.Dot(AttachToRail.dismountReferenceRail.transform.up, relPosition) < 0 && currPosDistance > prevPosDistance)
        {
            // if we're moving further from half way rail (currRail.transform.position)
            direction = new Vector3(prevRailForwardDir.x, 0f, prevRailForwardDir.z).normalized;
            currAccelMatrix = new Vector3((direction.x * currentAcceleration), 0, (direction.z * currentAcceleration));

        }
        if (Vector3.Dot(AttachToRail.dismountReferenceRail.transform.up, relPosition) < 0 && currPosDistance < prevPosDistance)
        {
            // if we're moving closer to half way rail (currRail.transform.position)
            direction = new Vector3(prevRailBackwardsDir.x, 0f, prevRailBackwardsDir.z).normalized;
            currAccelMatrix = new Vector3((direction.x * currentAcceleration), 0, (direction.z * currentAcceleration));
        }

        lastFramePos = Vector3.zero;
        secondPrevFramePos = Vector3.zero;
    }

    public void HandleRailJumpLockout()
    {
        currRailJumpLockoutTime = Time.time + railMountJumpTimeLockout;
    }

    bool isGrounded()
    {
        return Physics.Raycast(transform.position + controller.center, 
            Vector3.down, 
            controller.height * .5f + groundCheckDistance, 
            groundLayers) && !AttachToRail.isAttachedToRail;

    }

    public int getRailSpeed()
    {
        return (int)Mathf.Abs(currentAcceleration);
    }

    public int getSpeed()
    {
        return (int)Mathf.Abs(currAccelMatrix.magnitude);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawLine(transform.position + controller.center, (transform.position + controller.center) + (Vector3.down * (controller.height * .5f + (AttachToRail.isAttachedToRail ? 0 : groundCheckDistance))));
    }
}
