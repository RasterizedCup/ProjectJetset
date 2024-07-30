using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CamAimLogic : MonoBehaviour
{

    //tps 180 <-> 270 == cam -180 <-> -90
    [SerializeField]
    PlayerMovementManager playerMovementManager;
    [SerializeField]
    GameObject playerHead;
    [SerializeField]
    Transform followObjectBase;
    [SerializeField]
    Transform playerOrientationWhileOnRail;
    [SerializeField]
    Transform playerOrientationWhileOnWall;
    [SerializeField]
    Transform followObjectRail;
    [SerializeField]
    Transform followObjectWallLeft;
    [SerializeField]
    Transform followObjectWallRight;
    [SerializeField]
    Transform playerOrientation;
    [SerializeField]
    CinemachineFreeLook mainCam;
    [SerializeField]
    float xSens;
    [SerializeField]
    float ySens;

    CinemachineVirtualCamera aimCam;
    CinemachineTransposer bodyOfCam;
    [SerializeField]
    int basePriority;
    [SerializeField]
    int activePriority;

    bool isFirstRunUnaimedFromNonStandard = false;
    bool isFirstRunAimed = false;
    bool isAiming = false;

    Vector3 baseBodycamOffset;

    // needed updates
    // - add transitions from wall/rail aim states to standard aim states
    // -- (on initial rail/wall dismount, set player rotation to rotation of substate
    // then reset substate
    // - add functionality for a "lock-on" camera

    // Start is called before the first frame update
    void Start()
    {
        aimCam = GetComponent<CinemachineVirtualCamera>();
        bodyOfCam = aimCam.GetCinemachineComponent<CinemachineTransposer>();
        baseBodycamOffset = bodyOfCam.m_FollowOffset;
        aimCam.Priority = basePriority; // start one lower priority than mainCam
        isFirstRunAimed = true;
    }

    // Update is called once per frame
    void Update()
    {
        HandleCamAim();
        HandleAimRotation();
        UpdateAimStateInContext();
    }

    private void LateUpdate()
    {
        // handle Head Turn in lateUpdate
        if (Input.GetButton("Fire2"))
        {
                playerHead.transform.rotation = Quaternion.Euler(
                    aimCam.transform.rotation.eulerAngles.x,
                    aimCam.transform.rotation.eulerAngles.y,
                    aimCam.transform.rotation.eulerAngles.z);
        }
        else
        {
        }
    }

    void HandleAimRotation()
    {
        if (isAiming)
        {
            Debug.Log("Aim base turn Constant");
            float mouseMotionX = Input.GetAxisRaw("Mouse X") * xSens;
            float mouseMotionY = Input.GetAxisRaw("Mouse Y") * ySens;
            // X rotation
            if (AttachToRail.isAttachedToRail || RailDetect.isOnSmoothRail)
            {
                isFirstRunUnaimedFromNonStandard = true;
                playerOrientationWhileOnRail.Rotate(0, mouseMotionX * Time.deltaTime, 0);
            }
            else if (AttachToWall.isAttachedToWall)
            {
                isFirstRunUnaimedFromNonStandard = true;
                playerOrientationWhileOnWall.Rotate(0, mouseMotionX * Time.deltaTime, 0);
            }
            else
            {
                Debug.Log("Aim base turn");
                playerOrientation.Rotate(0, mouseMotionX * Time.deltaTime, 0);
            }
            
            // Y rotation
            bodyOfCam.m_FollowOffset = new Vector3(
                  0, 
                  bodyOfCam.m_FollowOffset.y - (mouseMotionY * Time.deltaTime), 
                  bodyOfCam.m_FollowOffset.z);

        }
    }

    void HandleCamAim()
    {
        if (Input.GetButton("Fire2"))
        {
            isAiming = true;
            if (isFirstRunAimed)
            {
                if(mainCam.m_XAxis.Value > -180 && mainCam.m_XAxis.Value < -90)
                {
                    if(AttachToRail.isAttachedToRail || RailDetect.isOnSmoothRail)
                    {
                        playerOrientationWhileOnRail.rotation = Quaternion.Euler(
                            playerOrientationWhileOnRail.rotation.x,
                            mainCam.m_XAxis.Value + 360,
                            playerOrientationWhileOnRail.rotation.z);
                        
                        aimCam.Follow = followObjectRail;
                        aimCam.LookAt = followObjectRail;
                    }
                    else if (AttachToWall.isAttachedToWall)
                    {
                        playerOrientationWhileOnWall.rotation = Quaternion.Euler(
                            playerOrientationWhileOnWall.rotation.x,
                            mainCam.m_XAxis.Value,
                            playerOrientationWhileOnWall.rotation.z);

                        if (AttachToWall.isLeftWallHit)
                        {
                            aimCam.Follow = followObjectWallRight;
                            aimCam.LookAt = followObjectWallRight;
                        }
                        else
                        {
                            aimCam.Follow = followObjectWallLeft;
                            aimCam.LookAt = followObjectWallLeft;
                        }
                    }
                    else
                    {
                        playerOrientation.rotation = Quaternion.Euler(
                            playerOrientation.rotation.x,
                            mainCam.m_XAxis.Value + 360,
                            playerOrientation.rotation.z);

                        aimCam.Follow = followObjectBase;
                        aimCam.LookAt = followObjectBase;

                        //playerOrientationWhileOnRail.localRotation = Quaternion.Euler(0, 0, 0);
                        //playerOrientationWhileOnWall.localRotation = Quaternion.Euler(0, 0, 0);
                    }
                }
                else
                {
                    if (AttachToRail.isAttachedToRail || RailDetect.isOnSmoothRail)
                    {
                        playerOrientationWhileOnRail.rotation = Quaternion.Euler(
                            playerOrientationWhileOnRail.rotation.x,
                            mainCam.m_XAxis.Value,
                            playerOrientationWhileOnRail.rotation.z);
                    }
                    else if (AttachToWall.isAttachedToWall)
                    {
                        playerOrientationWhileOnWall.rotation = Quaternion.Euler(
                            playerOrientationWhileOnWall.rotation.x,
                            mainCam.m_XAxis.Value,
                            playerOrientationWhileOnWall.rotation.z);

                        if (AttachToWall.isLeftWallHit)
                        {
                            aimCam.Follow = followObjectWallRight;
                            aimCam.LookAt = followObjectWallRight;
                        }
                        else
                        {
                            aimCam.Follow = followObjectWallLeft;
                            aimCam.LookAt = followObjectWallLeft;
                        }
                    }
                    else
                    {
                        playerOrientation.rotation = Quaternion.Euler(
                            playerOrientation.rotation.x,
                            mainCam.m_XAxis.Value,
                            playerOrientation.rotation.z);

                        aimCam.Follow = followObjectRail;
                        aimCam.LookAt = followObjectRail;

                        //playerOrientationWhileOnRail.localRotation = Quaternion.Euler(0, 0, 0);
                        //playerOrientationWhileOnWall.localRotation = Quaternion.Euler(0, 0, 0);
                    }
                }
                isFirstRunAimed = false;
            }
            else // allow main cam to track position even when aiming
            {
                if(playerOrientationWhileOnRail.rotation.eulerAngles.y <= 180 && 
                    playerOrientationWhileOnRail.rotation.y <= 270)
                {
                    if (AttachToRail.isAttachedToRail || RailDetect.isOnSmoothRail)
                    {
                        mainCam.m_XAxis.Value = playerOrientationWhileOnRail.rotation.eulerAngles.y - 360;
                        aimCam.Follow = followObjectRail;
                        aimCam.LookAt = followObjectRail;
                    }
                    else if (AttachToWall.isAttachedToWall)
                    {
                        mainCam.m_XAxis.Value = playerOrientationWhileOnWall.rotation.eulerAngles.y - 360;
                        if (AttachToWall.isLeftWallHit)
                        {
                            aimCam.Follow = followObjectWallRight;
                            aimCam.LookAt = followObjectWallRight;
                        }
                        else
                        {
                            aimCam.Follow = followObjectWallLeft;
                            aimCam.LookAt = followObjectWallLeft;
                        }
                    }
                    else
                    {
                        // handle transition from dismount of rail/wall to normal aim
                        if (isFirstRunUnaimedFromNonStandard)
                        {
                            float correctiveOffsetModifier = (playerMovementManager.getRailSpeedDirection()) ? 0 : 0;
                            float railOffSet = playerOrientation.rotation.eulerAngles.y;
                            
                            playerOrientation.rotation =
                                (playerOrientationWhileOnRail.localRotation.eulerAngles.magnitude > 0) ?
                                playerOrientationWhileOnRail.localRotation :
                                playerOrientationWhileOnWall.localRotation;

                            playerOrientation.rotation = Quaternion.Euler(
                                playerOrientation.rotation.eulerAngles.x,
                                playerOrientation.rotation.eulerAngles.y + correctiveOffsetModifier + railOffSet,
                                playerOrientation.rotation.eulerAngles.z);

                            isFirstRunUnaimedFromNonStandard = false;
                            Debug.Log("dismount rot corrected");
                        }

                        mainCam.m_XAxis.Value = playerOrientation.rotation.eulerAngles.y - 360;
                        playerOrientationWhileOnRail.localRotation = Quaternion.Euler(0, 0, 0);
                        playerOrientationWhileOnWall.localRotation = Quaternion.Euler(0, 0, 0);

                        aimCam.Follow = followObjectBase;
                        aimCam.LookAt = followObjectBase;
                    }
                }
                else
                {
                    if (AttachToRail.isAttachedToRail || RailDetect.isOnSmoothRail)
                    {
                        mainCam.m_XAxis.Value = playerOrientationWhileOnRail.rotation.eulerAngles.y;
                        aimCam.Follow = followObjectRail;
                        aimCam.LookAt = followObjectRail;
                    }
                    else if (AttachToWall.isAttachedToWall)
                    {
                        mainCam.m_XAxis.Value = playerOrientationWhileOnWall.rotation.eulerAngles.y;
                        if (AttachToWall.isLeftWallHit)
                        {
                            aimCam.Follow = followObjectWallRight;
                            aimCam.LookAt = followObjectWallRight;
                        }
                        else
                        {
                            aimCam.Follow = followObjectWallLeft;
                            aimCam.LookAt = followObjectWallLeft;
                        }
                    }
                    else
                    {
                        if (isFirstRunUnaimedFromNonStandard)
                        {
                            float correctiveOffsetModifier = (playerMovementManager.getRailSpeedDirection()) ? 0 : 0;
                            float railOffSet = playerOrientation.rotation.eulerAngles.y;
                            playerOrientation.rotation =
                                (playerOrientationWhileOnRail.localRotation.eulerAngles.magnitude > 0) ?
                                playerOrientationWhileOnRail.localRotation :
                                playerOrientationWhileOnWall.localRotation;

                            playerOrientation.rotation = Quaternion.Euler(
                                playerOrientation.rotation.eulerAngles.x,
                                playerOrientation.rotation.eulerAngles.y + correctiveOffsetModifier + railOffSet,
                                playerOrientation.rotation.eulerAngles.z);

                            isFirstRunUnaimedFromNonStandard = false;
                            Debug.Log("dismount rot corrected normal");
                        }

                        mainCam.m_XAxis.Value = playerOrientation.rotation.eulerAngles.y;
                        playerOrientationWhileOnRail.localRotation = Quaternion.Euler(0, 0, 0);
                        playerOrientationWhileOnWall.localRotation = Quaternion.Euler(0, 0, 0);

                        aimCam.Follow = followObjectBase;
                        aimCam.LookAt = followObjectBase;
                    }
                }
            }
            aimCam.Priority = activePriority;
        }
        else
        {
            isFirstRunUnaimedFromNonStandard = false;
            isAiming = false;
            isFirstRunAimed = true;
            aimCam.Priority = basePriority;
            playerOrientationWhileOnRail.localRotation = Quaternion.Euler(0, 0, 0);
            playerOrientationWhileOnWall.localRotation = Quaternion.Euler(0, 0, 0);
            //bodyOfCam.m_FollowOffset = baseBodycamOffset;
        }
    }

    void UpdateAimStateInContext()
    {
        playerMovementManager.getMoveContextReference().isAiming = isAiming;
    }
}
