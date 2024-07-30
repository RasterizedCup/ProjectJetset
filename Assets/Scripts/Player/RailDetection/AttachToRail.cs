using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/* 
 * Implement rail normal detection (handle like wallriding) so we can have curved rails and more precise detection
 */
public class AttachToRail : MonoBehaviour
{
    public static bool isAttachedToRail = false;
    public static bool movementCorrected = false;
    public static bool lockoutInitialized = false;
    public static Vector3 mountPoint;
    public static float transitionTime = .1f;
    public GameObject playerObj;
    public static GameObject railattachedTo;
    public static GameObject dismountReferenceRail;
    public static bool isInStraightRailTransition;
    public static Vector3 playerPosBeforeMountPoint;
    private GameObject prevRail;
    private GameObject lastAttachedRailBeforeDismount;
    public float detectionHeight = 1;
    public float baseDetectionSize;
    public float mountedDetectionSize;
    public float verticalMountPoint = 2.2f;
    public static bool invertMomentumFromNoInput = false;

    public static bool isMountPointTransition;

    public static bool isInitialDismount = false;

    public float newAttachGracePeriod = .1f; // can only attach to a new rail once ever {newAttachGracePeriod} seconds
    public float dismountLockoutTimer = .1f;
    public Vector3 BoxCastOffsetFromCenter;
    public float boxHeight;
    float boxRadius = 0;
    //HashSet<RaycastHit> cachedRailHits;
    HashSet<Collider> cachedRailHits;
    float currTime = 0;
    LayerMask railMask;
    bool graceTimeBypass = false;
    // Start is called before the first frame update
    void Start()
    {
        //cachedRailHits = new HashSet<RaycastHit>();
        cachedRailHits = new HashSet<Collider>();
        railattachedTo = null;
        isAttachedToRail = false;
        railMask = LayerMask.GetMask("rail");
    }

    // upon dismount, start timer
    // during this timer, prevent reattach to the same rail
    // if no remount (to a different rail) within timer time, set remount lockout
    // after remount lockout expires, remove same rail lockout as well

    public void FixedUpdate()
    {
        if (Time.time < currTime)
        {
        }
        else
        {
            graceTimeBypass = false;
        }

        boxRadius = (isAttachedToRail ? mountedDetectionSize : baseDetectionSize);
        RailTransitionHandlerV1(); // loaded with weird jitter, esp at lower framerates
    }

