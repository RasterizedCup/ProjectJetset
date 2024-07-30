using BezierSolution;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RailDetect : MonoBehaviour
{
    [SerializeField]
    LayerMask railMask;
    [SerializeField]
    float detectDistance;
    [SerializeField]
    float railReattachLockoutTime = .05f;
    [SerializeField]
    Vector3 DetectionVector = new Vector3 (1.5f, 1.1f, 1.5f);

    RaycastHit railHitInfo;

    public static bool isOnSmoothRail = false;
    public static Vector3 railCurrNormal = Vector3.zero;
    public static BezierSpline currSplineRail;
    public static bool isInitialMount = false;

    private float currTime;
    // Start is called before the first frame update
    void Start()
    {
        currTime = 0;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        GetSmoothRailAttach();
    }

    void GetSmoothRailAttach()
    {
        // do a overlap box that checks proximity. if we are in proximity
        bool isOnRail = Physics.Raycast(transform.position, transform.up * -1, out railHitInfo, detectDistance, railMask);
        Collider[] hitInfo = Physics.OverlapBox(transform.position, DetectionVector, transform.rotation, railMask);
        if (hitInfo.Length > 0 && Time.time > currTime)
        {
            if (!isOnSmoothRail)
                isInitialMount = true; // only set for the first iteration of our mount
            isOnSmoothRail = true;
            //railCurrNormal = railHitInfo.normal;
            //currSplineRail = railHitInfo.transform.gameObject.GetComponent<GetRailData>().GetMountedSpline();
            currSplineRail = hitInfo[0].transform.gameObject.GetComponent<GetRailData>().GetMountedSpline();
        }
        else
        {
            isOnSmoothRail = false;
        }
    }

    public void setReattachLockout()
    {
        currTime = Time.time + railReattachLockoutTime;
    }

    private void OnDrawGizmosSelected()
    {
       // Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
       // Gizmos.matrix = rotationMatrix;

        Gizmos.DrawRay(transform.position, transform.up * -1);
        Gizmos.DrawWireCube(transform.position, DetectionVector);
    }
}
