using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugLockOn : MonoBehaviour
{
    [SerializeField]
    GameObject initialLockonTarget;
    [SerializeField]
    GameObject capsuleLockonTarget;
    [SerializeField]
    GameObject lockonFocusPosition;
    [SerializeField]
    CinemachineFreeLook cmCam;
    [SerializeField]
    Camera mainCam;
    [SerializeField]
    float yAxisDamper;
    [SerializeField]
    float lockOnCamFollowSpeed;
    [SerializeField]
    float delayToAllowZeroDamp;

    bool isFirstLockonRun;
    float currTime = 0;
    float xAxisBaseSpeed, yAxisBaseSpeed;
    float baseColliderDamp, baseColliderDampOccluded;
    float dampValXBase, dampValYBase, dampValZBase;
    float virtualCamOffsetPrevFrameY, virtualCamOffsetPrevFrameX;
    // Start is called before the first frame update
    public static bool isBaseLockon;
    void Start()
    {
        isFirstLockonRun = true;
        isBaseLockon = true;
        xAxisBaseSpeed = 1.6f;
        yAxisBaseSpeed = .1f;
        baseColliderDampOccluded = cmCam.gameObject.GetComponent<CinemachineCollider>().m_DampingWhenOccluded;
        baseColliderDamp = cmCam.gameObject.GetComponent<CinemachineCollider>().m_Damping;

        dampValXBase = cmCam.GetRig(0).GetCinemachineComponent<CinemachineOrbitalTransposer>().m_XDamping;
        dampValYBase = cmCam.GetRig(0).GetCinemachineComponent<CinemachineOrbitalTransposer>().m_YDamping;
        dampValZBase = cmCam.GetRig(0).GetCinemachineComponent<CinemachineOrbitalTransposer>().m_ZDamping;
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            isBaseLockon = !isBaseLockon;
            if (isBaseLockon)
            {
                isFirstLockonRun = true;
                cmCam.LookAt = initialLockonTarget.transform;
                cmCam.m_XAxis.m_MaxSpeed = xAxisBaseSpeed;
                cmCam.m_YAxis.m_MaxSpeed = yAxisBaseSpeed;

                for (var i = 0; i <= 2; i++)
                {
                    cmCam.GetRig(i).GetCinemachineComponent<CinemachineOrbitalTransposer>().m_XDamping = dampValXBase;
                    cmCam.GetRig(i).GetCinemachineComponent<CinemachineOrbitalTransposer>().m_YDamping = dampValYBase;
                    cmCam.GetRig(i).GetCinemachineComponent<CinemachineOrbitalTransposer>().m_ZDamping = dampValZBase;
                }
                cmCam.gameObject.GetComponent<CinemachineCollider>().m_DampingWhenOccluded = baseColliderDampOccluded;
                cmCam.gameObject.GetComponent<CinemachineCollider>().m_Damping = baseColliderDamp;
                //cmCam.gameObject.GetComponent<CinemachineCollider>().m_Strategy = CinemachineCollider.ResolutionStrategy.PullCameraForward;
            }
            else
            {
                if (isFirstLockonRun)
                {
                    currTime = Time.time + delayToAllowZeroDamp;
                    isFirstLockonRun = false;
                }
                cmCam.m_XAxis.m_MaxSpeed = 0;
                cmCam.m_YAxis.m_MaxSpeed = 0;

                for (var i = 0; i <= 2; i++)
                {
                    cmCam.GetRig(i).GetCinemachineComponent<CinemachineOrbitalTransposer>().m_XDamping = 0;
                    cmCam.GetRig(i).GetCinemachineComponent<CinemachineOrbitalTransposer>().m_YDamping = 0;
                    cmCam.GetRig(i).GetCinemachineComponent<CinemachineOrbitalTransposer>().m_ZDamping = 0;
                }
                cmCam.gameObject.GetComponent<CinemachineCollider>().m_DampingWhenOccluded = 0;
                cmCam.gameObject.GetComponent<CinemachineCollider>().m_Damping = 0;
            }
        }
        // have a delay to allow camera to undampen its rotation
        if (!isBaseLockon)
        {
            float yAxisUpdate = (lockonFocusPosition.transform.rotation.eulerAngles.y - virtualCamOffsetPrevFrameY);
            // this value normalized between 0-1, just make a damper for now

            // adjust xAxis rotation values
            // xAxis bug: can loop under 360, making above calculation VERY massive (goes from 1 - 2.8 -> 358 - 1)
            float adjustedCurrentXRot = lockonFocusPosition.transform.rotation.eulerAngles.x;
            float adjustedPrevXRot = virtualCamOffsetPrevFrameX;
            // set arbitrary thresholds
            // case current higher
            if(adjustedCurrentXRot > 300 && adjustedPrevXRot < 60)
            {
                Debug.Log("adjustment needed");
                adjustedCurrentXRot = adjustedCurrentXRot - 360;
            }
            // case current lower
            else if(adjustedPrevXRot > 300 && adjustedCurrentXRot < 60)
            {
                Debug.Log("adjustment needed");
                adjustedPrevXRot = adjustedPrevXRot - 360;
            }
            float xAxisUpdate = (adjustedCurrentXRot - adjustedPrevXRot);
            // xAxis bug: can loop under 360, making above calculation VERY massive (goes from 1 - 2.8 -> 358 - 1)

            cmCam.m_XAxis.Value += yAxisUpdate;
            cmCam.m_YAxis.Value += xAxisUpdate / 116f; //-53 = 0, 63 = 1, 116 (max dampening value)
            
            cmCam.m_YAxis.Value = Mathf.Clamp(cmCam.m_YAxis.Value, 0, 1);


        }
        virtualCamOffsetPrevFrameY = lockonFocusPosition.transform.rotation.eulerAngles.y;
        virtualCamOffsetPrevFrameX = lockonFocusPosition.transform.rotation.eulerAngles.x;
        setVirtToMatchRotationAndPosition();
    }

    void setVirtToMatchRotationAndPosition()
    {
        lockonFocusPosition.transform.position = mainCam.transform.position;
        if (isBaseLockon)
            lockonFocusPosition.transform.rotation = mainCam.transform.rotation;
        else
        {
            // create camera correction after dampening is fixed

            //lockonFocusPosition.transform.LookAt(capsuleLockonTarget.transform, Vector3.up);
            var targetRot = Quaternion.LookRotation(capsuleLockonTarget.transform.position - lockonFocusPosition.transform.position);
            var deltaAngle = Quaternion.Angle(lockonFocusPosition.transform.rotation, targetRot);

            if (deltaAngle == 0.00F)
            { // Exit early if no update required
                return;
            }

            lockonFocusPosition.transform.rotation = Quaternion.Slerp(
                lockonFocusPosition.transform.rotation,
                targetRot,
                lockOnCamFollowSpeed * Time.deltaTime / deltaAngle);
        }
    }
}