    void RailTransitionHandlerV1()
    {
        var railHitsOverlap = Physics.OverlapBox(transform.position - BoxCastOffsetFromCenter, new Vector3(boxRadius, boxHeight, boxRadius), Quaternion.identity, railMask);
        var railHitsEntry = Physics.BoxCastAll(transform.position - BoxCastOffsetFromCenter, new Vector3(boxRadius, boxHeight, boxRadius), transform.forward, transform.rotation, 0);
        List<Collider> railHits = new List<Collider>();
        for (var i = 0; i < railHitsOverlap.Length; i++)
        {
            railHits.Add(railHitsOverlap[i]);
        }
        for (var i = 0; i < railHitsEntry.Length; i++)
        {
            railHits.Add(railHitsEntry[i].collider);
        }

        HashSet<GameObject> railHitUniques = new HashSet<GameObject>();
        for (var i = 0; i < railHits.Count; i++)
        {
            //Output the name of the Collider your Box hit
            // if we hit a rail that's cached, add it to unique list
            if (railHits[i].transform.CompareTag("rail") && cachedRailHits.Contains(railHits[i]))
            {
                railHitUniques.Add(railHits[i].transform.gameObject);
            }

            // only allow one rail update per fixed update?

            // on enter (we hit a rail that isn't cached)
            if (railHits[i].transform.CompareTag("rail") && (cachedRailHits.Count == 0 || !cachedRailHits.Contains(railHits[i])))
            {
               // Debug.Log("Mounting rail");
                cachedRailHits.Add(railHits[i]);
                railHitUniques.Add(railHits[i].transform.gameObject);
                if (railattachedTo != null)
                {
                    prevRail = railattachedTo;
                }
                // small grace period to prevent rail multimapping
                if (railattachedTo != railHits[i].transform.gameObject && currTime < Time.time)
                {
                    // prevent a jump after landing on a new rail for short period
                    currTime = Time.time + newAttachGracePeriod;
                    railattachedTo = railHits[i].transform.gameObject;
                   // Debug.Log($"Rail attached to: {railattachedTo}");
                    mountPoint = railHits[i].transform.gameObject.GetComponent<Collider>().ClosestPoint(transform.position);
                    mountPoint.y += verticalMountPoint;
                    // put 2.2f in the up direction of the rail (relative, and up direction for rail is forward)
                    playerPosBeforeMountPoint = playerObj.transform.localPosition;
                    isMountPointTransition = true;
                    // interpolate between playerPosBeforeMountPoint and mountPoint to get rid of small teleport choppiness
                    Debug.Log("rail attached, adjustments handled");
                    isAttachedToRail = true;
                    movementCorrected = false;
                    // only init lockout if our no prevRail
                    if (prevRail == null)
                        lockoutInitialized = true;
                    //Debug.Log(railattachedTo.transform.rotation.eulerAngles.y);
                    dismountReferenceRail = railattachedTo;
                    graceTimeBypass = true;
                    break;
                }
            }
        }

        // find way to optimize
        //var markToRemove = new List<RaycastHit>();
        var markToRemove = new List<Collider>();
        foreach (var cachedRailHit in cachedRailHits)
        {
            // exit condition 
            if (!railHitUniques.Contains(cachedRailHit.transform.gameObject))
            {
                if (cachedRailHit.transform.gameObject == prevRail)
                {
                    prevRail = null;
                }

                // **find out how to allow graceperiod time to be added correctly (adds as soon as we touch rail as of current)

                // need a previous update attached rail? 
                // if we are leaving currently attached rail (set to previously attached from prior function), handle
                if (cachedRailHit.transform.CompareTag("rail") && cachedRailHit.transform.gameObject == railattachedTo)
                {
                    // nullify
                    dismountReferenceRail = railattachedTo;
                    railattachedTo = null;
                    // check the case if we have a previous rail to still attach to
                    if (prevRail != null)
                    {
                       // Debug.Log("Dismounting Rail map to previous");
                        isMountPointTransition = true;
                        railattachedTo = prevRail;
                        prevRail = null;
                        mountPoint = railattachedTo.GetComponent<Collider>().ClosestPoint(transform.position);
                        mountPoint.y += verticalMountPoint;//1.4f;
                    }
                    else // if no rail, no longer attached
                    {
                        //Debug.Log("Dismounting Rail");
                        isAttachedToRail = false;
                        lastAttachedRailBeforeDismount = cachedRailHit.transform.gameObject;
                        isInStraightRailTransition = true;
                       if (currTime < Time.time && !graceTimeBypass) // only iterate once for detached element
                            currTime = Time.time + newAttachGracePeriod; // allow dismount, if no rail attached at all, block mounting for brief time
                        isInitialDismount = true;
                    }
                    movementCorrected = false;
                }
                // remove the exited rail from list
                markToRemove.Add(cachedRailHit);
            }
        }
        foreach (var removeItem in markToRemove)
        {
            cachedRailHits.Remove(removeItem);
        }
        //  if (railHitUniques.Count == 0 || !isAttachedToRail)
        //      cachedRailHits.Clear();
    }

    public void UpdateDismountLockoutTimer()
    {
        currTime = Time.time + dismountLockoutTimer;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireCube(transform.position - BoxCastOffsetFromCenter, new Vector3(boxRadius*2, boxHeight*2, boxRadius*2));
    }
}
