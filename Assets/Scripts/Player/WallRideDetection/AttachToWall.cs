using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/*
 * CHANGE TO BE A RAYCAST, USE THE OUT OBJECT FOR NORMAL DATA SO WE CAN HAVE CURVED WALLS
 */
public class AttachToWall : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField]
    GameObject PlayerObj;
    [SerializeField]
    Vector3 detectionDimensions = Vector3.zero;
    [SerializeField]
    Vector3 detectionPositiveOffsetFromCenter = Vector3.zero;
    [SerializeField]
    LayerMask WallrideMask;
    [SerializeField]
    Transform PlayerRelativeRight;
    [SerializeField]
    Transform PlayerRelativeLeft;
    [SerializeField]
    float RayFrontBackOffset;
    [SerializeField]
    float WallDismountLockoutTime;

    public static GameObject WallAttachedTo;
    public static GameObject DismountReferenceWall;
    public static bool isAttachedToWall;
    public static bool isRoundWall;
    public static Vector3 mountPoint;
    public static Vector3 wallCurrNormal;
    private GameObject prevWall;
    HashSet<Collider> cachedRightWallHits;
    HashSet<Collider> cachedLeftWallHits;

    RaycastHit rightWallHit;
    RaycastHit leftWallHit;
    public static bool isRightWallHit;
    public static bool isLeftWallHit;
    float currTime;
    bool isInitialAttach = false;
    public static bool isInitAttach = false;
    public static bool isNewDetach = false;
    public static bool isInTransition = false;
    void Start()
    {
        cachedRightWallHits = new HashSet<Collider>();
        cachedLeftWallHits = new HashSet<Collider>();

        // Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        //  Gizmos.matrix = rotationMatrix;

        currTime = 0;
        prevWall = null;
        WallAttachedTo = null;
        isAttachedToWall = false;
        DismountReferenceWall = null;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // transform.localRotation = GameObject.Find("ThirdPersonPlayer_physboned").transform.rotation;
        CheckWallReattachAllow();
        GetWallAttachmentsV2();
        //GetWallAttachments();
    }

    void CheckWallReattachAllow()
    {
        if (isNewDetach)
        {
            if(WallAttachedTo && WallAttachedTo.layer != LayerMask.GetMask("wallRideCurved")) // case needed for wall flickering with curved walls
                WallAttachedTo.GetComponentsInChildren<BoxCollider>(true)[1].enabled = false;
            WallAttachedTo = null;
            isNewDetach = false;
            currTime = Time.time + WallDismountLockoutTime;
            isAttachedToWall = false;
            isInitialAttach = false;
        }
    }

    void GetWallAttachmentsV2()
    {
        if (currTime > Time.time)
            return;

        Vector3 offsetPosFront = new Vector3(0, 0, RayFrontBackOffset);
        Vector3 offsetPosBack = new Vector3(0, 0, -RayFrontBackOffset);

        // cast 6 rays, 3 on each side of player
        // per side, have one ray at *front most* detection region
        // one at the *back most* detection region
        // one ray in the middle
        // same functionality as boxcast but less expensive
        bool isWallRightMax = false;
        bool isWallLeftMax = false;
        bool isWallRightMin = false;
        bool isWallLeftMin = false;
        bool isWallRight = Physics.Raycast(transform.position, transform.right, out rightWallHit, detectionDimensions.x, WallrideMask);
        bool isWallLeft = Physics.Raycast(transform.position, transform.right * -1, out leftWallHit, detectionDimensions.x, WallrideMask);
        if(!isWallRight)
            isWallRightMin = Physics.Raycast(transform.TransformPoint(offsetPosFront), transform.right, out rightWallHit, detectionDimensions.x, WallrideMask);
        if(!isWallLeft)
            isWallLeftMin = Physics.Raycast(transform.TransformPoint(offsetPosFront), transform.right * -1, out leftWallHit, detectionDimensions.x, WallrideMask);
        if(!isWallRightMin && !isWallRight)
            isWallRightMax = Physics.Raycast(transform.TransformPoint(offsetPosBack), transform.right, out rightWallHit, detectionDimensions.x, WallrideMask);
        if(!isWallLeftMin && !isWallLeft)
            isWallLeftMax = Physics.Raycast(transform.TransformPoint(offsetPosBack), transform.right * -1, out leftWallHit, detectionDimensions.x, WallrideMask);

        // if any wall is hit, handle case
        if (isWallRightMin || isWallLeftMin || isWallRightMax || isWallLeftMax || isWallRight || isWallLeft)
        {
            isInTransition = false;
            isAttachedToWall = true;
            isRightWallHit = (isWallRightMin || isWallRightMax || isWallRight);
            isLeftWallHit = (isWallLeftMin || isWallLeftMax || isWallLeft);
            wallCurrNormal = (isWallRightMin || isWallRightMax || isWallRight) ? rightWallHit.normal : leftWallHit.normal;
            DismountReferenceWall = (isWallRightMin || isWallRightMax || isWallRight) ? rightWallHit.transform.gameObject : leftWallHit.transform.gameObject;
            WallAttachedTo = DismountReferenceWall;
            isRoundWall = WallAttachedTo.layer == LayerMask.NameToLayer("wallRideCurved");
            if (WallAttachedTo)
                WallAttachedTo.GetComponentsInChildren<BoxCollider>(true)[1].enabled = true;

            if (!isInitialAttach)
            {
                isInitialAttach = true;
                isInitAttach = true;
            }
        }
        else
        {
            if (WallAttachedTo)
                WallAttachedTo.GetComponentsInChildren<BoxCollider>(true)[1].enabled = false;
            isAttachedToWall = false;
            if (isInitialAttach)
            {
                // if we attached to a rail and just detached,
                // introduce a brief lockout before we can reattatch
                isInitialAttach = false;
            }
            isRoundWall = false;
            PlayerMovementContext.isWallRunning = false;
        }

    }

    void GetWallAttachments()
    {
        // use box cast and send direction instead
        var getWallHitsRight = Physics.OverlapBox(PlayerRelativeRight.position, detectionDimensions, transform.rotation, WallrideMask);
        List<Collider> wallHitsRight = new List<Collider>();
        for (var i = 0; i < getWallHitsRight.Length; i++)
        {
             Debug.Log("Wall felt right");
            wallHitsRight.Add(getWallHitsRight[i]);
        }
        HashSet<GameObject> wallHitRightUniques = new HashSet<GameObject>();
        for (var i = 0; i < wallHitsRight.Count; i++)
        {
            //Output the name of the Collider your Box hit
            // if we hit a rail that's cached, add it to unique list
            if (wallHitsRight[i].transform.CompareTag("rideableWall") && cachedRightWallHits.Contains(wallHitsRight[i]))
            {
                wallHitRightUniques.Add(wallHitsRight[i].transform.gameObject);
            }

            // on enter (we hit a rail that isn't cached)
            if (wallHitsRight[i].transform.CompareTag("rideableWall") && (cachedRightWallHits.Count == 0 || !cachedRightWallHits.Contains(wallHitsRight[i])))
            {
                // Debug.Log("Mounting rail");
                cachedRightWallHits.Add(wallHitsRight[i]);
                wallHitRightUniques.Add(wallHitsRight[i].transform.gameObject);
                if (WallAttachedTo != null)
                {
                    prevWall = WallAttachedTo;
                }
                // small grace period to prevent wall multimapping
                if (WallAttachedTo != wallHitsRight[i].transform.gameObject && currTime < Time.time)
                {
                   
                    // prevent a jump after landing on a new wall for short period
                   // currTime = Time.time + newAttachGracePeriod;

                    WallAttachedTo = wallHitsRight[i].transform.gameObject;
                    Debug.Log(WallAttachedTo);
                    mountPoint = wallHitsRight[i].transform.gameObject.GetComponent<Collider>().ClosestPoint(transform.position);

                    isAttachedToWall = true;

                    DismountReferenceWall = WallAttachedTo;
                    break;
                }
            }
        }
            var getWallHitsLeft = Physics.OverlapBox(transform.position - detectionPositiveOffsetFromCenter, detectionDimensions, transform.rotation, WallrideMask);

    }

    private void OnDrawGizmosSelected()
    {
        // right detector
        // Gizmos.DrawWireCube(transform.position - rightBoxOffset, detectionDimensions);

        Vector3 initPos = transform.position;
        Vector3 offsetPosFront = new Vector3(0, 0, .4f);
        Vector3 offsetPosBack = new Vector3(0, 0, -.4f);
        Quaternion rotation = PlayerObj.transform.rotation;
        //Vector3 finalPos = initPos + (offsetPos * rotation);
        Ray rightRay = new Ray(transform.position, transform.right);
        Ray leftRay = new Ray(transform.position, transform.right * -1);
        Ray rightRayMin = new Ray(transform.TransformPoint(offsetPosFront), transform.right);
        Ray leftRayMin = new Ray(transform.TransformPoint(offsetPosFront), transform.right * -1);
        Ray rightRayMax = new Ray(transform.TransformPoint(offsetPosBack), transform.right * -1);
        Ray leftRayMax = new Ray(transform.TransformPoint(offsetPosBack), transform.right);
        //Gizmos.DrawWireCube(PlayerRelativeRight.position, detectionDimensions);        // left detector
        //Gizmos.DrawWireCube(PlayerRelativeLeft.position, detectionDimensions);
        Gizmos.DrawRay(rightRayMin);
        Gizmos.DrawRay(leftRayMin);
        Gizmos.DrawRay(rightRayMax);
        Gizmos.DrawRay(leftRayMax);
        Gizmos.DrawRay(rightRay);
        Gizmos.DrawRay(leftRay);
    }
}
